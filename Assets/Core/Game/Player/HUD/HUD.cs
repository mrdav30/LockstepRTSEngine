using System.Collections.Generic;
using UnityEngine;

using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Menu.UI;
using RTSLockstep.Utility;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.BuildSystem;
using RTSLockstep.Data;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using RTSLockstep.LSResources.Audio;

namespace RTSLockstep.Player
{
    public class HUD : MonoBehaviour
    {
        #region Properties
        // public 
        public GUISkin RawMaterialSkin, OrdersSkin, SelectBoxSkin;
        private Texture2D _activeCursor;
        public Texture2D PointerCursor, SelectCursor, LeftCursor, RightCursor, UpCursor, DownCursor;
        public Texture2D[] MoveCursors, AttackCursors, HarvestCursors, DepositCursors, ConstructCursors, GarrisonCursors;
        public GUISkin MouseCursorSkin;
        public RawMaterialsHUD RawMaterialIcons;
        // buildFrame provides a border around each buildImage
        // buildMask is used to show the user how far through the current build the object at the front of the queue is
        public Texture2D ButtonHover, ButtonClick;
        public Texture2D BuildFrame, BuildMask;
        public Texture2D SmallButtonHover, SmallButtonClick;
        public Texture2D RallyPointCursor;
        public Texture2D Healthy, Damaged, Critical;
        public Material NotAllowedMaterial, AllowedMaterial;
        public GUISkin PlayerDetailsSkin;
        public AudioClip ClickSound;
        public float ClickVolume = 1.0f;

        private LSPlayer _cachedPlayer;
        private RawMaterialManager _cachedResourceManager;
        private bool _cursorLocked;
        public bool IsMouseOverHUD { get; private set; }
        private const int _ordersBarWidth = 150, _resourceBarHeight = 40;
        private const int _selectionNameHeight = 15;
        private CursorState _activeCursorState;
        private int _currentFrame = 0;
        private const int _iconWidth = 32, _iconHeight = 32, _textWidth = 128, _textHeight = 32;
        private LSAgent _lastSelection;
        private float _sliderValue;
        private const int _buildImageWidth = 64, _buildImageHeight = 64;
        // value for determining the height of the area we will draw the actions in
        private int _buildAreaHeight = 0;
        private int _statusAreaHeight = 0;
        private const int _buttonSpacing = 7;
        private const int _scrollBarWidth = 22;
        private const int _buildImagePadding = 8;
        private AudioElement _audioElement;
        #endregion

        #region MonoBehavior
        // Use this for initialization
        public void OnSetup()
        {
            _cachedPlayer = GetComponentInParent<LSPlayer>();
            _cachedResourceManager = GetComponentInParent<RawMaterialManager>();

            if (_cachedPlayer && _cachedPlayer.IsCurrentPlayer)
            {
                Dictionary<RawMaterialType, Texture2D> resourceHealthBarTextures = new Dictionary<RawMaterialType, Texture2D>();
                foreach (var keyValuePair in RawMaterialIcons)
                {
                    resourceHealthBarTextures.Add(keyValuePair.Key, keyValuePair.Value.RawMaterialHealthBar);
                }

                GameResourceManager.SetResourceHealthBarTextures(resourceHealthBarTextures);
                GameResourceManager.StoreSelectBoxItems(SelectBoxSkin, Healthy, Damaged, Critical);
                GameResourceManager.StoreConstructionMaterials(AllowedMaterial, NotAllowedMaterial);
                _buildAreaHeight = Screen.height - _resourceBarHeight - _selectionNameHeight - 2 * _buttonSpacing;
                SetCursorState(CursorState.Select);

                List<AudioClip> sounds = new List<AudioClip>();
                List<float> volumes = new List<float>();
                sounds.Add(ClickSound);
                volumes.Add(ClickVolume);
                _audioElement = new AudioElement(sounds, volumes, "HUD", null);
            }
        }

        public void OnVisualize()
        {
            IsMouseOverHUD = !MouseInBounds() && _activeCursorState != CursorState.PanRight && _activeCursorState != CursorState.PanUp;
        }

