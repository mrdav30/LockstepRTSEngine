using RTSLockstep.Abilities.Essential;
using RTSLockstep.BuildSystem.BuildGrid;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Attaches to wall base prefab 
 */
namespace RTSLockstep.BuildSystem
{
    public static class WallPositioningHelper
    {
        /// <summary>
        /// Empty wall prefabs to assist in wall building.
        /// </summary>
        /// <value>Empty game objects that contain the meshes for a wall pillar and segment.</value>
        private static GameObject tempWallPillarGO;
        private static GameObject tempWallSegmentGO;
        private static LSBody tempWallSegmentBody;
        // distance between pillar to trigger spawning next pillar
        // set to z (for length) of the object's localscale
        private static int _pillarOffset;
        private static int _rotationOrigin;
        // distance between last pillar to trigger spawning next wall segment
        private const int _wallSegmentOffset = 1;
        private const int _pillarRangeOffset = 3;

        private static Vector3 _currentPos;
        private static bool _isPlacingWall = false;

        private static GameObject _startPillar;
        private static GameObject _lastPillar;

        private static List<GameObject> _pillarPrefabs = new List<GameObject>();
        private static Dictionary<int, GameObject> _wallPrefabs = new Dictionary<int, GameObject>();
        private static bool _startSnapped = false;
        private static bool _endSnapped = false;

        private static long _originalWallLength;
        private static long _lastWallLength = 0;

        public static Transform OrganizerWalls { get; private set; }

        public static void Initialize()
        {
            OrganizerWalls = LSUtility.CreateEmpty().transform;
            OrganizerWalls.transform.parent = ConstructionHandler.OrganizerStructures;
            OrganizerWalls.gameObject.name = "OrganizerWalls";
        }

        public static void Setup()
        {
            tempWallPillarGO = ConstructionHandler.GetTempStructureGO();
            tempWallSegmentGO = tempWallPillarGO.GetComponent<Structure>().WallSegmentGO;

            if (tempWallSegmentGO)
            {
                tempWallSegmentBody = tempWallSegmentGO.GetComponent<UnityLSBody>().InternalBody;
            }

            _pillarOffset = (int)Math.Round(tempWallSegmentGO.transform.localScale.z);
        }

        public static void Visualize()
        {
            _currentPos = tempWallPillarGO.transform.position;

            if (!_isPlacingWall)
            {
                GameObject closestPillar = ClosestPillar(_currentPos, _pillarRangeOffset);
                if (closestPillar.IsNotNull())
                {
                    tempWallPillarGO.transform.position = closestPillar.transform.position;
                    tempWallPillarGO.transform.rotation = closestPillar.transform.rotation;
                    _startSnapped = true;
                }
                else
                {
                    _startSnapped = false;
                    tempWallPillarGO.transform.position = StructurePositionHelper.GetSnappedPosition(_currentPos);
                }
            }
            else
            {
                UpdateWall();
            }
        }

        public static void OnLeftClick()
        {
            if (_isPlacingWall)
            {
                SetWallConstructionQueue();
            }
            else
            {
                CreateStartPillar();
            }
        }

        private static void CreateStartPillar()
        {
            Vector3 startPos = tempWallPillarGO.transform.position;

            // create initial pole
            if (!_startSnapped)
            {
                _startPillar = UnityEngine.Object.Instantiate(tempWallPillarGO, startPos, Quaternion.identity) as GameObject;
                _startPillar.transform.parent = OrganizerWalls;
            }
            else
            {
                GameObject closestPillar = ClosestPillar(startPos, _pillarRangeOffset);
                _startPillar = UnityEngine.Object.Instantiate(closestPillar);
            }

            _startPillar.gameObject.name = tempWallPillarGO.gameObject.name;
            _pillarPrefabs.Add(_startPillar);

            _lastPillar = _startPillar;
            _lastWallLength = 0;
            _isPlacingWall = true;
        }

