using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class ConstructorAI : DeterminismAI
    {
        public override void OnInitialize()
        {
            base.OnInitialize();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.Tag != AgentTag.Builder)
            {
                // agent isn't a constructor....
                return false;
            }
            else if (searchCount <= 0)
            {
                searchCount = SearchRate;
                if (!cachedAgent.MyStats.CachedConstruct.IsFocused && !cachedAgent.MyStats.CachedConstruct.IsBuildMoving)
                {
                    // We're ready to go but have no target
                    return true;
                }
            }

            if (cachedAgent.MyStats.CachedConstruct.IsFocused || cachedAgent.MyStats.CachedConstruct.IsBuildMoving)
            {
                // busy building
                searchCount -= 1;
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();

            InfluenceConstruction();
        }

        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                bool agentConditional(RTSAgent other)
                {
                    Structure structure = other.GetAbility<Structure>();
                    return other.GlobalID != cachedAgent.GlobalID
                            && CachedAgentValid(other)
                            && cachedAgent.GlobalID != other.GlobalID
                            && structure.IsNotNull()
                            && structure.NeedsConstruction;
                }

                return agentConditional;
            }
        }

        protected override Func<byte, bool> AllianceConditional
        {
            get
            {
                bool allianceConditional(byte bite)
                {
                    return ((cachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Friendly) != 0);
                }
                return allianceConditional;
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

                    // send a flag for agent to register to construction group
                    constructCom.Add(new DefaultData(DataType.Bool, true));

                    constructCom.Add(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                    constructCom.ControllerID = cachedAgent.Controller.ControllerID;

                    constructCom.Add(new Influence(cachedAgent));

                    CommandManager.SendCommand(constructCom);
                }
            }
        }
    }
}