using RTSLockstep;
using UnityEngine;

namespace RTSLockstep
{
    public class SelectionController : Ability
    {
        private Health cachedHealth;
        private Harvest cachedHarvest;
        private Spawner cachedSpawner;
        private Structure cachedStructure;
        private AgentController cachedController;
        private AgentCommander cachedCommander;
        protected Rect selectBox;
        protected GUIStyle healthStyle = new GUIStyle();
        protected float healthPercentage = 1.0f;

        protected override void OnSetup()
        {
            Agent.onSelectedChange += HandleSelectedChange;
            Agent.onHighlightedChange += HandleHighlightedChange;

            cachedHealth = Agent.GetAbility<Health>();
            cachedHarvest = Agent.GetAbility<Harvest>();
            cachedSpawner = Agent.GetAbility<Spawner>();
            cachedStructure = Agent.GetAbility<Structure>();

            cachedController = PlayerManager.MainController;
            cachedCommander = cachedController.Commander;
        }

        protected override void OnInitialize()
        {
            if (Agent.Body.GetSelectionBounds() == null)
            {
                Agent.Body.CalculateBounds();
            }
        }

        protected override void OnVisualize()
        {
            if (Agent && UserInputHelper.GUIManager.CameraChanged)
            {
                Agent.Body.CalculateBounds();
            }
        }

        protected override void OnGUI()
        {
            if (Agent && !ResourceManager.MenuOpen)
            {
                if (Agent.IsSelected)
                {
                    DrawSelection();
                }
                if (cachedStructure && cachedStructure.UnderConstruction())
                {
                    DrawBuildProgress();
                }
            }
        }

        public void HandleSelectedChange()
        {
            if (ReplayManager.IsPlayingBack)
            {
                return;
            }

            (Agent as RTSAgent).SetPlayingArea(cachedCommander.CachedHud.GetPlayingArea());
        }

        public void HandleHighlightedChange()
        {
            if (ReplayManager.IsPlayingBack)
            {
                return;
            }

            if (Agent && Agent.IsActive)
            {
                if (!Agent.IsSelected && Agent.IsHighlighted)
                {
                    if ((Agent as RTSAgent).Controller.Commander && (Agent as RTSAgent).Controller.Commander == cachedCommander)
                    {
                        //belongs to current cachedCommander
                        cachedCommander.CachedHud.SetCursorState(CursorState.Select);
                    }
                    else if (Agent.Controller.GetAllegiance(cachedController) != AllegianceType.Friendly && cachedController.SelectedAgents.Count > 0
                        && Agent.MyAgentType != AgentType.Resource)
                    {
                        if ((Agent.MyAgentType == AgentType.Unit || Agent.MyAgentType == AgentType.Building) && Agent.GetAbility<Attack>())
                        {
                            cachedCommander.CachedHud.SetCursorState(CursorState.Attack);
                        }
                        else
                        {
                            cachedCommander.CachedHud.SetCursorState(CursorState.Select);
                        }
                    }
                }
                else if (Agent.IsSelected && Agent == Selector.MainSelectedAgent)
                {
                    if (!SelectionManager.MousedAgent)
                    {
                        if (Agent.MyAgentType == AgentType.Building && cachedSpawner && cachedSpawner.GetFlagState() == FlagState.SettingFlag)
                        {
                            cachedCommander.CachedHud.SetCursorState(CursorState.RallyPoint);
                        }
                        else if (Agent.GetAbility<Move>() && Agent.GetAbility<Move>().CanMove)
                        {
                            cachedCommander.CachedHud.SetCursorState(CursorState.Move);
                        }
                    }
                    else
                    {
                        if (cachedHarvest)
                        {
                            if (SelectionManager.MousedAgent.MyAgentType == AgentType.Resource
                                && !SelectionManager.MousedAgent.GetAbility<ResourceDeposit>().IsEmpty())
                            {
                                cachedCommander.CachedHud.SetCursorState(CursorState.Harvest);
                            }
                            else if (SelectionManager.MousedAgent.MyAgentType == AgentType.Building && (Agent as RTSAgent).objectName == cachedHarvest.ResourceStoreName
                                && cachedHarvest.GetCurrentLoad() > 0)
                            {
                                cachedCommander.CachedHud.SetCursorState(CursorState.Deposit);
                            }
                        }
                    }
                }
            }
        }

        private void DrawSelection()
        {
            GUI.skin = ResourceManager.SelectBoxSkin;
            Rect selectBox = WorkManager.CalculateSelectionBox(Agent.Body.GetSelectionBounds(), (Agent as RTSAgent).GetPlayerArea());
            // Draw the selection box around the currently selected object, within the bounds of the playing area
            GUI.BeginGroup((Agent as RTSAgent).GetPlayerArea());
            DrawSelectionBox(selectBox);
            GUI.EndGroup();
        }

        protected void DrawSelectionBox(Rect selectBox)
        {
            GUI.Box(selectBox, "");
            CalculateCurrentHealth(0.35f, 0.65f);
            DrawHealthBar(selectBox, "");
            if (cachedHarvest)
            {
                long currentLoad = cachedHarvest.GetCurrentLoad();
                if (currentLoad > 0)
                {
                    float percentFull = (float)currentLoad / (float)cachedHarvest.Capacity;
                    float maxHeight = selectBox.height - 4;
                    float height = maxHeight * percentFull;
                    float leftPos = selectBox.x + selectBox.width - 7;
                    float topPos = selectBox.y + 2 + (maxHeight - height);
                    float width = 5;
                    Texture2D resourceBar = ResourceManager.GetResourceHealthBar(cachedHarvest.HarvestType);
                    if (resourceBar)
                    {
                        GUI.DrawTexture(new Rect(leftPos, topPos, width, height), resourceBar);
                    }
                }
            }
        }

        public void CalculateCurrentHealth(float lowSplit, float highSplit)
        {
            if (Agent.MyAgentType == AgentType.Unit || Agent.MyAgentType == AgentType.Building)
            {
                healthPercentage = cachedHealth.HealthAmount / (float)cachedHealth.MaxHealth;
                //(float)hitPoints / (float)maxHitPoints;
                if (healthPercentage > highSplit)
                {
                    healthStyle.normal.background = ResourceManager.HealthyTexture;
                }
                else if (healthPercentage > lowSplit)
                {
                    healthStyle.normal.background = ResourceManager.DamagedTexture;
                }
                else
                {
                    healthStyle.normal.background = ResourceManager.CriticalTexture;
                }
            }
            else if (Agent.MyAgentType == AgentType.Resource)
            {
                healthPercentage = (float)Agent.GetAbility<ResourceDeposit>().AmountLeft / (float)Agent.GetAbility<ResourceDeposit>().Capacity;
                healthStyle.normal.background = ResourceManager.GetResourceHealthBar(Agent.GetAbility<ResourceDeposit>().ResourceType);
            }
        }

        public void DrawHealthBar(Rect selectBox, string label)
        {
            healthStyle.padding.top = -20;
            healthStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), label, healthStyle);
        }

        private void DrawBuildProgress()
        {
            GUI.skin = ResourceManager.SelectBoxSkin;
            Rect selectBox = WorkManager.CalculateSelectionBox(Agent.Body.GetSelectionBounds(), (Agent as RTSAgent).GetPlayerArea());
            //Draw the selection box around the currently selected object, within the bounds of the main draw area
            GUI.BeginGroup((Agent as RTSAgent).GetPlayerArea());
            CalculateCurrentHealth(0.5f, 0.99f);
            DrawHealthBar(selectBox, "Building ...");
            GUI.EndGroup();
        }
    }
}