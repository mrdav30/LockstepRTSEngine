using RTSLockstep.Environment;
using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using UnityEngine;

namespace RTSLockstep.Simulation.Grid
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

            CachedBody = GetComponent<UnityLSBody>().InternalBody;

            if (BlockPathfinding)
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
