using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using UnityEngine;

namespace RTSLockstep.Grouping
{
    [DisallowMultipleComponent]
    public class AttackGroupHelper : BehaviourHelper
    {
        public override ushort ListenInput
        {
            get
            {
                return AbilityDataItem.FindInterfacer(typeof(Attack)).ListenInputID;
            }
        }
        public static AttackGroup LastCreatedGroup { get; private set; }
        private static readonly FastBucket<AttackGroup> activeGroups = new FastBucket<AttackGroup>();
        private static readonly FastStack<AttackGroup> pooledGroups = new FastStack<AttackGroup>();

        public static AttackGroupHelper Instance { get; private set; }

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
                    AttackGroup attackGroup = activeGroups[i];
                    attackGroup.LocalSimulate();
                }
            }
        }

        protected override void OnLateSimulate()
        {
            for (int i = 0; i < activeGroups.PeakCount; i++)
            {
                if (activeGroups.arrayAllocation[i])
                {
                    AttackGroup attackGroup = activeGroups[i];
                    attackGroup.LateSimulate();
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

            Debug.LogError("No instance of AttackGroupHelper found. Please configure the scene to have a AttackGroupHelper for the script that requires it.");
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
            AttackGroup attackGroup = pooledGroups.Count > 0 ? pooledGroups.Pop() : new AttackGroup();

            attackGroup.IndexID = activeGroups.Add(attackGroup);
            LastCreatedGroup = attackGroup;
            attackGroup.Initialize(com);
        }

        public static void Pool(AttackGroup group)
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