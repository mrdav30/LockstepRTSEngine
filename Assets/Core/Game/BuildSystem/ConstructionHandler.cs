using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.BuildSystem.BuildGrid;
using RTSLockstep.Data;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using System.Collections.Generic;
using UnityEngine;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;

namespace RTSLockstep.BuildSystem
{
    public static class ConstructionHandler
    {
        #region Properties
        private static GameObject tempObject;
        private static Structure tempStructure;
        private static LSBody tempStructureBody;
        private static Vector3 lastLocation;
        private static bool _findingPlacement = false;
        private static bool _constructingWall = false;

        private static LSPlayer _cachedPlayer;
        private static Dictionary<string, Material> oldMaterials = new Dictionary<string, Material>();

        private static FastList<QStructure> constructionQueue = new FastList<QStructure>();

        public static Transform OrganizerStructures { get; private set; }
        #endregion

        public static void Initialize()
        {
            _cachedPlayer = PlayerManager.MainController.Player;
            GridBuilder.Initialize();

            OrganizerStructures = LSUtility.CreateEmpty().transform;
            OrganizerStructures.transform.parent = PlayerManager.MainController.Player.GetComponentInChildren<LSAgents>().transform;
            OrganizerStructures.gameObject.name = "OrganizerStructures";

            WallPositioningHelper.Initialize();

            PlayerInputHelper.OnSingleLeftTapDown += HandleSingleLeftClick;
            PlayerInputHelper.OnSingleRightTapDown += HandleSingleRightClick;
        }

        // Update is called once per frame
        public static void Visualize()
        {
            if (IsFindingBuildingLocation())
            {
                if (!_cachedPlayer.CachedHud._mouseOverHud)
                {
                    if (!GridBuilder.IsMovingBuilding)
                    {
                        GridBuilder.StartMove(tempStructure as IBuildable);
                    }

                    FindStructureLocation();

                    Vector2d pos = new Vector2d(tempObject.transform.position.x, tempObject.transform.position.z);
                    tempStructure.ValidPlacement = GridBuilder.UpdateMove(pos);

                    if (tempStructure.ValidPlacement &&
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
                if (tempStructure.ValidPlacement)
                {
                    if (_constructingWall)
                    {
                        WallPositioningHelper.OnLeftClick();
                    }
                    else
                    {
                        // only constructing 1 object, place it in the agents construct queue
                        SetConstructionQueue(tempObject);
                        SendConstructCommand();
                    }
                }
                else
                {
                    Debug.Log("Invalid end placement!");
                }

            }
        }

        private static void HandleSingleRightClick()
        {
            if (IsFindingBuildingLocation())
            {
                //Reset construction handler
                Reset();
            }
        }

        public static void CreateStructure(string buildingName, LSAgent constructingAgent)
        {
            Vector2d buildPoint = new Vector2d(constructingAgent.transform.position.x, constructingAgent.transform.position.z + 10);
            LSAgent buildingTemplate = GameResourceManager.GetAgentTemplate(buildingName);

            if (buildingTemplate.MyAgentType == AgentType.Structure && buildingTemplate.GetComponent<Structure>())
            {
                // check that the Player has the resources available before allowing them to create a new structure
                if (!_cachedPlayer.CachedResourceManager.CheckResources(buildingTemplate))
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

                        tempStructure.gameObject.transform.position = StructurePositionHelper.GetSnappedPosition(buildPoint.ToVector3());
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

                tempStructure.transform.position = StructurePositionHelper.GetSnappedPosition(newLocation);

                if (_constructingWall)
                {
                    WallPositioningHelper.Visualize();
                }
            }
        }

        public static void SetConstructionQueue(GameObject buildingProject, long adjustHalfWidth = 0, long adjustHalfLength = 0)
        {
            QStructure qStructure = new QStructure
            {
                StructureName = buildingProject.gameObject.name,
                BuildPoint = new Vector2d(buildingProject.transform.localPosition.x, buildingProject.transform.localPosition.z),
                RotationPoint = new Vector2d(buildingProject.transform.localRotation.w, buildingProject.transform.localRotation.y),
                LocalScale = new Vector3d(buildingProject.transform.localScale),

                HalfWidth = adjustHalfWidth > 0 ? adjustHalfWidth : tempStructureBody.HalfWidth,
                HalfLength = adjustHalfLength > 0 ? adjustHalfLength : tempStructureBody.HalfLength
            };

            constructionQueue.Add(qStructure);
        }

        public static void SendConstructCommand()
        {
            // send construct command for selected agent to start construction queue
            Command constructCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);

            for (int i = 0; i < constructionQueue.Count; i++)
            {
                constructCom.Add(new QueueStructure(constructionQueue[i]));
            }

            PlayerInputHelper.SendCommand(constructCom);

            //Reset construction handler
            Reset();
        }

        public static void HelpConstruct()
        {
            // send construct command for selected agent to help construct an agent
            Command constructCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);

            // send a flag for agent to register to construction group
            constructCom.Add(new DefaultData(DataType.Bool, true));

            constructCom.Add(new DefaultData(DataType.UShort, RTSInterfacing.MousedAgent.GlobalID));

            PlayerInputHelper.SendCommand(constructCom);
        }

        public static void Reset()
        {
            _findingPlacement = false;
            if (_constructingWall)
            {
                _constructingWall = false;
                WallPositioningHelper.Reset();
            }

            constructionQueue.Clear();

            //temp structure no longer required
            Object.Destroy(tempObject);
            tempStructure = null;
            tempStructureBody = null;
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
}