using System;
using UnityEngine;
using RTSLockstep.Data;
using FastCollections;

namespace RTSLockstep
{
    public sealed class LSProjectile : CerealBehaviour
    {
        public const int DefaultTickRate = LockstepManager.FrameRate / 4;
        private const long Gravity = FixedMath.One * 98 / 10;

        private static Vector2d agentPos;
        private static Vector3 newPos;
        private static Vector2d tempDirection;
        private const int defaultMaxDuration = LockstepManager.FrameRate * 16;
        private GameObject cachedGameObject;
        private Transform cachedTransform;

        public Vector3d Position;

        [FixedNumber, SerializeField]
        public long _speed;

        private int CountDown;

        public Vector3d Velocity { get; private set; }

        public Vector3d Direction { get; set; }

        private Vector2d lastDirection;

        private long speedPerFrame;

        private long HeightSpeed;

        private long arcStartVerticalSpeed;

        private long arcStartHeight;
        private long linearHeightSpeed;

        [SerializeField]
        private bool _visualArc;

        [SerializeField, FrameCount]
        private int _delay;

        [SerializeField]
        private bool _attachEndEffectToTarget;

        public bool AttachEndEffectToTarget { get { return _attachEndEffectToTarget; } }

        [SerializeField, DataCode("Effects"), UnityEngine.Serialization.FormerlySerializedAs("_endEffect")]
        private string _hitFX;

        public string HitFX { get { return _hitFX; } }

        public bool CanRotate = true;

        [SerializeField, DataCode("Effects"), UnityEngine.Serialization.FormerlySerializedAs("_startEffect")]
        private string _startFX;

        public string StartFX { get { return _startFX; } }

        public bool IsActive;

        public bool UseEffects;

        [SerializeField]
        private bool _canVisualize = true;

        public bool CanVisualize { get { return _canVisualize; } }


        [SerializeField]
        private AgentTag _exclusiveTargetType;

        [SerializeField]
        private TargetingType _targetingBehavior;

        public TargetingType TargetingBehavior { get; set; }


        [SerializeField]
        private HitType _hitBehavior;
        public HitType HitBehavior { get; set; }

        [FixedNumberAngle, SerializeField]
        private long _angle = FixedMath.TenDegrees;

        [FixedNumber, SerializeField]
        private long _radius = FixedMath.Create(1);


        [SerializeField, FrameCount]
        private int _lastingDuration;

        [SerializeField, FrameCount]
        private int _tickRate = DefaultTickRate;

        public uint SpawnVersion { get; private set; }
        public bool TargetCurrent { get; private set; }
        //PAPPS ADDED THIS:	it detaches the children particle effects inside the projectile, and siwtches em off.. REALLY NECESSARY
        public bool DoReleaseChildren = false;

        public int AliveTime
        {
            get;
            private set;
        }

        public long Angle
        {
            get { return _angle; }
            set { _angle = value; }
        }

        public long Damage { get; set; }

        public int Delay { get; set; }

        public int LastingDuration { get; set; }

        public int TickRate { get; set; }

        public Vector3 EndPoint
        {
            get
            {
                return Target.CachedTransform.position;
            }
        }

        public long ExclusiveDamageModifier
        {
            get;
            set;
        }

        public AgentTag ExclusiveTargetType
        {
            get;
            set;
        }

        private bool heightReached;

        private bool GetHeightReached()
        {
            return heightReached;
        }

        private void SetHeightReached(bool value)
        {
            heightReached = value;
        }

        public int ID
        {
            get;
            private set;
        }

        public long InterpolationRate
        {
            get;
            set;
        }

        private int MaxDuration
        {
            get
            {
                int minTime = Delay + LastingDuration;
                return (LockstepManager.FrameRate * 16 <= minTime) ? minTime : LockstepManager.FrameRate * 16;
            }
        }

        public string MyProjCode
        {
            get;
            private set;
        }

        public long Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public long Speed { get; set; }

        public RTSAgent Target
        {
            get;
            set;
        }

        public long TargetHeight
        {
            get;
            set;
        }

        public Vector2d TargetPosition
        {
            get;
            set;
        }

        private uint TargetVersion
        {
            get;
            set;
        }

        public Vector2d Forward { get; set; }

        private Action<RTSAgent> HitEffect { get; set; }

        static FastList<RTSAgent> ScanOutput = new FastList<RTSAgent>();

        public Func<byte, bool> BucketConditional { get; private set; }

        public Func<RTSAgent, bool> AgentConditional { get; private set; }

        public bool Deterministic { get; private set; }

