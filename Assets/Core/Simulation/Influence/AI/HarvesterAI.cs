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
            if (CachedAgent.Tag != AgentTag.Harvester)
            {
                // were not even a harvester....
                return false;
            }
            else if (searchCount <= 0)
            {
                searchCount = SearchRate;
                if (!CachedAgent.MyStats.CachedHarvest.IsCasting
                    && (CachedAgent.MyStats.CachedHarvest.IsHarvesting || CachedAgent.MyStats.CachedHarvest.IsEmptying))
                {
                    // We're ready to go but have no target
                    return true;
                }
            }

            if (CachedAgent.MyStats.CachedHarvest.IsEmptying || CachedAgent.MyStats.CachedHarvest.IsHarvesting)
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

                if (CachedAgent.MyStats.CachedHarvest.IsEmptying)
                {
                    agentConditional = (other) =>
                    {
                        Structure structure = other.GetAbility<Structure>();
                        return other.GlobalID != CachedAgent.GlobalID
                                && CachedAgentValid(other)
                                && structure.IsNotNull()
                                && structure.CanStoreResources(CachedAgent.MyStats.CachedHarvest.HarvestType)
                                && !structure.NeedsConstruction;
                    };
                }
                else if (CachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    agentConditional = (other) =>
                    {
                        ResourceDeposit resourceDeposit = other.GetAbility<ResourceDeposit>();
                        return other.GlobalID != CachedAgent.GlobalID
                                && CachedAgentValid(other)
                                && resourceDeposit.IsNotNull()
                                && resourceDeposit.ResourceType == CachedAgent.MyStats.CachedHarvest.HarvestType
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

                if (CachedAgent.MyStats.CachedHarvest.IsEmptying)
                {
                    allianceConditional = (bite) =>
                    {
                        return ((CachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Friendly) != 0); ;
                    };
                }
                else if (CachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    allianceConditional = (bite) =>
                    {
                        return ((CachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Neutral) != 0); ;
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
                Vector2d scanPos = CachedAgent.Body.Position;
                if (CachedAgent.MyStats.CachedHarvest.IsHarvesting)
                {
                    if (CachedAgent.MyStats.CachedHarvest.LastResourceTarget.IsNotNull())
                    {
                        if (!CachedAgent.MyStats.CachedHarvest.LastResourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
                        {
                            // no need to search, we still got some goods
                            return CachedAgent.MyStats.CachedHarvest.LastResourceTarget;
                        }
                        else
                        {
                            // Search where the last resource target was for new goods
                            scanPos = CachedAgent.MyStats.CachedHarvest.LastResourceTarget.Body.Position;
                        }
                    }
                }

                agent = InfluenceManager.Scan(
                     scanPos,
                     CachedAgent.MyStats.Sight,
                     agentConditional,
                     allianceConditional
                 );

                // agent was harvesting but no longer has target resource in sight
                // send them back to deposit
                // or
                // agent couldn't find storage within sight
                // double check storage doesn't exist somewhere, anywhere!
                if (agent.IsNull()
                    && (CachedAgent.MyStats.CachedHarvest.IsHarvesting && CachedAgent.MyStats.CachedHarvest.GetCurrentLoad() > 0
                    || CachedAgent.MyStats.CachedHarvest.IsEmptying))
                {
                    agent = CachedAgent.MyStats.CachedHarvest.LastStorageTarget.IsNotNull() ? CachedAgent.MyStats.CachedHarvest.LastStorageTarget
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

                if (closestResource.IsNotNull()|| closestResourceStore.IsNotNull())
                {
                    // send harvest command
                    Command harvestCom = new Command(AbilityDataItem.FindInterfacer("Harvest").ListenInputID);
                    harvestCom.Add(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));

                    harvestCom.ControllerID = CachedAgent.Controller.ControllerID;
                    harvestCom.Add(new Influence(CachedAgent));

                    CommandManager.SendCommand(harvestCom);

                    base.ResetAwareness();
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
            foreach (RTSAgent child in CachedAgent.Controller.Commander.GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>())
            {
                if (child.GetAbility<Structure>()
                    && child.GetAbility<Structure>().CanStoreResources(CachedAgent.MyStats.CachedHarvest.HarvestType)
                    && !child.GetAbility<Structure>().NeedsConstruction)
                {
                    playerBuildings.Add(child);
                }
            }
            if (playerBuildings.Count > 0)
            {
                RTSAgent nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(playerBuildings, CachedAgent.transform.position) as RTSAgent;
                return nearestObject;
            }
            else
            {
                return null;
            }
        }
    }
}