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

        public override void OnInitialize()
        {
            base.OnInitialize();
            cachedConstruct = cachedInfluencer.Agent.GetAbility<Construct>();
            cachedHarvest = cachedInfluencer.Agent.GetAbility<Harvest>();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedInfluencer.Agent.Tag == AgentTag.Harvester && (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying))
            {
                return false;
            }
            else if (cachedInfluencer.Agent.Tag == AgentTag.Builder && cachedConstruct.IsBuilding)
            {
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            if (cachedInfluencer.Agent.Tag == AgentTag.Harvester && !cachedHarvest.IsLoadAtCapacity())
            {
                InfluenceHarvest();
            }
            else if (cachedInfluencer.Agent.Tag == AgentTag.Builder)
            {
                InfluenceConstruction();
            }
        }

        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;

                if (cachedInfluencer.Agent.Tag == AgentTag.Harvester
                    && !cachedHarvest.IsLoadAtCapacity())
                {
                    agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return resourceDeposit != null && !resourceDeposit.IsEmpty() && other.IsActive;
                    };

                }
                else if (cachedInfluencer.Agent.Tag == AgentTag.Builder)
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

        private void InfluenceHarvest()
        {
            if (nearbyAgent)
            {
                ResourceDeposit closestResource = nearbyAgent.GetAbility<ResourceDeposit>();

                if (closestResource && closestResource.ResourceType == cachedHarvest.HarvestType)
                {
                    // send harvest command
                    Command harvestCom = new Command(AbilityDataItem.FindInterfacer("Harvest").ListenInputID);
                    harvestCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));

                    harvestCom.ControllerID = cachedInfluencer.Agent.Controller.ControllerID;
                    harvestCom.Add<Influence>(new Influence(cachedInfluencer.Agent));

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
                    constructCom.ControllerID = cachedInfluencer.Agent.Controller.ControllerID;

                    constructCom.Add<Influence>(new Influence(cachedInfluencer.Agent));

                    CommandManager.SendCommand(constructCom);
                }
            }
            else
            {
                cachedConstruct.SetCurrentProject(null);
            }
        }

        public override void OnSaveDetails(JsonWriter writer)
        {
            base.OnSaveDetails(writer);
        }

        public override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);

        }
    }
}