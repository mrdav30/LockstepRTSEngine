using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class WorkerAI : DeterminismAI
    {
        protected Construct cachedConstruct;
        protected Harvest cachedHarvest;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();
            cachedConstruct = Agent.GetAbility<Construct>();
            cachedHarvest = Agent.GetAbility<Harvest>();
        }

        public override bool CanAttack()
        {
            if (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying || cachedConstruct.IsBuilding)
            {
                return false;
            }

            return true;
        }

        public override bool ShouldMakeDecision()
        {
            if (Agent.Tag == AgentTag.Harvester && (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying))
            {
                return false;
            }
            else if (Agent.Tag == AgentTag.Builder && cachedConstruct.IsBuilding)
            {
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            if (Agent.Tag == AgentTag.Harvester && !cachedHarvest.IsFocused)
            {
                InfluenceHarvest();
            }
            else if (Agent.Tag == AgentTag.Builder && !cachedConstruct.IsFocused)
            {
                InfluenceConstruction();
            }
        }

        private void InfluenceHarvest()
        {
            if (nearbyAgent)
            {
                ResourceDeposit closestResource = nearbyAgent.GetAbility<ResourceDeposit>();
                // only harvest resources the worker is assigned to
                if (closestResource
                    && closestResource.ResourceType == cachedHarvest.HarvestType)
                {
                    // send harvest command
                    Command harvestCom = new Command(AbilityDataItem.FindInterfacer("Harvest").ListenInputID);
                    harvestCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                    harvestCom.ControllerID = Agent.Controller.ControllerID;

                    harvestCom.Add<Influence>(new Influence(Agent));

                    CommandManager.SendCommand(harvestCom);
                }
            }
            else
            {
                cachedHarvest.SetResourceTarget(null);
            }
        }

        private void InfluenceConstruction()
        {
            if (nearbyAgent)
            {
                Structure closestBuilding = nearbyAgent.GetComponent<Structure>();
                if (closestBuilding)
                {
                    // send construct command
                    Command constructCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
                    constructCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                    constructCom.ControllerID = Agent.Controller.ControllerID;

                    constructCom.Add<Influence>(new Influence(Agent));

                    CommandManager.SendCommand(constructCom);
                }
            }
            else
            {
                cachedConstruct.SetCurrentProject(null);
            }
        }

        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;

                if (Agent.Tag == AgentTag.Harvester)
                {
                    agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return resourceDeposit != null && !resourceDeposit.IsEmpty() && other.IsActive;
                    };
                }
                else if (Agent.Tag == AgentTag.Builder)
                {
                    agentConditional = (other) =>
                    {
                        Structure structure = other.GetAbility<Structure>();
                        return structure != null && structure.UnderConstruction() && other.IsActive;
                    };
                }
                return agentConditional;
            }
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);

        }
    }
}