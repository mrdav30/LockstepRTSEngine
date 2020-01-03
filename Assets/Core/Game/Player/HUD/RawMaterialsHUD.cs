using System;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

using RTSLockstep.LSResources;

namespace RTSLockstep.Player
{
    [Serializable]
    public class RawMaterialsHUDInfo
    {
        public Texture2D RawMaterialIcon;
        public Texture2D RawMaterialHealthBar;
    }

    [Serializable]
    public class RawMaterialsHUD : SerializableDictionaryBase<RawMaterialType, RawMaterialsHUDInfo> { };
}
