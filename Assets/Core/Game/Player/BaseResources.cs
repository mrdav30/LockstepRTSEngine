using RotaryHeart.Lib.SerializableDictionary;
using RTSLockstep.LSResources;
using System;
using UnityEngine;

namespace RTSLockstep.Player
{
    [Serializable]
    public class BaseResourceInfo
    {
        [Range(0, 5000)]
        public long startValue;
        [Range(0, 5000)]
        public long startLimit;
    }

    [Serializable]
    public class BaseResources : SerializableDictionaryBase<ResourceType, BaseResourceInfo> { };
}
