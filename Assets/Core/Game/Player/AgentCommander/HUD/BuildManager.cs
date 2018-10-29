using System.Collections;
using System.Collections.Generic;
using RTSLockstep;
using RTSLockstep;
using UnityEngine;
using RTSLockstep.Data;

public class BuildManager : MonoBehaviour
{
    #region Properties
    private RTSAgent tempBuilding;
    private RTSAgent tempCreator;
    private bool findingPlacement = false;
    private AgentCommander cachedCommander;

    public Material notAllowedMaterial, allowedMaterial;
    #endregion

    // Use this for initialization
    private void Start()
    {
        cachedCommander = transform.GetComponentInParent<AgentCommander>();
    }

    // Update is called once per frame
    public void Update()
    {
        if (findingPlacement)
        {
            if (CanPlaceBuilding())
            {
                tempBuilding.SetTransparentMaterial(allowedMaterial, false);
            }
            else
            {
                tempBuilding.SetTransparentMaterial(notAllowedMaterial, false);
            }
        }
    }

    public void CreateBuilding(RTSAgent agent, string buildingName)
    {
        Vector2d buildPoint = new Vector2d(agent.transform.position.x, agent.transform.position.z + 10);
        if (cachedCommander)
        {
            //cleanup later...
            CreateBuilding(buildingName, buildPoint, agent, agent.GetPlayerArea());
        }
    }

    public void CreateBuilding(string buildingName, Vector2d buildPoint, RTSAgent creator, Rect playingArea)
    {
        GameObject newBuilding = Instantiate(ResourceManager.GetAgentTemplate(buildingName).gameObject);

        tempBuilding = newBuilding.GetComponent<RTSAgent>();
        if (tempBuilding.MyAgentType == AgentType.Building)
        {
            tempBuilding.name = buildingName;
            tempBuilding.gameObject.transform.position = buildPoint.ToVector3();
            tempCreator = creator;
            findingPlacement = true;
            tempBuilding.SetTransparentMaterial(notAllowedMaterial, true);
        }
        else
        {
            Destroy(newBuilding);
        }
    }

    public bool IsFindingBuildingLocation()
    {
        return findingPlacement;
    }

    public void FindBuildingLocation()
    {
        Vector3 newLocation = RTSInterfacing.GetWorldPos3(Input.mousePosition);
        if (RTSInterfacing.HitPointIsGround(Input.mousePosition))
        {
            tempBuilding.transform.position = newLocation;
        }
    }

    // fires a ray into the world to find the first object we would hit, which should be the ground.
    // If the selection manager is over an existing Agent, then space is already occupied and so it cannot be built in.
    public bool CanPlaceBuilding()
    {
        bool canPlace = true;
        if (SelectionManager.MousedAgent || !RTSInterfacing.HitPointIsGround(tempBuilding.transform.position))
        {
            canPlace = false;
        }
        return canPlace;
    }

    // move to build ability?
    public void StartConstruction()
    {
        findingPlacement = false;
        Vector2d buildPoint = new Vector2d(tempBuilding.transform.position.x, tempBuilding.transform.position.z);
        RTSAgent newBuilding = cachedCommander.CachedController.CreateAgent(tempBuilding.gameObject.name, buildPoint, Vector2d.right) as RTSAgent;
        Destroy(tempBuilding.gameObject);

        newBuilding.SetState(AnimState.Building);
        newBuilding.RestoreMaterials();
        newBuilding.SetPlayingArea(tempCreator.GetPlayerArea());
        newBuilding.GetAbility<Health>().HealthAmount = FixedMath.Create(0);
        newBuilding.SetCommander();

        // send build command
        Command buildCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
        buildCom.Add<DefaultData>(new DefaultData(DataType.UShort, newBuilding.GlobalID));
        UserInputHelper.SendCommand(buildCom);

        newBuilding.GetAbility<Structure>().StartConstruction();
        // check that the Player has the resources available before allowing them to create a new Unit / Building
        cachedCommander.RemoveResource(ResourceType.Gold, newBuilding.cost);
    }

    public void CancelBuildingPlacement()
    {
        findingPlacement = false;
        Destroy(tempBuilding.gameObject);
        tempBuilding = null;
        tempCreator = null;
    }

}
