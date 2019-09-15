using RTSLockstep.Grid;
using UnityEngine;

namespace RTSLockstep.Pathfinding
{
    /// <summary>
    /// Pathfinding queries require 2 valid nodes. When one is not valid, this is used to find the best nearest node to path to instead.
    /// </summary>
    public class AlternativeNodeFinder
    {
        public static AlternativeNodeFinder Instance = new AlternativeNodeFinder();
        private int XGrid, YGrid, MaxTestDistance;
        private GridNode closestNode;
        private bool castNodeFound;
        private Vector2d WorldPos;
        private Vector2d OffsettedPos;

        private int dirX, dirY;
        private int layer;

        private long closestDistance;

        public bool CheckValidNeighbor(GridNode node)
        {
            for (int i = 0; i < 8; i++)
            {
                var temp = node.NeighborNodes[i];
                if (temp.IsNotNull() && temp.Unwalkable == false)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetValues(Vector2d worldPos, int xGrid, int yGrid, int maxTestDistance)
        {
            XGrid = xGrid;
            YGrid = yGrid;
            MaxTestDistance = maxTestDistance;
            WorldPos = worldPos;
            OffsettedPos = GridManager.GetOffsettedPos(worldPos);
            closestNode = null;
            castNodeFound = false;
            layer = 1;
        }

        public GridNode GetNode()
        {
            //Calculated closest side to raycast in first
            long xDif = OffsettedPos.x - XGrid;
            xDif = xDif.ClampOne();
            long yDif = OffsettedPos.y - YGrid;
            yDif = yDif.ClampOne();
            long nodeHalfWidth = FixedMath.One / 2;
            //Check to see if we should raycast towards corner first
            if ((xDif.Abs() >= nodeHalfWidth / 2)
                && (yDif.Abs() >= nodeHalfWidth / 2))
            {
                dirX = FixedMath.RoundToInt(xDif);
                dirY = FixedMath.RoundToInt(yDif);
            }
            else
            {
                if (xDif.Abs() < yDif.Abs())
                {
                    dirX = 0;
                    dirY = yDif.RoundToInt();
                }
                else
                {
                    dirX = xDif.RoundToInt();
                    dirY = 0;
                }
            }

            int layerStartX = dirX,
                layerStartY = dirY;
            int iterations = 0; // <- this is for debugging

            for (layer = 1; layer <= this.MaxTestDistance;)
            {
                GridNode checkNode = GridManager.GetNode(XGrid + dirX, YGrid + dirY);
                if (checkNode != null)
                {
                    this.CheckPathNode(checkNode);
                    if (this.castNodeFound)
                    {
                        return this.closestNode;
                    }
                }
                AdvanceRotation();
                //If we make a full loop
                if (layerStartX == dirX && layerStartY == dirY)
                {
                    layer++;
                    //Advance a layer instead of rotation
                    if (dirX > 0) dirX = layer;
                    else if (dirX < 0) dirX = -layer;
                    if (dirY > 0) dirY = layer;
                    else if (dirY < 0) dirY = -layer;
                    layerStartX = dirX;
                    layerStartY = dirY;
                }
                iterations++;
                if (iterations > 500)
                {
                    Debug.Log("too many");
                    break;
                }
            }

            //If the cast node is found or the side has been checked, do not raycast on that side

            if (!castNodeFound)
                return null;
            return closestNode;
        }
        //Advances the rotation clockwise
        private void AdvanceRotation()
        {
            //sides
            if (dirX == 0)
            {
                //up
                if (dirY == 1)
                {
                    dirX = layer;
                }
                //down
                else
                {
                    dirX = -layer;
                }
            }
            else if (dirY == 0)
            {
                //right
                if (dirX == 1)
                {
                    dirY = -layer;
                }
                //left
                else
                {
                    dirY = layer;
                }
            }
            //corners
            else if (dirX > 0)
            {
                //top-right
                if (dirY > 0)
                {
                    dirY = 0;
                }
                //bot-right
                else
                {
                    dirX = 0;
                }
            }
            else
            {
                //top-left
                if (dirY > 0)
                {
                    dirX = 0;
                }
                else
                {
                    dirY = 0;
                }
            }
        }

        private void CheckPathNode(GridNode node)
        {
            if (node != null && node.Unwalkable == false)
            {
                long distance = node.WorldPos.FastDistance(this.WorldPos);
                if (closestNode == null || distance < closestDistance)
                {
                    closestNode = node;
                    closestDistance = distance;
                    castNodeFound = true;
                }
                else
                {
                    castNodeFound = false;
                }
            }
        }
    }
}
