using RTSLockstep;
using RTSLockstep.Data;
using System.Collections.Generic;
using UnityEngine;

public static class ConstructionHandler
{
    #region Properties
    private static GameObject tempObject;
    private static Structure tempStructure;
    private static LSBody tempStructureBody;
    private static Vector3 lastLocation;
    private static RTSAgent cachedAgent;
    private static bool _findingPlacement = false;
    private static bool _constructingWall = false;

    private static AgentCommander _cachedCommander;
    private static bool _validPlacement;
    private static Dictionary<string, Material> oldMaterials = new Dictionary<string, Material>();

    public static Transform OrganizerStructures { get; private set; }
    #endregion

    public static void Initialize()
    {
        _cachedCommander = PlayerManager.MainController.Commander;
        GridBuilder.Initialize();

        OrganizerStructures = LSUtility.CreateEmpty().transform;
        OrganizerStructures.transform.parent = PlayerManager.MainController.Commander.GetComponentInChildren<RTSAgents>().transform;
        OrganizerStructures.gameObject.name = "OrganizerStructures";

        WallPositioningHelper.Initialize();

        UserInputHelper.OnSingleLeftTapDown += HandleSingleLeftClick;
        UserInputHelper.OnSingleRightTapDown += HandleSingleRightClick;
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
                    GridBuilder.StartMove(tempStructure);
                }

                FindStructureLocation();

                Vector2d pos = new Vector2d(tempObject.transform.position.x, tempObject.transform.position.z);
                _validPlacement = GridBuilder.UpdateMove(pos);

