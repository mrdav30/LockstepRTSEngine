using UnityEngine;

using RTSLockstep.BuildSystem.BuildGrid;
using RTSLockstep.Integration.CustomComparion;
using RTSLockstep.Simulation.Grid;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.Integration
{
    public class GridDebugger : MonoBehaviour
    {
        //Show the grid debugging?
        public bool ShowGrid;
        //type of grid to show... can be changed in runtime. Possibilities: Construct grid, LOS grid
        public GridType LeGridType;
        [DrawIf("LeGridType", ComparisonType.Equals, GridType.Pathfinding)]
        public bool ShowClearanceDegree;
        //Height of the grid
        //Size of each shown grid node
        [Range(1, 25)]
        public float LeHeight;
        [Range(.1f, .9f)]
        public float NodeSize = .4f;

        public enum GridType
        {
            Building,
            Pathfinding
        }

        private Vector3 nodeScale;

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && ShowGrid)
            {
                nodeScale = new Vector3(NodeSize, NodeSize, NodeSize);
                //Switch for which grid to show
                switch (this.LeGridType)
                {
                    case GridType.Pathfinding:
                        DrawPathfinding();
                        break;
                    case GridType.Building:
                        if (BuildGridAPI.MainBuildGrid.IsNotNull())
                        {
                            DrawBuildingSites();
                        }

                        break;
                }
            }
        }

        private void DrawBuildingSites()
        {
            int length = BuildGridAPI.MainBuildGrid.GridLength;
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    BuildGridNode node = BuildGridAPI.MainBuildGrid.Grid[i, j];
                    if (node.Occupied)
                    {
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                    }
                    Gizmos.DrawCube(BuildGridAPI.ToWorldPos(new Coordinate(i, j)).ToVector3(LeHeight), nodeScale);
                }
            }
        }

        private void DrawPathfinding()
        {
            for (int i = 0; i < GridManager.GridSize; i++)
            {
                //Gets every pathfinding node and shows the draws a cube for the node
                GridNode node = GridManager.Grid[i];
                //Color depends on whether or not the node is walkable
                //Red = Unwalkable, Green = Walkable
                if (node.Unwalkable)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.green; //I'm part colorblind... grey doesn't work very well with red
                }

                Gizmos.DrawCube(node.WorldPos.ToVector3(LeHeight), nodeScale);

                if (ShowClearanceDegree)
                {
                    if (node.ClearanceDegree != GridNode.DefaultDegree)
                    {
                        UnityEditor.Handles.color = Color.red;
                        UnityEditor.Handles.Label(node.WorldPos.ToVector3(LeHeight), "d" + node.ClearanceDegree.ToString());
                    }
                }
            }
        }
    }
}
