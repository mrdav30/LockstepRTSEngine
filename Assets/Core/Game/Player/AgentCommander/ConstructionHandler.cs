using RTSLockstep;
using RTSLockstep.Data;
using System.Collections.Generic;
using UnityEngine;

public static class ConstructionHandler
{
    #region Properties
    private static GameObject tempStructure;
    private static Vector3 lastLocation;
    private static RTSAgent tempCreator;
    private static bool findingPlacement = false;
    private static AgentCommander _cachedCommander;
    private static bool _validPlacement;
    private static List<Material> oldMaterials = new List<Material>();
    #endregion

    public static void Initialize()
    {
        _cachedCommander = PlayerManager.MainController.Commander;
        GridBuilder.Initialize();
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
                    SetTransparentMaterial(tempStructure, _cachedCommander.CachedHud.allowedMaterial, false);
                }
                else
                {
                    SetTransparentMaterial(tempStructure, _cachedCommander.CachedHud.notAllowedMaterial, false);
                }
            }
        }
    }

    public static void CreateBuilding(RTSAgent agent, string buildingName)
    {
        Vector2d buildPoint = new Vector2d(agent.transform.position.x, agent.transform.position.z + 10);
        if (_cachedCommander)
        {
            //cleanup later...
            CreateBuilding(buildingName, buildPoint, agent, agent.GetPlayerArea());
        }
    }

    public static void CreateBuilding(string buildingName, Vector2d buildPoint, RTSAgent creator, Rect playingArea)
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
                tempStructure = Object.Instantiate(buildingTemplate.GetComponent<Structure>().tempStructure) as GameObject;

                if (tempStructure.GetComponent<TempStructure>())
                {
                    tempStructure.name = buildingName;
                    tempStructure.gameObject.transform.position = Positioning.GetSnappedPosition(buildPoint.ToVector3());
                    // retrieve build size from agent template
                    tempStructure.GetComponent<TempStructure>().BuildSize = buildingTemplate.GetComponent<Structure>().BuildSize;
                    GridBuilder.StartMove(tempStructure.GetComponent<TempStructure>());
                    tempCreator = creator;
                    SetTransparentMaterial(tempStructure, _cachedCommander.CachedHud.allowedMaterial, true);

                    findingPlacement = true;
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
        return findingPlacement;
    }

    //this isn't updating LSBody rotation correctly...
    public static void HandleRotationTap(UserInputKeyMappings direction)
    {
        if (findingPlacement && tempStructure)
        {
            switch (direction)
            {
                case UserInputKeyMappings.RotateLeftShortCut:
                    tempStructure.transform.Rotate(0, 90, 0);
                    break;
                case UserInputKeyMappings.RotateRightShortCut:
                    tempStructure.transform.Rotate(0, -90, 0);
                    break;
                default:
                    break;
            }
        }
    }

    public static void FindBuildingLocation()
    {
        Vector3 newLocation = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        if (RTSInterfacing.HitPointIsGround(Input.mousePosition) && lastLocation != newLocation)
        {
            lastLocation = newLocation;
            if (!GridBuilder.IsMovingBuilding)
            {
                GridBuilder.StartMove(tempStructure.GetComponent<TempStructure>());
            }
            tempStructure.transform.position = Positioning.GetSnappedPosition(newLocation);
            Vector2d pos = new Vector2d(tempStructure.transform.position.x, tempStructure.transform.position.z);
            _validPlacement = GridBuilder.UpdateMove(pos);
        }
    }

    public static bool CanPlaceStructure()
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

    public static void StartConstruction()
    {
        findingPlacement = false;
        Vector2d buildPoint = new Vector2d(tempStructure.transform.position.x, tempStructure.transform.position.z);
        RTSAgent newBuilding = _cachedCommander.GetController().CreateAgent(tempStructure.gameObject.name, buildPoint, new Vector2d(0, tempStructure.transform.rotation.y)) as RTSAgent;

        // remove temporary structure from grid
        GridBuilder.Unbuild(tempStructure.GetComponent<TempStructure>());
        Object.Destroy(tempStructure.gameObject);

        if (GridBuilder.Place(newBuilding.GetAbility<Structure>(), newBuilding.Body.Position))
        {
            _cachedCommander.CachedResourceManager.RemoveResources(newBuilding);
            RestoreMaterials(newBuilding.gameObject);
            newBuilding.SetState(AnimState.Building);
            newBuilding.SetPlayingArea(tempCreator.GetPlayerArea());
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

    public static void CancelBuildingPlacement()
    {
        findingPlacement = false;
        Object.Destroy(tempStructure.gameObject);
        tempStructure = null;
        tempCreator = null;
    }

    public static void SetTransparentMaterial(GameObject structure, Material material, bool storeExistingMaterial)
    {
        if (storeExistingMaterial)
        {
            oldMaterials.Clear();
        }
        Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (storeExistingMaterial)
            {
                oldMaterials.Add(renderer.material);
            }
            renderer.material = material;
        }
    }

    public static void RestoreMaterials(GameObject structure)
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
