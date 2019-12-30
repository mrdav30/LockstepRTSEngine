using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using UnityEngine;

namespace RTSLockstep.Grouping
{
    [DisallowMultipleComponent]
    public class HarvestGroupHelper : BehaviourHelper
    {
        public override ushort ListenInput
        {
            get
            {
                return AbilityDataItem.FindInterfacer(typeof(Harvest)).ListenInputID;
            }
        }
        public static HarvestGroup LastCreatedGroup { get; private set; }
        private static readonly FastBucket<HarvestGroup> activeGroups = new FastBucket<HarvestGroup>();
        private static readonly FastStack<HarvestGroup> pooledGroups = new FastStack<HarvestGroup>();

        public static HarvestGroupHelper Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
            activeGroups.FastClear();
        }

        protected override void OnSimulate()
        {
            for (int i = 0; i < activeGroups.PeakCount; i++)
            {
                if (activeGroups.arrayAllocation[i])
                {
                    HarvestGroup harvestGroup = activeGroups[i];
                    harvestGroup.LocalSimulate();
                }
            }
        }

        protected override void OnLateSimulate()
        {
            for (int i = 0; i < activeGroups.PeakCount; i++)
            {
                if (activeGroups.arrayAllocation[i])
                {
                    HarvestGroup harvestGroup = activeGroups[i];
                    harvestGroup.LateSimulate();
                }
            }
        }

        private static bool CheckValid()
        {
            return Instance != null;
        }

        public static bool CheckValidAndAlert()
        {
            if (CheckValid())
            {
                return true;
            }

            Debug.LogError("No instance of HarvestGroupHelper found. Please configure the scene to have a HarvestGroupHelper for the script that requires it.");
            return false;
        }

        protected override void OnExecute(Command com)
        {
            StaticExecute(com);
        }

        public static void StaticExecute(Command com)
        {
            CreateGroup(com);
        }

        private static void CreateGroup(Command com)
        {
            HarvestGroup harvestGroup = pooledGroups.Count > 0 ? pooledGroups.Pop() : new HarvestGroup();

            harvestGroup.IndexID = activeGroups.Add(harvestGroup);
            LastCreatedGroup = harvestGroup;
            harvestGroup.Initialize(com);
        }

        public static void Pool(HarvestGroup group)
        {
            int indexID = group.IndexID;
            activeGroups.RemoveAt(indexID);
            pooledGroups.Add(group);
        }

        protected override void OnDeactivate()
        {
            Instance = null;
            LastCreatedGroup = null;
        }
    }
}