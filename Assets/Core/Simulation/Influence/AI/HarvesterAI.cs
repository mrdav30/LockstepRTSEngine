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
            _targetAllegiance = AllegianceType.Neutral;
            cachedHarvest = cachedAgent.GetAbility<Harvest>();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.Tag != AgentTag.Harvester
                || (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying)
                || cachedHarvest.IsLoadAtCapacity())
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
                Func<RTSAgent, bool> agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return resourceDeposit != null && !resourceDeposit.IsEmpty() && other.IsActive;
                    };


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

                    harvestCom.ControllerID = cachedAgent.Controller.ControllerID;
                    harvestCom.Add<Influence>(new Influence(cachedAgent));

                    CommandManager.SendCommand(harvestCom);
                }
            }
            else
            {
                cachedHarvest.SetResourceTarget(null);
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