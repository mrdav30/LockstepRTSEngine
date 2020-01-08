using UnityEngine;

using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;

namespace RTSLockstep.Simulation.Grid
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityLSBody))]
    public class DynamicBlocker : Ability
    {
        private static readonly FastList<Vector2d> _bufferCoordinates = new FastList<Vector2d>();
        private FastList<GridNode> _lastCoordinates = new FastList<GridNode>();
        private LSBody _cachedBody;
        private bool isTransparent = false;

        protected override void OnSetup()
        {
            _cachedBody = Agent.Body;
            UpdateCoordinates();
        }

        protected override void OnInitialize()
        {

        }

        protected override void OnLateSimulate()
        {
            if (!isTransparent && (_cachedBody.PositionChangedBuffer || _cachedBody.RotationChangedBuffer))
            {
                RemoveLastCoordinates();
                UpdateCoordinates();
            }
        }


        public void SetTransparent(bool setTransparency)
        {
            if (setTransparency)
            {
                RemoveLastCoordinates();
            }
            else
            {
                RemoveLastCoordinates();
                UpdateCoordinates();
            }

            isTransparent = setTransparency;
        }

        protected override void OnDeactivate()
        {
            RemoveLastCoordinates();
        }

        private void RemoveLastCoordinates()
        {
            for (int i = 0; i < _lastCoordinates.Count; i++)
            {
                GridNode node = _lastCoordinates[i];
                node.RemoveObstacle();

                var test = GridManager.Grid[node.gridIndex];
                continue;
            }

            _lastCoordinates.FastClear();
        }

        private void UpdateCoordinates()
        {
            const long gridSpacing = FixedMath.One;
            _bufferCoordinates.FastClear();
            _cachedBody.GetCoveredSnappedPositions(gridSpacing, _bufferCoordinates);
            foreach (Vector2d vec in _bufferCoordinates)
            {
                GridNode node = GridManager.GetNodeByPos(vec.x, vec.y);

                if (node.IsNull())
                {
                    continue;
                }

                node.AddObstacle();
                _lastCoordinates.Add(node);
            }
        }
    }
}