        private static void SetWallConstructionQueue()
        {
            // Add the last pole to the construction queue
            if (!_endSnapped && _pillarPrefabs.Count == _wallPrefabs.Count)
            {
                //Perform one final adjustment
                //         AdjustWallSegments();
                Vector3 endPos = tempWallPillarGO.transform.position;
                CreateWallPillar(endPos, true);
            }

            for (int i = 0; i < _pillarPrefabs.Count; i++)
            {
                // ignore first entry if start pillar was snapped, don't want to construct twice!
                if (!(_startSnapped && i == 0) && _pillarPrefabs[i].GetComponent<Structure>().ValidPlacement)
                {
                    ConstructionHandler.SetConstructionQueue(_pillarPrefabs[i]);
                }

                if (_wallPrefabs.Count >= 0)
                {
                    GameObject wallSegement;
                    if (_wallPrefabs.TryGetValue(i, out wallSegement) && wallSegement.GetComponent<Structure>().ValidPlacement)
                    {
                        long adjustHalfWidth = 0;
                        long adjustHalfLength = 0;

                        long currentHalfWidth = tempWallSegmentBody.HalfWidth;
                        long currentHalfLength = tempWallSegmentBody.HalfLength;

                        if (_originalWallLength != wallSegement.transform.localScale.z)
                        {
                            currentHalfLength = (long)wallSegement.transform.localScale.z * FixedMath.Half;
                        }

                        adjustHalfWidth = currentHalfWidth;
                        adjustHalfLength = currentHalfLength;
                        float currentRotation = wallSegement.transform.localEulerAngles.y;

                        // if segment has rotated past 45 degree angle, need to rotate length & width
                        if (currentRotation >= 45 && currentRotation <= 135
                            || currentRotation >= 225 && currentRotation <= 315)
                        {
                            adjustHalfWidth = currentHalfLength;
                            adjustHalfLength = currentHalfWidth;
                        }

                        ConstructionHandler.SetConstructionQueue(wallSegement, adjustHalfWidth, adjustHalfLength);
                    }
                }
            }

            ConstructionHandler.SendConstructCommand();
        }

        private static void UpdateWall()
        {
            if (_pillarPrefabs.Count > 0)
            {
                _lastPillar = _pillarPrefabs[_pillarPrefabs.Count - 1];
            }
            else
            {
                _lastPillar = _startPillar;
            }

            if (_lastPillar.IsNotNull())
            {
                GameObject closestPillar = ClosestPillar(_currentPos, _pillarRangeOffset);

                if (closestPillar.IsNotNull())
                {
                    if (!closestPillar.transform.position.Equals(_lastPillar.transform.position))
                    {
                        tempWallPillarGO.transform.position = closestPillar.transform.position;
                        tempWallPillarGO.transform.rotation = closestPillar.transform.rotation;
                        _endSnapped = true;
                    }
                }
                else
                {
                    _endSnapped = false;
                }

                //distance from start pillar to last instantiated pillar
                int startToLastDistance = (int)Math.Round(Vector3.Distance(_startPillar.transform.position, _lastPillar.transform.position));
                //distance from current position to last instantiated pillar
                int currentToLastDistance = (int)Math.Ceiling(Vector3.Distance(_currentPos, _lastPillar.transform.position));
                //distance from start pillar to current position
                int currentWallLength = (int)Math.Round(Vector3.Distance(_startPillar.transform.position, _currentPos));

                if (currentWallLength != _lastWallLength)
                {
                    if (currentWallLength > _lastWallLength && currentWallLength > startToLastDistance)
                    {
                        //check if end pole is far enough from start pillar
                        if (currentToLastDistance >= _pillarOffset)
                        {
                            CreateWallPillar(_currentPos);
                        }

                        if (currentToLastDistance >= _wallSegmentOffset)
                        {
                            CreateWallSegment(_currentPos);
                        }

                        _lastWallLength = currentWallLength;
                    }
                    else if (currentWallLength < _lastWallLength)
                    {
                        // remove segments if length is shorter than last check
                        // and mouse is within distance of last instantiated pillar
                        if (currentToLastDistance <= 1 || currentWallLength < startToLastDistance)
                        {
                            RemoveLastWallSegment();
                        }

                        _lastWallLength = currentWallLength;
                    }
                }

                AdjustWallSegments();
            }
        }

