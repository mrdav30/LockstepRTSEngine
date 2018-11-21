using UnityEngine;
using System.Collections;

public class LineGenerator : MonoBehaviour
{
    public GameObject emptyGO;

    private GameObject instantiatedGO;
    private GameObject currentSnap;
    private bool phase1;
    private Vector3 lineWantedPosition;

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit1;

            if (Physics.Raycast(ray1, out hit1, Mathf.Infinity))
            {
                Debug.Log(hit1.collider.gameObject.name);
               if (hit1.collider.gameObject.CompareTag("Snap") &&  hit1.collider.gameObject == this.gameObject)
                {
                    Debug.Log("Phase1");
                    currentSnap = hit1.collider.gameObject;
                    phase1 = true;
                    instantiatedGO = (GameObject)Instantiate(emptyGO, currentSnap.transform.position, Quaternion.identity);
                    instantiatedGO.transform.parent = currentSnap.transform.parent;
                }
            }

        }

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("LinerGenerator Button up!");
            Ray phaseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit phaseHit;

            phase1 = false;
            instantiatedGO = null;
            currentSnap = null;

            if (Physics.Raycast(phaseRay, out phaseHit, Mathf.Infinity))
            {
                if (phaseHit.collider.gameObject == currentSnap || phaseHit.collider.gameObject.tag != "Snap")
                {
                    Debug.Log("Phase2");
                    Destroy(instantiatedGO);
                }

            }
        }

        if (phase1 == true)
        {
            MouseRayGenerator();
            LinesSpawn();
        }
    }

    void MouseRayGenerator()
    {
        if (phase1 == true)
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit mouseHit;

            if (Physics.Raycast(mouseRay, out mouseHit, Mathf.Infinity))
            {
                lineWantedPosition = new Vector3(mouseHit.point.x, mouseHit.point.y, mouseHit.point.z);
            }
        }
    }

    void LinesSpawn()
    {
        if (phase1 == true)
        {
            LineRenderer line;
            line = instantiatedGO.GetComponent<LineRenderer>();
            line.enabled = true;

            Ray lineRay = new Ray(instantiatedGO.transform.position, lineWantedPosition);

            line.SetPosition(0, lineRay.origin);
            line.SetPosition(1, lineWantedPosition);
        }
    }
}