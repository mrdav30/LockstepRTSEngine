using RTSLockstep.LSResources;
using RTSLockstep.Projectiles;
using UnityEditor;

namespace RTSLockstep.Integration
{
    [CustomEditor(typeof(LSProjectile))]
    public class EditorLSProjectile : Editor
    {
        private SerializedObject _serializedObject { get { return serializedObject; } }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            //Targeting
            EditorGUILayout.LabelField("Targeting Settings", EditorStyles.boldLabel);
            _serializedObject.PropertyField("_targetingBehavior");
            TargetingType targetingBehavior = (TargetingType)_serializedObject.FindProperty("_targetingBehavior").enumValueIndex;
            switch (targetingBehavior)
            {
                case TargetingType.Directional:
                    _serializedObject.PropertyField("_speed");
                    break;
                case TargetingType.Positional:
                case TargetingType.Homing:
                    _serializedObject.PropertyField("_speed");
                    _serializedObject.PropertyField("_visualArc");
                    break;
                case TargetingType.Timed:
                    _serializedObject.PropertyField("_delay");
                    _serializedObject.PropertyField("_lastingDuration");
                    _serializedObject.PropertyField("_tickRate");
                    break;
            }
            EditorGUILayout.Space();

            //Damage
            EditorGUILayout.LabelField("Damage Settings", EditorStyles.boldLabel);
            _serializedObject.PropertyField("_hitBehavior");
            switch ((HitType)_serializedObject.FindProperty("_hitBehavior").enumValueIndex)
            {
                case HitType.Cone:
                    _serializedObject.PropertyField("_angle");
                    _serializedObject.PropertyField("_radius");
                    break;
                case HitType.Area:
                    _serializedObject.PropertyField("_radius");
                    break;

                case HitType.Single:

                    break;
            }
            EditorGUILayout.Space();

            //Trajectory
            EditorGUILayout.LabelField("Trajectory Settings", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            //Visuals
            EditorGUILayout.LabelField("Visuals Settings", EditorStyles.boldLabel);

            SerializedProperty useEffectProp = _serializedObject.FindProperty("UseEffects");

            EditorGUILayout.PropertyField(useEffectProp);


            if (useEffectProp.boolValue)
            {
                _serializedObject.PropertyField("_startFX");
                _serializedObject.PropertyField("_hitFX");
                _serializedObject.PropertyField("_attachEndEffectToTarget");
            }

            //PAPPS ADDED THIS:
            _serializedObject.PropertyField("DoReleaseChildren");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }
}