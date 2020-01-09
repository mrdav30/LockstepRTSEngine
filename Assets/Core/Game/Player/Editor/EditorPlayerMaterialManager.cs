using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.RawMaterials;

namespace RTSLockstep
{
    [CustomEditor(typeof(PlayerMaterialManager))]
    public class EditorPlayerMaterialManager : Editor
    {
        private PlayerMaterialManager targetValue;

        private void OnEnable()
        {
            targetValue = (PlayerMaterialManager)target;

            if (targetValue.BaseRawMaterials.IsNull() || targetValue.BaseRawMaterials.Count != GameResourceManager.GameRawMaterials.Length)
            {
                targetValue.BaseRawMaterials = new RawMaterialSetLimit();
                foreach (var type in GameResourceManager.GameRawMaterials)
                {
                    targetValue.BaseRawMaterials.Add(type, null);
                }
            }
        }
    }
}
