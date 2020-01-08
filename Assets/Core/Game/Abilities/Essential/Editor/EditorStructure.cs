using UnityEditor;

using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.LSResources;
using RTSLockstep.RawMaterials;

namespace RTSLockstep
{
    [CustomEditor(typeof(Structure))]
    public class EditorStructure : Editor
    {
        private Structure targetValue;

        private void OnEnable()
        {
            targetValue = (Structure)target;

            if (targetValue.CanStoreRawMaterial
                && (targetValue.RawMaterialStorageDetails.Count == 0
                || targetValue.RawMaterialStorageDetails.Count != GameResourceManager.GameRawMaterials.Length))
            {
                targetValue.RawMaterialStorageDetails = new RawMaterialSetLimit();
                foreach (var type in GameResourceManager.GameRawMaterials)
                {
                    targetValue.RawMaterialStorageDetails.Add(type, null);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            LSEditorUtility.PropertyField(serializedObject, "CanProvision");
            if (targetValue.CanProvision)
            {
                LSEditorUtility.PropertyField(serializedObject, "ProvisionAmount");
            }
            else if (targetValue.ProvisionAmount > 0)
            {
                targetValue.ProvisionAmount = 0;
            }
            LSEditorUtility.PropertyField(serializedObject, "CanStoreRawMaterial");
            if (targetValue.CanStoreRawMaterial)
            {
                if (targetValue.RawMaterialStorageDetails.Count == 0)
                {
                    targetValue.RawMaterialStorageDetails = new RawMaterialSetLimit();
                    foreach (var type in GameResourceManager.GameRawMaterials)
                    {
                        targetValue.RawMaterialStorageDetails.Add(type, null);
                    }
                }
                else
                {
                    LSEditorUtility.PropertyField(serializedObject, "RawMaterialStorageDetails");
                }
            }
            else if (targetValue.RawMaterialStorageDetails.Count > 0)
            {
                targetValue.RawMaterialStorageDetails.Clear();
            }
            LSEditorUtility.PropertyField(serializedObject, "StructureType");
            if(targetValue.StructureType == StructureType.Wall)
            {
                LSEditorUtility.PropertyField(serializedObject, "WallSegmentGO");
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(targetValue);
            }
        }
    }
}
