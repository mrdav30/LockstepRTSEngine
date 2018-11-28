using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

public class DynamicWalls : MonoBehaviour
{

    bool creating;
    public GameObject polePrefab;
    public GameObject wallPrefab;

    private GameObject startPole;
    private GameObject lastPole;

    private Stack<GameObject> wallSegments;

    private DragDirection _startDragDirection;
    private DragDirection _curDragDirection;


    // Use this for initialization
    void Start()
    {
        _startDragDirection = DragDirection.None;
        _curDragDirection = DragDirection.None;
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
        wallSegments.Push(startPole);
        lastPole = startPole;
    }

    void setWall()
    {
        creating = false;
        _startDragDirection = DragDirection.None;
        _curDragDirection = DragDirection.None;
    }

    void updateWall()
    {
        GetDragDirection();
        Vector3 currentPos = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        currentPos = Positioning.GetSnappedPosition(currentPos);
        if (_startDragDirection == DragDirection.Left || _startDragDirection == DragDirection.Right)
        {
            currentPos = new Vector3(startPole.transform.position.x, currentPos.y + 0.3f, currentPos.z);
        }
        else if (_startDragDirection == DragDirection.Up || _startDragDirection == DragDirection.Down)
        {
            currentPos = new Vector3(currentPos.x, currentPos.y + 0.3f, startPole.transform.position.z);
        }

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
        wallSegments.Push(newPole);
        Vector3 middle = Vector3.Lerp(newPole.transform.position, lastPole.transform.position, .05f);
        GameObject newWall = Instantiate(wallPrefab, middle, Quaternion.identity);
        newWall.transform.LookAt(lastPole.transform);
        wallSegments.Push(newWall);

        lastPole = newPole;
    }

    private void GetDragDirection()//Vector3 dragVector
    {
        if (Input.GetAxis("Mouse X") < 0)
        {
            //Code for action on mouse moving left
            _curDragDirection = DragDirection.Left;
        }
        else if (Input.GetAxis("Mouse X") > 0)
        {
            //Code for action on mouse moving right
            _curDragDirection = DragDirection.Right;
        }
        else if (Input.GetAxis("Mouse Y") < 0)
        {
            //Code for action on mouse moving left
            _curDragDirection = DragDirection.Down;
        }
        else if (Input.GetAxis("Mouse Y") > 0)
        {
            //Code for action on mouse moving right
            _curDragDirection = DragDirection.Up;
        }

        if (_startDragDirection == DragDirection.None)
        {
            _startDragDirection = _curDragDirection;
        }

        Debug.Log("start direction " + _startDragDirection);
        Debug.Log("cur direction " + _curDragDirection);
    }

    private enum DragDirection
    {
        Up,
        Down,
        Right,
        Left,
        None
    }
}
