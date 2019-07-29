using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class HarvesterAI : DeterminismAI
    {
        protected Harvest cachedHarvest;

        #region Serialized Values (Further description in properties)
        #endregion

        public override void OnInitialize()
        {
            base.OnInitialize();
            cachedHarvest = cachedAgent.GetAbility<Harvest>();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.Tag != AgentTag.Harvester
                || (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying))
            {
                searchCount -= 1;
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            InfluenceHarvest();
        }

        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;

                if (cachedAgent.GetAbility<Harvest>().IsLoadAtCapacity())
                {
                    _targetAllegiance = AllegianceType.Friendly;
                    agentConditional = (other) =>
                    {
                        Structure structure = other.GetAbility<Structure>();
                        return structure != null && !structure.UnderConstruction();
                    };
                }
                else
                {
                    _targetAllegiance = AllegianceType.Neutral;
                    agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return resourceDeposit != null && !resourceDeposit.IsEmpty() && other.IsActive;
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
                Structure closestResourceStore = nearbyAgent.GetAbility<Structure>();

                if (closestResource && closestResource.ResourceType == cachedHarvest.HarvestType
                    || closestResourceStore && nearbyAgent.GetAbility<Structure>().canStoreResources)
                {
                    // send harvest command
                    Command harvestCom = new Command(AbilityDataItem.FindInterfacer("Harvest").ListenInputID);
                    harvestCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));

                    harvestCom.ControllerID = cachedAgent.Controller.ControllerID;
                    harvestCom.Add<Influence>(new Influence(cachedAgent));

                    CommandManager.SendCommand(harvestCom);
                }
            }
        }
    }
}