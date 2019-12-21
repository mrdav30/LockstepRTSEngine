//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
//=======================================================================

using FastCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [System.Serializable]
    public partial class LSBody
    {
        #region Core deterministic variables
        //TODO: Account for teleports when culling.
        /// <summary>
        /// Used to prevent distance culling for very large objects.
        /// </summary>
        [SerializeField, Tooltip("Useful for fast-moving objects that might pass through if not checked for a frame.")]
        private bool _preventCulling = false;
        #endregion

        #region Lockstep variables
        private bool _forwardNeedsSet = false;

        private Vector2d _forward;
        public Vector2d Forward
        {
            get
            {
                return Rotation.ToDirection();
            }
            set
            {
                Rotation = value.ToRotation();
            }
        }

        [Lockstep]
        public bool PositionChanged { get; set; }
        [SerializeField] //For inspector debugging
        internal Vector2d _position;
        [Lockstep]
        public Vector2d Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                PositionChanged = true;
            }
        }

        private bool _rotationChanged;
        [Lockstep]
        public bool RotationChanged
        {
            get
            {
                return _rotationChanged;
            }
            set
            {
                if (value)
                {
                    _forwardNeedsSet = true;
                }

                _rotationChanged = value;
            }
        }
        [SerializeField]
        internal Vector2d _rotation = Vector2d.up;
        [Lockstep]
        public Vector2d Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
                RotationChanged = true;
            }
        }

        [Lockstep]
        public bool HeightPosChanged { get; set; }
        [SerializeField, FixedNumber]
        internal long _heightPos;
        [Lockstep]
        public long HeightPos
        {
            get { return _heightPos; }
            set
            {
                _heightPos = value;
                HeightPosChanged = true;
            }
        }

        [Lockstep]
        public bool VelocityChanged { get; set; }
        /// <summary>
        /// Units per second the unit is moving.
        /// </summary>
        /// <value>The velocity.</value>
        [SerializeField]
        internal Vector2d _velocity;
        [Lockstep]
        public Vector2d Velocity
        {
            get { return _velocity; }
            set
            {
                _velocity = value;
                VelocityChanged = true;
            }
        }
        public Vector2d LastPosition { get; private set; }

        internal uint RaycastVersion { get; set; }
        internal bool PreventCulling { get { return _preventCulling; } }

        #endregion

        public event CollisionFunction OnContact;
        public event CollisionFunction OnContactEnter;
        public event CollisionFunction OnContactExit;
        public int Priority { get; set; }
        public int ID { get; private set; }
        public RTSAgent Agent { get; private set; }
        public long FastRadius { get; private set; }
        public bool PositionChangedBuffer { get; private set; } //D
        public bool RotationChangedBuffer { get; private set; } //D
        public long VelocityMagnitude { get; private set; }
        public bool Active { get; private set; }

        public long XMin { get; private set; }
        public long XMax { get; private set; }
        public long YMin { get; private set; }
        public long YMax { get; private set; }

        public int PastGridXMin { get; set; }
        public int PastGridXMax { get; set; }
        public int PastGridYMin { get; set; }
        public int PastGridYMax { get; set; }

        public long HeightMin { get; private set; }
        public long HeightMax { get; private set; }

        public delegate void CollisionFunction(LSBody other);

        public Vector2d ImmovableCollisionDirection;

        internal Vector3 _visualPosition;
        public Vector3 VisualPosition { get { return _visualPosition; } }

        private int _settingVisualsCounter;
        bool SettingVisuals { get { return _settingVisualsCounter > 0; } }

        private const int SETTING_VISUALS_COUNT = LockstepManager.FrameRate;
        private bool Setted { get; set; }

        private FastBucket<LSBody> Children;
        public Vector2d[] RealPoints;
        public Vector2d[] Edges;
        public Vector2d[] EdgeNorms;

        private Dictionary<int, CollisionPair> _collisionPairs;
        private HashSet<int> _collisionPairHolders;

        /// <summary>
        /// Used for preventing culling for the first frame this object is added to a new partition node.
        /// </summary>
        internal bool PartitionChanged { get; set; }
        /// <summary>
        /// TODO: Do away with CollisionPairs? Just dynamically collide... much easier and less memory for mobile.
        /// Potentially faster especially for less physics objects.
        /// </summary>
        internal Dictionary<int, CollisionPair> CollisionPairs
        {
            get
            {
                return _collisionPairs.IsNotNull() ? _collisionPairs : (_collisionPairs = new Dictionary<int, CollisionPair>());
            }
        }
        internal HashSet<int> CollisionPairHolders
        {
            get
            {
                return _collisionPairHolders ?? (_collisionPairHolders = new HashSet<int>());
            }
        }
        internal void NotifyContact(LSBody other, bool isColliding, bool isChanged)
        {
            if (isColliding)
            {
                if (isChanged)
                {
                    if (OnContactEnter.IsNotNull())
                        OnContactEnter(other);
                }
                if (OnContact != null)
                    OnContact(other);

            }
            else
            {
                if (isChanged)
                {
                    if (OnContactExit != null)
                        OnContactExit(other);
                }
            }
        }

        internal int DynamicID = -1;

        #region Serialized

        [SerializeField]
        protected ColliderType _shape = ColliderType.None;
        public ColliderType Shape { get { return _shape; } }

        [SerializeField]
        private bool _isTrigger;
        public bool IsTrigger { get { return _isTrigger; } }

        [SerializeField]
        private int _layer;
        public int Layer { get { return _layer; } }

        [Lockstep]
        public bool WidthChanged { get; set; }
        [SerializeField, FixedNumber]
        private long _halfWidth = FixedMath.Half;
        [HideInInspector]
        public long HalfWidth
        {
            get { return _halfWidth; }
            set
            {
                _halfWidth = value;
                WidthChanged = true;
            }
        }
        [Lockstep]
        public bool LengthChanged { get; set; }
        [SerializeField, FixedNumber]
        private long _halfLength = FixedMath.Half;
        [HideInInspector]
        public long HalfLength
        {
            get { return _halfLength; }
            set
            {
                _halfLength = value;
                LengthChanged = true;
            }
        }

        [SerializeField, FixedNumber]
        protected long _radius = FixedMath.Half;
        /// <summary>
        /// Gets the bounding circle radius.
        /// </summary>
        /// <value>The radius.</value>
        public long Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
            }
        }

        [SerializeField]
        protected bool _immovable;
        public bool GetSavedImmovable()
        {
            return _immovable;
        }
        public bool Immovable { get; private set; }

        [SerializeField]
        private int _basePriority;
        public int BasePriority { get { return _basePriority; } }

        [SerializeField]
        private Vector2d[] _vertices;
        public Vector2d[] Vertices { get { return _vertices; } }

        [SerializeField, FixedNumber]
        private long _height = FixedMath.One;
        public long Height { get { return _height; } }

        [SerializeField]
        private Transform _positionalTransform;
        public Transform PositionalTransform { get; set; }

        [SerializeField]
        private Transform _rotationalTransform;
        public Transform RotationalTransform { get; set; }
        #endregion

        #region Runtime Values
        private bool _canSetVisualPosition;
        public bool CanSetVisualPosition
        {
            get
            {
                return _canSetVisualPosition;
            }
            set
            {
                _canSetVisualPosition = value && PositionalTransform != null;
            }
        }

        private bool _canSetvisualRotationation;
        public bool CanSetvisualRotation
        {
            get
            {
                return _canSetvisualRotationation && RotationalTransform != null;
            }
            set
            {
                _canSetvisualRotationation = value;
            }
        }

        public Vector3d Position3d
        {
            get
            {
                return _position.ToVector3d(HeightPos);
            }
        }
        public Transform Transform { get; internal set; }
        public bool OutMoreThan { get; private set; }

        private Vector2d[] RotatedPoints;
        private Vector3 velocityPosition;
        private bool OutMoreThanSet { get; set; }
        #endregion

        private Quaternion GetVisualRotation()
        {
            return Quaternion.LookRotation(Forward.ToVector3(0));
        }

        private Vector3 lastVisualPos;

        //Testing out vectors instead of quaternions for interpolation
        private Quaternion lastvisualRotationation;
        private Quaternion visualRotation;

        #region Behavior
        public void Setup(RTSAgent agent)
        {
            if (Shape == ColliderType.Polygon)
            {
                // TODO
            }
            if (Shape != ColliderType.None)
            {
                GeneratePoints();
                GenerateBounds();
            }
            Agent = agent;
            Setted = true;

            Immovable = _immovable || (Shape != ColliderType.Circle) || Shape == ColliderType.None;
        }

        public void Initialize(Vector3d StartPosition, Vector2d StartRotation, bool isDynamic = true)
        {
            Active = true;
            PositionalTransform = _positionalTransform;
            RotationalTransform = _rotationalTransform;
            if (!Setted)
            {
                Setup(null);
            }

            RaycastVersion = 0;

            HeightPosChanged = true;

            PositionChanged = true;
            RotationChanged = true;
            VelocityChanged = true;
            PositionChangedBuffer = true;
            RotationChangedBuffer = true;

            Priority = BasePriority;
            Velocity = Vector2d.zero;
            VelocityMagnitude = 0;
            LastPosition = _position = StartPosition.ToVector2d();
            _heightPos = StartPosition.z;
            _rotation = StartRotation;
            _forwardNeedsSet = true;
            FastRadius = Radius * Radius;

            XMin = 0;
            XMax = 0;
            YMin = 0;
            YMax = 0;

            PastGridXMin = int.MaxValue;
            PastGridXMax = int.MaxValue;
            PastGridYMin = int.MaxValue;
            PastGridYMax = int.MaxValue;

            if (Shape != ColliderType.None)
            {
                BuildPoints();
                BuildBounds();
            }

            ID = PhysicsManager.Assimilate(this, isDynamic);
            Partition.PartitionObject(this);
            if (PositionalTransform != null)
            {
                CanSetVisualPosition = true;
                _visualPosition = _position.ToVector3(HeightPos.ToFloat());
                lastVisualPos = _visualPosition;
                PositionalTransform.position = _visualPosition;
            }
            else
            {
                CanSetVisualPosition = false;
            }

            if (RotationalTransform != null)
            {
                CanSetvisualRotation = Agent.MyAgentType != AgentType.Structure ? true : false;

                if (Forward != Vector2d.zero)
                {
                    visualRotation = GetVisualRotation();
                }

                lastvisualRotationation = visualRotation;
            }
            else
            {
                CanSetvisualRotation = false;
            }

            SetVisuals();

            velocityPosition = Vector3.zero;
            ImmovableCollisionDirection = Vector2d.zero;
            PartitionChanged = true;
        }

        public void Simulate()
        {
            if (VelocityChanged)
            {
                VelocityMagnitude = _velocity.Magnitude();
                VelocityChanged = false;
            }

            LastPosition = _position;

            //  Move agents based on forces being applied (aka physics)
            if (VelocityMagnitude != 0)
            {
                //  Apply the force
                _position.x += _velocity.x / LockstepManager.FrameRate;
                _position.y += _velocity.y / LockstepManager.FrameRate;
                PositionChanged = true;
            }

            BuildChangedValues();

            PartitionChanged = false;
            if (PositionChanged || PositionChangedBuffer)
            {
                Partition.UpdateObject(this);
            }

            if (SettingVisuals)
            {
                _settingVisualsCounter--;
            }
        }
        #endregion

        private void AddChild(LSBody child)
        {
            if (Children.IsNull())
            {
                Children = new FastBucket<LSBody>();
            }

            Children.Add(child);
        }

        private void RemoveChild(LSBody child)
        {
            Children.Remove(child);
        }

        public void GeneratePoints()
        {
            if (Shape == ColliderType.Polygon)
            {
                RotatedPoints = new Vector2d[Vertices.Length];
                RealPoints = new Vector2d[Vertices.Length];
                Edges = new Vector2d[Vertices.Length];
                EdgeNorms = new Vector2d[Vertices.Length];
            }
        }

        public void GenerateBounds()
        {
            if (Shape == ColliderType.Circle)
            {
                _radius = Radius;
            }
            else if (Shape == ColliderType.AABox)
            {
                _radius = FixedMath.Sqrt((HalfLength * HalfLength + HalfWidth * HalfWidth) >> FixedMath.SHIFT_AMOUNT);
            }
            else if (Shape == ColliderType.Polygon)
            {
                long BiggestSqrRadius = Vertices[0].SqrMagnitude();
                for (int i = 1; i < Vertices.Length; i++)
                {
                    long sqrRadius = Vertices[i].SqrMagnitude();
                    if (sqrRadius > BiggestSqrRadius)
                    {
                        BiggestSqrRadius = sqrRadius;
                    }
                }
                _radius = FixedMath.Sqrt(BiggestSqrRadius);
                FastRadius = Radius * Radius;
            }
        }

        private void BuildChangedValues()
        {
            if (PositionChanged || RotationChanged)
            {
                BuildPoints();
                BuildBounds();
                //Reset this value so we're not permanently considered colliding against wall
                ImmovableCollisionDirection = Vector2d.zero;
            }

            if (PositionChanged || HeightPosChanged)
            {
                PositionChangedBuffer = PositionChanged ? true : false;
                PositionChanged = false;
                HeightPosChanged = false;
                _settingVisualsCounter = SETTING_VISUALS_COUNT;
            }
            else
            {
                PositionChangedBuffer = false;
            }

            if (RotationChanged)
            {
                _rotation.Normalize();
                RotationChangedBuffer = true;
                RotationChanged = false;
                _settingVisualsCounter = SETTING_VISUALS_COUNT;
            }
            else
            {
                RotationChangedBuffer = false;
            }
        }

        public void BuildPoints()
        {
            if (Shape == ColliderType.Polygon)
            {
                int VertLength = Vertices.Length;

                if (RotationChanged)
                {
                    for (int i = 0; i < VertLength; i++)
                    {
                        RotatedPoints[i] = Vertices[i];
                        RotatedPoints[i].Rotate(_rotation.x, _rotation.y);
                    }
                    for (int i = VertLength - 1; i >= 0; i--)
                    {
                        int nextIndex = i + 1 < VertLength ? i + 1 : 0;
                        Vector2d point = RotatedPoints[nextIndex];
                        point.Subtract(ref RotatedPoints[i]);
                        point.Normalize();
                        Edges[i] = point;
                        point.RotateRight();
                        EdgeNorms[i] = point;
                    }
                    if (!OutMoreThanSet)
                    {
                        OutMoreThanSet = true;
                        long dot = Edges[0].Cross(Edges[1]);
                        OutMoreThan = dot < 0;
                    }
                }

                for (int i = 0; i < VertLength; i++)
                {
                    RealPoints[i].x = RotatedPoints[i].x + _position.x;
                    RealPoints[i].y = RotatedPoints[i].y + _position.y;
                }
            }
        }

        public void BuildBounds()
        {
            HeightMin = HeightPos;
            HeightMax = HeightPos + Height;
            if (Shape == ColliderType.Circle)
            {
                XMin = -Radius + _position.x;
                XMax = Radius + _position.x;
                YMin = -Radius + _position.y;
                YMax = Radius + _position.y;
            }
            else if (Shape == ColliderType.AABox)
            {
                XMin = -HalfWidth + _position.x;
                XMax = HalfWidth + _position.x;
                YMin = -HalfLength + _position.y;
                YMax = HalfLength + _position.y;
            }
            else if (Shape == ColliderType.Polygon)
            {
                XMin = _position.x;
                XMax = _position.x;
                YMin = _position.y;
                YMax = _position.y;
                for (int i = 0; i < Vertices.Length; i++)
                {
                    Vector2d vec = RealPoints[i];
                    if (vec.x < XMin)
                    {
                        XMin = vec.x;
                    }
                    else if (vec.x > XMax)
                    {
                        XMax = vec.x;
                    }

                    if (vec.y < YMin)
                    {
                        YMin = vec.y;
                    }
                    else if (vec.y > YMax)
                    {
                        YMax = vec.y;
                    }
                }
            }
        }

        public void SetVisuals()
        {
            if (SettingVisuals)
            {
                if (PhysicsManager.ResetAccumulation)
                {
                    DoSetVisualPosition(_position.ToVector3(HeightPos.ToFloat()));
                    DoSetVisualRotation(Rotation);
                }
                //PositionalTransform.position = Vector3.SmoothDamp (lastVisualPos, _visualPosition, ref velocityPosition, PhysicsManager.LerpTime);
                if (CanSetVisualPosition)
                {
                    PositionalTransform.position = Vector3.Lerp(lastVisualPos, VisualPosition, (float)PhysicsManager.ExpectedAccumulation);
                }

                if (CanSetvisualRotation)
                {
                    RotationalTransform.rotation = Quaternion.Slerp(lastvisualRotationation, visualRotation, (float)PhysicsManager.ExpectedAccumulation);
                }
            }
        }

        private void DoSetVisualPosition(Vector3 pos)
        {
            if (CanSetVisualPosition)
            {
                lastVisualPos = PositionalTransform.position;
                _visualPosition = pos;
            }
        }

        private void DoSetVisualRotation(Vector2d rot)
        {
            if (CanSetvisualRotation)
            {
                lastvisualRotationation = RotationalTransform.rotation;
                if (Forward != Vector2d.zero)
                {
                    visualRotation = GetVisualRotation();
                }
            }
        }

        public void Rotate(long cos, long sin)
        {
            _rotation.Rotate(cos, sin);
            RotationChanged = true;
        }

        public void SetRotation(long x, long y)
        {
            _rotation = new Vector2d(x, y);
            RotationChanged = true;
        }

        static void DeactivatePair(CollisionPair collisionPair)
        {
            PhysicsManager.DeactivateCollisionPair(collisionPair);
        }

        /// <summary>
        /// Call this to deactivate this body and remove from simulation.
        /// </summary>
        public void Deactivate()
        {
            //Don't double deactivate
            if (!Active)
            {
                return;
            }

            Partition.UpdateObject(this, false);

            foreach (var collisionPair in CollisionPairs.Values)
            {
                collisionPair.Body2.CollisionPairHolders.Remove(ID);
                DeactivatePair(collisionPair);

            }

            CollisionPairs.Clear();
            foreach (var id in CollisionPairHolders)
            {
                LSBody other = PhysicsManager.SimObjects[id];
                if (other.IsNotNull())
                {
                    if (other.CollisionPairs.TryGetValue(ID, out CollisionPair collisionPair))
                    {
                        other.CollisionPairs.Remove(ID);
                        DeactivatePair(collisionPair);
                    }
                    else
                    {
                        Debug.Log("nope " + ID);
                    }
                }
            }
            CollisionPairHolders.Clear();

            PhysicsManager.Dessimilate(this);
            Active = false;
        }

        public bool HeightOverlaps(long heightPos)
        {
            return heightPos >= HeightMin && heightPos <= HeightMax;
        }

        public bool HeightOverlaps(long heightMin, long heightMax)
        {
            return heightMax >= HeightMin && heightMin <= HeightMax;
        }

        private long GetCeiledSnap(long f, long snap)
        {
            return (f + snap - 1) / snap * snap;
        }

        private long GetFlooredSnap(long f, long snap)
        {
            return (f / snap) * snap;
        }

        public void GetCoveredNodePositions(long resolution, FastList<Vector2d> output)
        {
            long xmin = GetFlooredSnap(XMin - FixedMath.Half, FixedMath.One);
            long ymin = GetFlooredSnap(YMin - FixedMath.Half, FixedMath.One);

            long xmax = GetCeiledSnap(XMax + FixedMath.Half - xmin, FixedMath.One) + xmin;
            long ymax = GetCeiledSnap(YMax + FixedMath.Half - ymin, FixedMath.One) + ymin;

            long xAcc = 0;
            long yAcc = 0;
            for (long x = xmin; x < xmax;)
            {
                for (long y = ymin; y < ymax;)
                {
                    Vector2d checkPos = new Vector2d(x + xAcc, y + xAcc);
                    if (IsPositionCovered(checkPos))
                    {
                        output.Add(checkPos);
                    }
                    yAcc += resolution;
                    if (yAcc >= FixedMath.One)
                    {
                        //Move on to next node position
                        yAcc -= FixedMath.One;
                        y += FixedMath.One;
                    }
                }
                xAcc += resolution;
                if (xAcc >= FixedMath.One)
                {
                    xAcc -= FixedMath.One;
                    x += FixedMath.One;
                }
            }
        }

        public void GetCoveredSnappedPositions(long snapSpacing, FastList<Vector2d> output)
        {
            //long referenceX = 0,
            //referenceY = 0;
            long xmin = GetFlooredSnap(XMin - FixedMath.Half, snapSpacing);
            long ymin = GetFlooredSnap(YMin - FixedMath.Half, snapSpacing);

            long xmax = GetCeiledSnap(XMax + FixedMath.Half - xmin, snapSpacing) + xmin;
            long ymax = GetCeiledSnap(YMax + FixedMath.Half - ymin, snapSpacing) + ymin;

            //Used for getting snapped positions this body covered
            for (long x = xmin; x < xmax; x += snapSpacing)
            {
                for (long y = ymin; y < ymax; y += snapSpacing)
                {
                    Vector2d checkPos = new Vector2d(x, y);
                    if (IsPositionCovered(checkPos))
                    {
                        output.Add(checkPos);
                    }
                }
            }
        }

        //Checks if this body covers a position
        public bool IsPositionCovered(Vector2d position)
        {
            //Different techniques for different shapes
            switch (Shape)
            {
                case ColliderType.Circle:
                    long maxDistance = Radius + FixedMath.Half;
                    maxDistance *= maxDistance;
                    if ((_position - position).FastMagnitude() > maxDistance)
                    {
                        return false;
                    }
                    goto case ColliderType.AABox;
                case ColliderType.AABox:
                    return position.x + FixedMath.Half > XMin && position.x - FixedMath.Half < XMax
                    && position.y + FixedMath.Half > YMin && position.y - FixedMath.Half < YMax;
                case ColliderType.Polygon:
                    for (int i = EdgeNorms.Length - 1; i >= 0; i--)
                    {
                        Vector2d norm = EdgeNorms[i];
                        long posProj = norm.Dot(position);
                        CollisionPair.ProjectPolygon(norm.x, norm.y, this, out long polyMin, out long polyMax);
                        if (posProj <= polyMin && posProj >= polyMax)
                        {
                            return false;
                        }
                    }
                    return true;
            }

            return false;
        }

        internal void Reset()
        {
            _positionalTransform = Transform;
            _rotationalTransform = Transform;
        }

        private void OnDrawGizmos()
        {
            //return;
            //Don't draw gizmos before initialization
            if (!Application.isPlaying)
            {
                return;
            }

            switch (Shape)
            {
                case ColliderType.Circle:
                    Gizmos.DrawWireSphere(_position.ToVector3(HeightPos.ToFloat()), Radius.ToFloat());
                    break;
                case ColliderType.AABox:
                    Gizmos.DrawWireCube(
                        _position.ToVector3(HeightPos.ToFloat() + Height.ToFloat() / 2),
                        new Vector3(HalfWidth.ToFloat() * 2, Height.ToFloat(), HalfLength.ToFloat() * 2));
                    break;
                case ColliderType.Polygon:
                    if (RealPoints.Length > 1)
                    {
                        for (int i = 0; i < RealPoints.Length; i++)
                        {
                            Gizmos.DrawLine(RealPoints[i].ToVector3(), RealPoints[i + 1 < RealPoints.Length ? i + 1 : 0].ToVector3());
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Returns 0 if not implemented or invalid.
        /// </summary>
        /// <value>The size of the grid square.</value>
        public long SquareSize
        {
            get
            {
                switch (Shape)
                {
                    case ColliderType.Circle:
                        return Radius * 2;
                    case ColliderType.AABox:
                        if (HalfWidth > HalfLength)
                        {
                            return HalfWidth * 2;
                        }
                        else
                        {
                            return HalfLength * 2;
                        }
                    default:
                        return 0;
                }
            }
        }

        public LSBody Clone()
        {
            return Clone<LSBody>();
        }

        public TLSBody Clone<TLSBody>() where TLSBody : LSBody, new()
        {
            TLSBody body = new TLSBody
            {
                _shape = _shape,
                _isTrigger = _isTrigger,
                _layer = _layer,
                _halfWidth = _halfWidth,
                _halfLength = _halfLength,
                _radius = _radius,
                _immovable = _immovable,
                _basePriority = _basePriority,
                _vertices = _vertices,
                _height = _height,
                _positionalTransform = _positionalTransform,
                _rotationalTransform = _rotationalTransform
            };

            return body;
        }
    }

    public enum ColliderType : byte
    {
        None,
        Circle,
        AABox,
        Polygon
    }
}