        private static void CreateWallPillar(Vector3 _currentPos, bool isLast = false)
        {
            GameObject newPillar = UnityEngine.Object.Instantiate(tempWallPillarGO, _currentPos, Quaternion.identity);
            newPillar.gameObject.name = tempWallPillarGO.gameObject.name;
            ConstructionHandler.SetTransparentMaterial(newPillar, GameResourceManager.AllowedMaterial);

            if (_lastPillar)
            {
                newPillar.transform.LookAt(_lastPillar.transform);
            }
            else
            {
                newPillar.transform.LookAt(_startPillar.transform);
            }
            _pillarPrefabs.Add(newPillar);
            newPillar.transform.parent = OrganizerWalls;

            if (isLast)
            {
                // we know the placement is valid, otherwise we wouldn't know 
                // this was the last pillar from left click
                newPillar.GetComponent<Structure>().ValidPlacement = true;
                _lastPillar = newPillar;
            }
        }

        private static void CreateWallSegment(Vector3 _currentPos)
        {
            int ndx = _pillarPrefabs.IndexOf(_lastPillar);
            //only create wall segment if dictionary doesn't contain pillar index
            if (!_wallPrefabs.ContainsKey(ndx))
            {
                Vector3 middle = 0.5f * (_currentPos + _lastPillar.transform.position);

                GameObject newWall = UnityEngine.Object.Instantiate(tempWallSegmentGO, middle, Quaternion.identity);
                newWall.gameObject.name = tempWallSegmentGO.gameObject.name;
                ConstructionHandler.SetTransparentMaterial(newWall, GameResourceManager.AllowedMaterial);

                _originalWallLength = (long)newWall.transform.localScale.z;
                newWall.SetActive(true);
                _wallPrefabs.Add(ndx, newWall);
                newWall.transform.parent = OrganizerWalls;
            }
        }

        private static void RemoveLastWallSegment()
        {
            if (_pillarPrefabs.Count > 0)
            {
                int ndx = _pillarPrefabs.Count - 1;
                //don't remove first pillar!
                if (ndx > 0)
                {
                    UnityEngine.Object.Destroy(_pillarPrefabs[ndx].gameObject);
                    _pillarPrefabs.RemoveAt(ndx);
                    if (_wallPrefabs.Count > 0)
                    {
                        GameObject wallSegement;
                        if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                        {
                            UnityEngine.Object.Destroy(wallSegement.gameObject);
                            _wallPrefabs.Remove(ndx);
                        }
                    }
                }
            }
        }

