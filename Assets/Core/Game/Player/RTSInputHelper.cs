using System;
using System.Collections.Generic;
using UnityEngine;

using RTSLockstep.Abilities.Essential;
using RTSLockstep.BuildSystem;
using RTSLockstep.Data;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;
using RTSLockstep.Player.UI;

/// <summary>
/// Handles user input outside of HUD
/// </summary>

namespace RTSLockstep.Player
{
    public class RTSInputHelper : InputHelper
    {
        public static AbilityDataItem QuickMove;
        public static AbilityDataItem QuickTarget;
        public static AbilityDataItem QuickHarvest;
        public static AbilityDataItem QuickRally;

        public static event Action<UserInputKeyMappings> OnRotateLeft;
        public static event Action<UserInputKeyMappings> OnRotateRight;

        // limits for angle in tilt x axis
        protected float yaw = 0f;
        protected float pitch = 0f;
        protected float minPitch = -30f;
        protected float maxPitch = 60f;

        #region BehaviorHelper
        public override void OnSetup()
        {
            if (GUIManager.IsNull())
            {
                GUIManager = new RTSGUIManager();
            }

            // set to starting camera angels
            yaw = GUIManager.MainCam.transform.eulerAngles.y;
            pitch = GUIManager.MainCam.transform.eulerAngles.x;


            QuickMove = AbilityDataItem.FindInterfacer("Move");
            QuickTarget = AbilityDataItem.FindInterfacer("Attack");
            QuickHarvest = AbilityDataItem.FindInterfacer("Harvest");
            QuickRally = AbilityDataItem.FindInterfacer("Rally");

            base.OnSetup();
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            RTSInterfacing.Initialize();
            ConstructionHandler.Initialize();
        }

        public override void OnVisualize()
        {
            if (ConstructionHandler.IsFindingBuildingLocation())
            {
                SelectionManager.CanBox = false;
            }
            else
            {
                SelectionManager.CanBox = true;
            }
            //Update RTSInterfacing, a useful tool that automatically generates useful data for user-interfacing
            RTSInterfacing.Visualize();
            //Update Construction handler which handles placing buildings on a grid
            ConstructionHandler.Visualize();
            base.OnVisualize();

            // other defined keys
            foreach (KeyValuePair<UserInputKeyMappings, KeyCode> inputKey in UserInputKeys)
            {
                if (Input.GetKeyDown(inputKey.Value))
                {
                    switch (inputKey.Key)
                    {
                        // these should probably be switched to events...
                        case UserInputKeyMappings.RotateLeftShortCut:
                            OnRotateLeft?.Invoke(UserInputKeyMappings.RotateLeftShortCut);
                            break;
                        case UserInputKeyMappings.RotateRightShortCut:
                            OnRotateRight?.Invoke(UserInputKeyMappings.RotateRightShortCut);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void OnUpdateGUI()
        {
            if (_boxStyle.IsNull())
            {
                return;
            }

            DrawBox(_boxStyle);
        }
        #endregion

        #region Protected
        protected override void MoveCamera()
        {
            float xpos = Input.mousePosition.x;
            float ypos = Input.mousePosition.y;
            Vector3 movement = new Vector3(0, 0, 0);

            bool mouseScroll = false;

            //horizontal camera movement
            if (xpos >= 0 && xpos < GameResourceManager.ScrollWidth)
            {
                movement.x -= GameResourceManager.ScrollSpeed;
                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.PanLeft);
                mouseScroll = true;
            }
            else if (xpos <= Screen.width && xpos > Screen.width - GameResourceManager.ScrollWidth)
            {
                movement.x += GameResourceManager.ScrollSpeed;
                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.PanRight);
                mouseScroll = true;
            }

            //vertical camera movement
            if (ypos >= 0 && ypos < GameResourceManager.ScrollWidth)
            {
                movement.z -= GameResourceManager.ScrollSpeed;
                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.PanDown);
                mouseScroll = true;
            }
            else if (ypos <= Screen.height && ypos > Screen.height - GameResourceManager.ScrollWidth)
            {
                movement.z += GameResourceManager.ScrollSpeed;
                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.PanUp);
                mouseScroll = true;
            }

            // make sure movement is in the direction the camera is pointing
            // but ignore the vertical tilt of the camera to get sensible scrolling
            movement = GUIManager.MainCam.transform.TransformDirection(movement);
            movement.y = 0;

            // away from ground movement
            movement.y -= GameResourceManager.ScrollSpeed * Input.GetAxis("Mouse ScrollWheel");

            // calculate desiered camera position based on received input
            Vector3 origin = GUIManager.MainCam.transform.position;
            Vector3 destination = origin;
            destination.x += movement.x;
            destination.y += movement.y;
            destination.z += movement.z;

            // limit away from ground movement to be between a minimum and maximum distance
            if (destination.y > GameResourceManager.MaxCameraHeight)
            {
                destination.y = GameResourceManager.MaxCameraHeight;
            }
            else if (destination.y < GameResourceManager.MinCameraHeight)
            {
                destination.y = GameResourceManager.MinCameraHeight;
            }

            // if a change in position is destected, perform necessary update
            if (destination != origin)
            {
                GUIManager.MainCam.transform.position = Vector3.MoveTowards(origin, destination, Time.deltaTime * GameResourceManager.ScrollSpeed);
            }

            if (!SelectionManager.MousedAgent
                && !mouseScroll
                && !PlayerManager.CurrentPlayer.PlayerHUD.GetCursorLockState()
                && !PlayerManager.CurrentPlayer.PlayerHUD.IsMouseOverHUD)
            {
                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Select);
            }
        }

