using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Attack : ActiveAbility
    {
        #region Properties
        public const long MissModifier = FixedMath.One / 2;

        public AttackGroup MyAttackGroup;
        [HideInInspector]
        public int MyAttackGroupID;

        public Move CachedMove { get; private set; }

        public bool IsAttackMoving { get; private set; }

        private RTSAgent _currentTarget;
        public RTSAgent CurrentTarget
        {
            get
            {
                if (MyAttackGroup.IsNotNull())
                {
                    return MyAttackGroup.CurrentTarget;
                }
                else
                {
                    return _currentTarget;
                }
            }
            set
            {
                if (MyAttackGroup.IsNotNull())
                {
                    MyAttackGroup.CurrentTarget = value;
                }
                else
                {
                    _currentTarget = value;
                }
            }
        }

        public virtual bool IsOffensive { get { return _isOffensive; } }

        //Range
        public virtual long Range { get { return _range; } }
        //Approximate radius that's scanned for targets
        public virtual long Sight { get { return _sight; } }

        //Damage of attack
        public virtual long Damage { get { return _damage; } }
        //Frames between each attack
        public virtual long AttackInterval { get { return _attackInterval; } }
        //Whether or not to require the unit to face the target for attacking
        public virtual bool TrackAttackAngle { get { return _trackAttackAngle; } }
        //The angle in front of the unit that the target must be located in
        public long AttackAngle { get { return _attackAngle; } }

        public virtual Vector3d[] ProjectileOffsets
        {
            get
            {
                if (cachedProjectileOffsets == null)
                {
                    cachedProjectileOffsets = new Vector3d[_secondaryProjectileOffsets.Length + 1];
                    cachedProjectileOffsets[0] = _projectileOffset;
                    for (int i = 0; i < _secondaryProjectileOffsets.Length; i++)
                    {
                        cachedProjectileOffsets[i + 1] = _secondaryProjectileOffsets[i];
                    }
                }
                return cachedProjectileOffsets;
            }
        }

        /// <summary>
        /// The projectile to be fired in OnFire.
        /// </summary>
        /// <value>The current projectile.</value>
        public int CycleCount { get; private set; }

        public static Attack LastAttack;

        public event Action<RTSAgent, bool> ExtraOnHit;
        public event Action OnStopAttack;

        private Vector3d[] cachedProjectileOffsets;

        private bool canMove;
        private Turn cachedTurn;
        private bool canTurn;
        private Health cachedTargetHealth;
        private LSBody _cachedBody { get { return Agent.Body; } }

        //Stuff for the logic
        private bool inRange;

        private long fastMag;
        private Vector2d targetDirection;
        private long fastRangeToTarget;

        private int basePriority;
        private uint targetVersion;
        private long attackCount;

        private int loadedTargetId = -1;

        [Lockstep(true)]
        private bool IsWindingUp { get; set; }
        private long windupCount;

        private Action<RTSAgent> CachedOnHit;

        protected virtual AnimState EngagingAnimState
        {
            get { return AnimState.Engaging; }
        }
        protected virtual AnimImpulse AttackAnimImpulse
        {
            get { return AnimImpulse.Attack; }
        }

        #region variables for quick fix for repathing to target's new position
        const long repathDistance = FixedMath.One * 2;
        FrameTimer repathTimer = new FrameTimer();
        const int repathInterval = LockstepManager.FrameRate * 2;
        int repathRandom;
        #endregion

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
        // Allegiance of the target
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
        protected long _windup = 0;
        [SerializeField]
        protected bool _increasePriority = true;
        #endregion
        #endregion Properties

        protected override void OnSetup()
        {
            CachedMove = Agent.GetAbility<Move>();
            cachedTurn = Agent.GetAbility<Turn>();

            if (Sight < Range)
            {
                _sight = Range + FixedMath.One * 5;
            }

            //fastRange = (Range * Range);
            basePriority = _cachedBody.Priority;
            canMove = CachedMove.IsNotNull();

            if (canMove)
            {
                CachedMove.onArrive += HandleOnArrive;
            }

            canTurn = cachedTurn.IsNotNull();
        }

        protected override void OnInitialize()
        {
            attackCount = 0;
            CurrentTarget = null;

            IsAttackMoving = false;

            MyAttackGroupID = -1;

            inRange = false;
            IsFocused = false;

            CycleCount = 0;
            //   Destination = Vector2d.zero;

            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            //caching parameters
            uint spawnVersion = Agent.SpawnVersion;
            AgentController controller = Agent.Controller;
            CachedOnHit = (target) => OnHitTarget(target, spawnVersion, controller);

            if (Agent.GetCommander() && loadedSavedValues && loadedTargetId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(loadedTargetId);
                if (obj.MyAgentType == AgentType.Unit || obj.MyAgentType == AgentType.Building)
                {
                    CurrentTarget = obj;
                }
            }
        }

        protected override void OnSimulate()
        {
            if (Agent.Tag == AgentTag.Offensive)
            {
                if (attackCount > _attackInterval)
                {
                    //reset attackCount overcharge if left idle
                    attackCount = _attackInterval;
                }
                else if (attackCount < _attackInterval)
                {
                    //charge up attack
                    attackCount += LockstepManager.DeltaTime;
                }

                if (Agent && Agent.IsActive)
                {
                    if (CurrentTarget.IsNotNull())
                    {
                        BehaveWithTarget();
                    }
                }

                if (canMove && IsAttackMoving)
                {
                    CachedMove.StartLookingForStopPause();
                }
            }
        }

        protected override void OnExecute(Command com)
        {
            Agent.StopCast(ID);
            RegisterAttackGroup();
        }

        protected virtual void OnStartAttackMove()
        {
            cachedTargetHealth = CurrentTarget.GetAbility<Health>();
            if (cachedTargetHealth.IsNotNull())
            {
                if (!CheckRange())
                {
                    if (canMove)
                    {
                        CachedMove.StartMove(CurrentTarget.Body.Position);
                    }
                }
            }

            IsAttackMoving = true;
            IsFocused = false;
        }

        protected virtual void OnStartWindup()
        {

        }

        protected virtual void OnAttack(RTSAgent target)
        {
            if (_cycleProjectiles)
            {
                CycleCount++;
                if (CycleCount >= ProjectileOffsets.Length)
                {
                    CycleCount = 0;
                }

                FullFireProjectile(_projectileCode, ProjectileOffsets[CycleCount], target);
            }
            else
            {
                for (int i = 0; i < ProjectileOffsets.Length; i++)
                {
                    FullFireProjectile(_projectileCode, ProjectileOffsets[i], target);
                }
            }
        }

        protected virtual void OnPrepareProjectile(LSProjectile projectile)
        {

        }

        protected virtual void OnHitTarget(RTSAgent target, uint agentVersion, AgentController controller)
        {
            // If the shooter died, certain effects or records can't be completed
            bool isCurrent = Agent.IsNotNull() && agentVersion == Agent.SpawnVersion;
            Health healther = target.GetAbility<Health>();
            AttackerInfo info = new AttackerInfo(isCurrent ? Agent : null, controller);
            healther.TakeDamage(_damage, info);
            CallExtraOnHit(target, isCurrent);
        }

        protected override void OnDeactivate()
        {
            StopAttack(true);
        }

        protected sealed override void OnStopCast()
        {
            StopAttack(true);
        }

        protected virtual bool HardAgentConditional()
        {
            Health health = CurrentTarget.GetAbility<Health>();
            if (health != null)
            {
                if (_damage >= 0)
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

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            //    SaveManager.WriteVector2d(writer, "Destination", Destination);
            SaveManager.WriteUInt(writer, "TargetVersion", targetVersion);
            SaveManager.WriteBoolean(writer, "AttackMoving", IsAttackMoving);
            if (CurrentTarget)
            {
                SaveManager.WriteInt(writer, "TargetID", CurrentTarget.GlobalID);
            }

            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", inRange);
            SaveManager.WriteLong(writer, "AttackCount", attackCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", fastRangeToTarget);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {

                //case "Destination":
                //    Destination = LoadManager.LoadVector2d(reader);
                //    break;
                case "TargetVersion":
                    targetVersion = (uint)readValue;
                    break;
                case "AttackMoving":
                    IsAttackMoving = (bool)readValue;
                    break;
                case "TargetID":
                    loadedTargetId = (int)(long)readValue;
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

        public void OnAttackGroupProcessed()
        {
            Agent.Tag = AgentTag.Offensive;

            IsFocused = true;
            IsAttackMoving = false;

            targetVersion = CurrentTarget.SpawnVersion;
            IsCasting = true;

            fastRangeToTarget = _range + (CurrentTarget.Body.IsNotNull() ? CurrentTarget.Body.Radius : 0) + Agent.Body.Radius;
            fastRangeToTarget *= fastRangeToTarget;
        }

        private void RegisterAttackGroup()
        {
            if (AttackGroupHelper.CheckValidAndAlert())
            {
                AttackGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        private void HandleOnArrive()
        {
            if (IsAttackMoving)
            {
                IsFocused = true;
                IsAttackMoving = false;
            }
        }

        private void BehaveWithTarget()
        {
            if (CurrentTarget.IsActive == false
                || CurrentTarget.SpawnVersion != targetVersion
                || (_targetAllegiance & Agent.GetAllegiance(CurrentTarget)) == 0)
            {
                // Target's lifecycle has ended
                StopAttack();
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange())
                    {
                        if (!inRange)
                        {
                            if (canMove)
                            {
                                CachedMove.Arrive();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetState(EngagingAnimState);


                        targetDirection.Normalize(out long mag);
                        bool withinTurn = TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          _cachedBody.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && _cachedBody.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn && canTurn)
                        {
                            cachedTurn.StartTurnDirection(targetDirection);
                        }
                        else if (attackCount >= _attackInterval)
                        {
                            StartWindup();
                        }
                    }
                    else
                    {
                        if (canMove)
                        {
                            CachedMove.PauseAutoStop();
                            CachedMove.PauseCollisionStop();
                            if (!CachedMove.IsMoving
                                && !CachedMove.MoveOnGroupProcessed)
                            {
                                OnStartAttackMove();
                                _cachedBody.Priority = basePriority;
                            }
                            else
                            {
                                if (inRange)
                                {
                                    CachedMove.Destination = CurrentTarget.Body.Position;
                                }
                                else
                                {
                                    if (repathTimer.AdvanceFrame())
                                    {
                                        if (CurrentTarget.Body.PositionChangedBuffer &&
                                            CurrentTarget.Body.Position.FastDistance(CachedMove.Destination.x, CachedMove.Destination.y) >= (repathDistance * repathDistance))
                                        {
                                            OnStartAttackMove(); ;
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
                    if (canTurn)
                    {
                        Vector2d targetVector = CurrentTarget.Body.Position - _cachedBody.Position;
                        cachedTurn.StartTurnVector(targetVector);
                    }

                    if (windupCount >= _windup)
                    {
                        windupCount = 0;
                        StartAttack();
                        while (attackCount >= _attackInterval)
                        {
                            //resetting back down after attack is fired
                            attackCount -= (_attackInterval);
                        }
                        attackCount += _windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (canMove && inRange)
                {
                    CachedMove.PauseAutoStop();
                    CachedMove.PauseCollisionStop();
                }
            }
        }

        private bool CheckRange()
        {
            targetDirection = CurrentTarget.Body.Position - _cachedBody.Position;
            fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
            Agent.ApplyImpulse(AttackAnimImpulse);
            OnStartWindup();
        }

        private void CallExtraOnHit(RTSAgent agent, bool isCurrent)
        {
            ExtraOnHit?.Invoke(agent, isCurrent);
        }

        private void StartAttack()
        {
            if (canMove)
            {
                // we don't want to be able to fire and move!
                CachedMove.StopMove();
            }
            _cachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            OnAttack(CurrentTarget);
        }

        private LSProjectile FullFireProjectile(string projectileCode, Vector3d projOffset, RTSAgent target)
        {
            LSProjectile proj = PrepareProjectile(projectileCode, projOffset, target);
            FireProjectile(proj);
            return proj;
        }

        private LSProjectile PrepareProjectile(string projectileCode, Vector3d projOffset, RTSAgent target)
        {
            LastAttack = this;
            LSProjectile currentProjectile = ProjectileManager.Create(
                                                 projectileCode,
                                                 Agent,
                                                 projOffset,
                                                 _targetAllegiance,
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
                    throw new Exception("Not implemented yet.");
                    //break;
            }
            OnPrepareProjectile(currentProjectile);

            return currentProjectile;
        }

        private LSProjectile PrepareProjectile(string projectileCode, Vector3d projOffset, Vector3d targetPos)
        {
            LSProjectile currentProjectile = ProjectileManager.Create(
                                                 projectileCode,
                                                 Agent,
                                                 projOffset,
                                                 _targetAllegiance,
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
                    throw new Exception("Not implemented yet.");
                    //break;
            }

            return currentProjectile;
        }

        private void FireProjectile(LSProjectile projectile)
        {
            ProjectileManager.Fire(projectile);
        }

        private void StopAttack(bool complete = false)
        {
            inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (MyAttackGroup.IsNotNull() && complete)
            {
                MyAttackGroup.Remove(this);
            }

            if (complete)
            {
                IsAttackMoving = false;
                Agent.Tag = AgentTag.None;
            }
            else if (CurrentTarget.IsNotNull())
            {
                if (IsAttackMoving)
                {
                    CachedMove.StartMove(CurrentTarget.Body.Position);
                }
                else if (canMove && !inRange)
                {
                    CachedMove.StopMove();
                }
            }

            _cachedBody.Priority = basePriority;

            IsCasting = false;

            OnStopAttack?.Invoke();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Agent.IsNull() || !Agent.IsActive)
            {
                return;
            }

            if (Agent.Body.IsNull())
            {
                Debug.Log(Agent.gameObject);
            }

            Gizmos.DrawWireSphere(Application.isPlaying ? Agent.Body._visualPosition : transform.position, Range.ToFloat());
        }
#endif
    }
}