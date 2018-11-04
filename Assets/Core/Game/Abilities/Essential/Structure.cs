using Newtonsoft.Json;
using UnityEngine;

namespace RTSLockstep
{
    public class Structure : Ability
    {
        public bool provisioner;
        public int provisionAmount;

        private bool _needsBuilding;
        private bool _needsRepair;
        private bool _provisioned;
        private int upgradeLevel;
        private Health cachedHealth;
        private Spawner cachedSpawner;

        protected override void OnSetup()
        {
            cachedHealth = Agent.GetAbility<Health>();
            cachedSpawner = Agent.GetAbility<Spawner>();
        }

        protected override void OnInitialize()
        {
            _needsBuilding = false;
            _needsRepair = false;
            _provisioned = false;

            upgradeLevel = 1;
        }

        protected override void OnSimulate()
        {
            if (!_needsBuilding && cachedHealth.HealthAmount != cachedHealth.MaxHealth)
            {
                _needsRepair = true;
            }

            if (cachedHealth.HealthAmount == cachedHealth.MaxHealth)
            {
                if (provisioner && !_provisioned)
                {
                    _provisioned = true;
                    Agent.GetCommander().CachedResourceManager.IncrementResourceLimit(ResourceType.Provision, provisionAmount);
                }
            }
        }

        public void StartConstruction()
        {
            Agent.Body.CalculateBounds();
            _needsBuilding = true;
            IsCasting = true;
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
                IsCasting = false;
                Agent.SetTeamColor();
                if (provisioner && !_provisioned)
                {
                    _provisioned = true;
                    Agent.GetCommander().CachedResourceManager.IncrementResourceLimit(ResourceType.Provision, provisionAmount);
                }
            }
        }

        public int GetUpgradeLevel()
        {
            return this.upgradeLevel;
        }

        protected override void OnDeactivate()
        {
            if (provisioner)
            {
                Agent.GetCommander().CachedResourceManager.DecrementResourceLimit(ResourceType.Provision, provisionAmount);
            }
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "NeedsBuilding", _needsBuilding);
            SaveManager.WriteBoolean(writer, "NeedsRepair", _needsRepair);
            if (_needsBuilding)
            {
                SaveManager.WriteRect(writer, "PlayingArea", Agent.GetPlayerArea());
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
                case "NeedsRepair":
                    _needsRepair = (bool)readValue;
                    break;
                case "PlayingArea":
                    Agent.SetPlayingArea(LoadManager.LoadRect(reader));
                    break;
                default: break;
            }
        }
    }
}