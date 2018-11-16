using RTSLockstep;
using RTSLockstep.Data;
using System.Collections.Generic;
using UnityEngine;

public static class ConstructionHandler
{
    #region Properties
    private static GameObject tempBuilding;
    private static RTSAgent tempCreator;
    private static bool findingPlacement = false;
    private static AgentCommander _cachedCommander;
    private static bool _canPlace;
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
            FindBuildingLocation();
            if (_canPlace)
            {
                SetTransparentMaterial(tempBuilding, _cachedCommander.CachedHud.allowedMaterial, false);
            }
            else
            {
                SetTransparentMaterial(tempBuilding, _cachedCommander.CachedHud.notAllowedMaterial, false);
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
            //put resources in LS db
            if (!_cachedCommander.CachedResourceManager.CheckResources(buildingTemplate))
            {
                Debug.Log("Not enough resources!");
            }
            else
            {
                tempBuilding = Object.Instantiate(buildingTemplate.GetComponent<Structure>().tempStructure) as GameObject;

                if (tempBuilding.GetComponent<TempStructure>())
                {
                    tempBuilding.name = buildingName;
                    tempBuilding.gameObject.transform.position = Positioning.GetSnappedPosition(buildPoint.ToVector3());
                    // retrieve build size from agent template
                    tempBuilding.GetComponent<TempStructure>().BuildSize = buildingTemplate.GetComponent<Structure>().BuildSize;
                    GridBuilder.StartMove(tempBuilding.GetComponent<TempStructure>());
                    tempCreator = creator;
                    SetTransparentMaterial(tempBuilding, _cachedCommander.CachedHud.allowedMaterial, true);

                    findingPlacement = true;
                }
                else
                {
                    Object.Destroy(tempBuilding);
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
        if (findingPlacement && tempBuilding)
        {
            switch (direction)
            {
                case UserInputKeyMappings.RotateLeftShortCut:
                    tempBuilding.transform.Rotate(0, 90, 0);
                    break;
                case UserInputKeyMappings.RotateRightShortCut:
                    tempBuilding.transform.Rotate(0, -90, 0);
                    break;
                default:
                    break;
            }
        }
    }

    public static void FindBuildingLocation()
    {
        Vector3 newLocation = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        if (RTSInterfacing.HitPointIsGround(Input.mousePosition))
        {
            tempBuilding.transform.position = Positioning.GetSnappedPosition(newLocation);
            Vector2d pos = new Vector2d(tempBuilding.transform.position.x, tempBuilding.transform.position.z);
            _canPlace = GridBuilder.UpdateMove(pos);
        }
    }

    public static bool CanPlaceStructure()
    {
        return _canPlace;
    }

    public static void StartConstruction()
    {
        findingPlacement = false;
        Vector2d buildPoint = new Vector2d(tempBuilding.transform.position.x, tempBuilding.transform.position.z);
        RTSAgent newBuilding = _cachedCommander.GetController().CreateAgent(tempBuilding.gameObject.name, buildPoint, new Vector2d(0, tempBuilding.transform.rotation.y)) as RTSAgent;
        Object.Destroy(tempBuilding.gameObject);

        if (GridBuilder.Place(newBuilding.GetAbility<Structure>(), buildPoint))
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
        Object.Destroy(tempBuilding.gameObject);
        tempBuilding = null;
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
