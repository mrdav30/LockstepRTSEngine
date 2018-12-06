using RTSLockstep;
using RTSLockstep.Data;
using System.Collections.Generic;
using UnityEngine;

public static class ConstructionHandler
{
    #region Properties
    private static GameObject tempStructure;
    private static AgentTag tempStructureTag;
    private static Vector3 lastLocation;
    private static RTSAgent tempConstructor;
    private static bool findingPlacement = false;
    private static bool _constructingWall = false;

    private static AgentCommander _cachedCommander;
    private static bool _validPlacement;
    private static List<Material> oldMaterials = new List<Material>();

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
        UserInputHelper.OnLeftTapUp += HandleLeftClickRelease;
        //  UserInputHelper.OnLeftTapHoldDown += HandleLeftClickDrag;
    }

    // Update is called once per frame
    public static void Visualize()
    {
        if (findingPlacement)
        {
            if (!_cachedCommander.CachedHud._mouseOverHud)
            {
                FindBuildingLocation();

                if (_validPlacement)
                {
                    SetTransparentMaterial(GameResourceManager.AllowedMaterial, false);
                }
                else
                {
                    SetTransparentMaterial(GameResourceManager.NotAllowedMaterial, false);
                }
            }
        }
    }

    private static void HandleSingleLeftClick()
    {
        if (_constructingWall)
        {
            tempStructure.GetComponent<WallPositioningHelper>().OnLeftClick();
        }
    }

    private static void HandleLeftClickRelease()
    {
        if (_constructingWall)
        {
            tempStructure.GetComponent<WallPositioningHelper>().OnLeftClickUp();
        }
    }

    //private static void HandleLeftClickDrag()
    //{
    //    if (_constructingWall)
    //    {
    //        tempStructure.GetComponent<WallPositioningHelper>().OnLeftClickDrag();
    //    }
    //}

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
                tempStructure = Object.Instantiate(buildingTemplate.GetComponent<Structure>().tempStructure, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

                if (tempStructure.GetComponent<TempStructure>())
                {
                    tempStructure.name = buildingName;
                    // retrieve build size from agent template
                    tempStructure.GetComponent<TempStructure>().BuildSizeLow = buildingTemplate.GetComponent<Structure>().BuildSizeLow;
                    tempStructure.GetComponent<TempStructure>().BuildSizeHigh = buildingTemplate.GetComponent<Structure>().BuildSizeHigh;
                    tempStructureTag = buildingTemplate.Tag;
                    tempConstructor = constructingAgent;

                    findingPlacement = true;
                    SetTransparentMaterial(GameResourceManager.AllowedMaterial, true);
                    tempStructure.gameObject.transform.position = Positioning.GetSnappedPosition(buildPoint.ToVector3());

                    if (tempStructureTag == AgentTag.Wall)
                    {
                        _constructingWall = true;
                        tempStructure.GetComponent<WallPositioningHelper>().Setup();
                    }
                    else
                    {
                        GridBuilder.StartMove(tempStructure.GetComponent<TempStructure>());
                    }
                }
                else
                {
                    Object.Destroy(tempStructure);
                }
            }
        }
    }

    public static bool IsFindingBuildingLocation()
    {
        return _constructingWall && findingPlacement;
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

    public static void FindBuildingLocation()
    {
        Vector3 newLocation = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        if (RTSInterfacing.HitPointIsGround(Input.mousePosition)
            && lastLocation != newLocation)
        {
            lastLocation = newLocation;

            if (!GridBuilder.IsMovingBuilding)
            {
                GridBuilder.StartMove(tempStructure.GetComponent<TempStructure>());
            }

            if (_constructingWall)
            {
                tempStructure.GetComponent<WallPositioningHelper>().Visualize(newLocation);
            }
            else
            {
                tempStructure.transform.position = Positioning.GetSnappedPosition(newLocation);
            }

            Vector2d pos = new Vector2d(tempStructure.transform.position.x, tempStructure.transform.position.z);
            _validPlacement = GridBuilder.UpdateMove(pos);
        }
    }

    public static bool CanPlaceStructure()
    {
        //// ensure that user is not placing walls
        if (!_constructingWall)
        {
            if (GridBuilder.EndMove() == PlacementResult.Placed)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public static void StartConstruction()
    {
        findingPlacement = false;
        Vector2d buildPoint = new Vector2d(tempStructure.transform.position.x, tempStructure.transform.position.z);
        RTSAgent newBuilding = _cachedCommander.GetController().CreateAgent(tempStructure.gameObject.name, buildPoint, new Vector2d(0, 0)) as RTSAgent;

        // remove temporary structure from grid
        GridBuilder.Unbuild(tempStructure.GetComponent<TempStructure>());
        newBuilding.GetAbility<Structure>().BuildSizeLow = tempStructure.GetComponent<TempStructure>().BuildSizeLow;
        newBuilding.GetAbility<Structure>().BuildSizeHigh = tempStructure.GetComponent<TempStructure>().BuildSizeHigh;
        Object.Destroy(tempStructure.gameObject);

        if (GridBuilder.Place(newBuilding.GetAbility<Structure>(), newBuilding.Body.Position))
        {
            _cachedCommander.CachedResourceManager.RemoveResources(newBuilding);
            RestoreMaterials(newBuilding.gameObject);
            newBuilding.SetState(AnimState.Building);
            newBuilding.SetPlayingArea(tempConstructor.GetPlayerArea());
            newBuilding.GetAbility<Health>().HealthAmount = FixedMath.Create(0);
            newBuilding.SetCommander(_cachedCommander);

            // send build command
            Command buildCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
            buildCom.Add<DefaultData>(new DefaultData(DataType.UShort, newBuilding.GlobalID));
            UserInputHelper.SendCommand(buildCom);

            newBuilding.GetAbility<Structure>().StartConstruction();
        }
        else
        {
            Debug.Log("Couldn't place building!");
        }
    }

    public static void SetBuildQueue(GameObject buildingProject)
    {
        _buildQueue.Enqueue(buildingProject);
    }

    public static void ProcessBuildQueue()
    {
        findingPlacement = false;
        _constructingWall = false;

        if (CanPlaceStructure())
        {
            // remove temporary structure from grid
            GridBuilder.Unbuild(tempStructure.GetComponent<TempStructure>());
            Object.Destroy(tempStructure.gameObject);

            while (_buildQueue.Count > 0)
            {
                GameObject qStructure = _buildQueue.Dequeue();
                Vector2d buildPoint = new Vector2d(qStructure.transform.position.x, qStructure.transform.position.z);
                //  Vector2d rotation = new Vector2d(qStructure.transform.rotation.x, qStructure.transform.rotation.y);
                RTSAgent newBuilding = _cachedCommander.GetController().CreateAgent(qStructure.gameObject.name, buildPoint) as RTSAgent;

                newBuilding.transform.parent = OrganizerStructures.transform;
                newBuilding.GetComponentInChildren<WallPrefab>().transform.localScale = qStructure.transform.localScale;
                newBuilding.GetComponentInChildren<WallPrefab>().transform.localRotation = qStructure.transform.localRotation;
                //     newBuilding.GetAbility<Structure>().BuildSizeLow = tempStructure.GetComponent<TempStructure>().BuildSizeLow;
                //     newBuilding.GetAbility<Structure>().BuildSizeHigh = tempStructure.GetComponent<TempStructure>().BuildSizeHigh;
                Object.Destroy(qStructure.gameObject);

                //    if (GridBuilder.Place(newBuilding.GetAbility<Structure>(), newBuilding.Body.Position))
                //   {
                _cachedCommander.CachedResourceManager.RemoveResources(newBuilding);
                RestoreMaterials(newBuilding.gameObject);
                newBuilding.SetState(AnimState.Building);
                newBuilding.SetPlayingArea(tempConstructor.GetPlayerArea());
                newBuilding.GetAbility<Health>().HealthAmount = FixedMath.Create(0);
                newBuilding.SetCommander(_cachedCommander);

                // send build command
                //Command buildCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
                //buildCom.Add<DefaultData>(new DefaultData(DataType.UShort, newBuilding.GlobalID));
                //UserInputHelper.SendCommand(buildCom);

                newBuilding.GetAbility<Structure>().StartConstruction();
                //   }
                //     else
                //    {
                //   Debug.Log("Couldn't place building!");
                //     }
            }
        }

        Debug.Log(_buildQueue.Count);
    }

    public static void CancelBuildingPlacement()
    {
        findingPlacement = false;
        if (_constructingWall)
        {
            _constructingWall = false;
            tempStructure.GetComponent<WallPositioningHelper>().OnRightClick();
            //if (tempWallPole.IsNotNull())
            //{
            //    Object.Destroy(tempWallPole.gameObject);
            //    tempWallPole = null;
            //}
        }
        Object.Destroy(tempStructure.gameObject);
        tempStructure = null;
        tempConstructor = null;
    }

    private static void SetTransparentMaterial(Material material, bool storeExistingMaterial)
    {
        if (storeExistingMaterial)
        {
            oldMaterials.Clear();
        }

        Renderer[] renderers;

        if (!_constructingWall)
        {
            renderers = tempStructure.GetComponentsInChildren<Renderer>();
        }
        else
        {
            renderers = OrganizerStructures.GetComponentsInChildren<Renderer>();
        }

        foreach (Renderer renderer in renderers)
        {
            if (storeExistingMaterial)
            {
                oldMaterials.Add(renderer.material);
            }
            renderer.material = material;
        }
    }

    private static void RestoreMaterials(GameObject structure)
    {
        Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();

        if (oldMaterials.Count == renderers.Length)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = oldMaterials[i];
            }
        }
    }
}
