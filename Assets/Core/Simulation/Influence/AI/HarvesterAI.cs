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
                    // We're ready to go but have no target
                    searchCount = SearchRate;
                    return true;
                }
            }

            if (cachedAgent.MyStats.CachedHarvest.IsEmptying || cachedAgent.MyStats.CachedHarvest.IsHarvesting)
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
                        Structure structure = other.GetAbility<Structure>();
                        return other.GlobalID != cachedAgent.GlobalID
                                && CachedAgentValid(other)
                                && structure.IsNotNull()
                                && structure.CanStoreResources(cachedAgent.MyStats.CachedHarvest.HarvestType)
                                && !structure.NeedsConstruction;
                    };
                }
                else if (cachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return other.GlobalID != cachedAgent.GlobalID
                                && CachedAgentValid(other)
                                && resourceDeposit.IsNotNull()
                                && resourceDeposit.ResourceType == cachedAgent.MyStats.CachedHarvest.HarvestType
                                && !resourceDeposit.IsEmpty();
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
                    if (cachedAgent.MyStats.CachedHarvest.LastResourceTarget.IsNotNull())
                    {
                        if (!cachedAgent.MyStats.CachedHarvest.LastResourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
                        {
                            // no need to search, we still got some goods
                            return cachedAgent.MyStats.CachedHarvest.LastResourceTarget;
                        }
                        else
                        {
                            // Search where the last resource target was for new goods
                            scanPos = cachedAgent.MyStats.CachedHarvest.LastResourceTarget.Body.Position;
                        }
                    }
                }

                agent = InfluenceManager.Scan(
                     scanPos,
                     cachedAgent.MyStats.Sight,
                     agentConditional,
                     allianceConditional
                 );

                // agent was harvesting but no longer has target resource in sight
                // send them back to deposit
                // or
                // agent couldn't find storage within sight
                // double check storage doesn't exist somewhere, anywhere!
                if (agent.IsNull() 
                    && (cachedAgent.MyStats.CachedHarvest.IsHarvesting && cachedAgent.MyStats.CachedHarvest.GetCurrentLoad() > 0
                    || cachedAgent.MyStats.CachedHarvest.IsEmptying))
                {
                    agent = cachedAgent.MyStats.CachedHarvest.LastStorageTarget.IsNotNull() ? cachedAgent.MyStats.CachedHarvest.LastStorageTarget
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