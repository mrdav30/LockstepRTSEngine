using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class HarvesterAI : DeterminismAI
    {
        public override void OnInitialize()
        {
            base.OnInitialize();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.Tag != AgentTag.Harvester
                || (cachedAgent.MyStats.CachedHarvest.IsHarvesting || cachedAgent.MyStats.CachedHarvest.IsEmptying))
            {
                searchCount -= 1;
                return false;
            }
            else if (searchCount <= 0
                && cachedAgent.MyStats.CachedHarvest.LoadAtCapacity())
            {
                searchCount = SearchRate;
                //we are not doing anything at the moment
                return true;
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

                if (cachedAgent.GetAbility<Harvest>().LoadAtCapacity())
                {
                    _targetAllegiance = AllegianceType.Friendly;
                    agentConditional = (other) =>
                    {
                        Structure structure = other.GetAbility<Structure>();
                        return structure.IsNotNull() && !structure.NeedsConstruction;
                    };
                }
                else
                {
                    _targetAllegiance = AllegianceType.Neutral;
                    agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return resourceDeposit.IsNotNull() && !resourceDeposit.IsEmpty() && other.IsActive;
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

                if (closestResource && closestResource.ResourceType == cachedAgent.MyStats.CachedHarvest.HarvestType
                    || closestResourceStore && nearbyAgent.GetAbility<Structure>().CanStoreResources(cachedAgent.GetAbility<Harvest>().HarvestType))
                {
                    // send harvest command
                    Command harvestCom = new Command(AbilityDataItem.FindInterfacer("Harvest").ListenInputID);
                    harvestCom.Add(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));

                    harvestCom.ControllerID = cachedAgent.Controller.ControllerID;
                    harvestCom.Add(new Influence(cachedAgent));

                    CommandManager.SendCommand(harvestCom);
                }
            }
        }
    }
}