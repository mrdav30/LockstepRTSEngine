using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WallPositioningHelper : MonoBehaviour
{
    public GameObject pillarPrefab;
    public GameObject wallPrefab;
    // distance between poles to trigger spawning next segement
    public int poleOffset;  // = 10

    private Vector3 currentPos;
    private bool _isPlacingWall;

    private GameObject startPillar;
    private GameObject lastPillar;
    private Vector3 endPillarPos;

    private List<GameObject> pillarPrefabs;
    private Dictionary<int, GameObject> wallPrefabs;
    public Transform OrganizerWallSegments { get; private set; }

 //   private List<GameObject> wallSegments;
    private int lastEndToStartDistance;

    public void Setup()
    {
        OrganizerWallSegments = LSUtility.CreateEmpty().transform;
        OrganizerWallSegments.gameObject.name = "OrganizerWallSegments";

        pillarPrefabs = new List<GameObject>();
        wallPrefabs = new Dictionary<int, GameObject>();
        //wallSegments = new List<GameObject>();
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

    private void StartWall()
    {
        Vector3 startPos = ConstructionHandler.GetTempStructure().transform.position;
 
        // create initial pole
        startPillar = Instantiate(pillarPrefab, startPos, Quaternion.identity) as GameObject;
        startPillar.transform.parent = OrganizerWallSegments;

        lastPillar = startPillar;
        lastEndToStartDistance = 0;
        _isPlacingWall = true;
    }

    public GameObject ClosestPillarTo(Vector3 worldPoint, float distance)
    {
        GameObject closest = null;
        float currentDistance = Mathf.Infinity;
        string tag = "WallPillar";
        foreach(Transform child in OrganizerWallSegments)
        {
            if (child.gameObject.tag == tag)
            {
                currentDistance = Vector3.Distance(worldPoint, child.gameObject.transform.position);
                if (currentDistance < distance)
                {
                    closest = child.gameObject;
                }
            }
        }
        return closest;
    }

    private void SetWall()
    {
        //foreach (GameObject ws in wallSegments)
        //{
        //    ConstructionHandler.SetBuildQueue(ws);
        //}

        //wallSegments.Clear();
        _isPlacingWall = false;

        pillarPrefabs.Clear();
        wallPrefabs.Clear();
    }

    private void UpdateWall()
    {
        currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);

        if (pillarPrefabs.Count > 0)
        {
            lastPillar = pillarPrefabs[pillarPrefabs.Count - 1];
        }

        if (!currentPos.Equals(lastPillar.transform.position))
        {
            GameObject clostestPole = ClosestPillarTo(currentPos, 1);
            if (clostestPole
                && clostestPole.transform.position != lastPillar.transform.position)
            {
                Debug.Log("end snap!");
                ConstructionHandler.GetTempStructure().transform.position = clostestPole.transform.position;
            }

            endPillarPos = ConstructionHandler.GetTempStructure().transform.position;


            int endToStartDistance = (int)Math.Round(Vector3.Distance(startPillar.transform.position, endPillarPos));
            int lastToPosDistance = (int)Math.Round(Vector3.Distance(currentPos, lastPillar.transform.position));
            int endToLastDistance = (int)Math.Round(Vector3.Distance(endPillarPos, lastPillar.transform.position));
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
                if (endToLastDistance <= 1)
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
        GameObject newPillar = Instantiate(pillarPrefab, currentPos, Quaternion.identity);
        pillarPrefabs.Add(newPillar);
        newPillar.transform.parent = OrganizerWallSegments;

        Vector3 middle = 0.5f * (newPillar.transform.position + lastPillar.transform.position);

        GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
        newWall.SetActive(true);
        int ndx = pillarPrefabs.IndexOf(newPillar);
        wallPrefabs.Add(ndx, newWall);
        newWall.transform.parent = OrganizerWallSegments;
    }

    private void RemoveLastWallSegment()
    {
        if (pillarPrefabs.Count > 0)
        {
            int ndx = pillarPrefabs.Count - 1;
            Destroy(pillarPrefabs[ndx].gameObject);
            pillarPrefabs.RemoveAt(ndx);
            GameObject wallSegement = wallPrefabs[ndx];
            if (wallSegement)
            {
                Destroy(wallSegement.gameObject);
                wallPrefabs.Remove(ndx);
            }
        }

        if (pillarPrefabs.Count == 0)
        {
            lastPillar = startPillar;
        }
    }

    private void AdjustWallSegments()
    {
        startPillar.transform.LookAt(endPillarPos);
        ConstructionHandler.GetTempStructure().transform.LookAt(startPillar.transform.position);

        if (pillarPrefabs.Count > 0)
        {
            GameObject adjustBasePole = startPillar;
            foreach (GameObject p in pillarPrefabs)
            {

                Vector3 newPos = adjustBasePole.transform.position + startPillar.transform.TransformDirection(new Vector3(0, 0, poleOffset));
                p.transform.position = newPos;
                p.transform.rotation = startPillar.transform.rotation;

                float distance = Vector3.Distance(adjustBasePole.transform.position, p.transform.position);

                int ndx = pillarPrefabs.IndexOf(p);
                GameObject wallSegement = wallPrefabs[ndx];
                if (wallSegement)
                {
                    Vector3 middle = 0.5f * (p.transform.position + adjustBasePole.transform.position);
                    wallSegement.transform.position = middle;
                    wallSegement.transform.rotation = p.transform.rotation;
                    //  wallSegement.transform.localScale = new Vector3(wallSegement.transform.localScale.x, wallSegement.transform.localScale.y, distance);
                }
                adjustBasePole = p;
            }
        }
    }

    private void ClearTemporaryWalls()
    {
        _isPlacingWall = false;
        pillarPrefabs.Clear();
        wallPrefabs.Clear();
    }

    private void OnDestroy()
    {
        if (OrganizerWallSegments.IsNotNull()
            && OrganizerWallSegments.childCount > 0)
        {
            Destroy(OrganizerWallSegments.gameObject);
        }
    }
}
