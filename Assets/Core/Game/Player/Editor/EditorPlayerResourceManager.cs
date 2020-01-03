using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.LSResources;
using System;

namespace RTSLockstep
{
    [CustomEditor(typeof(RawMaterialManager))]
    public class EditorPlayerResourceManager : Editor
    {
        private RawMaterialManager targetValue;

        private void OnEnable()
        {
            targetValue = (RawMaterialManager)target;

            RawMaterialType[] values = (RawMaterialType[])Enum.GetValues(typeof(RawMaterialType));
            if (targetValue.BaseRawMaterials.IsNull() || targetValue.BaseRawMaterials.Count != values.Length)
            {
                foreach (var type in values)
                {
                    targetValue.BaseRawMaterials.Add(type, null);
                }
            }
        }
    }
}