        protected override void RotateCamera()
        {
            Vector3 origin = GUIManager.MainCam.transform.eulerAngles;

            float rotateAmountH = Input.GetAxis("Mouse X") * GameResourceManager.RotateSpeedH;
            float rotateAmountV = Input.GetAxis("Mouse Y") * GameResourceManager.RotateSpeedV;

            yaw += rotateAmountH;
            pitch -= rotateAmountV;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Vector3 destination = new Vector3(pitch, yaw, 0f);

            // if a change in position is detected, perform necessary update
            if (destination != origin)
            {
                GUIManager.MainCam.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
            }
        }

        protected override void HandleSingleLeftClick()
        {
            if (PlayerManager.CurrentPlayer.PlayerHUD.MouseInBounds())
            {
                if (!ConstructionHandler.IsFindingBuildingLocation())
                {
                    if (Selector.MainSelectedAgent
                        && Selector.MainSelectedAgent.IsActive
                        && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.CurrentPlayerController))
                    {
                        if (Selector.MainSelectedAgent.GetAbility<Rally>().IsNotNull()
                            && Selector.MainSelectedAgent.GetAbility<Rally>().GetFlagState() == FlagState.SettingFlag)
                        {
                            //call harvest command
                            SelectionManager.SetSelectionLock(true);
                            ProcessInterfacer((QuickRally));
                        }
                        else
                        {
                            SelectionManager.SetSelectionLock(false);
                        }
                    }
                    else
                    {
                        SelectionManager.SetSelectionLock(false);
                    }
                }
            }
        }

        protected override void HandleSingleRightClick()
        {
            if (PlayerManager.CurrentPlayer.PlayerHUD.MouseInBounds()
                && Selector.MainSelectedAgent
                && !ConstructionHandler.IsFindingBuildingLocation())
            {
                if (Selector.MainSelectedAgent
                    && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.CurrentPlayerController))
                {
                    if (Selector.MainSelectedAgent.GetAbility<Rally>()
                        && !Selector.MainSelectedAgent.GetAbility<Structure>().NeedsConstruction)
                    {
                        if (Selector.MainSelectedAgent.GetAbility<Rally>().GetFlagState() == FlagState.SettingFlag)
                        {
                            Selector.MainSelectedAgent.GetAbility<Rally>().SetFlagState(FlagState.SetFlag);
                            PlayerManager.CurrentPlayer.PlayerHUD.SetCursorLock(false);
                            PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Select);
                        }
                        else
                        {
                            Vector2d rallyPoint = RTSInterfacing.GetWorldPosD(Input.mousePosition);
                            Selector.MainSelectedAgent.GetAbility<Rally>().SetRallyPoint(rallyPoint.ToVector3());
                        }
                    }

                    if (RTSInterfacing.MousedAgent.IsNotNull())
                    {
                        // if moused agent is a resource, send selected agent to harvest
                        if (Selector.MainSelectedAgent.GetAbility<Harvest>()
                            && RTSInterfacing.MousedAgent.MyAgentType == AgentType.RawMaterial)
                        {
                            //call harvest command
                            ProcessInterfacer((QuickHarvest));
                        }
                        // moused agent is a building and owned by current player
                        else if (RTSInterfacing.MousedAgent.MyAgentType == AgentType.Structure
                            && RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.CurrentPlayerController))
                        {
                            // moused agent isn't under construction
                            if (!RTSInterfacing.MousedAgent.GetAbility<Structure>().NeedsConstruction)
                            {
                                // if moused agent is a harvester resource deposit, call harvest command to initiate deposit
                                if (Selector.MainSelectedAgent.GetAbility<Harvest>()
                                    && Selector.MainSelectedAgent.GetAbility<Harvest>().GetCurrentLoad() > 0)
                                {
                                    //call harvest command 
                                    ProcessInterfacer((QuickHarvest));
                                }
                            }
                            // moused agent is still under construction
                            else if (Selector.MainSelectedAgent.GetAbility<Construct>())
                            {
                                //call construct command
                                ConstructionHandler.HelpConstruct();
                            }
                        }
                        else if (Selector.MainSelectedAgent.GetAbility<Attack>()
                            && !RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.CurrentPlayerController)
                            && RTSInterfacing.MousedAgent.MyAgentType != AgentType.RawMaterial)
                        {
                            //If the selected agent has Attack (the ability behind attacking) and the mouse is over an agent, send a target command - right clicking on a unit
                            ProcessInterfacer((QuickTarget));
                        }
                    }
                    else
                    {
                        // If there is no agent under the mouse or the selected agent doesn't have Attack, send a Move command - right clicking on terrain
                        ProcessInterfacer((QuickMove));
                    }
                }
            }
        }

        protected override void MouseHover()
        {
            if (PlayerManager.CurrentPlayer.PlayerHUD.MouseInBounds()
                && Selector.MainSelectedAgent
                    && Selector.MainSelectedAgent.IsActive
                    && Selector.MainSelectedAgent.GetAbility<Move>()
                    && Selector.MainSelectedAgent.GetAbility<Move>().CanMove
                    && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.CurrentPlayerController)
                    && !SelectionManager.MousedAgent)
            {
                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Move);
            }
        }

        private void DrawBox(GUIStyle style)
        {
            SelectionManager.DrawBox(style);
        }
        #endregion
    }
}