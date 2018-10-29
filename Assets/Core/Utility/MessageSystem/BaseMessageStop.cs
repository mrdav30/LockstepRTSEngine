using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep
{
    public abstract class BaseMessageStop
    {
        public BaseMessageStop()
        {

        }

        public abstract BaseMessageChannel GetChannel(string channelID);
        public abstract void Clear ();
    }
}