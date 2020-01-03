using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;

namespace RTSLockstep
{
    [CustomEditor(typeof(HUD))]
    public class EditorHUD : Editor
    {
        private HUD targetValue;

        private void OnEnable()
        {
            targetValue = (HUD)target;

            if (targetValue.RawMaterialIcons.IsNull() || targetValue.RawMaterialIcons.Count != GameResourceManager.GameRawMaterials.Length)
            {
                targetValue.RawMaterialIcons = new RawMaterialsHUD();
                foreach (var type in GameResourceManager.GameRawMaterials)
                {
                    targetValue.RawMaterialIcons.Add(type, null);
                }
            }
        }
    }
}