        Func<LSBody, bool> BodyConditional;

        private bool IsLasting;
        private int tickTimer;

        public IProjectileData MyData { get; private set; }

        private FastList<LSBody> HitBodies = new FastList<LSBody>();


        public event Action OnDeactivate;

        public event Action OnHit;
        public event Action<RTSAgent> OnHitAgent;

        public event Action OnInitialize;

        public event Action OnSetup;
        public event Action OnVisualize;

        public int GetStateHash()
        {
            int hash = 13;
            hash ^= Position.StateHash;
            return hash;
        }

        private void ApplyArea(Vector2d center, long radius)
        {
            long num = radius * radius;
            Scan(center, radius);
            for (int i = 0; i < ScanOutput.Count; i++)
            {
                RTSAgent agent = ScanOutput[i];
                if (agent.Body.Position.FastDistance(center.x, center.y) < num)
                {
                    HitAgent(agent);
                }
            }
        }

        void HitAgent(RTSAgent agent)
        {
            if (UseEffects && AttachEndEffectToTarget)
            {
                LSEffect lSEffect = EffectManager.CreateEffect(HitFX);
                lSEffect.CachedTransform.parent = agent.VisualCenter;
                lSEffect.CachedTransform.localPosition = Vector3.up;
                lSEffect.CachedTransform.rotation = cachedTransform.rotation;
                lSEffect.Initialize();
            }
            HitEffect(agent);
            OnHitAgent?.Invoke(agent);
        }

        private void ApplyCone(Vector3d center3d, Vector2d forward, long radius, long angle)
        {
            Vector2d center = center3d.ToVector2d();
            long fastRange = radius * radius;
            Scan(center, radius);
            for (int i = 0; i < ScanOutput.Count; i++)
            {
                RTSAgent agent = ScanOutput[i];
                Vector2d agentPos = agent.Body.Position;
                Vector2d difference = agentPos - center;

                if (difference.FastMagnitude() > fastRange)
                {
                    continue;
                }

                if (forward.Dot(difference) < 0)
                {
                    continue;

                }
                difference.Normalize();

                long cross = forward.Cross(difference).Abs();
                if (cross > angle)
                {
                    continue;
                }
                HitAgent(agent);
            }
        }

        private bool CheckCollision()
        {
            return CheckCollision(Target.Body);
        }

        private bool CheckCollision(LSBody target)
        {
            return target._position.FastDistance(Position.x, Position.y) <= target.FastRadius;
        }

        private void Scan(Vector2d center, long radius)
        {
            InfluenceManager.ScanAll(
                center,
                radius,
                AgentConditional,
                BucketConditional,
                ScanOutput
            );

        }

        internal void Deactivate()
        {
            SpawnVersion = 0;
            TargetVersion = 0;
            IsActive = false;
            if (cachedGameObject.IsNotNull())
                cachedGameObject.SetActive(false);
            if (cachedTransform.IsNotNull())
            {
                cachedTransform.parent = null;
            }

            OnDeactivate?.Invoke();
        }

        public bool IsExclusiveTarget(AgentTag AgentTag)
        {
            return ExclusiveTargetType != AgentTag.None && AgentTag == ExclusiveTargetType;
        }

        public long CheckExclusiveDamage(AgentTag AgentTag)
        {
            return IsExclusiveTarget(AgentTag) ? Damage.Mul(ExclusiveDamageModifier) : Damage;
        }

        private void Hit(bool destroy = true)
        {

            OnProjectileHit();
            if (OnHit.IsNotNull())
            {
                OnHit();
            }

            if (TargetCurrent)
            {
                if (UseEffects)
                {
                    if (AttachEndEffectToTarget)
                    {
                        LSEffect lSEffect = EffectManager.CreateEffect(HitFX);
                        lSEffect.CachedTransform.parent = Target.VisualCenter;
                        lSEffect.CachedTransform.localPosition = Vector3.up;
                        lSEffect.CachedTransform.rotation = cachedTransform.rotation;
                        lSEffect.Initialize();
                    }
                    else
                    {
                        //Certain targeting types collide with a target
                        if (TargetingBehavior == TargetingType.Homing)
                        {
                            EffectManager.CreateCollisionEffect(HitFX, this, Target.Body);
                        }
                        else
                        {
                            EffectManager.CreateEffect(HitFX, cachedTransform.position, cachedTransform.rotation);
                        }
                    }
                }
            }

            if (destroy)
            {
                ProjectileManager.EndProjectile(this);
            }
        }

