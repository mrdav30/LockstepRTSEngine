using System;
using RTSLockstep.Data;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace RTSLockstep
{
    public class WorkerAI: DeterminismAI
    {
        protected Construct cachedBuild;
        protected Harvest cachedHarvest;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();
            cachedBuild = Agent.GetAbility<Construct>();
            cachedHarvest = Agent.GetAbility<Harvest>();
        }

        public override void CanAttack()
        {
            if (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying || cachedBuild.IsBuilding)
            {
                canAttack = false;
            }
            canAttack = true;
        }

        public override bool ShouldMakeDecision()
        {
            if (Agent.Tag == AgentTag.Harvester && (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying))
            {
                return false;
            }
            else if (Agent.Tag == AgentTag.Builder && cachedBuild.IsBuilding)
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
                        Agent.Execute(harvestCom);
                    }
                }
                else
                {
                    cachedHarvest.SetResourceTarget(null);
                }
            }
            if (Agent.Tag == AgentTag.Builder && !cachedBuild.IsFocused)
            {
                if (nearbyAgent)
                {
                    Structure closestBuilding = nearbyAgent.GetComponent<Structure>();
                    if (closestBuilding)
                    {
                        // send build command
                        Command buildCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
                        buildCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                        Agent.Execute(buildCom);
                    }
                }
                else
                {
                    cachedBuild.SetCurrentProject(null);
                }
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