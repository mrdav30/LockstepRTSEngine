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
            if (cachedAgent.Tag != AgentTag.Builder
                || cachedAgent.MyStats.CachedConstruct.IsBuildMoving
                || cachedAgent.MyStats.CachedConstruct.IsFocused
                || cachedAgent.MyStats.CachedConstruct.CurrentProject.IsNotNull())
            {
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
                Func<RTSAgent, bool> agentConditional = (other) =>
                    {
                        Structure structure = other.GetAbility<Structure>();
                        return structure != null && structure.NeedsConstruction && other.IsActive;
                    };

                return agentConditional;
            }
        }

        protected override Func<byte, bool> AllianceConditional
        {
            get
            {
                Func<byte, bool> allianceConditional = (bite) =>
                {
                    return ((cachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Friendly) != 0);
                };
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