        private static void AdjustWallSegments()
        {
            _startPillar.transform.LookAt(tempWallPillarGO.transform);
            tempWallPillarGO.transform.LookAt(_startPillar.transform.position);

            if (_pillarPrefabs.Count > 0)
            {
                GameObject adjustBasePole = _startPillar;
                for (int i = 0; i < _pillarPrefabs.Count; i++)
                {
                    // no need to adjust start pillar
                    if (i > 0)
                    {
                        Vector3 newPos = adjustBasePole.transform.position + _startPillar.transform.TransformDirection(new Vector3(0, 0, _pillarOffset));
                        _pillarPrefabs[i].transform.position = newPos;
                        _pillarPrefabs[i].transform.rotation = _startPillar.transform.rotation;
                    }

                    Vector2d posPillar = new Vector2d(_pillarPrefabs[i].transform.position.x, _pillarPrefabs[i].transform.position.z);
                    Coordinate coorPillar = BuildGridAPI.ToGridPos(posPillar);
                    _pillarPrefabs[i].GetComponent<Structure>().GridPosition = coorPillar;
                    _pillarPrefabs[i].GetComponent<Structure>().ValidPlacement = BuildGridAPI.CanBuild(coorPillar, _pillarPrefabs[i].GetComponent<Structure>() as IBuildable);

                    if (_pillarPrefabs[i].GetComponent<Structure>().ValidPlacement)
                    {
                        ConstructionHandler.SetTransparentMaterial(_pillarPrefabs[i], GameResourceManager.AllowedMaterial);
                    }
                    else
                    {
                        ConstructionHandler.SetTransparentMaterial(_pillarPrefabs[i], GameResourceManager.NotAllowedMaterial);
                    }

                    if (_wallPrefabs.Count > 0)
                    {
                        int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
                        GameObject wallSegement;
                        if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                        {
                            GameObject nextPillar;
                            if (i + 1 < _pillarPrefabs.Count)
                            {
                                nextPillar = _pillarPrefabs[i + 1];
                            }
                            else
                            {
                                nextPillar = tempWallPillarGO;
                            }

                            int wallSegmentLength = (int)Math.Round(Vector3.Distance(_pillarPrefabs[i].transform.position, nextPillar.transform.position));
                            wallSegement.transform.localScale = new Vector3(wallSegement.transform.localScale.x, wallSegement.transform.localScale.y, wallSegmentLength);
                            wallSegement.transform.localEulerAngles = adjustBasePole.transform.localEulerAngles;

                            Vector3 middle = 0.5f * (_pillarPrefabs[i].transform.position + nextPillar.transform.position);
                            wallSegement.transform.position = middle;

                            Vector2d posSegment = new Vector2d(wallSegement.transform.position.x, wallSegement.transform.position.z);
                            Coordinate coorSegment = BuildGridAPI.ToGridPos(posSegment);
                            wallSegement.GetComponent<Structure>().GridPosition = coorSegment;
                            wallSegement.GetComponent<Structure>().ValidPlacement = BuildGridAPI.CanBuild(coorSegment, wallSegement.GetComponent<Structure>() as IBuildable);

                            if (wallSegement.GetComponent<Structure>().ValidPlacement)
                            {
                                ConstructionHandler.SetTransparentMaterial(wallSegement, GameResourceManager.AllowedMaterial);
                            }
                            else
                            {
                                ConstructionHandler.SetTransparentMaterial(wallSegement, GameResourceManager.NotAllowedMaterial);
                            }
                        }
                    }

                    adjustBasePole = _pillarPrefabs[i];
                }
            }
        }

        private static void ClearTemporaryWalls()
        {
            _startSnapped = false;
            _endSnapped = false;
            _isPlacingWall = false;

            for (int i = 0; i < _pillarPrefabs.Count; i++)
            {
                UnityEngine.Object.Destroy(_pillarPrefabs[i].gameObject);
                if (_wallPrefabs.Count > 0)
                {
                    int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
                    GameObject wallSegement;
                    if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                    {
                        UnityEngine.Object.Destroy(wallSegement.gameObject);
                    }
                }
            }
            _pillarPrefabs.Clear();
            _wallPrefabs.Clear();
        }

        public static GameObject ClosestPillar(Vector3 worldPoint, float distance)
        {
            GameObject closest = null;
            float currentDistance = Mathf.Infinity;
            foreach (Transform child in OrganizerWalls)
            {
                currentDistance = Vector3.Distance(worldPoint, child.GetComponent<UnityLSBody>().InternalBody.Position.ToVector3());
                if (currentDistance < distance)
                {
                    closest = child.gameObject;
                }
            }
            return closest;
        }

        public static void Reset()
        {
            ClearTemporaryWalls();

            tempWallPillarGO = null;
            tempWallSegmentGO = null;
            tempWallSegmentBody = null;

            _startSnapped = false;
            _endSnapped = false;
            _isPlacingWall = false;
            _pillarPrefabs = new List<GameObject>();
            _wallPrefabs = new Dictionary<int, GameObject>();
            _lastWallLength = 0;
        }
    }
}