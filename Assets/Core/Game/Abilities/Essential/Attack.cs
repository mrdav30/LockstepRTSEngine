using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Attack : ActiveAbility
    {
        public const long MissModifier = FixedMath.One / 2;

        public virtual bool CanMove { get; private set; }

        protected bool CanTurn { get; private set; }

        public RTSAgent Target { get; private set; }

        public virtual bool IsOffensive { get { return _isOffensive; } }

        public virtual string ProjCode { get { return _projectileCode; } }

        public virtual long Range { get { return _range + RangeModifier; } }

        public long BaseRange { get { return _range; } }
        //Range

        [Lockstep(true)]
        public long RangeModifier { get; set; }

        public virtual long Sight { get { return _sight; } }
        //Approximate radius that's scanned for targets

        public virtual long Damage { get { return _damage; } }
        //Damage of attack

        public long BaseDamage { get { return _damage; } }


        public virtual long AttackInterval { get { return _attackInterval; } }
        //Frames between each attack

        public virtual bool TrackAttackAngle { get { return _trackAttackAngle; } }
        //Whether or not to require the unit to face the target for attacking

        public long AttackAngle { get { return _attackAngle; } }
        //The angle in front of the unit that the target must be located in

        public AllegianceType TargetAllegiance
        { //Allegiance to the target
            get { return this._targetAllegiance; }
        }

        public virtual Vector3d ProjectileOffset { get { return _projectileOffset; } }

        private Vector3d[] cachedProjectileOffsets;

        public virtual Vector3d[] ProjectileOffsets
        {
            get
            {
                if (cachedProjectileOffsets == null)
                {
                    cachedProjectileOffsets = new Vector3d[this._secondaryProjectileOffsets.Length + 1];
                    cachedProjectileOffsets[0] = this.ProjectileOffset;
                    for (int i = 0; i < this._secondaryProjectileOffsets.Length; i++)
                    {
                        cachedProjectileOffsets[i + 1] = this._secondaryProjectileOffsets[i];
                    }
                }
                return cachedProjectileOffsets;
            }
        }

        public bool CycleProjectiles { get { return this._cycleProjectiles; } }
        //Offset of projectile

        #region Serialized Values (Further description in properties)
        [SerializeField]
        protected bool _isOffensive;
        [SerializeField, DataCode("Projectiles")]
        protected string _projectileCode;
        [FixedNumber, SerializeField]
        protected long _range = FixedMath.One * 6;
        [FixedNumber, SerializeField]
        protected long _sight = FixedMath.One * 10;
        [FixedNumber, SerializeField]
        protected long _damage = FixedMath.One;
        [SerializeField, FixedNumber]
        protected long _attackInterval = 1 * FixedMath.One;
        [SerializeField, EnumMask]
        protected AllegianceType _targetAllegiance = AllegianceType.Enemy;

        [SerializeField]
        protected bool _trackAttackAngle = true;
        [FixedNumberAngle, SerializeField]
        protected long _attackAngle = FixedMath.TenDegrees;
        [SerializeField, Tooltip("Important: With Vector3d, the Z axis represents height!")]
        protected Vector3d _projectileOffset;
        [SerializeField]
        protected Vector3d[] _secondaryProjectileOffsets;
        [SerializeField]
        private bool _cycleProjectiles;
        [SerializeField, FixedNumber]
        protected long _windup;
        #endregion

        public long Windup { get { return _windup; } }
        [Lockstep(true)]
        public bool IsWindingUp { get; set; }

        long windupCount;

        [SerializeField]
        protected bool _increasePriority = true;

        public virtual bool IncreasePriority { get { return _increasePriority; } }

        //Stuff for the logic
        private bool inRange;
        //private long fastRange;
        private long fastRangeToTarget;
        private Vector2d Destination;

        private Move cachedMove;
        private Turn cachedTurn;
        private Health cachedTargetHealth;
        protected LSBody cachedBody { get { return Agent.Body; } }

        private int basePriority;
        private uint targetVersion;
        private long attackCount;

        public bool IsAttackMoving { get; private set; }

        private int loadedTargetId = -1;

        protected override void OnSetup()
        {
            cachedTurn = Agent.GetAbility<Turn>();
            cachedMove = Agent.GetAbility<Move>();

            if (Sight < Range)
            {
                _sight = Range + FixedMath.One * 5;
            }

            //fastRange = (Range * Range);
            basePriority = cachedBody.Priority;
            CanMove = cachedMove.IsNotNull();
            if (CanMove)
            {
                cachedMove.onArrive += HandleOnArrive;
                cachedMove.onGroupProcessed += _HandleMoveGroupProcessed;
            }

            CanTurn = cachedTurn.IsNotNull();
        }

        private void HandleOnArrive()
        {
            if (this.IsAttackMoving)
            {
                IsAttackMoving = false;
            }
        }

        #region variables for quick fix for repathing to target's new position

        const long repathDistance = FixedMath.One * 2;
        FrameTimer repathTimer = new FrameTimer();
        const int repathInterval = LockstepManager.FrameRate * 2;
        int repathRandom;

        #endregion

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            attackCount = 0;
            Target = null;
            IsAttackMoving = false;
            inRange = false;
            IsFocused = false;
            CycleCount = 0;
            this.Destination = Vector2d.zero;
            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            //caching parameters
            var spawnVersion = Agent.SpawnVersion;
            var controller = Agent.Controller;
            CachedOnHit = (target) => OnHit(target, spawnVersion, controller);

            if (Agent.GetCommander() && loadedSavedValues && loadedTargetId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(loadedTargetId);
                if (obj.MyAgentType == AgentType.Unit || obj.MyAgentType == AgentType.Building)
                {
                    Target = obj;
                }
            }
        }

        protected override void OnSimulate()
        {
            if (attackCount > AttackInterval)
            {
                //reset attackCount overcharge if left idle
                attackCount = AttackInterval;
            }
            else if (attackCount < AttackInterval)
            {
                //charge up attack
                attackCount += LockstepManager.DeltaTime;
            }

            if (Agent && Agent.IsActive)
            {
                if (Target != null)
                {
                    BehaveWithTarget();
                }
            }

            if (CanMove)
            {
                if (IsAttackMoving)
                {
                    cachedMove.StartLookingForStopPause();
                }
            }
        }

        void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
            Agent.ApplyImpulse(this.FireAnimImpulse);
            OnStartWindup();
        }

        protected virtual void OnStartWindup()
        {

        }

        protected virtual AnimState EngagingAnimState
        {
            get { return AnimState.Engaging; }
        }

        protected virtual AnimImpulse FireAnimImpulse
        {
            get { return AnimImpulse.Fire; }
        }

        bool CheckRange()
        {
            Vector2d targetDirection = Target.Body.Position - cachedBody.Position;
            long fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        void BehaveWithTarget()
        {
            if (Target.IsActive == false || Target.SpawnVersion != targetVersion ||
                (this.TargetAllegiance & Agent.GetAllegiance(Target)) == 0)
            {
                //Target's lifecycle has ended
                StopEngage();
            }
            else
            {
                if (!IsWindingUp)
                {
                    Vector2d targetDirection = Target.Body.Position - cachedBody.Position;
                    long fastMag = targetDirection.FastMagnitude();

                    //TODO: Optimize this instead of recalculating magnitude multiple times
                    if (CheckRange())
                    {
                        if (!inRange)
                        {
                            if (CanMove)
                            {
                                cachedMove.StopMove();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetState(EngagingAnimState);

                        long mag;
                        targetDirection.Normalize(out mag);
                        bool withinTurn = TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          cachedBody.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && cachedBody.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn)
                        {
                            if (CanTurn)
                            {
                                cachedTurn.StartTurnDirection(targetDirection);
                            }
                        }
                        else
                        {
                            if (attackCount >= AttackInterval)
                            {
                                StartWindup();
                            }
                        }

                    }
                    else
                    {
                        if (CanMove)
                        {
                            cachedMove.PauseAutoStop();
                            cachedMove.PauseCollisionStop();
                            if (cachedMove.IsMoving == false)
                            {
                                cachedMove.StartMove(Target.Body.Position);
                                cachedBody.Priority = basePriority;
                            }
                            else
                            {
                                if (inRange)
                                {
                                    cachedMove.Destination = Target.Body.Position;
                                }
                                else
                                {
                                    if (repathTimer.AdvanceFrame())
                                    {
                                        if (Target.Body.PositionChangedBuffer &&
                                            Target.Body.Position.FastDistance(cachedMove.Destination.x, cachedMove.Destination.y) >= (repathDistance * repathDistance))
                                        {
                                            cachedMove.StartMove(Target.Body.Position);
                                            //So units don't sync up and path on the same frame
                                            repathTimer.AdvanceFrames(repathRandom);
                                        }
                                    }
                                }
                            }
                        }

                        if (inRange == true)
                        {
                            inRange = false;
                        }

                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (CanTurn)
                    {
                        Vector2d targetVector = Target.Body.Position - cachedBody.Position;
                        cachedTurn.StartTurnVector(targetVector);
                    }
                    if (windupCount >= Windup)
                    {
                        windupCount = 0;
                        Fire();
                        while (this.attackCount >= AttackInterval)
                        {
                            //resetting back down after attack is fired
                            this.attackCount -= (this.AttackInterval);
                        }
                        this.attackCount += Windup;
                        IsWindingUp = false;
                    }

                }
                else
                {
                    windupCount = 0;
                }

                if (CanMove && inRange)
                {
                    cachedMove.PauseAutoStop();
                    cachedMove.PauseCollisionStop();
                }
            }
        }

        public event Action<RTSAgent, bool> ExtraOnHit;

        protected void CallExtraOnHit(RTSAgent agent, bool isCurrent)
        {
            if (ExtraOnHit != null)
                ExtraOnHit(agent, isCurrent);
        }

        protected virtual void OnHit(RTSAgent target, uint agentVersion, AgentController controller)
        {
            //If the shooter died, certain effects or records can't be completed
            bool isCurrent = Agent != null && agentVersion == Agent.SpawnVersion;
            Health healther = target.GetAbility<Health>();
            AttackerInfo info = new AttackerInfo(isCurrent ? Agent : null, controller);
            healther.TakeDamage(Damage, info);
            CallExtraOnHit(target, isCurrent);
        }

        private Action<RTSAgent> CachedOnHit;

        public void Fire()
        {

            if (CanMove)
            {
                cachedMove.StopMove();
            }
            cachedBody.Priority = IncreasePriority ? basePriority + 1 : basePriority;

            OnFire(Target);

        }

        /// <summary>
        /// The projectile to be fired in OnFire.
        /// </summary>
        /// <value>The current projectile.</value>

        public int CycleCount { get; private set; }

        protected virtual void OnFire(RTSAgent target)
        {
            if (this.CycleProjectiles)
            {
                CycleCount++;
                if (CycleCount >= ProjectileOffsets.Length)
                {
                    CycleCount = 0;
                }
                FullFireProjectile(this.ProjCode, ProjectileOffsets[CycleCount], target);


            }
            else
            {
                for (int i = 0; i < ProjectileOffsets.Length; i++)
                {
                    FullFireProjectile(ProjCode, ProjectileOffsets[i], target);

                }
            }

        }

        public LSProjectile FullFireProjectile(string projectileCode, Vector3d projOffset, RTSAgent target)
        {
            LSProjectile proj = (PrepareProjectile(projectileCode, projOffset, target));
            FireProjectile(proj);
            return proj;
        }

        public static Attack LastFire;

        public LSProjectile PrepareProjectile(string projectileCode, Vector3d projOffset, RTSAgent target)
        {
            LastFire = this;
            LSProjectile currentProjectile = ProjectileManager.Create(
                                                 projectileCode,
                                                 this.Agent,
                                                 projOffset,
                                                 this.TargetAllegiance,
                                                 (other) =>
                                                 {
                                                     Health healther = other.GetAbility<Health>();
                                                     return healther.IsNotNull() && healther.HealthAmount > 0;

                                                 },
                                                 CachedOnHit);

            switch (currentProjectile.TargetingBehavior)
            {
                case TargetingType.Homing:
                    currentProjectile.InitializeHoming(target);
                    break;
                case TargetingType.Timed:
                    currentProjectile.InitializeTimed(Agent.Body.Forward);
                    break;
                case TargetingType.Positional:
                    currentProjectile.InitializePositional(target.Body.Position.ToVector3d(target.Body.HeightPos));
                    break;
                case TargetingType.Directional:
                    //TODO
                    throw new System.Exception("Not implemented yet.");
                    //break;
            }
            OnPrepareProjectile(currentProjectile);
            return currentProjectile;
        }

        public LSProjectile PrepareProjectile(string projectileCode, Vector3d projOffset, Vector3d targetPos)
        {
            LSProjectile currentProjectile = ProjectileManager.Create(
                                                 projectileCode,
                                                 this.Agent,
                                                 projOffset,
                                                 this.TargetAllegiance,
                                                 (other) =>
                                                 {
                                                     Health healther = other.GetAbility<Health>();
                                                     return healther.IsNotNull() && healther.HealthAmount > 0;

                                                 },
                                                 CachedOnHit);

            switch (currentProjectile.TargetingBehavior)
            {
                case TargetingType.Timed:
                    currentProjectile.InitializeTimed(Agent.Body.Forward);
                    break;
                case TargetingType.Positional:
                    currentProjectile.InitializePositional(targetPos);
                    break;
                case TargetingType.Directional:
                    //TODO
                    throw new System.Exception("Not implemented yet.");
                    //break;
            }

            return currentProjectile;
        }

        protected virtual void OnPrepareProjectile(LSProjectile projectile)
        {

        }

        public void FireProjectile(LSProjectile projectile)
        {
            ProjectileManager.Fire(projectile);
        }

        public void Engage(RTSAgent other)
        {
            if (other != Agent && other != null)
            {
                cachedTargetHealth = other.GetAbility<Health>();
                if (cachedTargetHealth.IsNotNull())
                {
                    OnEngage(other);
                    Target = other;
                    targetVersion = Target.SpawnVersion;
                    IsCasting = true;
                    fastRangeToTarget = Range + (Target.Body.IsNotNull() ? Target.Body.Radius : 0) + Agent.Body.Radius;
                    fastRangeToTarget *= fastRangeToTarget;

                    if (!CheckRange())
                    {
                        if (CanMove)
                        {
                            cachedMove.StartMove(Target.Body.Position);
                        }
                    }
                }
            }
        }

        protected virtual void OnEngage(RTSAgent target)
        {

        }

        public void StopEngage(bool complete = false)
        {
            inRange = false;
            IsWindingUp = false;
            IsFocused = false;
            if (complete)
            {
                IsAttackMoving = false;
            }
            else
            {
                if (IsAttackMoving)
                {
                    cachedMove.StartMove(this.Destination);
                }
                else
                {
                    if (CanMove)
                    {
                        if (Target != null && inRange == false)
                        {
                            cachedMove.StopMove();
                        }
                    }
                }
            }

            Target = null;
            cachedBody.Priority = basePriority;

            IsCasting = false;
        }

        protected override void OnDeactivate()
        {
            StopEngage(true);
        }

        public void StartAttackMove(Vector2d position, bool isFormal = true)
        {
            Agent.StopCast(this.ID);

            //if formal (going through normal Execute routes), do the group stuff
            if (isFormal)
            {
                if (Target != null)
                {
                    cachedMove.RegisterGroup(false);
                }
                else
                {
                    cachedMove.RegisterGroup();
                }
            }
            else
            {
                if (Target == null)
                    cachedMove.StartMove(position);
            }
            IsAttackMoving = true;
            IsFocused = false;
        }

        protected override void OnExecute(Command com)
        {
            Vector2d pos;
            DefaultData target;
            if (com.TryGetData<Vector2d>(out pos) && CanMove)
            {
                StartAttackMove(pos);
            }
            else if (com.TryGetData<DefaultData>(out target) && target.Is(DataType.UShort))
            {
                IsFocused = true;
                IsAttackMoving = false;
                RTSAgent tempTarget;
                ushort targetValue = (ushort)target.Value;
                if (AgentController.TryGetAgentInstance(targetValue, out tempTarget))
                {
                    Engage(tempTarget);
                }
                else
                {
                    Debug.Log("nope");
                }
            }
        }

        protected sealed override void OnStopCast()
        {
            StopEngage(true);
        }

        Action _handleMoveGroupProcessed;

        Action _HandleMoveGroupProcessed { get { return _handleMoveGroupProcessed ?? (_handleMoveGroupProcessed = HandleMoveGroupProcessed); } }

        void HandleMoveGroupProcessed()
        {
            this.Destination = cachedMove.Destination;
        }

        protected virtual bool HardAgentConditional()
        {
            Health health = Target.GetAbility<Health>();
            if (health != null)
            {
                if (this.Damage >= 0)
                {
                    return health.CanLose;
                }
                else
                {
                    Debug.Log("asdf");
                    return health.CanGain;
                }
            }
            return true;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (Agent == null || Agent.IsActive == false)
            {
                return;
            }

            if (Agent.Body == null)
            {
                Debug.Log(Agent.gameObject);
            }

            Gizmos.DrawWireSphere(Application.isPlaying ? Agent.Body._visualPosition : this.transform.position, this.Range.ToFloat());
        }
#endif

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteVector2d(writer, "Destination", Destination);
            SaveManager.WriteUInt(writer, "TargetVersion", targetVersion);
            SaveManager.WriteBoolean(writer, "AttackMoving", IsAttackMoving);
            if (Target)
            {
                SaveManager.WriteInt(writer, "TargetID", Target.GlobalID);
            }

            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", inRange);
            SaveManager.WriteLong(writer, "AttackCount", attackCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", fastRangeToTarget);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {

                case "Destination":
                    Destination = LoadManager.LoadVector2d(reader);
                    break;
                case "TargetVersion":
                    targetVersion = (uint)readValue;
                    break;
                case "AttackMoving":
                    IsAttackMoving = (bool)readValue;
                    break;
                case "TargetID":
                    loadedTargetId = (int)(System.Int64)readValue;
                    break;
                case "Focused":
                    IsFocused = (bool)readValue;
                    break;
                case "InRange":
                    inRange = (bool)readValue;
                    break;
                case "AttackCount":
                    attackCount = (long)readValue;
                    break;
                case "FastRangeToTarget":
                    fastRangeToTarget = (long)readValue;
                    break;
                default: break;
            }
        }
    }
}