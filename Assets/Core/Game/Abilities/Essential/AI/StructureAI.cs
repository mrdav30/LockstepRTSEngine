using Newtonsoft.Json;
using RTSLockstep;
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

        protected override void OnVisualize()
        {

        }

        public override void CanAttack()
        {
            if (cachedAttack)
            {
                if (cachedStructure.UnderConstruction() || cachedHealth.HealthAmount == 0)
                {
                    cachedAttack.CanAttack = false;
                }
                cachedAttack.CanAttack = true;
            }
            cachedAttack.CanAttack = false;
        }

        public override bool ShouldMakeDecision()
        {
            if (!cachedAttack)
            {
                return false;
            }
            else
            {
                return base.ShouldMakeDecision();
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