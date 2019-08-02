using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Attaches to wall base prefab 
 */

public static class WallPositioningHelper
{
    /// <summary>
    /// Empty wall prefabs to assist in wall building.
    /// </summary>
    /// <value>Empty game objects that contain the meshes for a wall pillar and segment.</value>
    public static GameObject EmptyWallPillarGO;
    public static GameObject EmptyWallSegmentGO;
    // distance between pillar to trigger spawning next pillar
    private const int _pillarOffset = 10;
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

    private static int _lastWallLength = 0;

    public static Transform OrganizerWalls { get; private set; }

    public static void Initialize()
    {
        OrganizerWalls = LSUtility.CreateEmpty().transform;
        OrganizerWalls.transform.parent = ConstructionHandler.OrganizerStructures;
        OrganizerWalls.gameObject.name = "OrganizerWalls";
    }

    public static void Setup()
    {
        EmptyWallPillarGO = ConstructionHandler.GetTempStructure();
        EmptyWallSegmentGO = EmptyWallPillarGO.GetComponent<Structure>().WallSegmentGO;
    }

    public static void Visualize()
    {
        _currentPos = ConstructionHandler.GetTempStructure().transform.position;

        if (!_isPlacingWall)
        {
            GameObject closestPillar = ClosestPillar(_currentPos, _pillarRangeOffset);
            if (closestPillar.IsNotNull())
            {
                ConstructionHandler.GetTempStructure().transform.position = closestPillar.transform.position;
                ConstructionHandler.GetTempStructure().transform.rotation = closestPillar.transform.rotation;
                _startSnapped = true;
            }
            else
            {
                _startSnapped = false;
                ConstructionHandler.GetTempStructure().transform.position = Positioning.GetSnappedPosition(_currentPos);
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
            SetWall();
        }
        else
        {
            CreateStartPillar();
        }
    }

    private static void CreateStartPillar()
    {
        Vector3 startPos = ConstructionHandler.GetTempStructure().transform.position;

        // create initial pole
        if (!_startSnapped)
        {
            _startPillar = UnityEngine.Object.Instantiate(EmptyWallPillarGO, startPos, Quaternion.identity) as GameObject;
            _startPillar.transform.parent = OrganizerWalls;
        }
        else
        {
            GameObject closestPillar = ClosestPillar(startPos, _pillarRangeOffset);
            _startPillar = UnityEngine.Object.Instantiate(closestPillar);
        }

        _startPillar.AddComponent<TempStructure>();

        _startPillar.gameObject.name = EmptyWallPillarGO.gameObject.name;
        _pillarPrefabs.Add(_startPillar);

        _lastPillar = _startPillar;
        _lastWallLength = 0;
        _isPlacingWall = true;
    }

    private static void SetWall()
    {
        //check if last pole was ever set
        if (!_endSnapped && _pillarPrefabs.Count == _wallPrefabs.Count)
        {
            Vector3 endPos = ConstructionHandler.GetTempStructure().transform.position;
            CreateWallPillar(endPos);
        }

        for (int i = 0; i < _pillarPrefabs.Count; i++)
        {
            // ignore first entry if start pillar was snapped, don't want to construct twice!
            if (!(_startSnapped && i == 0))
            {
                ConstructionHandler.SetConstructionQueue(_pillarPrefabs[i]);
            }

            int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
            GameObject wallSegement;
            if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
            {
                ConstructionHandler.SetConstructionQueue(wallSegement);
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
                    ConstructionHandler.GetTempStructure().transform.position = closestPillar.transform.position;
                    ConstructionHandler.GetTempStructure().transform.rotation = closestPillar.transform.rotation;
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
                if (currentWallLength > _lastWallLength
                    && currentWallLength > startToLastDistance)
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

    private static void CreateWallPillar(Vector3 _currentPos)
    {
        GameObject newPillar = UnityEngine.Object.Instantiate(EmptyWallPillarGO, _currentPos, Quaternion.identity);
        newPillar.AddComponent<TempStructure>();
        newPillar.gameObject.name = EmptyWallPillarGO.gameObject.name;
        newPillar.transform.LookAt(_lastPillar.transform);
        _pillarPrefabs.Add(newPillar);
        newPillar.transform.parent = OrganizerWalls;
    }

    private static void CreateWallSegment(Vector3 _currentPos)
    {
        int ndx = _pillarPrefabs.IndexOf(_lastPillar);
        //only create wall segment if dictionary doesn't contain pillar index
        if (!_wallPrefabs.ContainsKey(ndx))
        {
            Vector3 middle = 0.5f * (_currentPos + _lastPillar.transform.position);

            GameObject newWall = UnityEngine.Object.Instantiate(EmptyWallSegmentGO, middle, Quaternion.identity);
            newWall.AddComponent<TempStructure>();
            newWall.gameObject.name = EmptyWallSegmentGO.gameObject.name;
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
        _startPillar.transform.LookAt(ConstructionHandler.GetTempStructure().transform);
        ConstructionHandler.GetTempStructure().transform.LookAt(_startPillar.transform.position);

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
                            nextPillar = ConstructionHandler.GetTempStructure();
                        }

                        float distance = Vector3.Distance(_pillarPrefabs[i].transform.position, nextPillar.transform.position);
                        wallSegement.transform.localScale = new Vector3(wallSegement.transform.localScale.x, wallSegement.transform.localScale.y, distance);
                        wallSegement.transform.rotation = adjustBasePole.transform.rotation;

                        Vector3 middle = 0.5f * (_pillarPrefabs[i].transform.position + nextPillar.transform.position);
                        wallSegement.transform.position = middle;
                    }
                }

                adjustBasePole = _pillarPrefabs[i];
            }
        }
    }

    public static void SetTransparentMaterial(Material material)
    {
        if (_pillarPrefabs.Count > 0)
        {
            List<Renderer> renderers = new List<Renderer>();

            for (int i = 0; i < _pillarPrefabs.Count; i++)
            {
                if (!(_startSnapped && i == 0))
                {
                    renderers.Add(_pillarPrefabs[i].GetComponentInChildren<Renderer>());

                    if (_wallPrefabs.Count > 0)
                    {
                        int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
                        GameObject wallSegement;
                        if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                        {
                            renderers.Add(wallSegement.GetComponentInChildren<Renderer>());
                        }
                    }

                    foreach (Renderer renderer in renderers)
                    {
                        renderer.material = material;
                    }
                }
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
            currentDistance = Vector3.Distance(worldPoint, child.GetComponent<UnityLSBody>().InternalBody._position.ToVector3());
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

        EmptyWallPillarGO = null;
        EmptyWallSegmentGO = null;

        _startSnapped = false;
        _endSnapped = false;
        _isPlacingWall = false;
        _pillarPrefabs = new List<GameObject>();
        _wallPrefabs = new Dictionary<int, GameObject>();
        _lastWallLength = 0;
    }
}
