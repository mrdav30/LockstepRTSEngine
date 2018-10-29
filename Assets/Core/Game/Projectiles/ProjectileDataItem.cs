using UnityEngine;
using System.Collections; using FastCollections;
using System;
using RTSLockstep;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace RTSLockstep.Data
{
	[Serializable]
	public class ProjectileDataItem : ObjectDataItem, IProjectileData
	{
        public LSProjectile GetProjectile () {
            return base.Prefab.GetComponent<LSProjectile>();
        }
    }
}