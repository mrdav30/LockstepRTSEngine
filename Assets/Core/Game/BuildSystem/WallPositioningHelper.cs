using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WallPositioningHelper : MonoBehaviour
{
    public GameObject pillarPrefab;
    public GameObject wallPrefab;
    // distance between poles to trigger spawning next segement
    public int PoleOffset;  // = 10
    private const int pillarRangeOffset = 3;

    private Vector3 _currentPos;
    private bool _isPlacingWall;

    private GameObject startPillar;
    private GameObject lastPillar;
    private Vector3 endPillarPos;

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
    }

    public void Visualize(Vector3 pos)
    {
        if (!_isPlacingWall)
        {
            GameObject closestPillar = ConstructionHandler.ClosestStructureTo(pos, pillarRangeOffset, "WallPillar");
            if (closestPillar.IsNotNull())
            {
                ConstructionHandler.GetTempStructure().transform.position = closestPillar.transform.position;
                ConstructionHandler.GetTempStructure().transform.rotation = closestPillar.transform.rotation;
                _startSnapped = true;
            }
            else
            {
                _startSnapped = false;
                ConstructionHandler.GetTempStructure().transform.position = Positioning.GetSnappedPosition(pos);
            }
        }
        else
        {
            ConstructionHandler.GetTempStructure().transform.position = Positioning.GetSnappedPosition(pos);

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
            startPillar = ConstructionHandler.ClosestStructureTo(startPos, pillarRangeOffset, "WallPillar");
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
                ConstructionHandler.SetBuildQueue(_pillarPrefabs[i]);
            }

            int ndx = _pillarPrefabs.IndexOf(_pillarPrefabs[i]);
            GameObject wallSegement;
            if (_wallPrefabs.TryGetValue(ndx, out wallSegement))
            {
                ConstructionHandler.SetBuildQueue(wallSegement);
            }
        }

        ClearTemporaryWalls();

        ConstructionHandler.ProcessBuildQueue();
    }

    private void UpdateWall()
    {
        _currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);

        if (_pillarPrefabs.Count > 0)
        {
            lastPillar = _pillarPrefabs[_pillarPrefabs.Count - 1];
        }

        if (lastPillar.IsNotNull())
        {
            if (!_currentPos.Equals(lastPillar.transform.position))
            {
                GameObject closestPillar = ConstructionHandler.ClosestStructureTo(_currentPos, pillarRangeOffset, "WallPillar");

                if (closestPillar.IsNotNull())
                {
                    if (closestPillar.transform.position != lastPillar.transform.position)
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

                endPillarPos = ConstructionHandler.GetTempStructure().transform.position;

                int wallLength = (int)Math.Round(Vector3.Distance(startPillar.transform.position, endPillarPos));
                int lastToPosDistance = (int)Math.Round(Vector3.Distance(_currentPos, lastPillar.transform.position));
                int endToLastDistance = (int)Math.Round(Vector3.Distance(endPillarPos, lastPillar.transform.position));
                // ensure end pole is far enough from start pole
                if (wallLength > lastWallLength)
                {
                    // ensure last instantiated pole is far enough from current pos
                    // and end pole is far enough from last pole
                    if (endToLastDistance >= PoleOffset)
                    {
                        CreateWallPillar(_currentPos);
                    }
                    else if (lastToPosDistance >= 1)
                    {
                        CreateWallSegment(_currentPos);
                    }
                }
                else if (wallLength < lastWallLength)
                {
                    if (lastToPosDistance <= 1)
                    {
                        RemoveLastWallSegment();
                    }
                }

                lastWallLength = wallLength;
            }
        }

        AdjustWallSegments();
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
            Vector3 middle = 0.5f * (endPillarPos + lastPillar.transform.position);

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

        if (_pillarPrefabs.Count == 0)
        {
            lastPillar = startPillar;
        }
    }

    private void AdjustWallSegments()
    {
        startPillar.transform.LookAt(endPillarPos);
        ConstructionHandler.GetTempStructure().transform.LookAt(startPillar.transform.position);

        if (_pillarPrefabs.Count > 0)
        {
            GameObject adjustBasePole = startPillar;
            for (int i = 0; i < _pillarPrefabs.Count; i++)
            {
                // no need to adjust start pillar
                if (i > 0)
                {
                    Vector3 newPos = adjustBasePole.transform.position + startPillar.transform.TransformDirection(new Vector3(0, 0, PoleOffset));
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

    private void ClearTemporaryWalls()
    {
        _startSnapped = false;
        _endSnapped = false;
        _isPlacingWall = false;
        _pillarPrefabs.Clear();
        _wallPrefabs.Clear();
    }
}
