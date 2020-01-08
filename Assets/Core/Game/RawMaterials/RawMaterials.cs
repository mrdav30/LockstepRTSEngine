using System;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

using RTSLockstep.LSResources;

namespace RTSLockstep.RawMaterials
{
    [Serializable]
    public class RawMaterialLimited
    {
        [Range(0, 5000)]
        public long CurrentValue;
        [Range(0, 5000)]
        public long CurrentLimit;
    }

    [Serializable]
    public class RawMaterialSetLimit : SerializableDictionaryBase<RawMaterialType, RawMaterialLimited> { };

    [Serializable]
    public class RawMaterialSetValue : SerializableDictionaryBase<RawMaterialType, int> { };
}
