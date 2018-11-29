using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WallPlacementManager : MonoBehaviour
{

    bool creating;
    public GameObject polePrefab;
    public GameObject wallPrefab;
    // distance between poles to trigger spawning next segement
    public int poleOffset;  // = 10
    // offet added to y axis of instantiated objects
    public float yOffSet; // = 0.3f

    private GameObject startPole;
    private GameObject lastPole;
    private GameObject endPole;

    private int lastEndToStartDistance;

    private bool xSnapping;
    private bool zSnapping;
    private bool poleSnapping;

    private List<GameObject> polePrefabs;
    private Dictionary<GameObject, GameObject> wallPrefabs;

    public static Transform OrganizerWallSegments;

    // Use this for initialization
    void Start()
    {
        OrganizerWallSegments = LSUtility.CreateEmpty().transform;
        OrganizerWallSegments.gameObject.name = "OrganizerWallSegments";

        polePrefabs = new List<GameObject>();
        wallPrefabs = new Dictionary<GameObject, GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        getInput();
    }

    void getInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startWall();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (creating)
            {
                setWall();
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
                updateWall();
            }
        }
    }

    Vector3 GridSnap(Vector3 originalPosition)
    {
        int granularity = 1;
        Vector3 snappedPosition = new Vector3(Mathf.Floor(originalPosition.x / granularity) * granularity, originalPosition.y, Mathf.Floor(originalPosition.z / granularity) * granularity);
        return snappedPosition;
    }

    void startWall()
    {
        creating = true;
        Vector3 startPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        startPos = GridSnap(startPos);

        // create initial pole
        startPole = Instantiate(polePrefab, startPos, Quaternion.identity) as GameObject;
        startPole.transform.parent = OrganizerWallSegments;
        if (poleSnapping)
        {
            startPole.transform.position = ClostestPoleTo(startPos).transform.position;
        }
        else
        {
            startPole.transform.position = new Vector3(startPos.x, startPos.y + yOffSet, startPos.z);
        }

        // create placement pole
        endPole = Instantiate(polePrefab, startPos, Quaternion.identity) as GameObject;
        endPole.transform.parent = OrganizerWallSegments;
        endPole.transform.position = new Vector3(startPos.x, startPos.y + yOffSet, startPos.z);

        lastPole = startPole;
        lastEndToStartDistance = 0;
    }

    private GameObject ClostestPoleTo(Vector3 worldPoint)
    {
        GameObject closest = null;
        float distance = Mathf.Infinity;
        float currentDistance = Mathf.Infinity;
        string tag = "PolePrefab";
        foreach (GameObject p in polePrefabs)
        {
            if (p.tag == tag)
            {
                currentDistance = Vector3.Distance(worldPoint, p.transform.position);
                if (currentDistance < distance)
                {
                    distance = currentDistance;
                    closest = p;
                }
            }
        }
        return closest;
    }

    void setWall()
    {
        creating = false;
        Vector3 endPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        endPos = GridSnap(endPos);

        if (xSnapping)
        {
            endPole.transform.position = new Vector3(startPole.transform.position.x, endPole.transform.position.y, endPole.transform.position.z);
        }
        else if (zSnapping)
        {
            endPole.transform.position = new Vector3(startPole.transform.position.x, endPole.transform.position.y, endPole.transform.position.z);
        }
        else
        {
            endPole.transform.position = new Vector3(endPos.x, endPos.y + yOffSet, endPos.z);
        }

        polePrefabs.Clear();
        wallPrefabs.Clear();
    }

    void updateWall()
    {
        Vector3 currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        currentPos = GridSnap(currentPos);
        currentPos = new Vector3(currentPos.x, currentPos.y + yOffSet, currentPos.z);

        if (xSnapping)
        {
            endPole.transform.position = new Vector3(startPole.transform.position.x, endPole.transform.position.y, endPole.transform.position.z);
        }
        else if (zSnapping)
        {
            endPole.transform.position = new Vector3(endPole.transform.position.x, endPole.transform.position.y, startPole.transform.position.z);
        }
        else
        {
            endPole.transform.position = currentPos;
        }

        if (polePrefabs.Count > 0)
        {
            lastPole = polePrefabs[polePrefabs.Count -1];
        }

        if (!currentPos.Equals(lastPole.transform.position))
        {
            int endToStartDistance = (int)Math.Round(Vector3.Distance(startPole.transform.position, endPole.transform.position));
            int lastToPosDistance = (int)Math.Round(Vector3.Distance(currentPos, lastPole.transform.position));
            int endToLastDistance = (int)Math.Round(Vector3.Distance(endPole.transform.position, lastPole.transform.position));
            // ensure end pole is far enough from start pole
            if (endToStartDistance >= lastEndToStartDistance)
            {
                // ensure last instantiated pole is far enough from current pos
                // and end pole is far enough from last pole
                if (lastToPosDistance >= poleOffset
                && endToLastDistance >= poleOffset)
                {
                    lastEndToStartDistance = endToStartDistance;
                    Debug.Log("endToStartDistance " + endToStartDistance);
                    Debug.Log("lastEndToStartDistance " + lastEndToStartDistance);
                    createWallSegment(currentPos);
                }
            }
        }

        adjustWallSegments();
    }

    void createWallSegment(Vector3 currentPos)
    {
        GameObject newPole = Instantiate(polePrefab, currentPos, Quaternion.identity);
        polePrefabs.Add(newPole);
        newPole.transform.parent = OrganizerWallSegments;

        Vector3 middle = Vector3.Lerp(newPole.transform.position, lastPole.transform.position, .05f);

        GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
        wallPrefabs.Add(newPole, newWall);
        newWall.transform.parent = OrganizerWallSegments;
    }

    void adjustWallSegments()
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

                GameObject wallSegement = wallPrefabs[p];
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
