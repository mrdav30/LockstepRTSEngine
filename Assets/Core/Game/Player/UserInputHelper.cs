using UnityEngine;
using RTSLockstep;
using RTSLockstep.Data;
using RTSLockstep.UI;

public class UserInputHelper : BehaviourHelper
{
    #region Properties
    //LSF
    private static RTSGUIManager _guiManager;
    public static RTSGUIManager GUIManager
    {
        get
        {
            return _guiManager;
        }
        set
        {
            _guiManager = value;
        }
    }

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

    [SerializeField]
    private GUIStyle _boxStyle;

    private static bool Setted = false;
    private static Command curCom;

    private AgentCommander cachedCommander;
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

        cachedCommander = PlayerManager.MainController.Commander;
    }

    protected override void OnInitialize()
    {
        if (!Setted)
            Setup();
        SelectionManager.Initialize();
        RTSInterfacing.Initialize();
        IsGathering = false;
        CurrentInterfacer = null;
    }

    protected override void OnVisualize()
    {
        if (cachedCommander.CachedBuilderManager.IsFindingBuildingLocation())
        {
            SelectionManager.CanBox = false;
        }
        else
        {
            SelectionManager.CanBox = true;
        }
        //Update the SelectionManager which handles box-selection.
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
            MouseActivity();
        }
        // }
    }

    protected virtual void OnGUI()
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
            cachedCommander.CachedHud.SetCursorState(CursorState.PanLeft);
            mouseScroll = true;
        }
        else if (xpos <= Screen.width && xpos > Screen.width - ResourceManager.ScrollWidth)
        {
            movement.x += ResourceManager.ScrollSpeed;
            cachedCommander.CachedHud.SetCursorState(CursorState.PanRight);
            mouseScroll = true;
        }

        //vertical camera movement
        if (ypos >= 0 && ypos < ResourceManager.ScrollWidth)
        {
            movement.z -= ResourceManager.ScrollSpeed;
            cachedCommander.CachedHud.SetCursorState(CursorState.PanDown);
            mouseScroll = true;
        }
        else if (ypos <= Screen.height && ypos > Screen.height - ResourceManager.ScrollWidth)
        {
            movement.z += ResourceManager.ScrollSpeed;
            cachedCommander.CachedHud.SetCursorState(CursorState.PanUp);
            mouseScroll = true;
        }

        // make sure movement is in the direction the camera is pointing
        // but ignore the vertical tilt of the camera to get sensible scrolling
        movement = Camera.main.transform.TransformDirection(movement);
        movement.y = 0;

        // away from ground movement
        movement.y -= ResourceManager.ScrollSpeed * Input.GetAxis("Mouse ScrollWheel");

        // calculate desiered camera position based on received input
        Vector3 origin = Camera.main.transform.position;
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
            Camera.main.transform.position = Vector3.MoveTowards(origin, destination, Time.deltaTime * ResourceManager.ScrollSpeed);
        }

        if (!SelectionManager.MousedAgent && !mouseScroll)
        {
            cachedCommander.CachedHud.SetCursorState(CursorState.Select);
        }
    }

    private void RotateCamera()
    {
        Vector3 origin = Camera.main.transform.eulerAngles;
        Vector3 destination = origin;

        // detect rotation amount if ALT is being heald and the Right mouse button is down
        if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButton(1))
        {
            destination.x -= Input.GetAxis("Mouse Y") * ResourceManager.RotateAmount;
            destination.y += Input.GetAxis("Mouse X") * ResourceManager.RotateAmount;
        }

        // if a change in position is detected, perform necessary update
        if (destination != origin)
        {
            Camera.main.transform.eulerAngles = Vector3.MoveTowards(origin, destination, Time.deltaTime * ResourceManager.RotateSpeed);
        }
    }

    private void MouseActivity()
    {
        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseClick();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            RightMouseClick();
        }
        MouseHover();
    }

    private void LeftMouseClick()
    {
        if (cachedCommander.CachedHud.MouseInBounds())
        {
            if (cachedCommander.CachedBuilderManager.IsFindingBuildingLocation())
            {
                if (cachedCommander.CachedBuilderManager.CanPlaceBuilding())
                {
                    cachedCommander.CachedBuilderManager.StartConstruction();
                }
            }
            else
            {
                if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsOwnedBy(cachedCommander.CachedController))
                {
                    if (Selector.MainSelectedAgent.GetAbility<Spawner>() != null && Selector.MainSelectedAgent.GetAbility<Spawner>().GetFlagState() == FlagState.SettingFlag)
                    {
                        //call harvest command
                        SelectionManager.CanClearSelection = false;
                        ProcessInterfacer((QuickRally));
                    }
                    else
                    {
                        SelectionManager.CanClearSelection = true;
                    }
                } else
                {
                    SelectionManager.CanClearSelection = true;
                }
            }
        }
    }

    private void RightMouseClick()
    {
        if (cachedCommander.CachedHud.MouseInBounds() && !Input.GetKey(KeyCode.LeftAlt) && Selector.MainSelectedAgent)
        {
            if (cachedCommander.CachedBuilderManager.IsFindingBuildingLocation())
            {
                cachedCommander.CachedBuilderManager.CancelBuildingPlacement();
            }
            else
            {
                if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsOwnedBy(cachedCommander.CachedController))
                {
                    if (Selector.MainSelectedAgent.GetAbility<Spawner>() != null && Selector.MainSelectedAgent.GetAbility<Spawner>().GetFlagState() == FlagState.SettingFlag)
                    {
                        Selector.MainSelectedAgent.GetAbility<Spawner>().SetFlagState(FlagState.SetFlag);
                        cachedCommander.CachedHud.SetCursorState(CursorState.Select);
                    }

                    if (RTSInterfacing.MousedAgent.IsNotNull())
                    {
                        if (Selector.MainSelectedAgent.GetAbility<Harvest>() != null && (RTSInterfacing.MousedAgent.MyAgentType == AgentType.Resource
                                || Selector.MainSelectedAgent.GetAbility<Harvest>().GetCurrentLoad() > 0 && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Building))
                        {
                            //call harvest command
                            ProcessInterfacer((QuickHarvest));
                        }
                        else if (Selector.MainSelectedAgent.GetAbility<Build>() != null && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Building
                        else if (Selector.MainSelectedAgent.GetAbility<Construct>() && RTSInterfacing.MousedAgent.MyAgentType == AgentType.Building
                                && RTSInterfacing.MousedAgent.IsOwnedBy(PlayerManager.MainController))
                        {
                            //call build command
                            ProcessInterfacer((QuickBuild));
                        }
                        else if (Selector.MainSelectedAgent.GetAbility<Attack>() != null && RTSInterfacing.MousedAgent.MyAgentType != AgentType.Resource)
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
        if (cachedCommander.CachedHud.MouseInBounds())
        {
            if (cachedCommander.CachedBuilderManager.IsFindingBuildingLocation())
            {
                cachedCommander.CachedBuilderManager.FindBuildingLocation();
            }
            else if (Selector.MainSelectedAgent)
            {
                Selector.MainSelectedAgent.GetAbility<SelectionController>().HandleHighlightedChange();
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
