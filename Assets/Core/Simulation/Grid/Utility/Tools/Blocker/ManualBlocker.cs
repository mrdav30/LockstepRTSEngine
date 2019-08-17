using UnityEngine;

namespace RTSLockstep
{
    //Blocker for static environment pieces in a scene.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityLSBody))]
    public class ManualBlocker : EnvironmentObject
    {
        [SerializeField]
        private Area[] _blockAreas;

        protected override void OnLateInitialize()
        {
            for (int i = 0; i < _blockAreas.Length; i++)
            {
                var block = _blockAreas[i];
                BlockArea(block);
            }
        }

        public static void BlockArea(Area block)
        {
            long xMin = block.XMin;
            long xMax = block.XMax;
            long yMin = block.YMin;
            long yMax = block.YMax;

            for (long x = xMin; x <= xMax; x += FixedMath.One)
            {
                for (long y = yMin; y <= yMax; y += FixedMath.One)
                {
                    var node = GridManager.GetNode(x, y);
                    if (node.IsNotNull())
                    {
                        node.AddObstacle();
                    }
                }
            }
        }
    }
}