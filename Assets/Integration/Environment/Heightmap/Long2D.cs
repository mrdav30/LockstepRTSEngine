using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep
{
    [System.Serializable]
    public class Long2D : RTSLockstep.Array2D<long>
    {
        public Long2D () {

        }
        public Long2D (int width, int height) :base (width,height) {

        }
    }
}