using RTSLockstep.Simulation.Grid;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;
using UnityEngine;

namespace RTSLockstep.Simulation.Pathfinding
{
    /// <summary>
    /// Pathfinding queries require 2 valid nodes. When one is not valid, this is used to find the best nearest node to path to instead.
    /// </summary>
    public class AlternativeNodeFinder
    {
        public static AlternativeNodeFinder Instance = new AlternativeNodeFinder();
        private int _xGrid, _yGrid, _maxTestDistance;
        private GridNode _closestNode;
        private bool _castNodeFound;
        private Vector2d _worldPos;
        private Vector2d _offsettedPos;

        private int _dirX, _dirY;
        private int _layer;


        private long _closestDistance;

        public void SetValues(Vector2d worldPos, int xGrid, int yGrid, int maxTestDistance)
        {
            _xGrid = xGrid;
            _yGrid = yGrid;
            _maxTestDistance = maxTestDistance;
            _worldPos = worldPos;
            _offsettedPos = GridManager.GetOffsettedPos(worldPos);
            _closestNode = null;
            _castNodeFound = false;
            _layer = 1;
        }

        public bool CheckValidNeighbor(GridNode node)
        {
            for (int i = 0; i < 8; i++)
            {
                var temp = node.NeighborNodes[i];
                if (temp.IsNotNull() && !temp.Unwalkable)
                {
                    return true;
                }
            }

            return false;
        }

        public GridNode GetNode()
        {
            //Calculated closest side to raycast in first
            long xDif = _offsettedPos.x - _xGrid;
            xDif = xDif.ClampOne();
            long yDif = _offsettedPos.y - _yGrid;
            yDif = yDif.ClampOne();
            long nodeHalfWidth = FixedMath.One / 2;

            //Check to see if we should raycast towards corner first
            if ((xDif.Abs() >= nodeHalfWidth / 2)
                && (yDif.Abs() >= nodeHalfWidth / 2))
            {
                _dirX = FixedMath.RoundToInt(xDif);
                _dirY = FixedMath.RoundToInt(yDif);
            }
            else
            {
                if (xDif.Abs() < yDif.Abs())
                {
                    _dirX = 0;
                    _dirY = yDif.RoundToInt();
                }
                else
                {
                    _dirX = xDif.RoundToInt();
                    _dirY = 0;
                }
            }

            int layerStartX = _dirX,
                layerStartY = _dirY;
            int iterations = 0; // <- this is for debugging

            for (_layer = 1; _layer <= _maxTestDistance;)
            {
                GridNode checkNode = GridManager.GetNode(_xGrid + _dirX, _yGrid + _dirY);
                if (checkNode.IsNotNull())
                {
                    CheckPathNode(checkNode);
                    if (_castNodeFound)
                    {
                        return _closestNode;
                    }
                }

                AdvanceRotation();
                //If we make a full loop
                if (layerStartX == _dirX && layerStartY == _dirY)
                {
                    _layer++;
                    //Advance a layer instead of rotation
                    if (_dirX > 0)
                    {
                        _dirX = _layer;
                    }
                    else if (_dirX < 0)
                    {
                        _dirX = -_layer;
                    }

                    if (_dirY > 0)
                    {
                        _dirY = _layer;
                    }
                    else if (_dirY < 0)
                    {
                        _dirY = -_layer;
                    }

                    layerStartX = _dirX;
                    layerStartY = _dirY;
                }

                iterations++;
                if (iterations > 500)
                {
                    Debug.Log("too many");
                    break;
                }
            }

            //If the cast node is found or the side has been checked, do not raycast on that side
            if (!_castNodeFound)
            {
                return null;
            }
            else
            {
                return _closestNode;
            }
        }
        //Advances the rotation clockwise
        private void AdvanceRotation()
        {
            //sides
            if (_dirX == 0)
            {
                //up
                if (_dirY == 1)
                {
                    _dirX = _layer;
                }
                //down
                else
                {
                    _dirX = -_layer;
                }
            }
            else if (_dirY == 0)
            {
                //right
                if (_dirX == 1)
                {
                    _dirY = -_layer;
                }
                //left
                else
                {
                    _dirY = _layer;
                }
            }
            //corners
            else if (_dirX > 0)
            {
                //top-right
                if (_dirY > 0)
                {
                    _dirY = 0;
                }
                //bot-right
                else
                {
                    _dirX = 0;
                }
            }
            else
            {
                //top-left
                if (_dirY > 0)
                {
                    _dirX = 0;
                }
                else
                {
                    _dirY = 0;
                }
            }
        }

        private void CheckPathNode(GridNode node)
        {
            if (node.IsNotNull() && !node.Unwalkable)
            {
                long distance = node.WorldPos.FastDistance(_worldPos);
                if (_closestNode.IsNull() || distance < _closestDistance)
                {
                    _closestNode = node;
                    _closestDistance = distance;
                    _castNodeFound = true;
                }
                else
                {
                    _castNodeFound = false;
                }
            }
        }
    }
}