        internal void Prepare(int id, Vector3d projectilePosition, Func<RTSAgent, bool> agentConditional, Func<byte, bool> bucketConditional, Action<RTSAgent> onHit, bool deterministic)
        {
            Deterministic = deterministic;

            IsActive = true;
            cachedGameObject.SetActive(true);

            ResetVariables();

            Position = projectilePosition;
            HitEffect = onHit;
            ID = id;

            AliveTime = 0;
            IsLasting = false;

            BucketConditional = bucketConditional;

            AgentConditional = agentConditional;

            Forward = Vector2d.up;
        }

        public void InitializeHoming(RTSAgent target)
        {
            SetHeightReached(false);
            Target = target;
            TargetVersion = Target.SpawnVersion;

            TargetPosition = Target.Body.Position;
            TargetHeight = Target.Body.HeightPos + Target.Body.Height / 2;

            cachedTransform.rotation = Quaternion.LookRotation(target.CachedTransform.position - Position.ToVector3());
        }

        public void InitializeTimed(Vector2d forward)
        {
            Forward = forward;
            Direction = forward.ToVector3d();
        }

        public void InitializeFree(Vector3d direction, Func<LSBody, bool> bodyConditional, bool useGravity = false)
        {

            BodyConditional = bodyConditional;
            Direction = direction;
            Forward = Direction.ToVector2d();

            cachedTransform.rotation = Quaternion.LookRotation(direction.ToVector3());
        }

        public void InitializePositional(Vector3d position)
        {
            TargetPosition = position.ToVector2d();
            TargetHeight = position.z;

        }

        public void UpdateVisuals()
        {
            if (!Forward.EqualsZero())
            {
                cachedTransform.rotation = Quaternion.LookRotation(Forward.ToVector3());
            }

            cachedTransform.position = Position.ToVector3();
        }

        public void LateInit()
        {

            if (TargetingBehavior != TargetingType.Timed)
            {
                cachedTransform.position = Position.ToVector3();
                speedPerFrame = Speed / 32L;
            }

            switch (TargetingBehavior)
            {
                case TargetingType.Timed:
                    CountDown = Delay;
                    break;
                case TargetingType.Positional:
                case TargetingType.Homing:
                    long f = Position.ToVector2d().Distance(TargetPosition);
                    long timeToHit = f.Div(Speed);
                    if (_visualArc)
                    {
                        arcStartHeight = Position.z;
                        if (timeToHit > 0)
                        {
                            arcStartVerticalSpeed = (TargetHeight - Position.z).Div(timeToHit) + timeToHit.Mul(Gravity);
                        }
                    }
                    else
                    {
                        if (timeToHit > 0)
                        {
                            linearHeightSpeed = (TargetHeight - Position.z).Div(timeToHit).Abs() / LockstepManager.FrameRate;
                        }
                    }
                    Forward = TargetPosition - Position.ToVector2d();
                    Forward.Normalize();
                    break;
                case TargetingType.Directional:
                    Vector3d vel = Direction;
                    vel.Mul(speedPerFrame);
                    Velocity = vel;
                    break;
            }

            if (CanRotate)
            {
                cachedTransform.LookAt(Direction.ToVector3());
            }
            UpdateVisuals();

            OnInitialize?.Invoke();

            if (UseEffects)
            {
                LSEffect effect = EffectManager.CreateEffect(StartFX, Position.ToVector3(), cachedTransform.rotation);
                if (effect.IsNotNull())
                {
                    effect.StartPos = Position.ToVector3();
                    effect.EndPos = TargetPosition.ToVector3(TargetHeight.ToFloat());
                    if (Target.IsNotNull())
                    {
                        effect.Target = Target.transform;
                    }
                }
            }
        }

        private void OnProjectileHit()
        {
            if (TargetingBehavior == TargetingType.Directional)
            {
                switch (HitBehavior)
                {
                    case HitType.Single:
                        //todo: Implement
                        break;
                }
            }
            else
            {
                switch (HitBehavior)
                {
                    case HitType.None:
                        break;
                    case HitType.Single:
                        if (Target.IsNull())
                        {
                            break;
                        }
                        HitAgent(Target);
                        break;
                    case HitType.Area:
                        ApplyArea(Position.ToVector2d(), Radius);
                        break;
                    case HitType.Cone:
                        Debug.Log(Forward);
                        ApplyCone(Position, Forward, Radius, Angle);
                        break;
                }
            }
        }

        private void ResetVariables()
        {
            ResetEffects();
            ResetTrajectory();
            ResetHit();
            ResetTargeting();
            ResetHelpers();
        }

