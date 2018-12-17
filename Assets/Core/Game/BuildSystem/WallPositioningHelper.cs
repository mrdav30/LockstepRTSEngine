using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Attaches to wall base prefab 
 */
public class WallPositioningHelper : MonoBehaviour
{
    public GameObject pillarPrefab;
    public GameObject wallPrefab;
    // distance between pillar to trigger spawning next pillar
    public int PillarOffset;  // = 10
    // distance between last pillar to trigger spawning next wall segment
    public int WallSegmentOffset; // = 1
    private const int pillarRangeOffset = 3;

    private Vector3 _currentPos;
    private bool _isPlacingWall;

    private GameObject startPillar;
    private GameObject lastPillar;

    private List<GameObject> _pillarPrefabs;
    private Dictionary<int, GameObject> _wallPrefabs;
    private bool _startSnapped;
    private bool _endSnapped;

    private int lastWallLength;

    public void Setup()
    {
        _startSnapped = false;
        _endSnapped = false;
        _isPlacingWall = false;
        _pillarPrefabs = new List<GameObject>();
        _wallPrefabs = new Dictionary<int, GameObject>();
        lastWallLength = 0;
    }

    public void Visualize()
    {
        _currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);

        if (!_isPlacingWall)
        {
            GameObject closestPillar = ConstructionHandler.ClosestStructureTo(_currentPos, pillarRangeOffset, "WallPillar");
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
            ConstructionHandler.GetTempStructure().transform.position = Positioning.GetSnappedPosition(_currentPos);

            UpdateWall();
        }
    }

    public void OnLeftClick()
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

    public void OnRightClick()
    {
        if (_isPlacingWall)
        {
            ClearTemporaryWalls();
        }
    }

    private void CreateStartPillar()
    {
        Vector3 startPos = ConstructionHandler.GetTempStructure().transform.position;

        // create initial pole
        if (!_startSnapped)
        {
            startPillar = Instantiate(pillarPrefab, startPos, Quaternion.identity) as GameObject;
            startPillar.transform.parent = ConstructionHandler.OrganizerStructures;
        }
        else
        {
            GameObject closestPillar = ConstructionHandler.ClosestStructureTo(startPos, pillarRangeOffset, "WallPillar");
            startPillar = Instantiate(closestPillar);
        }

        startPillar.gameObject.name = pillarPrefab.gameObject.name;
        _pillarPrefabs.Add(startPillar);

        lastPillar = startPillar;
        lastWallLength = 0;
        _isPlacingWall = true;
    }

    private void SetWall()
    {
        //check if last pole was ever set
        if (!_endSnapped
            && _pillarPrefabs.Count == _wallPrefabs.Count)
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

        ClearTemporaryWalls();

        ConstructionHandler.ProcessConstructionQueue();
    }

    private void UpdateWall()
    {
        if (_pillarPrefabs.Count > 0)
        {
            lastPillar = _pillarPrefabs[_pillarPrefabs.Count - 1];
        }
        else
        {
            lastPillar = startPillar;
        }

        if (lastPillar.IsNotNull())
        {
            GameObject closestPillar = ConstructionHandler.ClosestStructureTo(_currentPos, pillarRangeOffset, "WallPillar");

            if (closestPillar.IsNotNull())
            {
                if (!closestPillar.transform.position.Equals(lastPillar.transform.position))
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
            int startToLastDistance = (int)Math.Round(Vector3.Distance(startPillar.transform.position, lastPillar.transform.position));
            //distance from current position to last instantiated pillar
            int currentToLastDistance = (int)Math.Ceiling(Vector3.Distance(_currentPos, lastPillar.transform.position));
            //distance from start pillar to current position
            int currentWallLength = (int)Math.Round(Vector3.Distance(startPillar.transform.position, _currentPos));

            if (currentWallLength != lastWallLength)
            {
                if (currentWallLength > lastWallLength
                    && currentWallLength > startToLastDistance)
                {
                    //check if end pole is far enough from start pillar
                    if (currentToLastDistance >= PillarOffset)
                    {
                        CreateWallPillar(_currentPos);
                    }

                    if (currentToLastDistance >= WallSegmentOffset)
                    {
                        CreateWallSegment(_currentPos);
                    }

                    lastWallLength = currentWallLength;
                }
                else if (currentWallLength < lastWallLength)
                {
                    // remove segments if length is shorter than last check
                    // and mouse is within distance of last instantiated pillar
                    if (currentToLastDistance <= 1 || currentWallLength < startToLastDistance)
                    {
                        RemoveLastWallSegment();
                    }

                    lastWallLength = currentWallLength;
                }
            }

            AdjustWallSegments();
        }
    }

    private void CreateWallPillar(Vector3 _currentPos)
    {
        GameObject newPillar = Instantiate(pillarPrefab, _currentPos, Quaternion.identity);
        newPillar.gameObject.name = pillarPrefab.gameObject.name;
        newPillar.transform.LookAt(lastPillar.transform);
        _pillarPrefabs.Add(newPillar);
        newPillar.transform.parent = ConstructionHandler.OrganizerStructures;
    }

    private void CreateWallSegment(Vector3 _currentPos)
    {
        int ndx = _pillarPrefabs.IndexOf(lastPillar);
        //only create wall segment if dictionary doesn't contain pillar index
        if (!_wallPrefabs.ContainsKey(ndx))
        {
            Vector3 middle = 0.5f * (_currentPos + lastPillar.transform.position);

            GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
            newWall.gameObject.name = wallPrefab.gameObject.name;
            newWall.SetActive(true);
            _wallPrefabs.Add(ndx, newWall);
            newWall.transform.parent = ConstructionHandler.OrganizerStructures;
        }
    }

    private void RemoveLastWallSegment()
    {
        if (_pillarPrefabs.Count > 0)
        {
            int ndx = _pillarPrefabs.Count - 1;
            //don't remove first pillar!
            if (ndx > 0)
            {
                Destroy(_pillarPrefabs[ndx].gameObject);
                _pillarPrefabs.RemoveAt(ndx);
                if (_wallPrefabs.Count > 0)
                {
                    GameObject wallSegement;
                    if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                    {
                        Destroy(wallSegement.gameObject);
                        _wallPrefabs.Remove(ndx);
                    }
                }
            }
        }
    }

    private void AdjustWallSegments()
    {
        startPillar.transform.LookAt(ConstructionHandler.GetTempStructure().transform);
        ConstructionHandler.GetTempStructure().transform.LookAt(startPillar.transform.position);

        if (_pillarPrefabs.Count > 0)
        {
            GameObject adjustBasePole = startPillar;
            for (int i = 0; i < _pillarPrefabs.Count; i++)
            {
                // no need to adjust start pillar
                if (i > 0)
                {
                    Vector3 newPos = adjustBasePole.transform.position + startPillar.transform.TransformDirection(new Vector3(0, 0, PillarOffset));
                    _pillarPrefabs[i].transform.position = newPos;
                    _pillarPrefabs[i].transform.rotation = startPillar.transform.rotation;
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

    public void SetTransparentMaterial(Material material)
    {
        if (_pillarPrefabs.Count > 0)
        {
            List<Renderer> renderers = new List<Renderer>();

            for (int i = 0; i < _pillarPrefabs.Count; i++)
            {
                if (!(_startSnapped && i == 0))
                {
                    renderers.Add(_pillarPrefabs[i].GetComponent<Renderer>());

                    if (_wallPrefabs.Count > 0)
                    {
                        int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
                        GameObject wallSegement;
                        if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                        {
                            renderers.Add(wallSegement.GetComponent<Renderer>());
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

    private void ClearTemporaryWalls()
    {
        _startSnapped = false;
        _endSnapped = false;
        _isPlacingWall = false;



        for (int i = 0; i < _pillarPrefabs.Count; i++)
        {
            Destroy(_pillarPrefabs[i].gameObject);
            if (_wallPrefabs.Count > 0)
            {
                int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
                GameObject wallSegement;
                if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
                {
                    Destroy(wallSegement.gameObject);
                }
            }
        }
        _pillarPrefabs.Clear();
        _wallPrefabs.Clear();
    }
}