                if (_validPlacement &&
                    SelectionManager.MousedAgent.IsNull())
                {
                    SetTransparentMaterial(tempObject, GameResourceManager.AllowedMaterial);
                }
                else
                {
                    SetTransparentMaterial(tempObject, GameResourceManager.NotAllowedMaterial);
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
                WallPositioningHelper.OnLeftClick();
            }
            else
            {
                // only constructing 1 object, place it in the agents construct queue
                if (_validPlacement)
                {
                    SetConstructionQueue(tempObject);
                    SendConstructCommand();
                }
                else
                {
                    Debug.Log("Invalid end placement!");
                }
            }
        }
    }

    private static void HandleSingleRightClick()
    {
        if (IsFindingBuildingLocation())
        {
            // send false to clear agent's construct queue
            SendConstructCommand(false);
        }
    }

    public static void CreateStructure(string buildingName, RTSAgent constructingAgent, Rect playingArea)
    {
        Vector2d buildPoint = new Vector2d(constructingAgent.transform.position.x, constructingAgent.transform.position.z + 10);
        RTSAgent buildingTemplate = GameResourceManager.GetAgentTemplate(buildingName);

        if (buildingTemplate.MyAgentType == AgentType.Building && buildingTemplate.GetComponent<Structure>())
        {
            // check that the Player has the resources available before allowing them to create a new structure
            if (!_cachedCommander.CachedResourceManager.CheckResources(buildingTemplate))
            {
                Debug.Log("Not enough resources!");
            }
            else
            {
                tempObject = Object.Instantiate(buildingTemplate.gameObject, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                if (tempObject)
                {
                    _findingPlacement = true;
                    SetTransparentMaterial(tempObject, GameResourceManager.AllowedMaterial);
                    tempObject.gameObject.name = buildingName;

                    tempStructure = tempObject.GetComponent<Structure>();
                    if (tempStructure.StructureType == StructureType.Wall)
                    {
                        // walls require a little help since they are click and drag
                        _constructingWall = true;
                        tempStructure.IsOverlay = true;
                        WallPositioningHelper.Setup();
                    }

                    tempStructureBody = tempObject.GetComponent<UnityLSBody>().InternalBody;

                    // structure size is 2 times the size of halfwidth & halfheight
                    tempStructure.BuildSizeLow = (tempStructureBody.HalfWidth.CeilToInt() * 2);
                    tempStructure.BuildSizeHigh = (tempStructureBody.HalfLength.CeilToInt() * 2);

                    cachedAgent = constructingAgent;

                    tempStructure.gameObject.transform.position = Positioning.GetSnappedPosition(buildPoint.ToVector3());
                }
            }
        }
    }

    public static bool IsFindingBuildingLocation()
    {
        return _constructingWall || _findingPlacement;
    }

    public static GameObject GetTempStructureGO()
    {
        return tempObject;
    }

    //this isn't updating LSBody rotation correctly...
    public static void HandleRotationTap(UserInputKeyMappings direction)
    {
        if (_findingPlacement && !_constructingWall)
        {
            //  keep track of previous values to update agent's size
            long prevWidth = tempStructureBody.HalfWidth;
            long prevLength = tempStructureBody.HalfLength;

            switch (direction)
            {
                case UserInputKeyMappings.RotateLeftShortCut:
                    tempStructure.transform.Rotate(0, 90, 0);
                    AdjustStructureSize(prevLength, prevWidth);
                    break;
                case UserInputKeyMappings.RotateRightShortCut:
                    tempStructure.transform.Rotate(0, -90, 0);
                    AdjustStructureSize(prevLength, prevWidth);
                    break;
                default:
                    break;
            }
        }
    }

    public static void AdjustStructureSize(long newWidth, long newLength)
    {
        tempStructureBody.HalfWidth = newWidth;
        tempStructureBody.HalfLength = newLength;
        tempStructure.BuildSizeLow = (tempStructureBody.HalfWidth.CeilToInt() * 2);
        tempStructure.BuildSizeHigh = (tempStructureBody.HalfLength.CeilToInt() * 2);
    }

    public static void FindStructureLocation()
    {
        Vector3 newLocation = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        if (RTSInterfacing.HitPointIsGround(Input.mousePosition) && lastLocation != newLocation)
        {
            lastLocation = newLocation;

            tempStructure.transform.position = Positioning.GetSnappedPosition(newLocation);

            if (_constructingWall)
            {
                WallPositioningHelper.Visualize();
            }
        }
    }

    public static void SetConstructionQueue(GameObject buildingProject, long adjustHalfWidth = 0, long adjustHalfLength = 0)
    {
        QStructure qStructure = new QStructure();
        qStructure.StructureName = buildingProject.gameObject.name;
        qStructure.BuildPoint = new Vector2d(buildingProject.transform.localPosition.x, buildingProject.transform.localPosition.z);
        qStructure.RotationPoint = new Vector2d(buildingProject.transform.localRotation.w, buildingProject.transform.localRotation.y);
        qStructure.LocalScale = new Vector3d(buildingProject.transform.localScale);

        qStructure.HalfWidth = adjustHalfWidth > 0 ? adjustHalfWidth : tempStructureBody.HalfWidth;
        qStructure.HalfLength = adjustHalfLength > 0 ? adjustHalfLength : tempStructureBody.HalfLength;

        Command queueCommand = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
        queueCommand.Add(new QueueStructure(qStructure));

        UserInputHelper.SendCommand(queueCommand);
    }

    public static void SendConstructCommand(bool startConstruction = true)
    {
        //Reset construction handler
        Reset();

        // send construct command
        Command constructCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
        constructCom.Add(new DefaultData(DataType.Bool, startConstruction));

        UserInputHelper.SendCommand(constructCom);
    }

    public static void Reset()
    {
        _findingPlacement = false;
        if (_constructingWall)
        {
            _constructingWall = false;
            WallPositioningHelper.Reset();
        }

        //temp structure no longer required
        Object.Destroy(tempObject);
        tempStructure = null;
        tempStructureBody = null;
        cachedAgent = null;
        // remove temporary structure from grid
        GridBuilder.Reset();
    }

    public static void SetTransparentMaterial(GameObject structure, Material material, bool storeMaterial = false)
    {
        Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (storeMaterial)
            {
                if (!oldMaterials.ContainsKey(renderer.name))
                {
                    oldMaterials.Add(renderer.name, renderer.material);
                }
            }

            renderer.material = material;
        }
    }

    public static void RestoreMaterial(GameObject structure)
    {
        Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (oldMaterials.ContainsKey(renderer.name))
            {
                renderer.material = oldMaterials[renderer.name];
            }
        }
    }
}
