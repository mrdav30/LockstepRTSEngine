using Newtonsoft.Json;

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

        public override bool CanAttack()
        {
            if (cachedStructure && cachedAttack)
            {
                if (cachedStructure.UnderConstruction() || cachedHealth.HealthAmount == 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public override bool ShouldMakeDecision()
        {
            if (Agent.GetAbility<Structure>().UnderConstruction())
            {
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            InfluenceAttack();
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