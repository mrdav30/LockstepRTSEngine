using RTSLockstep;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallPlacementManager : MonoBehaviour
{

    public GameObject polePrefab;
    public GameObject wallPrefab;
    // distance between poles to trigger spawning next segement
    public int poleOffset;  // = 5
    // offet added to y axis of instantiated objects
    public float yOffSet; // = 0.3f

    private bool creating;
    private bool poleSnapping;

    private GameObject startPole;
    private GameObject lastPole;
    private GameObject endPole;

    private static Transform OrganizerWallSegments;
    private List<GameObject> snapPoles;
    private List<GameObject> polePrefabs;
    private Dictionary<int, GameObject> wallPrefabs;

    private int lastEndToStartDistance;

    //private bool xSnapping;
    //private bool zSnapping;

    // Use this for initialization
    // private void Start()
    public void Setup()
    {
        OrganizerWallSegments = LSUtility.CreateEmpty().transform;
        OrganizerWallSegments.gameObject.name = "OrganizerWallSegments";

        polePrefabs = new List<GameObject>();
        wallPrefabs = new Dictionary<int, GameObject>();
    }

    // Update is called once per frame
    private void Visualize()
    {
        GetInput();
    }

    private void GetInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartWall();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (creating)
            {
                SetWall();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            ClearTemporaryWalls();
        }
        else
        {
            if (creating)
            {
                UpdateWall();
            }
        }
    }

    private Vector3 GridSnap(Vector3 originalPosition)
    {
        int granularity = 1;
        Vector3 snappedPosition = new Vector3(Mathf.Floor(originalPosition.x / granularity) * granularity, originalPosition.y, Mathf.Floor(originalPosition.z / granularity) * granularity);
        return snappedPosition;
    }

    private void StartWall()
    {
        creating = true;
        Vector3 startPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        startPos = GridSnap(startPos);

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
        creating = false;
        Vector3 endPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        endPos = GridSnap(endPos);

        //if (xSnapping)
        //{
        //    endPole.transform.position = new Vector3(startPole.transform.position.x, endPole.transform.position.y, endPole.transform.position.z);
        //}
        //else if (zSnapping)
        //{
        //    endPole.transform.position = new Vector3(startPole.transform.position.x, endPole.transform.position.y, endPole.transform.position.z);
        //}
        //else
        //{
            endPole.transform.position = new Vector3(endPos.x, endPos.y + yOffSet, endPos.z);
        //}

        polePrefabs.Clear();
        wallPrefabs.Clear();
    }

    private void UpdateWall()
    {
        Vector3 currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        currentPos = GridSnap(currentPos);
        currentPos = new Vector3(currentPos.x, currentPos.y + yOffSet, currentPos.z);

        //if (xSnapping)
        //{
        //    endPole.transform.position = new Vector3(startPole.transform.position.x, endPole.transform.position.y, endPole.transform.position.z);
        //}
        //else if (zSnapping)
        //{
        //    endPole.transform.position = new Vector3(endPole.transform.position.x, endPole.transform.position.y, startPole.transform.position.z);
        //}
        //else
        //{
            endPole.transform.position = currentPos;
        //}

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
        int ndx = polePrefabs.IndexOf(newPole);
        wallPrefabs.Add(ndx, newWall);
        newWall.transform.parent = OrganizerWallSegments;
    }

    private void RemoveLastWallSegment()
    {
        if(polePrefabs.Count > 0)
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

        if(polePrefabs.Count == 0)
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
        creating = false;
        polePrefabs.Clear();
        wallPrefabs.Clear();
        foreach (Transform child in OrganizerWallSegments)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}
