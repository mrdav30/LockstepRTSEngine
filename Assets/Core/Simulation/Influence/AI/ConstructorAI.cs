using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class ConstructorAI : DeterminismAI
    {
        protected Construct cachedConstruct;

        #region Serialized Values (Further description in properties)
        #endregion

        public override void OnInitialize()
        {
            base.OnInitialize();
            _targetAllegiance = AllegianceType.Friendly;
            cachedConstruct = cachedAgent.GetAbility<Construct>();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.Tag != AgentTag.Builder || cachedConstruct.IsBuilding)
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
                Func<RTSAgent, bool> agentConditional = agentConditional = (other) =>
                    {
                        Structure structure = other.GetAbility<Structure>();
                        return structure != null && structure.UnderConstruction() && other.IsActive;
                    };

                return agentConditional;
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
                    constructCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                    constructCom.ControllerID = cachedAgent.Controller.ControllerID;

                    constructCom.Add<Influence>(new Influence(cachedAgent));

                    CommandManager.SendCommand(constructCom);
                }
            }
        }
    }
}