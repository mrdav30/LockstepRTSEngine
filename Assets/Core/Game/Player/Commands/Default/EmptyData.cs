using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep.Data;
namespace RTSLockstep
{
    public struct EmptyData : ICommandData
    {
        public void Read (Reader reader) {

        }
        public void Write (Writer writer) {

        }
    }
}