using FastCollections;
using UnityEngine;

namespace RTSLockstep.Grid
{
    //Blocker for static environment pieces in a scene.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityLSBody))]
    public class Blocker : EnvironmentObject
    {
        static readonly FastList<Vector2d> bufferCoordinates = new FastList<Vector2d>();

        [SerializeField]
        private bool _blockPathfinding = true;
        public bool BlockPathfinding { get { return _blockPathfinding; } }

        public LSBody CachedBody { get; private set; }

        protected override void OnLateInitialize()
        {
            base.OnInitialize();

            CachedBody = this.GetComponent<UnityLSBody>().InternalBody;

            if (this.BlockPathfinding)
            {
                bufferCoordinates.FastClear();
                CachedBody.GetCoveredNodePositions(FixedMath.One / 8, bufferCoordinates);

                foreach (Vector2d vec in bufferCoordinates)
                {
                    GridManager.GetCoordinates(vec.x, vec.y, out int gridX, out int gridY);
                    GridNode node = GridManager.GetNode(gridX, gridY);

                    if (node == null)
                    {
                        continue;
                    }

                    node.AddObstacle();
                }
            }
        }
    }
}
