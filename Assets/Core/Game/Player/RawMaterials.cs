using System;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

using RTSLockstep.LSResources;

namespace RTSLockstep.Player
{
    [Serializable]
    public class RawMaterialValues
    {
        [Range(0, 5000)]
        public long CurrentValue;
        [Range(0, 5000)]
        public long CurrentLimit;
    }

    [Serializable]
    public class RawMaterials : SerializableDictionaryBase<RawMaterialType, RawMaterialValues> { };

    [Serializable]
    public class RawMaterialCost : SerializableDictionaryBase<RawMaterialType, int> { };
}
