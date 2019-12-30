using UnityEngine;
using System;

namespace RTSLockstep.Data
{
    [Serializable]
    public class AvatarDataItem : MetaDataItem, IAvatarData
    {


        public AvatarDataItem(string name, string description) : this()
        {
            base._name = name;
            base._description = description;
        }
        public AvatarDataItem()
        {

        }

        public Texture2D GetAvatar()
        {
            if (Icon != null)
            {
                Texture2D icon = Icon.texture;
                if (icon)
                {
                    return icon;
                }
            }
            return null;
        }
    }
}
