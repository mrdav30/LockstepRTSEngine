using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using UnityEngine;

namespace RTSLockstep.Grouping
{
    public class ConstructionGroupHelper : BehaviourHelper
    {
        public override ushort ListenInput
        {
            get
            {
                return AbilityDataItem.FindInterfacer(typeof(Construct)).ListenInputID;
            }
        }

        public static ConstructGroup LastCreatedGroup { get; private set; }
        private static readonly FastBucket<ConstructGroup> activeGroups = new FastBucket<ConstructGroup>();
        private static readonly FastStack<ConstructGroup> pooledGroups = new FastStack<ConstructGroup>();

        public static ConstructionGroupHelper Instance { get; private set; }

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
                    ConstructGroup constructGroup = activeGroups[i];
                    constructGroup.LocalSimulate();
                }
            }
        }

        protected override void OnLateSimulate()
        {
            for (int i = 0; i < activeGroups.PeakCount; i++)
            {
                if (activeGroups.arrayAllocation[i])
                {
                    ConstructGroup constructGroup = activeGroups[i];
                    constructGroup.LateSimulate();
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

            Debug.LogError("No instance of ConstructionGroupHelper found. Please configure the scene to have a ConstructionGroupHelper for the script that requires it.");
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
            ConstructGroup constructGroup = pooledGroups.Count > 0 ? pooledGroups.Pop() : new ConstructGroup();

            constructGroup.IndexID = activeGroups.Add(constructGroup);
            LastCreatedGroup = constructGroup;
            constructGroup.Initialize(com);
        }

        public static void Pool(ConstructGroup group)
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