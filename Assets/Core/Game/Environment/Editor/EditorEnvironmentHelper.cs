using UnityEngine;
using UnityEditor;
using RTSLockstep.Environment;

namespace RTSLockstep
{
    [CustomEditor(typeof(EnvironmentHelper))]
    public class EditorEnvironmentHelper : Editor
    {
        public override void OnInspectorGUI()
        {
            EnvironmentHelper saver = (EnvironmentHelper)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("BasePriority"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_saverObject"));

            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("Scan and Save"))
            {
                saver.ScanAndSave();
            }

        }
    }
}