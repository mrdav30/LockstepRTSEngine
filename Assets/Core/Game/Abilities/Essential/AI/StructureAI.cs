using Newtonsoft.Json;
using RTSLockstep;
using System;
using UnityEngine;

namespace RTSLockstep
{
    public class StructureAI : DeterminismAI
    {
        private Structure cachedStructure;
        private Spawner cachedSpawner;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();
            cachedStructure = Agent.GetAbility<Structure>();
            cachedSpawner = Agent.GetAbility<Spawner>();
        }

        public override void CanAttack()
        {
            if (cachedAttack)
            {
                if (cachedStructure.UnderConstruction() || cachedHealth.HealthAmount == 0)
                {
                    canAttack = false;
                }
                canAttack = true;
            }
            canAttack = false;
        }

        public override bool ShouldMakeDecision()
        {
            if (Agent.GetAbility<Structure>().UnderConstruction())
            {              
                return false;
            }
            else
            {
                return base.ShouldMakeDecision();
            }
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
                        return Agent.GlobalID != other.GlobalID && health != null && health.CanLose && cachedAttack.CachedAgentValid(other);
                    };
                }
                else
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return Agent.GlobalID != other.GlobalID && health != null && health.CanGain && cachedAttack.CachedAgentValid(other);
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