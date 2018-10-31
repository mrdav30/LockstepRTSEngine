using RTSLockstep;
using RTSLockstep.Data;
using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour
{
    #region Properties
    // public 
    public GUISkin resourceSkin, ordersSkin, selectBoxSkin;
    private Texture2D activeCursor;
    public Texture2D pointerCursor, selectCursor, leftCursor, rightCursor, upCursor, downCursor;
    public Texture2D[] moveCursors, attackCursors, harvestCursors, depositCursors, garrisonCursors;
    public GUISkin mouseCursorSkin;
    public Texture2D[] resources;
    // buildFrame provides a border around each buildImage
    // buildMask is used to show the user how far through the current build the object at the front of the queue is
    public Texture2D buttonHover, buttonClick;
    public Texture2D buildFrame, buildMask;
    public Texture2D smallButtonHover, smallButtonClick;
    public Texture2D rallyPointCursor;
    public Texture2D healthy, damaged, critical;
    public Texture2D[] resourceHealthBars;
    public GUISkin playerDetailsSkin;
    public AudioClip clickSound;
    public float clickVolume = 1.0f;

    private AgentCommander cachedCommander;
    private bool _cursorLocked;
    public bool mouseOverHud { get; private set; }
    private const int ORDERS_BAR_WIDTH = 150, RESOURCE_BAR_HEIGHT = 40;
    private const int SELECTION_NAME_HEIGHT = 15;
    private CursorState activeCursorState;
    private int currentFrame = 0;
    private Dictionary<ResourceType, long> resourceValues;
    private Dictionary<ResourceType, long> resourceLimits;
    private const int ICON_WIDTH = 32, ICON_HEIGHT = 32, TEXT_WIDTH = 128, TEXT_HEIGHT = 32;
    private static Dictionary<ResourceType, Texture2D> resourceImages;
    private LSAgent lastSelection;
    private float sliderValue;
    private const int BUILD_IMAGE_WIDTH = 64, BUILD_IMAGE_HEIGHT = 64;
    private int buildAreaHeight = 0;    // value for determining the height of the area we will draw the actions in
    private const int BUTTON_SPACING = 7;
    private const int SCROLL_BAR_WIDTH = 22;
    private const int BUILD_IMAGE_PADDING = 8;
    private CursorState previousCursorState;
    private AudioElement audioElement;
    #endregion

    #region MonoBehavior
    // Use this for initialization
    public void Setup()
    {
        cachedCommander = transform.GetComponentInParent<AgentCommander>();
        if (cachedCommander && cachedCommander.human)
        {
            //    agentController = PlayerManager.GetAgentController(cachedCommander.playerIndex);
            resourceValues = new Dictionary<ResourceType, long>();
            resourceLimits = new Dictionary<ResourceType, long>();
            resourceImages = new Dictionary<ResourceType, Texture2D>();
            for (int i = 0; i < resources.Length; i++)
            {
                switch (resources[i].name)
                {
                    case "Gold":
                        resourceImages.Add(ResourceType.Gold, resources[i]);
                        resourceValues.Add(ResourceType.Gold, 0);
                        resourceLimits.Add(ResourceType.Gold, 0);
                        break;
                    case "Ore":
                        resourceImages.Add(ResourceType.Ore, resources[i]);
                        resourceValues.Add(ResourceType.Ore, 0);
                        resourceLimits.Add(ResourceType.Ore, 0);
                        break;
                    case "Stone":
                        resourceImages.Add(ResourceType.Stone, resources[i]);
                        resourceValues.Add(ResourceType.Stone, 0);
                        resourceLimits.Add(ResourceType.Stone, 0);
                        break;
                    case "Wood":
                        resourceImages.Add(ResourceType.Wood, resources[i]);
                        resourceValues.Add(ResourceType.Wood, 0);
                        resourceLimits.Add(ResourceType.Wood, 0);
                        break;
                    case "Crystal":
                        resourceImages.Add(ResourceType.Crystal, resources[i]);
                        resourceValues.Add(ResourceType.Crystal, 0);
                        resourceLimits.Add(ResourceType.Crystal, 0);
                        break;
                    case "Food":
                        resourceImages.Add(ResourceType.Food, resources[i]);
                        resourceValues.Add(ResourceType.Food, 0);
                        resourceLimits.Add(ResourceType.Food, 0);
                        break;
                    case "Army":
                        resourceImages.Add(ResourceType.Provision, resources[i]);
                        resourceValues.Add(ResourceType.Provision, 0);
                        resourceLimits.Add(ResourceType.Provision, 0);
                        break;
                    default: break;
                }
            }

            Dictionary<ResourceType, Texture2D> resourceHealthBarTextures = new Dictionary<ResourceType, Texture2D>();
            for (int i = 0; i < resourceHealthBars.Length; i++)
            {
                switch (resourceHealthBars[i].name)
                {
                    case "ore":
                        resourceHealthBarTextures.Add(ResourceType.Ore, resourceHealthBars[i]);
                        break;
                    case "stone":
                        resourceHealthBarTextures.Add(ResourceType.Stone, resourceHealthBars[i]);
                        break;
                    case "gold":
                        resourceHealthBarTextures.Add(ResourceType.Gold, resourceHealthBars[i]);
                        break;
                    case "crystal":
                        resourceHealthBarTextures.Add(ResourceType.Crystal, resourceHealthBars[i]);
                        break;
                    case "food":
                        resourceHealthBarTextures.Add(ResourceType.Food, resourceHealthBars[i]);
                        break;
                    case "wood":
                        resourceHealthBarTextures.Add(ResourceType.Wood, resourceHealthBars[i]);
                        break;
                    default: break;
                }
            }
            ResourceManager.SetResourceHealthBarTextures(resourceHealthBarTextures);
            ResourceManager.StoreSelectBoxItems(selectBoxSkin, healthy, damaged, critical);
            buildAreaHeight = Screen.height - RESOURCE_BAR_HEIGHT - SELECTION_NAME_HEIGHT - 2 * BUTTON_SPACING;
            SetCursorState(CursorState.Select);

            List<AudioClip> sounds = new List<AudioClip>();
            List<float> volumes = new List<float>();
            sounds.Add(clickSound);
            volumes.Add(clickVolume);
            audioElement = new AudioElement(sounds, volumes, "HUD", null);
        }
    }

    public void Visualize()
    {
        mouseOverHud = !MouseInBounds() && activeCursorState != CursorState.PanRight && activeCursorState != CursorState.PanUp;
    }

    // Update is called once per frame
    public void doGUI()
    {
        if (cachedCommander && cachedCommander.human && cachedCommander == PlayerManager.MainController.Commander)
        {
            DrawPlayerDetails();
            if (!ResourceManager.MenuOpen)
            {
                if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive)
                {
                    DrawOrdersBar();
                }
                DrawResourcesBar();
                // call last to ensure that the custom mouse cursor is seen on top of everything
                DrawMouseCursor();
            }
        }
    }
    #endregion

    #region Public
    public bool MouseInBounds()
    {
        // Screen coordinates start in the lower-left corner of the screen
        // not the top-left of the screen like drawing coordinate do
        Vector3 mousePos = Input.mousePosition;
        int screenHeight = Screen.height - RESOURCE_BAR_HEIGHT;
        int screenWidth = Screen.width;
        if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive)
        {
            screenWidth = screenWidth - ORDERS_BAR_WIDTH;
        }
        bool insideHeight = mousePos.y >= 0 && mousePos.y <= screenHeight;
        bool insideWidth = mousePos.x >= 0 && mousePos.x <= screenWidth;
        return insideWidth && insideHeight;
    }

    public Rect GetPlayingArea()
    {
        int screenHeight = Screen.height - RESOURCE_BAR_HEIGHT;
        int screenWidth = Screen.width;
        if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive)
        {
            screenWidth = screenWidth - ORDERS_BAR_WIDTH;
        }

        return new Rect(0, RESOURCE_BAR_HEIGHT, screenWidth, screenHeight);
    }

    public void SetCursorState(CursorState newState)
    {
        if (_cursorLocked)
        {
            return;
        }

        if (activeCursorState != newState)
        {
            previousCursorState = activeCursorState;
        }
        activeCursorState = newState;
        switch (newState)
        {
            case CursorState.Pointer:
                activeCursor = pointerCursor;
                break;
            case CursorState.Select:
                activeCursor = selectCursor;
                break;
            case CursorState.Attack:
                currentFrame = (int)Time.time % attackCursors.Length;
                activeCursor = attackCursors[currentFrame];
                break;
            case CursorState.Harvest:
                currentFrame = (int)Time.time % harvestCursors.Length;
                activeCursor = harvestCursors[currentFrame];
                break;
            case CursorState.Deposit:
                currentFrame = (int)Time.time % depositCursors.Length;
                activeCursor = depositCursors[currentFrame];
                break;
            case CursorState.Move:
                currentFrame = (int)Time.time % moveCursors.Length;
                activeCursor = moveCursors[currentFrame];
                break;
            case CursorState.PanLeft:
                activeCursor = leftCursor;
                break;
            case CursorState.PanRight:
                activeCursor = rightCursor;
                break;
            case CursorState.PanUp:
                activeCursor = upCursor;
                break;
            case CursorState.PanDown:
                activeCursor = downCursor;
                break;
            case CursorState.RallyPoint:
                activeCursor = rallyPointCursor;
                break;
            default:
                break;
        }
    }

    public void SetCursorLock(bool lockState)
    {
        if (lockState)
        {
            _cursorLocked = true;
        }
        else
        {
            _cursorLocked = false;
        }
    }

    public void SetResourceValues(Dictionary<ResourceType, long> resourceValues, Dictionary<ResourceType, long> resourceLimits)
    {
        this.resourceValues = resourceValues;
        this.resourceLimits = resourceLimits;
    }

    public CursorState GetCursorState()
    {
        return activeCursorState;
    }

    public bool GetCursorLockState()
    {
        return _cursorLocked;
    }
    #endregion

    #region Private
    private void DrawOrdersBar()
    {
        if (Selector.MainSelectedAgent.GetAbility<Structure>() && Selector.MainSelectedAgent.GetAbility<Structure>().UnderConstruction())
        {
            return;
        }
        GUI.skin = ordersSkin;
        GUI.BeginGroup(new Rect(Screen.width - ORDERS_BAR_WIDTH - BUILD_IMAGE_WIDTH, RESOURCE_BAR_HEIGHT, ORDERS_BAR_WIDTH + BUILD_IMAGE_WIDTH, Screen.height - RESOURCE_BAR_HEIGHT));
        GUI.Box(new Rect(BUILD_IMAGE_WIDTH + SCROLL_BAR_WIDTH, 0, ORDERS_BAR_WIDTH, Screen.height - RESOURCE_BAR_HEIGHT), "");
        string selectionName = "";

        RTSAgent selectedAgent = Selector.MainSelectedAgent as RTSAgent;
        selectionName = selectedAgent.GetComponent<RTSAgent>().objectName;
        if (selectedAgent.IsOwnedBy(cachedCommander.CachedController))
        {
            // reset slider value if the selected object has changed
            if (lastSelection && lastSelection != Selector.MainSelectedAgent)
            {
                sliderValue = 0.0f;
            }
            if (selectedAgent.MyAgentType == AgentType.Unit && selectedAgent.GetAbility<Construct>())
            {
                DrawActions(selectedAgent.GetAbility<Construct>().GetBuildActions());
            }
            else if (selectedAgent.MyAgentType == AgentType.Building && selectedAgent.GetAbility<Spawner>())
            {
                DrawActions(selectedAgent.GetAbility<Spawner>().GetSpawnActions());
            }
            // store the current selection
            lastSelection = selectedAgent;
            if (lastSelection.MyAgentType == AgentType.Building)
            {
                if (lastSelection.GetAbility<Spawner>())
                {
                    DrawBuildQueue(lastSelection.GetAbility<Spawner>().getBuildQueueValues(), lastSelection.GetAbility<Spawner>().getBuildPercentage());
                }
            }
            if (lastSelection.MyAgentType == AgentType.Building || lastSelection.MyAgentType == AgentType.Unit)
            {
                DrawStandardOptions(lastSelection as RTSAgent);
            }
        }
        if (!selectionName.Equals(""))
        {
            int leftPos = BUILD_IMAGE_WIDTH + SCROLL_BAR_WIDTH / 2;
            int topPos = buildAreaHeight + BUTTON_SPACING;
            GUI.Label(new Rect(leftPos, topPos, ORDERS_BAR_WIDTH, SELECTION_NAME_HEIGHT), selectionName);
        }
        GUI.EndGroup();
    }

    private void DrawBuildQueue(string[] buildQueue, float buildPercentage)
    {
        for (int i = 0; i < buildQueue.Length; i++)
        {
            float topPos = i * BUILD_IMAGE_HEIGHT - (i + 1) * BUILD_IMAGE_PADDING;
            Rect buildPos = new Rect(BUILD_IMAGE_PADDING, topPos, BUILD_IMAGE_WIDTH, BUILD_IMAGE_HEIGHT);
            GUI.DrawTexture(buildPos, ResourceManager.GetBuildImage(buildQueue[i]));
            GUI.DrawTexture(buildPos, buildFrame);
            topPos += BUILD_IMAGE_PADDING;
            float width = BUILD_IMAGE_WIDTH - 2 * BUILD_IMAGE_PADDING;
            float height = BUILD_IMAGE_HEIGHT - 2 * BUILD_IMAGE_PADDING;
            if (i == 0)
            {
                // shrink the build mask on the item currently being built to give an idea of progress
                topPos += height * buildPercentage;
                height *= (1 - buildPercentage);
            }
            GUI.DrawTexture(new Rect(2 * BUILD_IMAGE_PADDING, topPos, width, height), buildMask);
        }
    }

    private void DrawStandardOptions(RTSAgent agent)
    {
        GUIStyle buttons = new GUIStyle();
        buttons.hover.background = smallButtonHover;
        buttons.active.background = smallButtonClick;
        GUI.skin.button = buttons;
        int leftPos = BUILD_IMAGE_WIDTH + SCROLL_BAR_WIDTH + BUTTON_SPACING;
        int topPos = buildAreaHeight - BUILD_IMAGE_HEIGHT / 2;
        int width = BUILD_IMAGE_WIDTH / 2;
        int height = BUILD_IMAGE_HEIGHT / 2;
        if (cachedCommander.CachedController.SelectedAgents.Count == 1 && GUI.Button(new Rect(leftPos, topPos, width, height), agent.destroyImage))
        {
            PlayClick();
            agent.Die();
        }
        if (agent.GetAbility<Spawner>() && agent.GetAbility<Spawner>().hasSpawnPoint())
        {
            leftPos += width + BUTTON_SPACING;
            if (GUI.Button(new Rect(leftPos, topPos, width, height), agent.GetAbility<Spawner>().rallyPointImage))
            {
                PlayClick();
                if (activeCursorState != CursorState.RallyPoint)
                {
                    agent.GetAbility<Spawner>().SetFlagState(FlagState.SettingFlag);
                    SetCursorState(CursorState.RallyPoint);
                    SetCursorLock(true);
                }
                else
                {
                    agent.GetAbility<Spawner>().SetFlagState(FlagState.FlagSet);
                    SetCursorLock(false);
                    SetCursorState(CursorState.Select);
                }
            }
        }
    }

    private void DrawResourcesBar()
    {
        GUI.skin = resourceSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, RESOURCE_BAR_HEIGHT));
        GUI.Box(new Rect(0, 0, Screen.width, RESOURCE_BAR_HEIGHT), "");
        int topPos = 4, iconLeft = 4, textLeft = 20;
        DrawResourceIcon(ResourceType.Gold, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Food, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Ore, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Stone, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Wood, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Crystal, iconLeft, textLeft, topPos);
        iconLeft += TEXT_WIDTH;
        textLeft += TEXT_WIDTH;
        DrawResourceIcon(ResourceType.Provision, iconLeft, textLeft, topPos);
        int padding = 7;
        int buttonWidth = ORDERS_BAR_WIDTH - 2 * padding - SCROLL_BAR_WIDTH;
        int buttonHeight = RESOURCE_BAR_HEIGHT - 2 * padding;
        int leftPos = Screen.width - ORDERS_BAR_WIDTH / 2 - buttonWidth / 2 + SCROLL_BAR_WIDTH / 2;
        Rect menuButtonPosition = new Rect(leftPos, padding, buttonWidth, buttonHeight);

        if (GUI.Button(menuButtonPosition, "Menu"))
        {
            PlayClick();
            Time.timeScale = 0.0f;
            PauseMenu pauseMenu = GetComponent<PauseMenu>();
            if (pauseMenu)
            {
                pauseMenu.enabled = true;
            }
            UserInputHelper userInput = cachedCommander.GetComponent<UserInputHelper>();
            if (userInput)
            {
                userInput.enabled = false;
            }
        }
        GUI.EndGroup();
    }

    private void DrawResourceIcon(ResourceType type, int iconLeft, int textLeft, int topPos)
    {
        Texture2D icon = resourceImages[type];
        string text = resourceValues[type].ToString() + "/" + resourceLimits[type].ToString();
        GUI.DrawTexture(new Rect(iconLeft, topPos, ICON_WIDTH, ICON_HEIGHT), icon);
        GUI.Label(new Rect(textLeft, topPos, TEXT_WIDTH, TEXT_HEIGHT), text);
    }

    private void DrawMouseCursor()
    {
        Cursor.visible = false;

        // toggle back to pointer if over hud
        if (mouseOverHud)
        {
            SelectionManager.SetSelectionLock(false);
            if (_cursorLocked)
                SetCursorLock(false);
            SetCursorState(CursorState.Pointer);
        }
        // toggle back to rally point after hovering over hud
        else if (activeCursorState == CursorState.Pointer && previousCursorState == CursorState.RallyPoint)
        {
            SetCursorState(CursorState.RallyPoint);
            SetCursorLock(true);
        }

        if (!cachedCommander.CachedBuilderManager.IsFindingBuildingLocation())
        {
            GUI.skin = mouseCursorSkin;
            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
            UpdateCursorAnimation();
            Rect cursorPosition = GetCursorDrawPosition();
            GUI.Label(cursorPosition, activeCursor);
            GUI.EndGroup();
        }
    }

    private void UpdateCursorAnimation()
    {
        // sequence animation for cursor (based on mor ethan one iamge for the cursor)
        // change once per second, loops through array of images
        if (activeCursorState == CursorState.Move)
        {
            currentFrame = (int)Time.time % moveCursors.Length;
            activeCursor = moveCursors[currentFrame];
        }
        else if (activeCursorState == CursorState.Attack)
        {
            currentFrame = (int)Time.time % attackCursors.Length;
            activeCursor = attackCursors[currentFrame];
        }
        else if (activeCursorState == CursorState.Harvest)
        {
            currentFrame = (int)Time.time % harvestCursors.Length;
            activeCursor = harvestCursors[currentFrame];
        }
    }

    private Rect GetCursorDrawPosition()
    {
        // set base position for custom cursor image
        float leftPos = Input.mousePosition.x;
        float topPos = Screen.height - Input.mousePosition.y; // screen draw coordinates are inverted
        // adjust position base on the type of cursor being shown
        if (activeCursorState == CursorState.PanRight)
        {
            leftPos = Screen.width - activeCursor.width;
        }
        else if (activeCursorState == CursorState.PanDown)
        {
            topPos = Screen.height - activeCursor.height;
        }
        else if (activeCursorState == CursorState.Move || activeCursorState == CursorState.Select || activeCursorState == CursorState.Harvest)
        {
            topPos -= activeCursor.height / 2;
            leftPos -= activeCursor.width / 2;
        }
        else if (activeCursorState == CursorState.RallyPoint)
        {
            topPos -= activeCursor.height;
        }
        return new Rect(leftPos, topPos, activeCursor.width, activeCursor.height);
    }

    private void DrawActions(string[] actions)
    {
        GUIStyle buttons = new GUIStyle();
        buttons.hover.background = buttonHover;
        buttons.active.background = buttonClick;
        GUI.skin.button = buttons;
        int numActions = actions.Length;
        //define the area to draw the actions inside
        GUI.BeginGroup(new Rect(BUILD_IMAGE_WIDTH, 0, ORDERS_BAR_WIDTH, buildAreaHeight));
        //draw scroll bar for the list of actions if need be
        if (numActions >= MaxNumRows(buildAreaHeight))
        {
            DrawSlider(buildAreaHeight, numActions / 2.0f);
        }
        //display possible actions as buttons and handle the button click for each
        for (int i = 0; i < numActions; i++)
        {
            int column = i % 2;
            int row = i / 2;
            Rect pos = GetButtonPos(row, column);
            Texture2D action = ResourceManager.GetBuildImage(actions[i]);
            if (action)
            {
                //create the button and handle the click of that button
                if (GUI.Button(pos, action))
                {
                    RTSAgent agent = Selector.MainSelectedAgent as RTSAgent;
                    if (agent)
                    {
                        PlayClick();

                        if (agent.MyAgentType == AgentType.Unit && agent.GetAbility<Construct>())
                        {
                            cachedCommander.CachedBuilderManager.CreateBuilding(agent, actions[i]);
                        }
                        else if (agent.MyAgentType == AgentType.Building && agent.GetAbility<Spawner>())
                        {
                            // send spawn command
                            Command spawnCom = new Command(AbilityDataItem.FindInterfacer("Spawner").ListenInputID);
                            spawnCom.Add<DefaultData>(new DefaultData(DataType.String, actions[i]));
                            UserInputHelper.SendCommand(spawnCom);
                        }
                    }
                }
            }
        }
        GUI.EndGroup();
    }

    private int MaxNumRows(int areaHeight)
    {
        return areaHeight / BUILD_IMAGE_HEIGHT;
    }

    private Rect GetButtonPos(int row, int column)
    {
        int left = SCROLL_BAR_WIDTH + column * BUILD_IMAGE_WIDTH;
        float top = row * BUILD_IMAGE_HEIGHT - sliderValue * BUILD_IMAGE_HEIGHT;
        return new Rect(left, top, BUILD_IMAGE_WIDTH, BUILD_IMAGE_HEIGHT);
    }

    private void DrawSlider(int groupHeight, float numRows)
    {
        // slider goes from 0 to the number of rows that do not fit on screen
        sliderValue = GUI.VerticalSlider(GetScrollPos(groupHeight), sliderValue, 0.0f, numRows - MaxNumRows(groupHeight));
    }

    private Rect GetScrollPos(int groupHeight)
    {
        return new Rect(BUTTON_SPACING, BUTTON_SPACING, SCROLL_BAR_WIDTH, groupHeight - 2 * BUTTON_SPACING);
    }

    private void DrawPlayerDetails()
    {
        GUI.skin = playerDetailsSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        float height = ResourceManager.TextHeight;
        float leftPos = ResourceManager.Padding;
        float topPos = Screen.height - height - ResourceManager.Padding;
        Texture2D avatar = PlayerManager.GetPlayerAvatar();
        if (avatar)
        {
            //we want the texture to be drawn square at all times
            GUI.DrawTexture(new Rect(leftPos, topPos, height, height), avatar);
            leftPos += height + ResourceManager.Padding;
        }
        float minWidth = 0, maxWidth = 0;
        string playerName = PlayerManager.GetPlayerName();
        playerDetailsSkin.GetStyle("label").CalcMinMaxWidth(new GUIContent(playerName), out minWidth, out maxWidth);
        GUI.Label(new Rect(leftPos, topPos, maxWidth, height), playerName);
        GUI.EndGroup();
    }

    private void PlayClick()
    {
        if (audioElement != null) audioElement.Play(clickSound);
    }
    #endregion
}
