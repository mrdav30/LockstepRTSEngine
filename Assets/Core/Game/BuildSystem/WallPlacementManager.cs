using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

public class WallPlacementManager : MonoBehaviour
{

    bool creating;
    public GameObject polePrefab;
    public GameObject wallPrefab;
    // distance between poles to trigger spawning next segement
    public int poleOffset = 5;
    // offet added to y axis of instantiated objects
    public float yOffSet = 0.3f;

    private GameObject startPole;
    private GameObject lastPole;
    private GameObject endPole;

    private bool xSnapping;
    private bool zSnapping;
    private bool poleSnapping;
    private Stack<GameObject> wallSegments;

    public static Transform OrganizerWallSegments;

    // Use this for initialization
    void Start()
    {
        OrganizerWallSegments = LSUtility.CreateEmpty().transform;
        OrganizerWallSegments.gameObject.name = "OrganizerWallSegments";

        wallSegments = new Stack<GameObject>();
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
            setWall();
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

    void startWall()
    {
        creating = true;
        Vector3 startPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        startPos = Positioning.GetSnappedPosition(startPos);

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
        // don't add to poles stack until set
        endPole = Instantiate(polePrefab, startPos, Quaternion.identity) as GameObject;
        endPole.transform.parent = OrganizerWallSegments;
        endPole.transform.position = new Vector3(startPos.x, startPos.y + yOffSet, startPos.z);

        lastPole = startPole;
    }

    private GameObject ClostestPoleTo(Vector3 worldPoint)
    {
        GameObject closest = null;
        float distance = Mathf.Infinity;
        float currentDistance = Mathf.Infinity;
        string tag = "PolePrefab";
        foreach (GameObject p in wallSegments)
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
        endPos = Positioning.GetSnappedPosition(endPos);

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

        wallSegments.Push(startPole);
        wallSegments.Push(endPole);
    }

    void updateWall()
    {
        Vector3 currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        currentPos = Positioning.GetSnappedPosition(currentPos);

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
            endPole.transform.position = new Vector3(currentPos.x, currentPos.y + yOffSet, currentPos.z);
        }


        if (!currentPos.Equals(lastPole.transform.position))
        {
            float endPoleToStartPole = Vector3.Distance(startPole.transform.position, endPole.transform.position);
           // float lastPoleDistance = Vector3.Distance(currentPos, lastPole.transform.position);
            float endPoleToLastPole = Vector3.Distance(lastPole.transform.position, endPole.transform.position);
            // only if distance is equal to half of wall + pole zeta...
            if (endPoleToStartPole >= poleOffset
            //    && lastPoleDistance == poleOffset
                && endPoleToLastPole >= poleOffset)
            {
                createWallSegment(currentPos);
            }
        }

        adjustWallSegments();
    }

    void createWallSegment(Vector3 currentPos)
    {
        //    Vector3 closestPolePosition = ClostestPoleTo(currentPos).transform.position;

        GameObject newPole = Instantiate(polePrefab, currentPos, Quaternion.identity);
        wallSegments.Push(newPole);
        newPole.transform.parent = OrganizerWallSegments;

        Vector3 middle = Vector3.Lerp(newPole.transform.position, lastPole.transform.position, .05f);

        GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
        wallSegments.Push(newWall);
        newWall.transform.parent = OrganizerWallSegments;
      //  newWall.transform.LookAt(lastPole.transform);

        lastPole = newPole;
    }

    void adjustWallSegments()
    {
        startPole.transform.LookAt(endPole.transform.position);
        endPole.transform.LookAt(startPole.transform.position);
        float distance = Vector3.Distance(startPole.transform.position, endPole.transform.position);

        Transform lastPoleChecked = startPole.transform;
        foreach (GameObject ws in wallSegments)
        {
            if (ws.tag == "PolePrefab")
            {
                ws.transform.position = startPole.transform.position + distance / 2 * lastPoleChecked.forward;
                lastPoleChecked = ws.transform;
            }
            else if (ws.tag == "WallPrefab")
            {
                Vector3 middle = Vector3.Lerp(ws.transform.position, lastPoleChecked.transform.position, .05f);
                ws.transform.position = middle;
            }
         //   wall.transform.position = start.transform.position + distance / 2 * start.transform.forward;
            ws.transform.rotation = startPole.transform.rotation;
            //   ws.transform.localScale = new Vector3(ws.transform.localScale.x, ws.transform.localScale.y, distance);
        }
    }

    private void ClearTemporaryWalls()
    {
        creating = false;
        wallSegments.Clear();
        foreach (Transform child in OrganizerWallSegments)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}
