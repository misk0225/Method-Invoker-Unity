#region

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace MethodInvoker
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MethodContainer))]
    public class MethodContainerDrawer : PropertyDrawer
    {
    #region Private Variables

        private const float DividerHeight = 3f;
        private const float Spacing = 10f;

    #endregion

    #region Public Methods

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight * 2 + Spacing * 3; // Target field + header
            
            var targetGameObject = property.FindPropertyRelative("targetGameObject");
            if (targetGameObject.objectReferenceValue != null)
            {
                var methodEntries = property.FindPropertyRelative("methodEntries");
                if (methodEntries != null && methodEntries.isArray)
                {
                    height += DividerHeight + Spacing * 2;
                    
                    if (methodEntries.arraySize == 0)
                    {
                        // Add height for info message
                        height += EditorGUIUtility.singleLineHeight * 2 + Spacing;
                    }
                    else
                    {
                        int lastInstanceId = -9999;
                        for (int i = 0; i < methodEntries.arraySize; i++)
                        {
                            var element = methodEntries.GetArrayElementAtIndex(i);
                            var methodEntry = GetMethodEntryFromProperty(element);
                            
                            // Try to get target from Delegate first, then from DelegateInfo
                            Component comp = null;
                            if (methodEntry != null)
                            {
                                if (methodEntry.Delegate?.Target is Component c1)
                                {
                                    comp = c1;
                                }
                                else if (methodEntry.DelegateInfo.Target is Component c2)
                                {
                                    comp = c2;
                                }
                            }
                            
                            if (comp != null)
                            {
                                int instanceID = comp.GetInstanceID();
                                if (instanceID != lastInstanceId)
                                {
                                    if (lastInstanceId != -9999)
                                        height += DividerHeight + Spacing;
                                    height += EditorGUIUtility.singleLineHeight + Spacing;
                                    lastInstanceId = instanceID;
                                }
                            }
                            
                            height += EditorGUI.GetPropertyHeight(element, true) + Spacing;
                        }
                    }
                }
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var currentY = position.y;
            
            // Draw header box
            var headerRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight + Spacing * 2);
            GUI.Box(headerRect, GUIContent.none, EditorStyles.toolbar);
            
            // Draw target GameObject field
            var targetProperty = property.FindPropertyRelative("targetGameObject");
            var targetLabelRect = new Rect(position.x + 5, currentY + Spacing, position.width - 10, EditorGUIUtility.singleLineHeight);
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.cyan;
            EditorGUI.LabelField(targetLabelRect, "Target GameObject", EditorStyles.boldLabel);
            GUI.backgroundColor = oldColor;
            
            currentY += EditorGUIUtility.singleLineHeight + Spacing;
            
            var targetFieldRect = new Rect(position.x + 5, currentY, position.width - 10, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(targetFieldRect, targetProperty, GUIContent.none);
            
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                
                // Refresh entries
                var container = GetMethodContainer(property);
                if (container != null)
                {
                    container.RefreshEntries();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
            
            currentY += EditorGUIUtility.singleLineHeight + Spacing * 2;

            if (targetProperty.objectReferenceValue != null)
            {
                // Draw divider
                DrawDivider(new Rect(position.x, currentY, position.width, DividerHeight), Color.green);
                currentY += DividerHeight + Spacing * 2;

                var methodEntries = property.FindPropertyRelative("methodEntries");
                if (methodEntries != null && methodEntries.isArray)
                {
                    if (methodEntries.arraySize == 0)
                    {
                        // No methods found - show info message
                        var infoRect = new Rect(position.x + 5, currentY, position.width - 10, EditorGUIUtility.singleLineHeight * 2);
                        var oldColor2 = GUI.color;
                        GUI.color = new Color(1f, 1f, 0.5f, 0.3f);
                        GUI.Box(infoRect, GUIContent.none);
                        GUI.color = oldColor2;
                        
                        var labelStyle = new GUIStyle(EditorStyles.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true
                        };
                        EditorGUI.LabelField(infoRect, "No public void methods found on this GameObject.\nAdd components with public methods.", labelStyle);
                        
                        currentY += EditorGUIUtility.singleLineHeight * 2 + Spacing;
                    }
                    
                    int lastInstanceId = -9999;
                    
                    for (int i = 0; i < methodEntries.arraySize; i++)
                    {
                        var element = methodEntries.GetArrayElementAtIndex(i);
                        var methodEntry = GetMethodEntryFromProperty(element);
                        
                        // Try to get target from Delegate first, then from DelegateInfo
                        Component comp = null;
                        if (methodEntry != null)
                        {
                            if (methodEntry.Delegate?.Target is Component c1)
                            {
                                comp = c1;
                            }
                            else if (methodEntry.DelegateInfo.Target is Component c2)
                            {
                                comp = c2;
                            }
                        }
                        
                        if (comp != null)
                        {
                            int instanceID = comp.GetInstanceID();
                            if (instanceID != lastInstanceId)
                            {
                                if (lastInstanceId != -9999)
                                {
                                    DrawDivider(new Rect(position.x, currentY, position.width, DividerHeight), Color.red);
                                    currentY += DividerHeight + Spacing;
                                }
                                
                                var mbRect = new Rect(position.x + 5, currentY, position.width - 10, EditorGUIUtility.singleLineHeight);
                                EditorGUI.ObjectField(mbRect, comp, comp.GetType(), true);
                                currentY += EditorGUIUtility.singleLineHeight + Spacing;
                                
                                lastInstanceId = instanceID;
                            }
                        }
                        
                        var elementHeight = EditorGUI.GetPropertyHeight(element, true);
                        var elementRect = new Rect(position.x, currentY, position.width, elementHeight);
                        EditorGUI.PropertyField(elementRect, element, GUIContent.none, true);
                        currentY += elementHeight + Spacing;
                    }
                }
            }

            EditorGUI.EndProperty();
        }

    #endregion

    #region Private Methods

        private void DrawDivider(Rect rect, Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            s = 0.5f;
            var adjustedColor = Color.HSVToRGB(h, s, v);
            EditorGUI.DrawRect(rect, adjustedColor);
        }

        private MethodContainer GetMethodContainer(SerializedProperty property)
        {
            try
            {
                var targetObject = property.serializedObject.targetObject;
                var path = property.propertyPath;
                
                if (string.IsNullOrEmpty(path))
                    return null;
                
                object obj = targetObject;
                var elements = path.Split('.');
                
                foreach (var element in elements)
                {
                    if (obj == null) return null;
                    
                    if (element.Contains("["))
                    {
                        var elementName = element.Substring(0, element.IndexOf("["));
                        var index = int.Parse(element.Substring(element.IndexOf("[") + 1, element.IndexOf("]") - element.IndexOf("[") - 1));
                        var field = obj.GetType().GetField(elementName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field == null) return null;
                        
                        // Skip unsupported field types
                        if (IsUnsupportedType(field.FieldType))
                        {
                            return null;
                        }
                        
                        var list = field.GetValue(obj) as System.Collections.IList;
                        if (list == null || index >= list.Count) return null;
                        obj = list[index];
                    }
                    else
                    {
                        var field = obj.GetType().GetField(element, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field == null) return null;
                        
                        // Skip unsupported field types
                        if (IsUnsupportedType(field.FieldType))
                        {
                            return null;
                        }
                        
                        obj = field.GetValue(obj);
                    }
                }
                
                return obj as MethodContainer;
            }
            catch (System.Exception e)
            {
                // Silently catch reflection errors to prevent Unity crash
                UnityEngine.Debug.LogWarning($"[MethodInvoker] Failed to get MethodContainer: {e.Message}");
                return null;
            }
        }

        private MethodEntry GetMethodEntryFromProperty(SerializedProperty property)
        {
            try
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
                        var field = obj.GetType().GetField(elementName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field == null) return null;
                        
                        // Skip unsupported field types (Dictionary, etc.)
                        if (IsUnsupportedType(field.FieldType))
                        {
                            return null;
                        }
                        
                        var list = field.GetValue(obj) as System.Collections.IList;
                        if (list == null || index >= list.Count) return null;
                        obj = list[index];
                    }
                    else
                    {
                        var field = obj.GetType().GetField(element, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field == null) return null;
                        
                        // Skip unsupported field types (Dictionary, etc.)
                        if (IsUnsupportedType(field.FieldType))
                        {
                            return null;
                        }
                        
                        obj = field.GetValue(obj);
                    }
                }
                
                return obj as MethodEntry;
            }
            catch (System.Exception e)
            {
                // Silently catch reflection errors to prevent Unity crash
                UnityEngine.Debug.LogWarning($"[MethodInvoker] Failed to get MethodEntry from property: {e.Message}");
                return null;
            }
        }
        
        private bool IsUnsupportedType(System.Type type)
        {
            // Check for Dictionary and other problematic generic types
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(System.Collections.Generic.Dictionary<,>) ||
                    genericTypeDef == typeof(System.Collections.Generic.HashSet<>) ||
                    genericTypeDef == typeof(System.Collections.Generic.Queue<>) ||
                    genericTypeDef == typeof(System.Collections.Generic.Stack<>))
                {
                    return true;
                }
            }
            return false;
        }

    #endregion
    }
#endif
}