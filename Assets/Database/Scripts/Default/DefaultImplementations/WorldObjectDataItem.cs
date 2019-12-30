using UnityEngine;
using System;

namespace RTSLockstep.Data
{
    [Serializable]
    public class WorldObjectDataItem : ObjectDataItem, IWorldObjectData
    {
        public WorldObjectDataItem(string name, string description) : this()
        {
            base._name = name;
            base._description = description;
        }
        public WorldObjectDataItem()
        {

        }

        public GameObject GetWorldObject()
        {
            if (Prefab != null)
            {
                GameObject worldObject = Prefab.gameObject;
                if (worldObject)
                {
                    return worldObject;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        GameObject lastPrefab;
        protected override void OnManage()
        {

            if (lastPrefab != Prefab)
            {
                if (string.IsNullOrEmpty(Name))
                    _name = Prefab.name;
                lastPrefab = Prefab;
            }
        }
#endif
    }
}