        // Update is called once per frame
        public void OnUpdateGUI()
        {
            if (_cachedPlayer
                && _cachedPlayer.IsCurrentPlayer
                && PlayerManager.CurrentPlayerController.IsNotNull()
                && _cachedPlayer == PlayerManager.CurrentPlayerController.ControllingPlayer)
            {
                DrawPlayerDetails();
                if (!GameResourceManager.MenuOpen)
                {
                    if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive)
                    {
                        DrawOrdersBar();
                    }
                    DrawRawMaterialBar();
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
            int screenHeight = Screen.height - _resourceBarHeight;
            int screenWidth = Screen.width;
            if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive)
            {
                screenWidth -= _ordersBarWidth;
            }

            bool insideHeight = mousePos.y >= 0 && mousePos.y <= screenHeight;
            bool insideWidth = mousePos.x >= 0 && mousePos.x <= screenWidth;
            return insideWidth && insideHeight;
        }

        public Rect GetPlayingArea()
        {
            int screenHeight = Screen.height - _resourceBarHeight;
            int screenWidth = Screen.width;
            if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsActive)
            {
                screenWidth -= _ordersBarWidth;
            }

            return new Rect(0, _resourceBarHeight, screenWidth, screenHeight);
        }

        public void SetCursorState(CursorState newState)
        {
            if (_cursorLocked)
            {
                return;
            }

            _activeCursorState = newState;
            switch (newState)
            {
                case CursorState.Pointer:
                    _activeCursor = PointerCursor;
                    break;
                case CursorState.Select:
                    _activeCursor = SelectCursor;
                    break;
                case CursorState.Attack:
                    _currentFrame = (int)Time.time % AttackCursors.Length;
                    _activeCursor = AttackCursors[_currentFrame];
                    break;
                case CursorState.Harvest:
                    _currentFrame = (int)Time.time % HarvestCursors.Length;
                    _activeCursor = HarvestCursors[_currentFrame];
                    break;
                case CursorState.Deposit:
                    _currentFrame = (int)Time.time % DepositCursors.Length;
                    _activeCursor = DepositCursors[_currentFrame];
                    break;
                case CursorState.Construct:
                    _currentFrame = (int)Time.time % ConstructCursors.Length;
                    _activeCursor = ConstructCursors[_currentFrame];
                    break;
                case CursorState.Move:
                    _currentFrame = (int)Time.time % MoveCursors.Length;
                    _activeCursor = MoveCursors[_currentFrame];
                    break;
                case CursorState.PanLeft:
                    _activeCursor = LeftCursor;
                    break;
                case CursorState.PanRight:
                    _activeCursor = RightCursor;
                    break;
                case CursorState.PanUp:
                    _activeCursor = UpCursor;
                    break;
                case CursorState.PanDown:
                    _activeCursor = DownCursor;
                    break;
                case CursorState.RallyPoint:
                    _activeCursor = RallyPointCursor;
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

        public CursorState GetCursorState()
        {
            return _activeCursorState;
        }

        public bool GetCursorLockState()
        {
            return _cursorLocked;
        }
        #endregion

        #region Private
        private void DrawOrdersBar()
        {
            LSAgent selectedAgent = Selector.MainSelectedAgent as LSAgent;

            GUI.skin = OrdersSkin;
            GUI.BeginGroup(new Rect(Screen.width - _ordersBarWidth - _buildImageWidth, _resourceBarHeight, _ordersBarWidth + _buildImageWidth, Screen.height - _resourceBarHeight));
            GUI.Box(new Rect(_buildImageWidth + _scrollBarWidth, 0, _ordersBarWidth, Screen.height - _resourceBarHeight), "");
            string selectionName = selectedAgent.GetComponent<LSAgent>().AgentDescription;

            // reset height of agent status
            _statusAreaHeight = 0;
            if (_cachedPlayer.GetController().SelectedAgents.Count == 1)
            {
                DrawStatusBar(selectedAgent.GetAbility<AgentStats>());
            }

            if (selectedAgent.IsOwnedBy(_cachedPlayer.GetController()))
            {
                // reset slider value if the selected object has changed
                if (_lastSelection && _lastSelection != selectedAgent)
                {
                    _sliderValue = 0.0f;
                }
                if (selectedAgent.MyAgentType == AgentType.Unit && selectedAgent.GetAbility<Construct>())
                {
                    DrawActions(selectedAgent.GetAbility<Construct>().BuildActions);
                }
                else if (selectedAgent.MyAgentType == AgentType.Structure && selectedAgent.GetAbility<Spawner>() && !selectedAgent.GetAbility<Structure>().NeedsConstruction)
                {
                    DrawActions(selectedAgent.GetAbility<Spawner>().GetSpawnActions());
                }
                // store the current selection
                _lastSelection = selectedAgent;
                if (_lastSelection.MyAgentType == AgentType.Structure)
                {
                    if (_lastSelection.GetAbility<Spawner>())
                    {
                        DrawBuildQueue(_lastSelection.GetAbility<Spawner>().getBuildQueueValues(), _lastSelection.GetAbility<Spawner>().getBuildPercentage());
                    }
                }
                if (_lastSelection.MyAgentType == AgentType.Structure || _lastSelection.MyAgentType == AgentType.Unit)
                {
                    DrawStandardOptions(_lastSelection as LSAgent);
                }
            }

            if (!selectionName.Equals(""))
            {
                int leftPos = _buildImageWidth + _scrollBarWidth / 2;
                int topPos = _buildAreaHeight + _buttonSpacing;
                GUI.Label(new Rect(leftPos, topPos, _ordersBarWidth, _selectionNameHeight), selectionName);
            }

            GUI.EndGroup();
        }

        private void DrawStatusBar(AgentStats agentStats)
        {
            int leftPos = _buildImageWidth + _scrollBarWidth;
            int topPos = 0;

            if (agentStats.CachedHealth)
            {
                string healthStat = "Health: " + agentStats.CurrentHealth.CeilToInt() + " / " + agentStats.MaxHealth.CeilToInt();
                GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), healthStat);
            }
            else if (agentStats.CachedResourceDeposit)
            {
                string resourceStat = "Amount: " + agentStats.AmountLeft + " / " + agentStats.Capacity;
                GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), resourceStat);
            }


            if (agentStats.CachedMove)
            {
                topPos += _textHeight;
                string speedStat = "Move Speed: " + agentStats.MovementSpeed.CeilToInt();
                GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), speedStat);
            }

            if (agentStats.CachedAttack)
            {
                topPos += _textHeight;
                string damageStat = "Damage: " + agentStats.Damage.CeilToInt();
                GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), damageStat);

                topPos += _textHeight;
                string dpsStat = "DPS: " + agentStats.DPS.CeilToInt();
                GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), dpsStat);

                topPos += _textHeight;
                string rangeStat = "Range: " + agentStats.ActionRange.CeilToInt();
                GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), rangeStat);
            }

            topPos += _textHeight;
            string sightStat = "Sight: " + agentStats.Sight.CeilToInt();
            GUI.Label(new Rect(leftPos, topPos, _textWidth, _textHeight), sightStat);

            _statusAreaHeight = topPos += _textHeight;
        }

        private void DrawBuildQueue(string[] buildQueue, float buildPercentage)
        {
            for (int i = 0; i < buildQueue.Length; i++)
            {
                float topPos = i * _buildImageHeight - (i + 1) * _buildImagePadding;
                Rect buildPos = new Rect(_buildImagePadding, topPos, _buildImageWidth, _buildImageHeight);
                GUI.DrawTexture(buildPos, GameResourceManager.GetBuildImage(buildQueue[i]));
                GUI.DrawTexture(buildPos, BuildFrame);
                topPos += _buildImagePadding;
                float width = _buildImageWidth - 2 * _buildImagePadding;
                float height = _buildImageHeight - 2 * _buildImagePadding;
                if (i == 0)
                {
                    // shrink the build mask on the item currently being built to give an idea of progress
                    topPos += height * buildPercentage;
                    height *= (1 - buildPercentage);
                }

                GUI.DrawTexture(new Rect(2 * _buildImagePadding, topPos, width, height), BuildMask);
            }
        }

        // move this to call from agent, each ability should have it's own
        private void DrawStandardOptions(LSAgent agent)
        {
            GUIStyle buttons = new GUIStyle();
            buttons.hover.background = SmallButtonHover;
            buttons.active.background = SmallButtonClick;
            GUI.skin.button = buttons;
            int leftPos = _buildImageWidth + _scrollBarWidth + _buttonSpacing;
            int topPos = _buildAreaHeight - _buildImageHeight / 2;
            int width = _buildImageWidth / 2;
            int height = _buildImageHeight / 2;

            if (_cachedPlayer.GetController().SelectedAgents.Count == 1 && GUI.Button(new Rect(leftPos, topPos, width, height), agent.destroyImage))
            {
                PlayClick();
                agent.Die();
            }

            if (agent.GetAbility<Rally>() && agent.GetAbility<Rally>().hasSpawnPoint() && !agent.GetAbility<Structure>().NeedsConstruction)
            {
                leftPos += width + _buttonSpacing;
                if (GUI.Button(new Rect(leftPos, topPos, width, height), agent.GetAbility<Rally>().rallyPointImage))
                {
                    PlayClick();
                    if (_activeCursorState != CursorState.RallyPoint)
                    {
                        agent.GetAbility<Rally>().SetFlagState(FlagState.SettingFlag);
                        SetCursorState(CursorState.RallyPoint);
                        SetCursorLock(true);
                    }
                    else
                    {
                        agent.GetAbility<Rally>().SetFlagState(FlagState.FlagSet);
                        SetCursorLock(false);
                        SetCursorState(CursorState.Select);
                    }
                }
            }
        }

        private void DrawRawMaterialBar()
        {
            GUI.skin = RawMaterialSkin;
            GUI.BeginGroup(new Rect(0, 0, Screen.width, _resourceBarHeight));
            GUI.Box(new Rect(0, 0, Screen.width, _resourceBarHeight), "");
            int topPos = 4, iconLeft = 4, textLeft = 15;
            foreach (var keyValuePair in RawMaterialIcons)
            {
                DrawResourceIcon(keyValuePair.Key, keyValuePair.Value.RawMaterialIcon, iconLeft, textLeft, topPos);
                iconLeft += _textWidth;
                textLeft += _textWidth;
            }

            int padding = 7;
            int buttonWidth = _ordersBarWidth - 2 * padding - _scrollBarWidth;
            int buttonHeight = _resourceBarHeight - 2 * padding;
            int leftPos = Screen.width - _ordersBarWidth / 2 - buttonWidth / 2 + _scrollBarWidth / 2;
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
                PlayerInputHelper userInput = _cachedPlayer.GetComponent<PlayerInputHelper>();
                if (userInput)
                {
                    userInput.enabled = false;
                }
            }
            GUI.EndGroup();
        }

        private void DrawResourceIcon(RawMaterialType type, Texture2D icon, int iconLeft, int textLeft, int topPos)
        {
            string text = _cachedResourceManager.CurrentRawMaterials[type].CurrentValue.ToString() + "/" + _cachedResourceManager.CurrentRawMaterials[type].CurrentLimit.ToString();

            GUI.DrawTexture(new Rect(iconLeft, topPos, _iconWidth, _iconHeight), icon);
            GUI.Label(new Rect(textLeft, topPos, _textWidth, _textHeight), text);
        }

        private void DrawMouseCursor()
        {
            Cursor.visible = false;

            // toggle back to pointer if over hud
            if (IsMouseOverHUD && !_cursorLocked)
            {
                SelectionManager.SetSelectionLock(true);
                SetCursorState(CursorState.Pointer);
            }

            if (!ConstructionHandler.IsFindingBuildingLocation())
            {
                GUI.skin = MouseCursorSkin;
                GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
                UpdateCursorAnimation();
                Rect cursorPosition = GetCursorDrawPosition();
                GUI.Label(cursorPosition, _activeCursor);
                GUI.EndGroup();
            }
        }

        private void UpdateCursorAnimation()
        {
            // sequence animation for cursor (based on mor ethan one iamge for the cursor)
            // change once per second, loops through array of images
            if (_activeCursorState == CursorState.Move)
            {
                _currentFrame = (int)Time.time % MoveCursors.Length;
                _activeCursor = MoveCursors[_currentFrame];
            }
            else if (_activeCursorState == CursorState.Attack)
            {
                _currentFrame = (int)Time.time % AttackCursors.Length;
                _activeCursor = AttackCursors[_currentFrame];
            }
            else if (_activeCursorState == CursorState.Harvest)
            {
                _currentFrame = (int)Time.time % HarvestCursors.Length;
                _activeCursor = HarvestCursors[_currentFrame];
            }
            else if (_activeCursorState == CursorState.Construct)
            {
                _currentFrame = (int)Time.time % ConstructCursors.Length;
                _activeCursor = ConstructCursors[_currentFrame];
            }
        }

        private Rect GetCursorDrawPosition()
        {
            // set base position for custom cursor image
            float leftPos = Input.mousePosition.x;
            // screen draw coordinates are inverted
            // adjust position base on the type of cursor being shown
            float topPos = Screen.height - Input.mousePosition.y;

            if (_activeCursorState == CursorState.PanRight)
            {
                leftPos = Screen.width - _activeCursor.width;
            }
            else if (_activeCursorState == CursorState.PanDown)
            {
                topPos = Screen.height - _activeCursor.height;
            }
            else if (_activeCursorState == CursorState.Move || _activeCursorState == CursorState.Select || _activeCursorState == CursorState.Harvest)
            {
                topPos -= _activeCursor.height / 2;
                leftPos -= _activeCursor.width / 2;
            }
            else if (_activeCursorState == CursorState.RallyPoint)
            {
                topPos -= _activeCursor.height;
            }

            return new Rect(leftPos, topPos, _activeCursor.width, _activeCursor.height);
        }

        private void DrawActions(string[] actions)
        {
            GUIStyle buttons = new GUIStyle();
            buttons.hover.background = ButtonHover;
            buttons.active.background = ButtonClick;
            GUI.skin.button = buttons;
            int numActions = actions.Length;
            //define the area to draw the actions inside
            GUI.BeginGroup(new Rect(_buildImageWidth, _statusAreaHeight, _ordersBarWidth, _buildAreaHeight));
            //draw scroll bar for the list of actions if need be
            if (numActions >= MaxNumRows(_buildAreaHeight))
            {
                DrawSlider(_buildAreaHeight, numActions / 2.0f);
            }

            //display possible actions as buttons and handle the button click for each
            for (int i = 0; i < numActions; i++)
            {
                int column = i % 2;
                int row = i / 2;
                Rect pos = GetButtonPos(row, column);
                Texture2D action = GameResourceManager.GetBuildImage(actions[i]);

                if (action)
                {
                    //create the button and handle the click of that button
                    if (GUI.Button(pos, action))
                    {
                        LSAgent agent = Selector.MainSelectedAgent as LSAgent;
                        if (agent)
                        {
                            PlayClick();

                            if (agent.MyAgentType == AgentType.Unit
                                && agent.GetAbility<Construct>()
                                && !ConstructionHandler.IsFindingBuildingLocation())
                            {
                                ConstructionHandler.CreateStructure(actions[i], agent);
                            }
                            else if (agent.MyAgentType == AgentType.Structure
                                && !agent.GetAbility<Structure>().NeedsConstruction
                                && agent.GetAbility<Spawner>())
                            {
                                // send spawn command
                                Command spawnCom = new Command(AbilityDataItem.FindInterfacer("Spawner").ListenInputID);
                                spawnCom.Add(new DefaultData(DataType.String, actions[i]));
                                PlayerInputHelper.SendCommand(spawnCom);
                            }
                        }
                    }
                }
            }
            GUI.EndGroup();
        }

        private int MaxNumRows(int areaHeight)
        {
            return areaHeight / _buildImageHeight;
        }

        private Rect GetButtonPos(int row, int column)
        {
            int left = _scrollBarWidth + column * _buildImageWidth;
            float top = row * _buildImageHeight - _sliderValue * _buildImageHeight;

            return new Rect(left, top, _buildImageWidth, _buildImageHeight);
        }

        private void DrawSlider(int groupHeight, float numRows)
        {
            // slider goes from 0 to the number of rows that do not fit on screen
            _sliderValue = GUI.VerticalSlider(GetScrollPos(groupHeight), _sliderValue, 0.0f, numRows - MaxNumRows(groupHeight));
        }

        private Rect GetScrollPos(int groupHeight)
        {
            return new Rect(_buttonSpacing, _buttonSpacing, _scrollBarWidth, groupHeight - 2 * _buttonSpacing);
        }

        private void DrawPlayerDetails()
        {
            GUI.skin = PlayerDetailsSkin;
            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
            float height = GameResourceManager.TextHeight;
            float leftPos = GameResourceManager.Padding;
            float topPos = Screen.height - height - GameResourceManager.Padding;
            Texture2D avatar = PlayerManager.GetPlayerAvatar();
            if (avatar)
            {
                //we want the texture to be drawn square at all times
                GUI.DrawTexture(new Rect(leftPos, topPos, height, height), avatar);
                leftPos += height + GameResourceManager.Padding;
            }

            string playerName = PlayerManager.GetPlayerName();
            PlayerDetailsSkin.GetStyle("label").CalcMinMaxWidth(new GUIContent(playerName), out _, out float maxWidth);
            GUI.Label(new Rect(leftPos, topPos, maxWidth, height), playerName);
            GUI.EndGroup();
        }

        private void PlayClick()
        {
            if (_audioElement != null)
            {
                _audioElement.Play(ClickSound);
            }
        }
        #endregion
    }
}