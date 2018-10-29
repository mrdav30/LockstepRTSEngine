using Newtonsoft.Json;
using RTSLockstep;
using UnityEngine;

namespace RTSLockstep
{
    public class Structure : Ability
    {
        public Texture2D sellImage;

        private bool _needsBuilding = false;
        private Health cachedHealth;
        private Spawner cachedSpawner;

        protected override void OnSetup()
        {
            cachedHealth = Agent.GetAbility<Health>();
            cachedSpawner = Agent.GetAbility<Spawner>();
        }

        public void Sell()
        {
            if (PlayerManager.MainController.Commander)
            {
                PlayerManager.MainController.Commander.AddResource(ResourceType.Gold, (Agent as RTSAgent).sellValue);
            }
            Agent.Die(true);
        }

        public void StartConstruction()
        {
            Agent.Body.CalculateBounds();
            _needsBuilding = true;
            cachedHealth.HealthAmount = 0;
            if (cachedSpawner)
            {
                cachedSpawner.SetSpawnPoint();
            }
        }

        public bool UnderConstruction()
        {
            return this._needsBuilding;
        }

        public void Construct(long amount)
        {
            cachedHealth.HealthAmount += amount;
            if (cachedHealth.HealthAmount >= cachedHealth.BaseHealth)
            {
                cachedHealth.HealthAmount = cachedHealth.BaseHealth;
                _needsBuilding = false;
                (Agent as RTSAgent).SetTeamColor();
            }
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "NeedsBuilding", _needsBuilding);
            if (_needsBuilding)
            {
                SaveManager.WriteRect(writer, "PlayingArea", (Agent as RTSAgent).GetPlayerArea());
            }
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "NeedsBuilding":
                    _needsBuilding = (bool)readValue;
                    break;
                case "PlayingArea":
                    (Agent as RTSAgent).SetPlayingArea(LoadManager.LoadRect(reader));
                    break;
                default: break;
            }
        }
    }
}