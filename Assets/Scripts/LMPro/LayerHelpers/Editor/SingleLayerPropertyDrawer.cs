using UnityEditor;
using UnityEngine;

namespace LMPro.LayerHelpers.Editor
{
    [CustomPropertyDrawer(typeof(SingleLayer))]
    public class SingleLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            var layerIndex = property.FindPropertyRelative("layerIndex");
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            if (layerIndex != null) layerIndex.intValue = EditorGUI.LayerField(position, layerIndex.intValue);
            EditorGUI.EndProperty();
        }
    }
}