using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WallPositioningHelper : MonoBehaviour
{
    public GameObject polePrefab;
    public GameObject wallPrefab;
    // distance between poles to trigger spawning next segement
    public int poleOffset;  // = 5
    // offet added to y axis of instantiated objects
    public float yOffSet; // = 0.3f

    private Vector3 currentPos;
    private bool _isPlacingWall;
    private bool poleSnapping;

    private GameObject startPole;
    private GameObject lastPole;
    private GameObject endPole;

    public Transform OrganizerWallSegments;
    private List<GameObject> snapPoles;
    private List<GameObject> polePrefabs;
    private Dictionary<int, GameObject> wallPrefabs;

    private int lastEndToStartDistance;

    public void Setup()
    {
        OrganizerWallSegments = LSUtility.CreateEmpty().transform;
        OrganizerWallSegments.gameObject.name = "OrganizerWallSegments";

        polePrefabs = new List<GameObject>();
        wallPrefabs = new Dictionary<int, GameObject>();
    }

    // Update is called once per frame
    public void Visualize(Vector3 pos)
    {
        currentPos = pos;
    }

    public void OnRightClick()
    {
        if (_isPlacingWall)
        {
            ClearTemporaryWalls();
        }
    }

    public void OnLeftClickUp()
    {
        if (_isPlacingWall)
        {
            SetWall();
        }
    }

    public void OnLeftClickDrag()
    {
        if (_isPlacingWall)
        {
            UpdateWall();
        }
        else
        {
            StartWall();
        }
    }

    public bool IsPlacingWall()
    {
        return this._isPlacingWall;
    }

    public GameObject GetEndPole()
    {
        return this.endPole;
    }

    private void StartWall()
    {
        Vector3 startPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        startPos = Positioning.GetSnappedPosition(startPos);

        // create initial pole
        startPole = Instantiate(polePrefab, startPos, Quaternion.identity) as GameObject;
        startPole.transform.parent = OrganizerWallSegments;
        //if (poleSnapping)
        //{
        //    startPole.transform.position = ClostestPoleTo(startPos).transform.position;
        //}
        //else
        //{
        startPole.transform.position = new Vector3(startPos.x, startPos.y + yOffSet, startPos.z);
        //}

        // create placement pole
        endPole = Instantiate(polePrefab, startPos, Quaternion.identity) as GameObject;
        endPole.transform.parent = OrganizerWallSegments;
        endPole.transform.position = new Vector3(startPos.x, startPos.y + yOffSet, startPos.z);

        lastPole = startPole;
        lastEndToStartDistance = 0;
        _isPlacingWall = true;
    }

    //private GameObject ClostestPoleTo(Vector3 worldPoint)
    //{
    //    GameObject closest = null;
    //    float distance = Mathf.Infinity;
    //    float currentDistance = Mathf.Infinity;
    //    string tag = "PolePrefab";
    //    foreach (GameObject p in polePrefabs)
    //    {
    //        if (p.tag == tag)
    //        {
    //            currentDistance = Vector3.Distance(worldPoint, p.transform.position);
    //            if (currentDistance < distance)
    //            {
    //                distance = currentDistance;
    //                closest = p;
    //            }
    //        }
    //    }
    //    return closest;
    //}

    private void SetWall()
    {
        _isPlacingWall = false;
        endPole.transform.position = new Vector3(currentPos.x, currentPos.y + yOffSet, currentPos.z);

        polePrefabs.Clear();
        wallPrefabs.Clear();
    }

    private void UpdateWall()
    {
        currentPos = new Vector3(currentPos.x, currentPos.y + yOffSet, currentPos.z);
        endPole.transform.position = currentPos;

        if (polePrefabs.Count > 0)
        {
            lastPole = polePrefabs[polePrefabs.Count - 1];
        }

        if (!currentPos.Equals(lastPole.transform.position))
        {
            int endToStartDistance = (int)Math.Round(Vector3.Distance(startPole.transform.position, endPole.transform.position));
            int lastToPosDistance = (int)Math.Round(Vector3.Distance(currentPos, lastPole.transform.position));
            int endToLastDistance = (int)Math.Round(Vector3.Distance(endPole.transform.position, lastPole.transform.position));
            // ensure end pole is far enough from start pole
            if (endToStartDistance > lastEndToStartDistance)
            {
                // ensure last instantiated pole is far enough from current pos
                // and end pole is far enough from last pole
                if (lastToPosDistance >= poleOffset
                    && endToLastDistance >= poleOffset)
                {
                    CreateWallSegment(currentPos);
                }
            }
            else if (endToStartDistance < lastEndToStartDistance)
            {
                if (lastToPosDistance < poleOffset
                    && endToLastDistance < poleOffset)
                {
                    RemoveLastWallSegment();
                }
            }
            lastEndToStartDistance = endToStartDistance;
        }

        AdjustWallSegments();
    }

    private void CreateWallSegment(Vector3 currentPos)
    {
        GameObject newPole = Instantiate(polePrefab, currentPos, Quaternion.identity);
        polePrefabs.Add(newPole);
        newPole.transform.parent = OrganizerWallSegments;

        Vector3 middle = Vector3.Lerp(newPole.transform.position, lastPole.transform.position, .05f);

        GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
        newWall.SetActive(true);
        int ndx = polePrefabs.IndexOf(newPole);
        wallPrefabs.Add(ndx, newWall);
        newWall.transform.parent = OrganizerWallSegments;
    }

    private void RemoveLastWallSegment()
    {
        if (polePrefabs.Count > 0)
        {
            int ndx = polePrefabs.Count - 1;
            Destroy(polePrefabs[ndx].gameObject);
            polePrefabs.RemoveAt(ndx);
            GameObject wallSegement = wallPrefabs[ndx];
            if (wallSegement)
            {
                Destroy(wallSegement.gameObject);
                wallPrefabs.Remove(ndx);
            }
        }

        if (polePrefabs.Count == 0)
        {
            lastPole = startPole;
        }
    }

    private void AdjustWallSegments()
    {
        startPole.transform.LookAt(endPole.transform.position);
        endPole.transform.LookAt(startPole.transform.position);

        if (polePrefabs.Count > 0)
        {
            GameObject adjustBasePole = startPole;
            foreach (GameObject p in polePrefabs)
            {

                Vector3 newPos = adjustBasePole.transform.position + startPole.transform.TransformDirection(new Vector3(0, 0, poleOffset));
                p.transform.position = newPos;
                p.transform.rotation = startPole.transform.rotation;

                int ndx = polePrefabs.IndexOf(p);
                GameObject wallSegement = wallPrefabs[ndx];
                if (wallSegement)
                {
                    Vector3 middle = Vector3.Lerp(p.transform.position, adjustBasePole.transform.position, .05f);
                    wallSegement.transform.position = middle;
                    wallSegement.transform.rotation = p.transform.rotation;
                }
                adjustBasePole = p;
            }
        }
    }

    private void ClearTemporaryWalls()
    {
        _isPlacingWall = false;
        polePrefabs.Clear();
        wallPrefabs.Clear();
    }

    private void OnDestroy()
    {
        if (OrganizerWallSegments.IsNotNull())
        {
            Destroy(OrganizerWallSegments.gameObject);
        }
    }
}
