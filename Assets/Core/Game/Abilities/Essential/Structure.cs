using Assets.Integration.CustomComparison;
using Newtonsoft.Json;
using UnityEngine;

namespace RTSLockstep
{
    /*
     * Essential ability that attaches to any active structure 
     */
    public class Structure : Ability, IBuildable
    {
        public bool provisioner;
        public int provisionAmount;
        [SerializeField, Tooltip("Enter object names for resources this structure can store.")]
        private ResourceType[] _resourceStorage;
        public StructureType StructureType;
        /// <summary>
        /// Every wall pillar needs a corresponding section of wall to hold up.
        /// </summary>
        /// <value>The game object of the wall segement prefab</value>
        [DrawIf("StructureType", ComparisonType.Equals, StructureType.Wall)]
        public GameObject WallSegmentGO;
        /// <summary>
        /// Describes the width and height of the buildable. This value does not change on the buildable.
        /// </summary>
        /// <value>The size of the build.</value>
        public int BuildSizeLow { get; set; }
        public int BuildSizeHigh { get; set; }

        public Coordinate GridPosition { get; set; }
        /// <summary>
        /// Function that relays to the buildable whether or not it's on a valid building spot.
        /// </summary>
        public bool IsValidOnGrid { get; set; }
        public bool IsMoving { get; set; }
        public bool IsOverlay { get; set; }

        public bool ValidPlacement { get; set; }
        public bool ConstructionStarted { get; set; }
        public bool NeedsConstruction { get; private set; }
        private bool _needsRepair;
        private bool _provisioned;
        private int upgradeLevel;

        private Rally cachedRally;

        protected override void OnSetup()
        {
            cachedRally = Agent.GetAbility<Rally>();

            upgradeLevel = 1;
        }

        protected override void OnInitialize()
        {
            NeedsConstruction = false;
            _needsRepair = false;
            _provisioned = false;
        }

        protected override void OnSimulate()
        {
            if (!NeedsConstruction && Agent.MyStats.CachedHealth.CurrentHealth != Agent.MyStats.CachedHealth.MaxHealth)
            {
                _needsRepair = true;
            }

            if (Agent.MyStats.CachedHealth.CurrentHealth == Agent.MyStats.CachedHealth.MaxHealth)
            {
                if (provisioner && !_provisioned)
                {
                    _provisioned = true;
                    Agent.GetCommander().CachedResourceManager.IncrementResourceLimit(ResourceType.Provision, provisionAmount);
                }
            }
        }

        public void AwaitConstruction()
        {
            NeedsConstruction = true;
            IsCasting = true;
            Agent.MyStats.CachedHealth.CurrentHealth = FixedMath.Create(0);

            if (cachedRally)
            {
                cachedRally.SetSpawnPoint();
            }
        }

        public void BuildUp(long amount)
        {
            Agent.MyStats.CachedHealth.CurrentHealth += amount;
            if (Agent.MyStats.CachedHealth.CurrentHealth >= Agent.MyStats.CachedHealth.BaseHealth)
            {
                Agent.MyStats.CachedHealth.CurrentHealth = Agent.MyStats.CachedHealth.BaseHealth;
                NeedsConstruction = false;
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

        public bool CanStoreResources(ResourceType resourceType)
        {

            if (_resourceStorage.Length > 0)
            {
                for (int i = 0; i < _resourceStorage.Length; i++)
                {
                    if (_resourceStorage[i] == resourceType)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;

        }

        public void SetGridPosition(Vector2d pos)
        {
            Coordinate coord = new Coordinate(pos.x.ToInt(), pos.y.ToInt());
            GridPosition = coord;
        }

        protected override void OnDeactivate()
        {
            if (provisioner)
            {
                Agent.GetCommander().CachedResourceManager.DecrementResourceLimit(ResourceType.Provision, provisionAmount);
            }

            GridBuilder.Unbuild(this);
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "NeedsBuilding", NeedsConstruction);
            SaveManager.WriteBoolean(writer, "NeedsRepair", _needsRepair);
            if (NeedsConstruction)
            {
                SaveManager.WriteRect(writer, "PlayingArea", Agent.GetPlayerArea());
            }
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "NeedsBuilding":
                    NeedsConstruction = (bool)readValue;
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