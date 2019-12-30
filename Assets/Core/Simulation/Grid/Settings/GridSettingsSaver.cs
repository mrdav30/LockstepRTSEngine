using RTSLockstep.Environment;
using RTSLockstep.Simulation.LSMath;
using UnityEngine;

namespace RTSLockstep.Simulation.Grid
{
    public sealed class GridSettingsSaver : EnvironmentSaver
    {
        [SerializeField]
        private Vector2d _mapCenter;
        public Vector2d Offset
        {
            get
            {
                return _mapCenter - new Vector2d(_mapWidth / 2, _mapHeight / 2);
            }
        }

        [SerializeField]
        private int _mapWidth = 100;
        public int MapWidth { get { return _mapWidth; } }
        [SerializeField]
        private int _mapHeight = 100;
        public int MapHeight { get { return _mapHeight; } }

        [SerializeField]
        private bool _useDiagonalConnections = true;
        public bool UseDiagonalConnetions { get { return _useDiagonalConnections; } }

        protected override void OnSave()
        {
            _mapCenter = new Vector2d(transform.position);
        }

        protected override void OnEarlyApply()
        {
            GridManager.Settings = new GridSettings(MapWidth, MapHeight, Offset.x, Offset.y, UseDiagonalConnetions);
        }

#if UNITY_EDITOR
        public bool Show;

        void OnDrawGizmos()
        {
            if (!Show || Application.isPlaying)
            {
                return;
            }
            Gizmos.color = Color.green;
            Vector3 offset = Offset.ToVector3(transform.position.y);
            Vector3 scale = Vector3.one * .5f;
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    Vector3 drawPos = new Vector3(x, 0f, y);
                    drawPos += offset;
                    Gizmos.DrawCube(drawPos, scale);
                }
            }
        }
#endif
    }
}