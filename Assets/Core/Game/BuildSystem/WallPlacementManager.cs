using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

public class WallPlacementManager : MonoBehaviour
{

    bool creating;
    public GameObject polePrefab;
    public GameObject wallPrefab;

    private GameObject startPole;
    private GameObject lastPole;

    // Use this for initialization
    void Start()
    {
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
        startPole = Instantiate(polePrefab, startPos, Quaternion.identity) as GameObject;
        startPole.transform.position = new Vector3(startPos.x, startPos.y + 0.3f, startPos.z);
        lastPole = startPole;
    }

    void setWall()
    {
        creating = false;
    }

    void updateWall()
    {
        Vector3 currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        currentPos = Positioning.GetSnappedPosition(currentPos);

        if (!currentPos.Equals(lastPole.transform.position))
        {
            float distance = Vector3.Distance(currentPos, lastPole.transform.position);
            // only if distance is equal to half of wall + pole zeta...
            if (distance >= 5)
            {
                createWallSegment(currentPos);
            }
        }
    }

    void createWallSegment(Vector3 currentPos)
    {
        GameObject newPole = Instantiate(polePrefab, currentPos, Quaternion.identity);
        Vector3 middle = Vector3.Lerp(newPole.transform.position, lastPole.transform.position, .05f);
        GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
        newWall.transform.LookAt(lastPole.transform);

        lastPole = newPole;
    }
}
