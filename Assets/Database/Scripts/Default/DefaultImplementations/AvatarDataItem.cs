using UnityEngine;
using System.Collections; using FastCollections;
using System;
using System.Collections.Generic;

using RTSLockstep.Data;
namespace RTSLockstep.Data
{
	[Serializable]
	public class AvatarDataItem : MetaDataItem, IAvatarData
	{


        public AvatarDataItem (string name, string description) : this(){
            base._name = name;
            base._description = description;
        }
        public AvatarDataItem(){
            
        }

        public Texture2D GetAvatar () {
            if (this.Icon != null)
            {
                Texture2D icon = this.Icon.texture;
                if (icon) {
                    return icon;
                }
            }
            return null;
        }
	}
}
