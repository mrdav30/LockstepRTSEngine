using UnityEngine;
using System.Collections; using FastCollections;
using System;
using System.Collections.Generic;

using RTSLockstep.Data;
namespace RTSLockstep.Data
{
	[Serializable]
	public class WorldObjectDataItem : ObjectDataItem, IWorldObjectData
	{
        public WorldObjectDataItem(string name, string description) : this(){
            base._name = name;
            base._description = description;
        }
        public WorldObjectDataItem(){
            
        }

        public GameObject GetWorldObject () {
            if (this.Prefab != null)
            {
                GameObject worldObject = this.Prefab.gameObject;
                if (worldObject) {
                    return worldObject;
                }
            }
            return null;
        }

#if UNITY_EDITOR
		GameObject lastPrefab;
		protected override void OnManage ()
		{
			
			if (lastPrefab != Prefab) {
				if (string.IsNullOrEmpty(Name))
					this._name = Prefab.name;
				lastPrefab = Prefab;
			}
		}	
#endif
	}
}
