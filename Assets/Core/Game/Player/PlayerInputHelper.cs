using System;
using System.Collections.Generic;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

using RTSLockstep.Abilities;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.BuildSystem;
using RTSLockstep.Data;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.UI;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using RTSLockstep.Menu.UI;
using RTSLockstep.Utility;
using RTSLockstep.Agents.AgentController;

/// <summary>
/// Handles user input outside of HUD
/// </summary>

namespace RTSLockstep.Player
{
    [Serializable]
    public class PlayerInputKeys : SerializableDictionaryBase<UserInputKeyMappings, KeyCode> { };

    public class PlayerInputHelper : BehaviourHelper
    {
        #region Properties
#pragma warning disable 0649
        [SerializeField]
        protected GUIStyle _boxStyle;
        [SerializeField]
        public PlayerInputKeys UserInputKeys;
#pragma warning restore 0649
        public static GUIManager GUIManager;
        /// <summary>
        /// The current ability to cast. Set this to a non-null value to automatically start the gathering process.
        /// </summary>
        protected static AbilityDataItem _currentInterfacer;
        public static AbilityDataItem CurrentInterfacer
        {
            get { return _currentInterfacer; }
            set
            {
                if (value.IsNotNull())
                {
                    IsGathering = true;
                }
                _currentInterfacer = value;
            }
        }
        //Helper function that takes in a type rather than AbilityDataItem to cast an ability
        public static void CastAbility<TAbility>() where TAbility : ActiveAbility
        {
            CurrentInterfacer = AbilityDataItem.FindInterfacer<TAbility>();
        }

        public static event Action OnSingleLeftTapDown;
        public static event Action OnLeftTapUp;
        public static event Action OnLeftTapHoldDown;
        public static event Action OnSingleRightTapDown;
        public static event Action OnDoubleLeftTapDown;

        //Defines the maximum time between two taps to make it double tap
        protected static float tapThreshold = 0.25f;
        protected static float tapTimer = 0.0f;
        protected static bool tap = false;

        protected static bool _isGathering;
        public static bool IsGathering
        {
            get { return _isGathering; }
            private set
            {
                SelectionManager.IsGathering = value;
                _isGathering = value;
            }
        }

        protected static bool _isDragging = false;

        protected static bool Setted = false;
        protected static Command curCom;
        #endregion

        #region BehaviorHelper
        protected virtual void Setup()
        {
            if (GUIManager.IsNull())
            {
                GUIManager = new ExampleGUIManager();
            }

            Setted = true;
        }

        protected override void OnInitialize()
        {
            if (!Setted)
            {
                Setup();
            }

            SelectionManager.Initialize();

            IsGathering = false;
            CurrentInterfacer = null;
        }

        protected override void OnVisualize()
        {
            //Update the SelectionManager which handles mouse-selection.
            SelectionManager.Update();

            if (IsGathering)
            {
                //We are currently gathering mouse information. The next click will trigger the command with the mouse position.
                //I.e. Press "T" to use the 'Psionic Storm' ability. Then left click on a position to activate it there.

                //Right click to cancel casting the abiility by setting IsGathering to false
                if (Input.GetMouseButtonDown(1))
                {
                    IsGathering = false;
                    return;
                }

                //If we left click to release the ability
                //Or if the ability we're activating requires no mouse-based information (i.e. CurrentInterfacer.InformationGather)
                //Trigger the ability
                if (Input.GetMouseButtonDown(0) || CurrentInterfacer.InformationGather == InformationGatherType.None)
                {
                    ProcessInterfacer(CurrentInterfacer);
                }
            }
            else
            {
                //We are not gathering information. Instead, allow quickcasted abilities with the mouse. I.e. Right click to move or attack.
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    OpenPauseMenu();
                }

                MoveCamera();

                // prevent user input if mouse is over hud
                bool mouseOverHud = PlayerManager.CurrentPlayerController.GetPlayerHUD().IsMouseOverHUD;
                if (!mouseOverHud)
                {
                    // detect rotation amount if no agents selected & Right mouse button is down
                    if (PlayerManager.CurrentPlayerController.SelectedAgents.Count <= 0 && Input.GetMouseButton(1)
                        || Input.GetMouseButton(1) && Input.GetKeyDown(KeyCode.LeftAlt))
                    {
                        // lock the cursor to prevent movement during rotation
                        Cursor.lockState = CursorLockMode.Locked;

                        RotateCamera();
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.None;
                    }

                    MouseHover();

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (Time.time < tapTimer + tapThreshold)
                        {
                            // left double click action
                            OnDoubleLeftTapDown?.Invoke(); tap = false;
                            return;
                        }

                        tap = true;
                        tapTimer = Time.time;
                    }
                    // right click action
                    else if (Input.GetMouseButtonDown(1))
                    {
                        HandleSingleRightClick();
                        OnSingleRightTapDown?.Invoke();
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        _isDragging = false;
                        OnLeftTapUp?.Invoke();
                    }

                    if (tap && Time.time > (tapTimer + tapThreshold))
                    {
                        tap = false;

                        // left click hold action
                        if (Input.GetMouseButton(0))
                        {
                            _isDragging = true;
                            OnLeftTapHoldDown?.Invoke();
                        }
                        else
                        {
                            // left click action
                            _isDragging = false;
                            HandleSingleLeftClick();
                            OnSingleLeftTapDown?.Invoke();
                        }
                    }

                    // other defined keys
                    foreach (KeyValuePair<UserInputKeyMappings, KeyCode> inputKey in UserInputKeys)
                    {
                        if (Input.GetKeyDown(inputKey.Value))
                        {
                            switch (inputKey.Key)
                            {
                                // these should probably be switched to events...
                                case UserInputKeyMappings.RotateLeftShortCut:
                                    ConstructionHandler.HandleRotationTap(UserInputKeyMappings.RotateLeftShortCut);
                                    break;
                                case UserInputKeyMappings.RotateRightShortCut:
                                    ConstructionHandler.HandleRotationTap(UserInputKeyMappings.RotateRightShortCut);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Public
        //LSF
        public static void ProcessInterfacer(AbilityDataItem facer)
        {
            Command com = RTSInterfacing.GetProcessInterfacer(facer);
            Send(com);
        }

        public static void SendCommand(Command curCom)
        {
            Send(curCom);
        }
        #endregion

        #region Private
        protected virtual void MoveCamera()
        {
        }

        protected virtual void RotateCamera()
        {
        }

        protected virtual void HandleSingleLeftClick()
        {
        }

        protected virtual void HandleSingleRightClick()
        {
        }

        protected virtual void MouseHover()
        {
        }

        private void OpenPauseMenu()
        {
            Time.timeScale = 0.0f;
            GetComponentInChildren<PauseMenu>().enabled = true;
            GetComponent<PlayerInputHelper>().enabled = false;
            Cursor.visible = true;
            GameResourceManager.MenuOpen = true;
        }

        //LSF
        private static void Send(Command com)
        {
            IsGathering = false;
            GlobalAgentController.SendCommand(com);
        }
        #endregion
    }
}