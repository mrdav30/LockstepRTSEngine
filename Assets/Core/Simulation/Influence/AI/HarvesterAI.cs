using RTSLockstep.Data;
using System;
using System.Collections.Generic;

namespace RTSLockstep
{
    public class HarvesterAI : DeterminismAI
    {
        private AgentTag action;

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
            else if (searchCount <= 0)
            {
                if (!cachedAgent.MyStats.CachedHarvest.IsCasting
                    && (cachedAgent.MyStats.CachedHarvest.IsHarvesting || cachedAgent.MyStats.CachedHarvest.IsEmptying))
                {
                    searchCount = SearchRate;
                    // We're ready but have no target
                    return true;
                }
            }
            else if (cachedAgent.MyStats.CachedHarvest.IsEmptying || cachedAgent.MyStats.CachedHarvest.IsHarvesting)
            {
                // busy harvesting or emptying
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
                else if (cachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    allianceConditional = (bite) =>
                    {
                        return ((cachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Neutral) != 0); ;
                    };
                }

                return allianceConditional;
            }
        }

        protected override RTSAgent DoScan()
        {
            Func<RTSAgent, bool> agentConditional = AgentConditional;
            Func<byte, bool> allianceConditional = AllianceConditional;

            RTSAgent agent = null;

            if (agentConditional.IsNotNull())
            {
                Vector2d scanPos = cachedAgent.Body.Position;
                if (cachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    if(cachedAgent.MyStats.CachedHarvest.CurrentResourceTarget.IsNotNull())
                    {
                        if (!cachedAgent.MyStats.CachedHarvest.TargetResource.IsEmpty())
                        {
                            // no need to search, we still got some goods
                            return cachedAgent.MyStats.CachedHarvest.CurrentResourceTarget;
                        }
                        else
                        {
                            // Search where the last resource target was for new goods
                            scanPos = cachedAgent.MyStats.CachedHarvest.CurrentResourceTarget.Body.Position;
                        }
                    }
                }

                agent = InfluenceManager.Scan(
                     scanPos,
                     cachedAgent.MyStats.Sight,
                     agentConditional,
                     allianceConditional
                 );

                // If we couldn't find storage within sight
                // Double check storage doesn't exist somewhere, anywhere!
                if (agent.IsNull() && cachedAgent.MyStats.CachedHarvest.IsEmptying)
                {
                    agent = cachedAgent.MyStats.CachedHarvest.CurrentStorageTarget.IsNotNull() ? cachedAgent.MyStats.CachedHarvest.CurrentStorageTarget
                        : ClosestResourceStorage();
                }
            }

            return agent;
        }

        private void InfluenceHarvest()
        {
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