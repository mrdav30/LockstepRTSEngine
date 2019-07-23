using Newtonsoft.Json;
using System;

namespace RTSLockstep
{
    public class OffensiveAI : DeterminismAI
    {
        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();
            cachedAttack = Agent.GetAbility<Attack>();
        }

        public override bool CanAttack()
        {
            if (cachedAttack)
            {
                if (cachedAttack.IsAttackMoving || cachedHealth.HealthAmount == 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            InfluenceAttack();
        }

        //TODO: Consolidate the checks in LSInfluencer
        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;

                if (cachedAttack.Damage >= 0)
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return Agent.GlobalID != other.GlobalID && health != null && health.CanLose && CachedAgentValid(other);
                    };
                }
                else
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return Agent.GlobalID != other.GlobalID && health != null && health.CanGain && CachedAgentValid(other);
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