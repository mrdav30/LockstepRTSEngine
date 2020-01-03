using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;

namespace RTSLockstep
{
    [CustomEditor(typeof(RawMaterialManager))]
    public class EditorPlayerResourceManager : Editor
    {
        private RawMaterialManager targetValue;

        private void OnEnable()
        {
            targetValue = (RawMaterialManager)target;

            if (targetValue.BaseRawMaterials.IsNull() || targetValue.BaseRawMaterials.Count != GameResourceManager.GameRawMaterials.Length)
            {
                targetValue.BaseRawMaterials = new RawMaterials();
                foreach (var type in GameResourceManager.GameRawMaterials)
                {
                    targetValue.BaseRawMaterials.Add(type, null);
                }
            }
        }
    }
}
