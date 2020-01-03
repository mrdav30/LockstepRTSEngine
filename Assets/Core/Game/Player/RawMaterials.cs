using RotaryHeart.Lib.SerializableDictionary;
using RTSLockstep.LSResources;
using System;
using UnityEngine;

namespace RTSLockstep.Player
{
    [Serializable]
    public class RawMaterialInfo
    {
        [Range(0, 5000)]
        public long currentValue;
        [Range(0, 5000)]
        public long currentLimit;
    }

    [Serializable]
    public class RawMaterials : SerializableDictionaryBase<RawMaterialType, RawMaterialInfo> { };
}
