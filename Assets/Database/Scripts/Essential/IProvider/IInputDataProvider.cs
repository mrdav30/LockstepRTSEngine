using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IInputDataProvider
    {
        InputDataItem[] InputData { get; }
    }
}