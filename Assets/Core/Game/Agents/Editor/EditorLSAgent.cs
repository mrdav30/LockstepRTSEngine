using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Agents;

namespace RTSLockstep
{
    [CustomEditor(typeof(LSAgent))]
    public class EditorLSAgent : Editor
    {
        private LSAgent targetValue;

        private void OnEnable()
        {
            targetValue = (LSAgent)target;

            if (targetValue.RawMaterialCost.IsNull() || targetValue.RawMaterialCost.Count != GameResourceManager.GameRawMaterials.Length)
            {
                targetValue.RawMaterialCost = new RawMaterialCost();
                foreach (var type in GameResourceManager.GameRawMaterials)
                {
                    targetValue.RawMaterialCost.Add(type, 0);
                }
            }
        }
    }
}
