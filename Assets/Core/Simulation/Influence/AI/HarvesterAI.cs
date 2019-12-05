using RTSLockstep.Data;
using System;
using System.Collections.Generic;

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
            if (cachedAgent.Tag != AgentTag.Harvester)
            {
                // were not even a harvester....
                return false;
            }
            else if ((cachedAgent.MyStats.CachedHarvest.IsHarvesting && cachedAgent.MyStats.CachedHarvest.CurrentResourceTarget.IsNotNull()))
            {
                // busy harvesting
                searchCount -= 1;
                return false;
            }
            else if (searchCount <= 0 && cachedAgent.MyStats.CachedHarvest.IsEmptying && cachedAgent.MyStats.CachedHarvest.CurrentStorageTarget.IsNull())
            {
                searchCount = SearchRate;
                // We have a deposit but are not doing anything at the moment
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

                if (cachedAgent.MyStats.CachedHarvest.IsEmptying)
                {
                    agentConditional = (other) =>
                    {
                        if (other != cachedAgent)
                        {
                            Structure structure = other.GetAbility<Structure>();
                            return structure.IsNotNull() && structure.CanStoreResources(cachedAgent.MyStats.CachedHarvest.HarvestType) && !structure.NeedsConstruction;
                        }
                        else
                        {
                            return false;
                        }
                    };
                }
                else if (cachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    agentConditional = (other) =>
                    {
                        if (other != cachedAgent)
                        {
                            ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                            return resourceDeposit.IsNotNull() && !resourceDeposit.IsEmpty() && other.IsActive;
                        }
                        else
                        {
                            return false;
                        }
                    };
                }

                return agentConditional;
            }
        }

        protected override Func<byte, bool> AllianceConditional
        {
            get
            {
                Func<byte, bool> allianceConditional = null;

                if (cachedAgent.MyStats.CachedHarvest.IsEmptying)
                {
                    allianceConditional = (bite) =>
                    {
                        return ((cachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Friendly) != 0); ;
                    };
                }

                return allianceConditional;
            }
        }

        private void InfluenceHarvest()
        {
            if (!nearbyAgent
                && cachedAgent.MyStats.CachedHarvest.IsEmptying
                && cachedAgent.MyStats.CachedHarvest.CurrentStorageTarget.IsNull())
            {
                // double check one doesn't exist somewhere on the map
                nearbyAgent = ClosestResourceStorage();
            }

            if (nearbyAgent.IsNotNull())
            {
                ResourceDeposit closestResource = nearbyAgent.GetAbility<ResourceDeposit>();
                Structure closestResourceStore = nearbyAgent.GetAbility<Structure>();

                if (closestResource.IsNotNull()
                    && closestResource.ResourceType == cachedAgent.MyStats.CachedHarvest.HarvestType
                    || closestResourceStore.IsNotNull()
                    && nearbyAgent.GetAbility<Structure>().CanStoreResources(cachedAgent.MyStats.CachedHarvest.HarvestType)
                )
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

        // Backup in case agent can't find storage within range
        // default is to always go as far as it takes to store them goods
        private RTSAgent ClosestResourceStorage()
        {
            //change list to fastarray
            List<RTSAgent> playerBuildings = new List<RTSAgent>();
            // use RTS influencer?
            foreach (RTSAgent child in cachedAgent.Controller.Commander.GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>())
            {
                if (child.GetAbility<Structure>()
                    && child.GetAbility<Structure>().CanStoreResources(cachedAgent.MyStats.CachedHarvest.HarvestType)
                    && !child.GetAbility<Structure>().NeedsConstruction)
                {
                    playerBuildings.Add(child);
                }
            }
            if (playerBuildings.Count > 0)
            {
                RTSAgent nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(playerBuildings, cachedAgent.transform.position) as RTSAgent;
                return nearestObject;
            }
            else
            {
                return null;
            }
        }
    }
}