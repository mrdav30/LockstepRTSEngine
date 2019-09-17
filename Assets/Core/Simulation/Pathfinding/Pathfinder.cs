//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
// @modified 2019 David Oravsky
//=======================================================================

using RTSLockstep.Grid;

namespace RTSLockstep.Pathfinding
{
    public static class Pathfinder
    {
        #region Path Variables
        private static GridNode currentNode;
        #endregion

        public static bool NeedsPath(GridNode startNode, GridNode endNode, int unitSize = 1)
        {
            int dx, dy, error, ystep, x, y, t;
            int x0, y0, x1, y1;
            int compare1, compare2;
            int retX, retY;
            bool steep;

            //Tests if there is a direct path. If there is, no need to run AStar.
            x0 = startNode.gridX;
            y0 = startNode.gridY;
            x1 = endNode.gridX;
            y1 = endNode.gridY;
            if (y1 > y0)
            {
                compare1 = y1 - y0;
            }
            else
            {
                compare1 = y0 - y1;
            }

            if (x1 > x0)
            {
                compare2 = x1 - x0;
            }
            else
            {
                compare2 = x0 - x1;
            }

            steep = compare1 > compare2;
            if (steep)
            {
                t = x0; // swap x0 and y0
                x0 = y0;
                y0 = t;
                t = x1; // swap x1 and y1
                x1 = y1;
                y1 = t;
            }
            if (x0 > x1)
            {
                t = x0; // swap x0 and x1
                x0 = x1;
                x1 = t;
                t = y0; // swap y0 and y1
                y0 = y1;
                y1 = t;
            }
            dx = x1 - x0;

            dy = (y1 - y0);
            if (dy < 0)
            {
                dy = -dy;
            }

            error = dx / 2;
            ystep = (y0 < y1) ? 1 : -1;
            y = y0;
            GridNode.PrepareUnpassableCheck(unitSize);

            for (x = x0; x <= x1; x++)
            {
                retX = (steep ? y : x);
                retY = (steep ? x : y);

                currentNode = GridManager.Grid[GridManager.GetGridIndex(retX, retY)];
                if (currentNode.IsNotNull() && currentNode.Unpassable())
                {
                    break;
                }
                else if (x == x1)
                {
                    return false;
                }

                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds closest next-best-node also when destination is off invalid
        /// </summary>
        /// <param name="from"></param>
        /// <param name="dest"></param>
        /// <param name="returnNode"></param>
        /// <returns></returns>
        public static bool GetEndNode(Vector2d from, Vector2d dest, out GridNode outputNode, bool allowUnwalkableEndNode = false)
        {
            outputNode = GridManager.GetNode(dest.x, dest.y);
            if (outputNode.IsNull())
            {
                //If null, it is off the grid. Raycast back onto grid for closest viable node to the destination.
                foreach (var coordinate in PanLineAlgorithm.FractionalLineAlgorithm.Trace(dest.x.ToDouble(), dest.y.ToDouble(), from.x.ToDouble(), from.y.ToDouble()))
                {
                    outputNode = GridManager.GetNode(FixedMath.Create(coordinate.X), FixedMath.Create(coordinate.Y));
                    if (outputNode.IsNotNull())
                    {
                        return true;
                    }
                }

                return false;
            }
            else if (outputNode.Unwalkable)
            {
                if (allowUnwalkableEndNode && AlternativeNodeFinder.Instance.CheckValidNeighbor(outputNode))
                {
                    return true;
                }

                return StarCast(dest, out outputNode);
            }

            return true;
        }

        /// <summary>
        /// Finds closest next-best-node
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="returnNode"></param>
        /// <returns></returns>
        public static bool GetStartNode(Vector2d dest, out GridNode returnNode)
        {
            returnNode = GridManager.GetNode(dest.x, dest.y);
            if (returnNode == null || (returnNode.Unwalkable))
            {
                return StarCast(dest, out returnNode);
            }
            return true;
        }

        public static bool StarCast(Vector2d dest, out GridNode returnNode)
        {
            int xGrid, yGrid;
            GridManager.GetCoordinates(dest.x, dest.y, out xGrid, out yGrid);
            const int maxTestDistance = 3;
            AlternativeNodeFinder.Instance.SetValues(dest, xGrid, yGrid, maxTestDistance);

            returnNode = AlternativeNodeFinder.Instance.GetNode();
            if (returnNode.IsNull())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool GetClosestViableNode(Vector2d from, Vector2d dest, int pathingSize, out GridNode returnNode)
        {
            returnNode = GridManager.GetNode(dest.x, dest.y);

            if (returnNode.Unwalkable)
            {
                bool valid = false;
                PanLineAlgorithm.FractionalLineAlgorithm.Coordinate cacheCoord = new PanLineAlgorithm.FractionalLineAlgorithm.Coordinate();
                bool validTriggered = false;
                pathingSize = (pathingSize + 1) / 2;
                int minSqrMag = pathingSize * pathingSize;
                minSqrMag *= 2;

                foreach (var coordinate in PanLineAlgorithm.FractionalLineAlgorithm.Trace(dest.x.ToDouble(), dest.y.ToDouble(), from.x.ToDouble(), from.y.ToDouble()))
                {
                    currentNode = GridManager.GetNode(FixedMath.Create(coordinate.X), FixedMath.Create(coordinate.Y));
                    if (!validTriggered)
                    {
                        if (currentNode.IsNotNull() && !currentNode.Unwalkable)
                        {
                            validTriggered = true;
                        }
                        else
                        {
                            cacheCoord = coordinate;
                        }
                    }
                    if (validTriggered)
                    {
                        if (currentNode.IsNotNull() || !currentNode.Unwalkable)
                        {
                            //calculate sqrMag to last invalid node
                            int testMag = coordinate.X - cacheCoord.X;
                            testMag *= testMag;
                            int buffer = coordinate.Y - cacheCoord.Y;
                            buffer *= buffer;
                            testMag += buffer;
                            if (testMag >= minSqrMag)
                            {
                                valid = true;
                                break;
                            }
                        }
                    }
                }

                if (!valid)
                {
                    return false;
                }
                else
                {
                    returnNode = currentNode;
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}