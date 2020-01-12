using UnityEditor;
using UnityEngine;

using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.LSEditor
{
    [CustomEditor(typeof(UnityLSBody), true), CanEditMultipleObjects]
    public class EditorLSBody : Editor
    {
        //Enum
        private SerializedProperty _shape;
        //bool
        private SerializedProperty _isTrigger;
        //int
        private SerializedProperty _layer;
        //int
        private SerializedProperty _basePriority;
        //long
        private SerializedProperty _halfWidth;
        //long
        private SerializedProperty _halfLength;
        //long
        private SerializedProperty _radius;
        //bool
        private SerializedProperty _immovable;
        //Vector2d[]
        private SerializedProperty _vertices;
        //long
        private SerializedProperty _height;
        //transform
        private SerializedProperty _positionalTransform;
        //transform
        private SerializedProperty _rotationalTransform;

        private bool _moreThanOne;

        private static GUIStyle _labelStyle;
        public static GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle.IsNull())
                {
                    _labelStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 20
                    };

                }

                return _labelStyle;
            }
        }
        public static float MoveHandleSize { get { return .6f; } }

        private void OnEnable()
        {
            _moreThanOne = targets.Length > 1;
            _shape = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_shape");
            _isTrigger = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_isTrigger");
            _layer = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_layer");
            _basePriority = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_basePriority");
            _halfWidth = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_halfWidth");
            _halfLength = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_halfLength");
            _radius = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_radius");
            _immovable = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_immovable");
            _vertices = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_vertices");
            _height = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_height");
            _positionalTransform = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_positionalTransform");
            _rotationalTransform = serializedObject.FindProperty("_internalBody").FindPropertyRelative("_rotationalTransform");
        }

        private void ResetValues()
        {
            _shape.intValue = 0;
            _isTrigger.boolValue = false;
            _halfWidth.longValue = 0;
            _halfLength.longValue = 0;
            _radius.longValue = 0;
            _immovable.boolValue = false;
            _vertices.arraySize = 0;
            _height.longValue = 0;
        }

        public override void OnInspectorGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUI.BeginChangeCheck();
                if (GUILayout.Button("Reset Transforms"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        SerializedObject ser = new SerializedObject(targets[i]);

                        ser.FindProperty("_internalBody").FindPropertyRelative("_positionalTransform").objectReferenceValue = ((UnityLSBody)targets[i]).transform;
                        ser.FindProperty("_internalBody").FindPropertyRelative("_rotationalTransform").objectReferenceValue = ((UnityLSBody)targets[i]).transform;
                        ser.ApplyModifiedProperties();

                        ser.Dispose();
                    }

                    serializedObject.Update();
                }
                if (targets.Length == 1)
                {
                    _positionalTransform.Draw();
                    _rotationalTransform.Draw();
                }

                _shape.Draw();
                ColliderType shape = (ColliderType)_shape.intValue;
                if (shape != ColliderType.None)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("General Collider Settings", EditorStyles.boldLabel);
                    _layer.Draw();
                    _basePriority.Draw();
                    _isTrigger.Draw();
                    if (!_isTrigger.boolValue)
                    {
                        _immovable.Draw();
                    }

                    _height.Draw();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
                    if (shape == ColliderType.Circle)
                    {
                        _radius.Draw();
                    }
                    else if (shape == ColliderType.AABox)
                    {
                        _halfWidth.Draw();
                        _halfLength.Draw();
                    }
                    else if (shape == ColliderType.Polygon)
                    {
                        EditorGUIUtility.labelWidth = 0;
                        EditorGUIUtility.fieldWidth = 0;

                        _vertices.Draw();
                    }
                }
                else
                {
                    ResetValues();
                }

                SceneView.RepaintAll();
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                //Debug view when playing
                EditorGUILayout.LabelField("Runtime Debugging", EditorStyles.boldLabel);
                base.OnInspectorGUI();
            }
        }

        void OnSceneGUI()
        {
            if (!_moreThanOne)
            {
                //Have to reinitialize everything because can't apply modified properties on base.serializedObject
                SerializedObject so = new SerializedObject(target);
                so.Update();
                SerializedProperty Shape = so.FindProperty("_internalBody").FindPropertyRelative("_shape");
                SerializedProperty HalfWidth = so.FindProperty("_internalBody").FindPropertyRelative("_halfWidth");
                SerializedProperty HalfLength = so.FindProperty("_internalBody").FindPropertyRelative("_halfLength");
                SerializedProperty Radius = so.FindProperty("_internalBody").FindPropertyRelative("_radius");
                SerializedProperty Height = so.FindProperty("_internalBody").FindPropertyRelative("_height");

                ColliderType shape = (ColliderType)Shape.intValue;

                if (shape != ColliderType.None)
                {
                    Handles.color = Color.blue;
                    LSBody Body = ((UnityLSBody)target).InternalBody;
                    Transform transform = ((UnityLSBody)target).transform;
                    Vector3 targetPos = transform.position;
                    const int ImprecisionLimit = 100000;
                    if (Mathf.Abs(targetPos.x) <= ImprecisionLimit
                        && Mathf.Abs(targetPos.y) <= ImprecisionLimit
                        && Mathf.Abs(targetPos.z) <= ImprecisionLimit)
                    {
                        const float spread = .02f;
                        int spreadMin = -1;
                        int spreadMax = 1;
                        Handles.CapFunction dragCap = Handles.SphereHandleCap;
                        float height = targetPos.y;
                        float xModifier = 0f;
                        if (shape == ColliderType.Circle)
                        {
                            //Minus so the move handle doesn't end up on the same axis as the transform.position move handle
                            float oldRadius = Radius.longValue.ToFloat();
                            float newRadius = Mathf.Abs((Handles.FreeMoveHandle(new Vector3(targetPos.x - Radius.longValue.ToFloat(), targetPos.y, targetPos.z),
                                        Quaternion.identity,
                                        MoveHandleSize,
                                        Vector3.zero,
                                        Handles.SphereHandleCap)).x - targetPos.x);
                            if (Mathf.Abs(oldRadius - newRadius) >= .02f)
                            {
                                Radius.longValue = FixedMath.Create(newRadius);
                            }

                            Handles.DrawLine(targetPos, new Vector3(targetPos.x + Radius.longValue.ToFloat(), targetPos.y, targetPos.z));
                            float baseHeight = targetPos.y;
                            for (int i = spreadMin; i <= spreadMax; i++)
                            {
                                Handles.CircleHandleCap(1,
                                    new Vector3(targetPos.x, baseHeight + i * spread, targetPos.z),
                                    Quaternion.Euler(90, 0, 0),
                                    Radius.longValue.ToFloat(),
                                    EventType.Repaint);
                            }

                            baseHeight = targetPos.y + Height.longValue.ToFloat();
                            for (int i = spreadMin; i <= spreadMax; i++)
                            {
                                Handles.CircleHandleCap(1,
                                    new Vector3(targetPos.x, baseHeight + i * spread, targetPos.z),
                                    Quaternion.Euler(90, 0, 0),
                                    Radius.longValue.ToFloat(),
                                    EventType.Repaint);
                            }

                            xModifier = 0;//Radius.longValue.ToFloat();

                        }
                        else if (shape == ColliderType.AABox)
                        {
                            float oldWidth = HalfWidth.longValue.ToFloat();
                            float newWidth = Mathf.Abs(Handles.FreeMoveHandle(new Vector3(targetPos.x - (float)HalfWidth.longValue.ToFormattedDouble(), targetPos.y, targetPos.z),
                                        Quaternion.identity,
                                        MoveHandleSize,
                                        Vector3.zero,
                                        dragCap).x - targetPos.x);
                            if (Mathf.Abs(newWidth - oldWidth) >= .02f)
                            {
                                HalfWidth.longValue = FixedMath.Create(newWidth);
                            }

                            float oldLength = HalfLength.longValue.ToFloat();
                            float newLength = System.Math.Abs(Handles.FreeMoveHandle(new Vector3(targetPos.x, targetPos.y, targetPos.z - (float)HalfLength.longValue.ToFormattedDouble()),
                                        Quaternion.identity,
                                        MoveHandleSize,
                                        Vector3.zero,
                                        dragCap).z - targetPos.z);
                            if (Mathf.Abs(newLength - oldLength) >= .02f)
                            {
                                HalfLength.longValue = FixedMath.Create(newLength);
                            }

                            float halfWidth = HalfWidth.longValue.ToFloat();
                            float halfLength = HalfLength.longValue.ToFloat();
                            for (int i = 0; i < 1; i++)
                            {
                                height = targetPos.y + i * spread;
                                Vector3[] lines = new Vector3[]
                                {
                        new Vector3(targetPos.x + halfWidth, height, targetPos.z + halfLength),
                        new Vector3(targetPos.x + halfWidth, height, targetPos.z - halfLength),

                        new Vector3(targetPos.x + halfWidth, height, targetPos.z - halfLength),
                        new Vector3(targetPos.x - halfWidth, height, targetPos.z - halfLength),

                        new Vector3(targetPos.x - halfWidth, height, targetPos.z - halfLength),
                        new Vector3(targetPos.x - halfWidth, height, targetPos.z + halfLength),

                        new Vector3(targetPos.x - halfWidth, height, targetPos.z + halfLength),
                        new Vector3(targetPos.x + halfWidth, height, targetPos.z + halfLength)
                                };
                                Handles.DrawPolyLine(lines);
                            }

                            for (int i = 0; i < 1; i++)
                            {
                                height = targetPos.y + i * spread + Height.longValue.ToFloat();
                                Vector3[] lines = new Vector3[]
                                {
                        new Vector3(targetPos.x + halfWidth, height, targetPos.z + halfLength),
                        new Vector3(targetPos.x + halfWidth, height, targetPos.z - halfLength),

                        new Vector3(targetPos.x + halfWidth, height, targetPos.z - halfLength),
                        new Vector3(targetPos.x - halfWidth, height, targetPos.z - halfLength),

                        new Vector3(targetPos.x - halfWidth, height, targetPos.z - halfLength),
                        new Vector3(targetPos.x - halfWidth, height, targetPos.z + halfLength),

                        new Vector3(targetPos.x - halfWidth, height, targetPos.z + halfLength),
                        new Vector3(targetPos.x + halfWidth, height, targetPos.z + halfLength)
                                };
                                Handles.DrawPolyLine(lines);
                            }

                            xModifier = 0;//halfWidth;
                        }
                        else if (shape == ColliderType.Polygon)
                        {
                            float yRot = transform.eulerAngles.y * Mathf.Deg2Rad;

                            Vector2d rotation = Vector2d.CreateRotation(yRot);
                            bool changed = false;

                            Vector3[] draws = new Vector3[Body.Vertices.Length + 1];

                            for (int i = 0; i < Body.Vertices.Length; i++)
                            {
                                Vector2d vertex = Body.Vertices[i];
                                vertex.Rotate(rotation.x, rotation.y);
                                Vector3 drawPos = vertex.ToVector3() + targetPos;
                                Vector3 newDrawPos = Handles.FreeMoveHandle(drawPos, Quaternion.identity,
                                    MoveHandleSize,
                                    new Vector3(0, float.PositiveInfinity, 0),
                                    Handles.SphereHandleCap);
                                if ((newDrawPos - (drawPos)).magnitude >= .01f)
                                {
                                    newDrawPos -= targetPos;
                                    vertex = new Vector2d(newDrawPos);
                                    vertex.RotateInverse(rotation.x, rotation.y);
                                    Body.Vertices[i] = vertex;
                                    changed = true;
                                }

                                draws[i] = drawPos;
                                Handles.Label(drawPos, "V: " + i.ToString(), LabelStyle);
                            }

                            if (Body.Vertices.Length > 0)
                            {
                                draws[draws.Length - 1] = draws[0];
                                Handles.DrawPolyLine(draws);
                                for (int i = 0; i < draws.Length; i++)
                                {
                                    Vector3 highPos = draws[i];
                                    highPos.y += Body.Height.ToFloat();
                                    Handles.DrawLine(draws[i], highPos);
                                    draws[i] = highPos;
                                }

                                Handles.DrawPolyLine(draws);
                            }

                            if (changed)
                            {
                                so.Update();
                            }
                        }

                        Handles.DrawLine(new Vector3(targetPos.x + xModifier, targetPos.y, targetPos.z),
                            new Vector3(targetPos.x + xModifier, targetPos.y + Height.longValue.ToFloat(), targetPos.z));

                        Vector3 movePos = targetPos;
                        movePos.x += xModifier;
                        movePos.y += (float)Height.longValue.ToFormattedDouble();
                        Vector3 lastMovePos = movePos;
                        movePos = Handles.FreeMoveHandle(movePos,
                            Quaternion.identity,
                            MoveHandleSize,
                            Vector3.zero,
                            dragCap);

                        if ((lastMovePos - movePos).sqrMagnitude >= .1f)
                        {
                            Height.longValue = FixedMath.Create(Mathf.Max(Mathf.Abs(movePos.y - targetPos.y)));
                        }

                        so.ApplyModifiedProperties();
                    }
                }

                so.Dispose();
            }          
        }
    }

    internal static class SerializedPropertyDraw
    {
        public static void Draw(this SerializedProperty prop)
        {
            if (prop.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Fields with different values not multi-editable", MessageType.None);
                //TODO: Throw something like what default inspectors do
                return;
            }

            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