        private void ResetHit()
        {
            ExclusiveTargetType = _exclusiveTargetType;
            OnHit = null;
            OnHitAgent = null;
            Target = null;
            HitBehavior = _hitBehavior;
            TargetCurrent = true;
        }

        private void ResetEffects()
        {
        }

        private void ResetHelpers()
        {
            lastDirection = Vector2d.zero;
            Velocity = default;
            Direction = Vector2d.up.ToVector3d();
        }

        private void ResetTargeting()
        {
            Delay = _delay;
            Speed = _speed;
            LastingDuration = _lastingDuration;
            TickRate = _tickRate;
            TargetingBehavior = _targetingBehavior;
        }

        private void ResetTrajectory()
        {
        }

        public void Setup(IProjectileData dataItem)
        {
            SpawnVersion = 1u;
            MyData = dataItem;
            MyProjCode = dataItem.Name;
            cachedGameObject = gameObject;
            cachedTransform = transform;
            DontDestroyOnLoad(cachedGameObject);

            OnSetup?.Invoke();
        }

        public void Simulate()
        {
            AliveTime++;

            if (AliveTime > MaxDuration)
            {
                ProjectileManager.EndProjectile(this);
                return;
            }
            switch (TargetingBehavior)
            {
                case TargetingType.Timed:
                    CountDown--;

                    if (!IsLasting)
                    {
                        if (CountDown <= 0)
                        {
                            IsLasting = true;
                            tickTimer = 0;
                        }
                    }
                    if (IsLasting)
                    {
                        tickTimer--;
                        if (tickTimer <= 0)
                        {
                            tickTimer = TickRate;
                            Hit((AliveTime + TickRate - Delay) >= LastingDuration);
                        }
                    }
                    break;
                case TargetingType.Homing:
                    if (TargetingBehavior == TargetingType.Homing && HitBehavior == HitType.Single && Target.SpawnVersion != TargetVersion)
                    {
                        //Switch to positional to move to target's last position and not seek deceased target
                        TargetingBehavior = TargetingType.Positional;
                        Target = null;
                        TargetCurrent = false;
                        goto case TargetingType.Positional;
                    }
                    if (CheckCollision())
                    {
                        TargetPosition = Target.Body.Position;
                        Hit();
                    }
                    else
                    {
                        TargetPosition = Target.Body.Position;
                        TargetHeight = Target.Body.HeightPos + Target.Body.Height / 2;

                        MoveToTargetPosition();
                    }
                    break;
                case TargetingType.Directional:
                    RaycastMove(Velocity);
                    break;
                case TargetingType.Positional:
                    MoveToTargetPosition();

                    break;
            }
        }

        void MoveToTargetPosition()
        {
            if (_visualArc)
            {
                long progress = FixedMath.Create(AliveTime) / 32;
                long height = arcStartHeight + arcStartVerticalSpeed.Mul(progress) - Gravity.Mul(progress.Mul(progress));
                Position.z = height;
            }
            else
            {
                Position.z = FixedMath.MoveTowards(Position.z, TargetHeight, linearHeightSpeed);
            }

            tempDirection = TargetPosition - Position.ToVector2d();
            if (tempDirection.Dot(lastDirection.x, lastDirection.y) < 0L || tempDirection == Vector2d.zero)
            {
                Hit();
            }
            else
            {
                tempDirection.Normalize();
                Forward = tempDirection;
                lastDirection = tempDirection;
                tempDirection *= speedPerFrame;
                Position.Add(tempDirection.ToVector3d());
            }
        }

        public void RaycastMove(Vector3d delta)
        {
#if true
            Vector3d nextPosition = Position;
            nextPosition.Add(ref delta);
            HitBodies.FastClear();
            foreach (LSBody body in Raycaster.RaycastAll(Position, nextPosition))
            {
                if (BodyConditional(body))
                {
                    HitBodies.Add(body);
                }
            }

            if (HitBodies.Count > 0)
            {
                Hit();
            }

            Position = nextPosition;
#endif
        }

        public void Visualize()
        {
            if (IsActive)
            {
                if (CanVisualize)
                {
                    newPos = Position.ToVector3();
                    Vector3 shiftVelocity = LSProjectile.newPos - cachedTransform.position;
                    cachedTransform.position = LSProjectile.newPos;
                    if (shiftVelocity.sqrMagnitude > 0)
                    {
                        cachedTransform.rotation = Quaternion.LookRotation(shiftVelocity);
                    }
                }

                OnVisualize?.Invoke();
            }
            else
            {
                cachedGameObject.SetActive(false);
            }
        }
    }
}
