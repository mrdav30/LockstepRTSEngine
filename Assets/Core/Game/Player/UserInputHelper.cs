using RotaryHeart.Lib.SerializableDictionary;
using RTSLockstep;
using RTSLockstep.Data;
using RTSLockstep.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles user input outside of HUD
/// </summary>

[Serializable]
public class UserInputKeys : SerializableDictionaryBase<UserInputKeyMappings, KeyCode> { };

public class UserInputHelper : BehaviourHelper
{
    #region Properties
#pragma warning disable 0649
    [SerializeField]
    private GUIStyle _boxStyle;
    [SerializeField]
    private UserInputKeys userInputKeys;
#pragma warning restore 0649
    public static RTSGUIManager GUIManager;
    /// <summary>
    /// The current ability to cast. Set this to a non-null value to automatically start the gathering process.
    /// </summary>
    private static AbilityDataItem _currentInterfacer;
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

    public static AbilityDataItem QuickMove;
    public static AbilityDataItem QuickTarget;
    public static AbilityDataItem QuickHarvest;
    public static AbilityDataItem QuickRally;

    public static event Action OnSingleLeftTapDown;
    public static event Action OnLeftTapUp;
    public static event Action OnLeftTapHoldDown;
    public static event Action OnSingleRightTapDown;
    public static event Action OnDoubleLeftTapDown;

    //Defines the maximum time between two taps to make it double tap
    private static float tapThreshold = 0.25f;
    private static float tapTimer = 0.0f;
    private static bool tap = false;

    private static bool _isGathering;
    public static bool IsGathering
    {
        get { return _isGathering; }
        private set
        {
            SelectionManager.IsGathering = value;
            _isGathering = value;
        }
    }

    private static bool _isDragging = false;

    private static bool Setted = false;
    private static Command curCom;

    // limits for angle in tilt x axis
    private float yaw = 0f;
    private float pitch = 0f;
    private float minPitch = -30f;
    private float maxPitch = 60f;
    #endregion

    #region BehaviorHelper
    protected void Setup()
    {
        QuickMove = AbilityDataItem.FindInterfacer("Move");
        QuickTarget = AbilityDataItem.FindInterfacer("Attack");
        QuickHarvest = AbilityDataItem.FindInterfacer("Harvest");
        QuickRally = AbilityDataItem.FindInterfacer("Rally");

        if (GUIManager == null)
        {
            GUIManager = new RTSGUIManager();
        }

        Setted = true;

        // set to starting camera angels
        yaw = GUIManager.MainCam.transform.eulerAngles.y;
        pitch = GUIManager.MainCam.transform.eulerAngles.x;
    }

    protected override void OnInitialize()
    {
        if (!Setted)
            Setup();
        SelectionManager.Initialize();
        RTSInterfacing.Initialize();
        ConstructionHandler.Initialize();
        IsGathering = false;
        CurrentInterfacer = null;
    }

