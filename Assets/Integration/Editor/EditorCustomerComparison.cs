using RTSLockstep.Integration.CustomComparion;
using UnityEditor;
using UnityEngine;

namespace Assets.Integration.Editor
{
    [CustomPropertyDrawer(typeof(DrawIfAttribute))]
    public class DrawIfPropertyDrawer : PropertyDrawer
    {
        #region Properties
        // Reference to the attribute on the property.
        private DrawIfAttribute drawIf;

        // Field that is being compared.
        private SerializedProperty comparedField;
        #endregion

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If the condition is met, simply draw the field.
            if (ShowMe(property))
            {
                EditorGUI.PropertyField(position, property);
            } //...check if the disabling type is read only. If it is, draw it disabled
            else if (drawIf.DisablingType == DisablingType.ReadOnly)
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property);
                GUI.enabled = true;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ShowMe(property) && drawIf.DisablingType == DisablingType.DontDraw)
            {
                return 0f;
            }

            // The height of the property should be defaulted to the default height.
            return base.GetPropertyHeight(property, label);
        }

        /// <summary>
        /// Errors default to showing the property.
        /// </summary>
        private bool ShowMe(SerializedProperty property)
        {
            drawIf = attribute as DrawIfAttribute;
            // Replace propertyname to the value from the parameter
            string path = property.propertyPath.Contains(".") ? System.IO.Path.ChangeExtension(property.propertyPath, drawIf.ComparedPropertyName) : drawIf.ComparedPropertyName;

            comparedField = property.serializedObject.FindProperty(path);

            if (comparedField == null)
            {
                Debug.LogError("Cannot find property with name: " + path);
                return true;
            }

            // get the value & compare based on types
            switch (comparedField.type)
            { // Possible extend cases to support your own type
                case "bool":
                    return drawIf.ComparisonType == ComparisonType.Equals ? comparedField.boolValue.Equals(drawIf.ComparedValue) :
                        drawIf.ComparisonType == ComparisonType.NotEqual ? !comparedField.boolValue.Equals(drawIf.ComparedValue) : false;
                case "Enum":
                    return drawIf.ComparisonType == ComparisonType.Equals ? comparedField.enumValueIndex.Equals((int)drawIf.ComparedValue) :
                        drawIf.ComparisonType == ComparisonType.NotEqual ? !comparedField.enumValueIndex.Equals((int)drawIf.ComparedValue) : false; ;
                default:
                    Debug.LogError("Error: " + comparedField.type + " is not supported of " + path);
                    return true;
            }
        }
    }
}
