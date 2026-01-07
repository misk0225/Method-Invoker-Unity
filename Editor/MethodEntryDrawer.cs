#region

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace MethodInvoker
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MethodEntry))]
    public class MethodEntryDrawer : PropertyDrawer
    {
    #region Private Variables

        private const float ButtonHeight = 22f;
        private const float Spacing = 2f;

    #endregion

    #region Public Methods

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var methodEntry = GetMethodEntry(property);
            if (methodEntry == null) return EditorGUIUtility.singleLineHeight;
            
            MethodInfo method = methodEntry.Delegate?.Method ?? methodEntry.DelegateInfo.Method;
            if (method == null) return EditorGUIUtility.singleLineHeight;

            var parameters = method.GetParameters();
            float height = EditorGUIUtility.singleLineHeight * 2 + Spacing * 3; // Box + method name
            
            // Parameters
            height += parameters.Length * (EditorGUIUtility.singleLineHeight + Spacing);
            
            // Invoke button
            height += ButtonHeight + Spacing * 2;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var methodEntry = GetMethodEntry(property);
            
            // Debug: Draw something even if methodEntry is null
            if (methodEntry == null)
            {
                EditorGUI.LabelField(position, "MethodEntry is NULL", EditorStyles.miniLabel);
                return;
            }
            
            MethodInfo method = methodEntry.Delegate?.Method ?? methodEntry.DelegateInfo.Method;
            if (method == null)
            {
                EditorGUI.LabelField(position, "Method is NULL", EditorStyles.miniLabel);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var boxRect = new Rect(position.x, position.y, position.width, GetPropertyHeight(property, label));
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            var currentY = position.y + Spacing;
            var methodFullName = method.ToString().Remove(0, 5);

            // Draw method name
            var methodNameRect = new Rect(position.x + 5, currentY, position.width - 10, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(methodNameRect, methodFullName, EditorStyles.boldLabel);
            currentY += EditorGUIUtility.singleLineHeight + Spacing * 2;

            // Draw parameters
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramRect = new Rect(position.x + 10, currentY, position.width - 20, EditorGUIUtility.singleLineHeight);
                
                if (methodEntry.ParameterValues == null || i >= methodEntry.ParameterValues.Length)
                    continue;

                EditorGUI.BeginChangeCheck();
                var newValue = DrawParameterField(paramRect, param.Name, methodEntry.ParameterValues[i], param.ParameterType);
                if (EditorGUI.EndChangeCheck())
                {
                    methodEntry.ParameterValues[i] = newValue;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }

                currentY += EditorGUIUtility.singleLineHeight + Spacing;
            }

            // Draw invoke button
            currentY += Spacing;
            var buttonRect = new Rect(position.x + 10, currentY, position.width - 20, ButtonHeight);
            if (GUI.Button(buttonRect, "Invoke"))
            {
                methodEntry.Invoke();
            }

            EditorGUI.EndProperty();
        }

    #endregion

    #region Private Methods

        private MethodEntry GetMethodEntry(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var path = property.propertyPath;
            
            if (string.IsNullOrEmpty(path)) return null;
            
            object obj = targetObject;
            var elements = path.Split('.');
            
            foreach (var element in elements)
            {
                if (obj == null) return null;
                
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = int.Parse(element.Substring(element.IndexOf("[") + 1, element.IndexOf("]") - element.IndexOf("[") - 1));
                    var field = obj.GetType().GetField(elementName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null) return null;
                    var list = field.GetValue(obj) as System.Collections.IList;
                    if (list == null || index >= list.Count) return null;
                    obj = list[index];
                }
                else
                {
                    var field = obj.GetType().GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null) return null;
                    obj = field.GetValue(obj);
                }
            }
            
            return obj as MethodEntry;
        }

        private object DrawParameterField(Rect rect, string label, object value, Type type)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            var fieldRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);

            EditorGUI.LabelField(labelRect, label);

            // Handle by-reference types (e.g., Vector3&)
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            // Handle different types
            if (type == typeof(int))
                return EditorGUI.IntField(fieldRect, value != null ? (int)value : 0);
            else if (type == typeof(float))
                return EditorGUI.FloatField(fieldRect, value != null ? (float)value : 0f);
            else if (type == typeof(double))
                return EditorGUI.DoubleField(fieldRect, value != null ? (double)value : 0.0);
            else if (type == typeof(string))
                return EditorGUI.TextField(fieldRect, value as string ?? "");
            else if (type == typeof(bool))
                return EditorGUI.Toggle(fieldRect, value != null ? (bool)value : false);
            else if (type == typeof(Vector2))
                return EditorGUI.Vector2Field(fieldRect, GUIContent.none, value != null ? (Vector2)value : Vector2.zero);
            else if (type == typeof(Vector3))
                return EditorGUI.Vector3Field(fieldRect, GUIContent.none, value != null ? (Vector3)value : Vector3.zero);
            else if (type == typeof(Vector4))
                return EditorGUI.Vector4Field(fieldRect, GUIContent.none, value != null ? (Vector4)value : Vector4.zero);
            else if (type == typeof(Color))
                return EditorGUI.ColorField(fieldRect, value != null ? (Color)value : Color.white);
            else if (type.IsEnum)
                return EditorGUI.EnumPopup(fieldRect, value as Enum ?? (Enum)Enum.GetValues(type).GetValue(0));
            else if (typeof(Object).IsAssignableFrom(type))
                return EditorGUI.ObjectField(fieldRect, value as Object, type, true);
            else
            {
                EditorGUI.LabelField(fieldRect, value?.ToString() ?? "null");
                return value;
            }
        }

    #endregion
    }
#endif
}