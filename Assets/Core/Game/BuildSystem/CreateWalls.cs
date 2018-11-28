using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CreateWalls : MonoBehaviour
{

    bool creating;
    public GameObject start;
    public GameObject end;

    public GameObject wallPrefab;
    GameObject wall;

    bool xSnapping;
    bool zSnapping;

    bool poleSnapping;

    List<GameObject> poles;

    // Use this for initialization
    void Start()
    {
        poles = new List<GameObject>();
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
            setStart();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            setEnd();
        }
        else
        {
            if (creating)
            {
                adjust();
            }
        }

        if (Input.GetKey(KeyCode.X))
        {
            zSnapping = true;
        }
        else
        {
            zSnapping = false;
        }

        if (Input.GetKey(KeyCode.Y))
        {
            xSnapping = true;
        }
        else
        {
            xSnapping = false;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            poleSnapping = !poleSnapping;
            if(GameObject.FindGameObjectsWithTag("Pole").Length == 0)
            {
                poleSnapping = false;
                Debug.Log("set some walls first!");
            }
        }
    }

    Vector3 gridSnap(Vector3 originalPosition)
    {
        int granularity = 1;
        Vector3 snappedPosition = new Vector3(Mathf.Floor(originalPosition.x / granularity) * granularity, originalPosition.y, Mathf.Floor(originalPosition.z / granularity) * granularity);
        return snappedPosition;
    }


    void setStart()
    {
        creating = true;
        start.transform.position = gridSnap(getWorldPoint());
        wall = (GameObject)Instantiate(wallPrefab, start.transform.position, Quaternion.identity);

        if (poleSnapping)
        {
            start.transform.position = clostestPoleTo(getWorldPoint()).transform.position;
        }
    }

    GameObject clostestPoleTo(Vector3 worldPoint)
    {
        GameObject closest = null;
        float distance = Mathf.Infinity;
        float currentDistance = Mathf.Infinity;
        foreach(GameObject p in poles)
        {
            currentDistance = Vector3.Distance(worldPoint, p.transform.position);
            if(currentDistance < distance)
            {
                distance = currentDistance;
                closest = p;
            }
        }
        return closest;
    }

    void setEnd()
    {
        creating = false;
        end.transform.position = gridSnap(getWorldPoint());
        if (xSnapping)
        {
            end.transform.position = new Vector3(start.transform.position.x, end.transform.position.y, end.transform.position.z);
        }
        if (zSnapping)
        {
            end.transform.position = new Vector3(start.transform.position.x, end.transform.position.y, end.transform.position.z);
        }
        SetEndPoles();
    }

    void SetEndPoles()
    {
        GameObject p1 = Instantiate(wallPrefab, start.transform.position, start.transform.rotation) as GameObject;
        GameObject p2 = Instantiate(wallPrefab, end.transform.position, end.transform.rotation) as GameObject;
        p1.tag = "Pole";
        p2.tag = "Pole";
        poles.Add(p1);
        poles.Add(p2);
    }

    void adjust()
    {
        end.transform.position = gridSnap(getWorldPoint());
        if (xSnapping)
        {
            end.transform.position = new Vector3(start.transform.position.x, end.transform.position.y, end.transform.position.z);
        }
        if (zSnapping)
        {
            end.transform.position = new Vector3(end.transform.position.x, end.transform.position.y, start.transform.position.z);
        }
        adjustWall();

    }

    void adjustWall()
    {
        start.transform.LookAt(end.transform.position);
        end.transform.LookAt(start.transform.position);
        float distance = Vector3.Distance(start.transform.position, end.transform.position);
        wall.transform.position = start.transform.position + distance / 2 * start.transform.forward;
        wall.transform.rotation = start.transform.rotation;
        wall.transform.localScale = new Vector3(wall.transform.localScale.x, wall.transform.localScale.y, distance);
    }

    Vector3 getWorldPoint()
    {
        Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }
}
