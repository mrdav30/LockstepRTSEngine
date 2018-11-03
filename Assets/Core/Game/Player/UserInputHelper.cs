using RTSLockstep;
using RTSLockstep.Data;
using RTSLockstep.UI;
using UnityEngine;

public class UserInputHelper : BehaviourHelper
{
    #region Properties
#pragma warning disable 0649
    [SerializeField]
    private GUIStyle _boxStyle;
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
    public static AbilityDataItem QuickBuild;
    public static AbilityDataItem QuickRally;

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

    private static bool Setted = false;
    private static Command curCom;
    #endregion

    #region BehaviorHelper
    protected void Setup()
    {
        QuickMove = AbilityDataItem.FindInterfacer("Move");
        QuickTarget = AbilityDataItem.FindInterfacer("Attack");
        QuickHarvest = AbilityDataItem.FindInterfacer("Harvest");
        QuickBuild = AbilityDataItem.FindInterfacer("Construct");
        QuickRally = AbilityDataItem.FindInterfacer("Spawner");

        if (GUIManager == null)
            GUIManager = new RTSGUIManager();
        Setted = true;
    }

    protected override void OnInitialize()
    {
        if (!Setted)
            Setup();
        SelectionManager.Initialize();
        SelectionManager.OnSingleLeftTap += HandleSingleLeftClick;
        SelectionManager.OnSingleRightTap += HandleSingleRightClick;
        RTSInterfacing.Initialize();
        IsGathering = false;
        CurrentInterfacer = null;
    }

