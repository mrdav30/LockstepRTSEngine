using Newtonsoft.Json;

namespace RTSLockstep
{
    public class StructureAI : DeterminismAI
    {
        private Structure cachedStructure;
        private Spawner cachedSpawner;

        #region Serialized Values (Further description in properties)
        #endregion

        public override void OnInitialize()
        {
            base.OnInitialize();
            cachedStructure = cachedInfluencer.Agent.GetAbility<Structure>();
            cachedSpawner = cachedInfluencer.Agent.GetAbility<Spawner>();
        }

        public override bool ShouldMakeDecision()
        {
            if (cachedInfluencer.Agent.GetAbility<Structure>().UnderConstruction() || cachedHealth.HealthAmount == 0)
            {
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void OnSaveDetails(JsonWriter writer)
        {
            base.OnSaveDetails(writer);
        }

        public override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);

        }
    }
}