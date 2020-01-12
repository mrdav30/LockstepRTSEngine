using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;

namespace RTSLockstep
{
    [CustomEditor(typeof(RTSHud))]
    public class EditorHUD : Editor
    {
        private RTSHud targetValue;

        private void OnEnable()
        {
            targetValue = (RTSHud)target;

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
