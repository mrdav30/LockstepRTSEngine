using RTSLockstep;
using RTSLockstep.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class ConstructionHandler
{
    #region Properties
    private static GameObject tempStructure;
    private static Vector3 lastLocation;
    private static RTSAgent tempConstructor;
    private static bool findingPlacement = false;
    private static bool _constructingWall = false;

    private static AgentCommander _cachedCommander;
    private static bool _validPlacement;
    private static Dictionary<string, Material> oldMaterials = new Dictionary<string, Material>();

    private static Queue<GameObject> _buildQueue = new Queue<GameObject>();
    public static Transform OrganizerStructures { get; private set; }
    #endregion

    public static void Initialize()
    {
        _cachedCommander = PlayerManager.MainController.Commander;
        GridBuilder.Initialize();

        OrganizerStructures = LSUtility.CreateEmpty().transform;
        OrganizerStructures.transform.parent = PlayerManager.MainController.Commander.GetComponentInChildren<RTSAgents>().transform;
        OrganizerStructures.gameObject.name = "OrganizerStructures";

        UserInputHelper.OnSingleLeftTapDown += HandleSingleLeftClick;
    }

    // Update is called once per frame
    public static void Visualize()
    {
        if (IsFindingBuildingLocation())
        {
            if (!_cachedCommander.CachedHud._mouseOverHud)
            {
                if (!GridBuilder.IsMovingBuilding)
                {
                    GridBuilder.StartMove(tempStructure.GetComponent<TempStructure>());
                }

                if (_constructingWall)
                {
                    tempStructure.GetComponent<WallPositioningHelper>().Visualize();
                }
                else
                {
                    FindStructureLocation();
                }

                Vector2d pos = new Vector2d(tempStructure.transform.position.x, tempStructure.transform.position.z);
                _validPlacement = GridBuilder.UpdateMove(pos);

                if (_validPlacement)
                {
                    SetTransparentMaterial(tempStructure, GameResourceManager.AllowedMaterial);
                }
                else
                {
                    SetTransparentMaterial(tempStructure, GameResourceManager.NotAllowedMaterial);
                }
            }
        }
    }

    private static void HandleSingleLeftClick()
    {
        if (IsFindingBuildingLocation())
        {
            if (_constructingWall)
            {
                tempStructure.GetComponent<WallPositioningHelper>().OnLeftClick();

            }
            else
            {
                ProcessConstructionQueue();
            }
        }
    }

    public static void CreateBuilding(RTSAgent constructingAgent, string buildingName)
    {
        if (!IsFindingBuildingLocation())
        {
            Vector2d buildPoint = new Vector2d(constructingAgent.transform.position.x, constructingAgent.transform.position.z + 10);
            if (_cachedCommander)
            {
                //cleanup later...
                CreateBuilding(buildingName, buildPoint, constructingAgent, constructingAgent.GetPlayerArea());
            }
        }
    }

    public static void CreateBuilding(string buildingName, Vector2d buildPoint, RTSAgent constructingAgent, Rect playingArea)
    {
        RTSAgent buildingTemplate = GameResourceManager.GetAgentTemplate(buildingName);

        if (buildingTemplate.MyAgentType == AgentType.Building
            && buildingTemplate.GetComponent<Structure>())
        {
            // check that the Player has the resources available before allowing them to create a new structure
            if (!_cachedCommander.CachedResourceManager.CheckResources(buildingTemplate))
            {
                Debug.Log("Not enough resources!");
            }
            else
            {
                tempStructure = UnityEngine.Object.Instantiate(buildingTemplate.GetComponent<Structure>().tempStructure, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

                if (tempStructure.GetComponent<TempStructure>())
                {
                    if (buildingTemplate.Tag == AgentTag.Wall)
                    {
                        _constructingWall = true;
                        tempStructure.GetComponent<WallPositioningHelper>().Setup();
                    }

                    //get size based on the mesh renderer attached to the empty GO
                    Vector3 objectSize = Vector3.Scale(tempStructure.GetComponent<TempStructure>().EmptyGO.transform.localScale, tempStructure.GetComponent<TempStructure>().EmptyGO.GetComponentInChildren<MeshRenderer>().bounds.size);

                    // retrieve build size from agent template
                    tempStructure.GetComponent<TempStructure>().BuildSizeLow = (int)Math.Ceiling(objectSize.x);
                    tempStructure.GetComponent<TempStructure>().BuildSizeHigh = (int)Math.Ceiling(objectSize.z);

                    tempConstructor = constructingAgent;

                    findingPlacement = true;
                    SetTransparentMaterial(tempStructure, GameResourceManager.AllowedMaterial);
                    tempStructure.gameObject.transform.position = Positioning.GetSnappedPosition(buildPoint.ToVector3());
                }
                else
                {
                    UnityEngine.Object.Destroy(tempStructure);
                }
            }
        }
    }

    public static bool IsFindingBuildingLocation()
    {
        return _constructingWall || findingPlacement;
    }

    public static GameObject GetTempStructure()
    {
        return tempStructure;
    }

    //this isn't updating LSBody rotation correctly...
    public static void HandleRotationTap(UserInputKeyMappings direction)
    {
        if (findingPlacement
            && !_constructingWall)
        {
            //  keep track of previous values to update agent's size
            int prevLow = tempStructure.GetComponent<TempStructure>().BuildSizeLow;
            int prevhigh = tempStructure.GetComponent<TempStructure>().BuildSizeHigh;
            switch (direction)
            {
                case UserInputKeyMappings.RotateLeftShortCut:
                    tempStructure.transform.Rotate(0, 90, 0);
                    tempStructure.GetComponent<TempStructure>().BuildSizeLow = prevhigh;
                    tempStructure.GetComponent<TempStructure>().BuildSizeHigh = prevLow;
                    break;
                case UserInputKeyMappings.RotateRightShortCut:
                    tempStructure.transform.Rotate(0, -90, 0);
                    tempStructure.GetComponent<TempStructure>().BuildSizeLow = prevhigh;
                    tempStructure.GetComponent<TempStructure>().BuildSizeHigh = prevLow;
                    break;
                default:
                    break;
            }
        }
    }

    public static void FindStructureLocation()
    {
        Vector3 newLocation = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        if (RTSInterfacing.HitPointIsGround(Input.mousePosition)
            && lastLocation != newLocation)
        {
            lastLocation = newLocation;

            tempStructure.transform.position = Positioning.GetSnappedPosition(newLocation);
        }
    }

    public static bool CanPlaceStructure()
    {
        PlacementResult canPlace = GridBuilder.EndMove();
        if (canPlace == PlacementResult.Placed)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void SetConstructionQueue(GameObject buildingProject)
    {
        _buildQueue.Enqueue(buildingProject);
    }

    public static void ProcessConstructionQueue()
    {
        if (CanPlaceStructure())
        {
            // remove temporary structure from grid
            GridBuilder.Unbuild(tempStructure.GetComponent<TempStructure>());

            //if we're not building a wall, add the tempstructure to the build queue
            if (!_constructingWall
                && _buildQueue.Count == 0)
            {
                tempStructure.gameObject.name = tempStructure.GetComponent<TempStructure>().EmptyGO.name;
                SetConstructionQueue(tempStructure);
            }

            findingPlacement = false;
            _constructingWall = false;

            //temp structure no longer required
            UnityEngine.Object.Destroy(tempStructure.gameObject);

            int ndx = 0;
            while (_buildQueue.Count > 0)
            {
                GameObject qStructure = _buildQueue.Dequeue();
                Vector2d buildPoint = new Vector2d(qStructure.transform.localPosition.x, qStructure.transform.localPosition.z);
                Vector2d rotationPoint = new Vector2d(qStructure.transform.localRotation.w, qStructure.transform.localRotation.y);

                RTSAgent newBuilding = _cachedCommander.GetController().CreateAgent(qStructure.gameObject.name, buildPoint, rotationPoint) as RTSAgent;
                newBuilding.gameObject.name = qStructure.gameObject.name;
                newBuilding.transform.parent = OrganizerStructures.transform;

                if (newBuilding.Tag == AgentTag.Wall)
                {
                    newBuilding.transform.localScale = qStructure.transform.localScale;
                }

                Vector3 objectSize = Vector3.Scale(newBuilding.transform.localScale, newBuilding.GetComponentInChildren<MeshRenderer>().bounds.size);
                newBuilding.GetAbility<Structure>().BuildSizeLow = (int)Math.Ceiling(objectSize.x);
                newBuilding.GetAbility<Structure>().BuildSizeHigh = (int)Math.Ceiling(objectSize.z);

                //queue object no longer required
                UnityEngine.Object.Destroy(qStructure.gameObject);

                //send false to disable building spacing so walls can snap
                bool canSnap = newBuilding.Tag == AgentTag.Wall;
                if (GridBuilder.Place(newBuilding.GetAbility<Structure>(), newBuilding.Body.Position, canSnap))
                {
                    _cachedCommander.CachedResourceManager.RemoveResources(newBuilding);
                    newBuilding.SetState(AnimState.Building);
                    newBuilding.SetPlayingArea(tempConstructor.GetPlayerArea());
                    newBuilding.GetAbility<Health>().HealthAmount = FixedMath.Create(0);
                    newBuilding.SetCommander(_cachedCommander);

                    if (ndx == 0)
                    {
                        // send build command
                        Command buildCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
                        buildCom.Add<DefaultData>(new DefaultData(DataType.UShort, newBuilding.GlobalID));
                        UserInputHelper.SendCommand(buildCom);
                    }

                    newBuilding.GetAbility<Structure>().StartConstruction();
                }
                else
                {
                    Debug.Log("Couldn't place building!");
                    _buildQueue.Clear();
                }

                ndx++;
            }
        }
    }

    public static void CancelBuildingPlacement()
    {
        if (_constructingWall)
        {
            _constructingWall = false;
            tempStructure.GetComponent<WallPositioningHelper>().OnRightClick();
        }

        findingPlacement = false;
        UnityEngine.Object.Destroy(tempStructure.gameObject);
        tempStructure = null;
        tempConstructor = null;
        GridBuilder.Reset();
    }

    private static void SetTransparentMaterial(GameObject structure, Material material)
    {
        if (_constructingWall)
        {
            structure.GetComponentInChildren<WallPositioningHelper>().SetTransparentMaterial(material);
        }
        else
        {
            Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                renderer.material = material;
            }
        }
    }

    public static GameObject ClosestStructureTo(Vector3 worldPoint, float distance, string searchTag)
    {
        GameObject closest = null;
        float currentDistance = Mathf.Infinity;
        foreach (Transform child in ConstructionHandler.OrganizerStructures)
        {
            if (child.gameObject.tag == searchTag)
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
}
