using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Utility;
using System.Linq;
using UnityEditor;

namespace RTSLockstep
{
    [CustomEditor(typeof(RTSGameManager))]
    public class EditorGameManager : Editor
    {
        private RTSGameManager targetValue;

        private void OnEnable()
        {
            targetValue = (RTSGameManager)target;

            BehaviourHelper[] helpers = targetValue.gameObject.GetComponentsInChildren<BehaviourHelper>();

            if (targetValue.BehaviourHelpers.IsNull()
                || targetValue.BehaviourHelpers.Length == 0
                || targetValue.BehaviourHelpers.Length != helpers.Length)
            {
                for (int i = 0; i < helpers.Length; i++)
                {
                    if (helpers[i].BasePriority <= 0)
                    {
                        helpers[i].BasePriority = 9999;
                    }
                }

                targetValue.BehaviourHelpers = helpers.OrderBy(c => c.BasePriority).ToArray();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Helper Event Priority", EditorStyles.boldLabel);
            LSEditorUtility.ListField(serializedObject.FindProperty("BehaviourHelpers"), LSEditorUtility.DisableAddRemove);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(targetValue);
            }
        }
    }
}