    protected override void OnVisualize()
    {
        if (ConstructionHandler.IsFindingBuildingLocation())
        {
            SelectionManager.CanBox = false;
        }
        else
        {
            SelectionManager.CanBox = true;
        }
        //Update the SelectionManager which handles mouse-selection.
        SelectionManager.Update();
        //Update RTSInterfacing, a useful tool that automatically generates useful data for user-interfacing
        RTSInterfacing.Visualize();
        //Update Construction handler which handles placing buildings on a grid
        ConstructionHandler.Visualize();

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
            bool mouseOverHud = PlayerManager.MainController.GetCommanderHUD()._mouseOverHud;
            if (!mouseOverHud)
            {
                // detect rotation amount if no agents selected & Right mouse button is down
                if (PlayerManager.MainController.SelectedAgents.Count <= 0 && Input.GetMouseButton(1)
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

                if (tap == true && Time.time > tapTimer + tapThreshold)
                {
                    tap = false;

                    // left click hold action
                    if (Input.GetMouseButton(0))
                    {
                        if (OnLeftTapHoldDown != null)
                        {
                            _isDragging = true;
                            OnLeftTapHoldDown();
                        }
                    }
                    else
                    {
                        // left click action
                        HandleSingleLeftClick();
                        OnSingleLeftTapDown?.Invoke();
                    }
                }

                // other defined keys
                foreach (KeyValuePair<UserInputKeyMappings, KeyCode> inputKey in userInputKeys)
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

    protected override void doGUI()
    {
        if (_boxStyle == null)
        {
            return;
        }
        this.DrawBox(_boxStyle);
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
    private void MoveCamera()
    {
        float xpos = Input.mousePosition.x;
        float ypos = Input.mousePosition.y;
        Vector3 movement = new Vector3(0, 0, 0);

        bool mouseScroll = false;

        //horizontal camera movement
        if (xpos >= 0 && xpos < GameResourceManager.ScrollWidth)
        {
            movement.x -= GameResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanLeft);
            mouseScroll = true;
        }
        else if (xpos <= Screen.width && xpos > Screen.width - GameResourceManager.ScrollWidth)
        {
            movement.x += GameResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanRight);
            mouseScroll = true;
        }

        //vertical camera movement
        if (ypos >= 0 && ypos < GameResourceManager.ScrollWidth)
        {
            movement.z -= GameResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanDown);
            mouseScroll = true;
        }
        else if (ypos <= Screen.height && ypos > Screen.height - GameResourceManager.ScrollWidth)
        {
            movement.z += GameResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanUp);
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
            && !PlayerManager.MainController.GetCommanderHUD().GetCursorLockState()
            && !PlayerManager.MainController.GetCommanderHUD()._mouseOverHud)
        {
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.Select);
        }
    }

    private void RotateCamera()
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

    private void HandleSingleLeftClick()
    {
        if (PlayerManager.MainController.GetCommanderHUD().MouseInBounds())
        {
            if (!ConstructionHandler.IsFindingBuildingLocation())
            {
                if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.MainController))
                {
                    if (Selector.MainSelectedAgent.GetAbility<Rally>() != null && Selector.MainSelectedAgent.GetAbility<Rally>().GetFlagState() == FlagState.SettingFlag)
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

    private void HandleSingleRightClick()
    {
        if (PlayerManager.MainController.GetCommanderHUD().MouseInBounds() 
            && Selector.MainSelectedAgent
            && !ConstructionHandler.IsFindingBuildingLocation())
        {
            if (Selector.MainSelectedAgent
                && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.MainController))
            {
                if (Selector.MainSelectedAgent.GetAbility<Rally>()
                    && !Selector.MainSelectedAgent.GetAbility<Structure>().NeedsConstruction)
                {
                    if (Selector.MainSelectedAgent.GetAbility<Rally>().GetFlagState() == FlagState.SettingFlag)
                    {
                        Selector.MainSelectedAgent.GetAbility<Rally>().SetFlagState(FlagState.SetFlag);
                        PlayerManager.MainController.GetCommanderHUD().SetCursorLock(false);
                        PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.Select);
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
                        && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Resource)
                    {
                        //call harvest command
                        ProcessInterfacer((QuickHarvest));
                    }
                    // moused agent is a building and owned by current player
                    else if (RTSInterfacing.MousedAgent.MyAgentType == AgentType.Building
                        && RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.MainController))
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
                        && !RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.MainController)
                        && RTSInterfacing.MousedAgent.MyAgentType != AgentType.Resource)
                    {
                        //If the selected agent has Attack (the ability behind attacking) and the mouse is over an agent, send a target command - right clicking on a unit
                        ProcessInterfacer((QuickTarget));
                    }
                }
                else
                {
                    // If there is no agent under the mouse or the selected agent doesn't have Attack, send a Move command - right clicking on terrain
                    // stop casting all abilities
                    Selector.MainSelectedAgent.StopCast();
                    ProcessInterfacer((QuickMove));
                }
            }
        }
    }

    private void MouseHover()
    {
        if (PlayerManager.MainController.GetCommanderHUD().MouseInBounds()
            && Selector.MainSelectedAgent
                && Selector.MainSelectedAgent.IsActive
                && Selector.MainSelectedAgent.GetAbility<Move>()
                && Selector.MainSelectedAgent.GetAbility<Move>().CanMove
                && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.MainController)
                && !SelectionManager.MousedAgent)
        {
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.Move);
        }
    }

    private void OpenPauseMenu()
    {
        Time.timeScale = 0.0f;
        GetComponentInChildren<PauseMenu>().enabled = true;
        GetComponent<UserInputHelper>().enabled = false;
        Cursor.visible = true;
        GameResourceManager.MenuOpen = true;
    }

    //LSF
    private static void Send(Command com)
    {
        IsGathering = false;
        PlayerManager.SendCommand(com);
    }

    #endregion

    #region Protected
    //LSF
    protected virtual void DrawBox(GUIStyle style)
    {
        SelectionManager.DrawBox(style);
    }
    #endregion
}