    protected override void OnVisualize()
    {
        if (PlayerManager.MainController.GetCommanderBuilderManager().IsFindingBuildingLocation())
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
            RotateCamera();
            MouseHover();
        }
    }

    protected override void doGUI()
    {
        if (_boxStyle == null) return;
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
        if (xpos >= 0 && xpos < ResourceManager.ScrollWidth)
        {
            movement.x -= ResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanLeft);
            mouseScroll = true;
        }
        else if (xpos <= Screen.width && xpos > Screen.width - ResourceManager.ScrollWidth)
        {
            movement.x += ResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanRight);
            mouseScroll = true;
        }

        //vertical camera movement
        if (ypos >= 0 && ypos < ResourceManager.ScrollWidth)
        {
            movement.z -= ResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanDown);
            mouseScroll = true;
        }
        else if (ypos <= Screen.height && ypos > Screen.height - ResourceManager.ScrollWidth)
        {
            movement.z += ResourceManager.ScrollSpeed;
            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.PanUp);
            mouseScroll = true;
        }

        // make sure movement is in the direction the camera is pointing
        // but ignore the vertical tilt of the camera to get sensible scrolling
        movement = GUIManager.MainCam.transform.TransformDirection(movement);
        movement.y = 0;

        // away from ground movement
        movement.y -= ResourceManager.ScrollSpeed * Input.GetAxis("Mouse ScrollWheel");

        // calculate desiered camera position based on received input
        Vector3 origin = GUIManager.MainCam.transform.position;
        Vector3 destination = origin;
        destination.x += movement.x;
        destination.y += movement.y;
        destination.z += movement.z;

        // limit away from ground movement to be between a minimum and maximum distance
        if (destination.y > ResourceManager.MaxCameraHeight)
        {
            destination.y = ResourceManager.MaxCameraHeight;
        }
        else if (destination.y < ResourceManager.MinCameraHeight)
        {
            destination.y = ResourceManager.MinCameraHeight;
        }

        // if a change in position is destected, perform necessary update
        if (destination != origin)
        {
            GUIManager.MainCam.transform.position = Vector3.MoveTowards(origin, destination, Time.deltaTime * ResourceManager.ScrollSpeed);
        }

        //!Selector.MainSelectedAgent && 
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
        Vector3 destination = origin;

        // detect rotation amount if no agents selected & Right mouse button is down
        if (PlayerManager.MainController.SelectedAgents.Count <= 0 && Input.GetMouseButton(1)
            || Input.GetMouseButton(1) && Input.GetKeyDown(KeyCode.LeftAlt))
        {
            destination.x -= Input.GetAxis("Mouse Y") * ResourceManager.RotateAmount;
            destination.y += Input.GetAxis("Mouse X") * ResourceManager.RotateAmount;
        }

        // if a change in position is detected, perform necessary update
        if (destination != origin)
        {
            GUIManager.MainCam.transform.eulerAngles = Vector3.MoveTowards(origin, destination, Time.deltaTime * ResourceManager.RotateSpeed);
        }
    }

    private void HandleSingleLeftClick()
    {
        if (PlayerManager.MainController.GetCommanderHUD().MouseInBounds())
        {
            if (PlayerManager.MainController.GetCommanderBuilderManager().IsFindingBuildingLocation())
            {
                if (PlayerManager.MainController.GetCommanderBuilderManager().CanPlaceBuilding())
                {
                    PlayerManager.MainController.GetCommanderBuilderManager().StartConstruction();
                }
            }
            else
            {
                if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.MainController))
                {
                    if (Selector.MainSelectedAgent.GetAbility<Spawner>() != null && Selector.MainSelectedAgent.GetAbility<Spawner>().GetFlagState() == FlagState.SettingFlag)
                    {
                        //call harvest command
                        SelectionManager.SetSelectionLock(false);
                        ProcessInterfacer((QuickRally));
                    }
                    else
                    {
                        SelectionManager.SetSelectionLock(true);
                    }
                }
                else
                {
                    SelectionManager.SetSelectionLock(true);
                }
            }
        }
    }

    private void HandleSingleRightClick()
    {
        if (PlayerManager.MainController.GetCommanderHUD().MouseInBounds()
            && !Input.GetKey(KeyCode.LeftAlt) && Selector.MainSelectedAgent)
        {
            if (PlayerManager.MainController.GetCommanderBuilderManager().IsFindingBuildingLocation())
            {
                PlayerManager.MainController.GetCommanderBuilderManager().CancelBuildingPlacement();
            }
            else
            {
                if (Selector.MainSelectedAgent
                    && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.MainController))
                {
                    if (Selector.MainSelectedAgent.GetAbility<Spawner>())
                    {
                        if (Selector.MainSelectedAgent.GetAbility<Spawner>().GetFlagState() == FlagState.SettingFlag)
                        {
                            Selector.MainSelectedAgent.GetAbility<Spawner>().SetFlagState(FlagState.SetFlag);
                            PlayerManager.MainController.GetCommanderHUD().SetCursorLock(false);
                            PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.Select);
                        }
                        else
                        {
                            Vector2d rallyPoint = RTSInterfacing.GetWorldPosD(Input.mousePosition);
                            Selector.MainSelectedAgent.GetAbility<Spawner>().SetRallyPoint(rallyPoint.ToVector3());
                        }
                    }

                    if (RTSInterfacing.MousedAgent.IsNotNull())
                    {
                        // if moused agent is a resource, send selected agent to harvest
                        if (Selector.MainSelectedAgent.GetAbility<Harvest>() && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Resource)
                        {
                            //call harvest command
                            ProcessInterfacer((QuickHarvest));
                        }
                        // if moused agent is a harvester resource deposit, call harvest command to initiate deposit
                        else if (Selector.MainSelectedAgent.GetAbility<Harvest>() && Selector.MainSelectedAgent.GetAbility<Harvest>().GetCurrentLoad() > 0
                            && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Building
                            && !RTSInterfacing.MousedAgent.GetAbility<Structure>().UnderConstruction()
                            && RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.MainController))
                        {
                            //call harvest command 
                            ProcessInterfacer((QuickHarvest));
                        }
                        else if (Selector.MainSelectedAgent.GetAbility<Construct>() && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Building
                                && RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.MainController))
                        {
                            //call build command
                            ProcessInterfacer((QuickBuild));
                        }
                        else if (Selector.MainSelectedAgent.GetAbility<Attack>()
                            && RTSInterfacing.MousedAgent.MyAgentType != AgentType.Resource)
                        {
                            //If the selected agent has Attack (the ability behind attacking) and the mouse is over an agent, send a target command - right clicking on a unit
                            ProcessInterfacer((QuickTarget));
                        }
                    }
                    else
                    {
                        //If there is no agent under the mouse or the selected agent doesn't have Attack, send a Move command - right clicking on terrain
                        Selector.MainSelectedAgent.StopCast();
                        ProcessInterfacer((QuickMove));
                    }
                }
            }
        }
    }

    private void MouseHover()
    {
        if (PlayerManager.MainController.GetCommanderHUD().MouseInBounds())
        {
            if (PlayerManager.MainController.GetCommanderBuilderManager().IsFindingBuildingLocation())
            {
                PlayerManager.MainController.GetCommanderBuilderManager().FindBuildingLocation();
            }
            else if (Selector.MainSelectedAgent
                && Selector.MainSelectedAgent.IsActive
                && Selector.MainSelectedAgent.GetAbility<Move>()
                && Selector.MainSelectedAgent.GetAbility<Move>().CanMove
                && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.MainController)
                && !SelectionManager.MousedAgent)
            {
                PlayerManager.MainController.GetCommanderHUD().SetCursorState(CursorState.Move);
            }
        }
    }

    private void OpenPauseMenu()
    {
        Time.timeScale = 0.0f;
        GetComponentInChildren<PauseMenu>().enabled = true;
        GetComponent<UserInputHelper>().enabled = false;
        Cursor.visible = true;
        ResourceManager.MenuOpen = true;
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
