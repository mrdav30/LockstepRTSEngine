using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;

namespace RTSLockstep.Data
{
    [System.Serializable]
    public class EffectDataItem : ObjectDataItem, IEffectData
    {
        public LSEffect GetEffect () {
            return base.Prefab.GetComponent<LSEffect> ();
        }
    }
}