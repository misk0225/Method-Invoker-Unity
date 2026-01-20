#region

using System;
using System.Collections.Generic;
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
        
        // State management for complex types and arrays
        private static readonly System.Collections.Generic.Dictionary<string, bool> foldoutStates = new System.Collections.Generic.Dictionary<string, bool>();
        private static readonly System.Collections.Generic.Dictionary<string, int> constructorSelectionIndices = new System.Collections.Generic.Dictionary<string, int>();
        private static readonly System.Collections.Generic.Dictionary<string, object[]> constructorParameterValues = new System.Collections.Generic.Dictionary<string, object[]>();

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
            
            // Parameters - calculate dynamic heights
            for (int i = 0; i < parameters.Length; i++)
            {
                string parameterPath = $"{property.propertyPath}_param_{i}";
                object paramValue = (methodEntry.ParameterValues != null && i < methodEntry.ParameterValues.Length) 
                    ? methodEntry.ParameterValues[i] 
                    : null;
                height += GetParameterFieldHeight(parameters[i].ParameterType, paramValue, parameterPath) + Spacing;
            }
            
            // Invoke button
            height += ButtonHeight + Spacing * 2;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var methodEntry = GetMethodEntry(property);
            
            Debug.Log($"[MethodEntryDrawer] OnGUI called, methodEntry={(methodEntry != null ? "not null" : "NULL")}");
            
            // Debug: Draw something even if methodEntry is null
            if (methodEntry == null)
            {
                EditorGUI.LabelField(position, "MethodEntry is NULL", EditorStyles.miniLabel);
                return;
            }
            
            MethodInfo method = methodEntry.Delegate?.Method ?? methodEntry.DelegateInfo.Method;
            Debug.Log($"[MethodEntryDrawer] Method={(method != null ? method.Name : "NULL")}, ParamCount={method?.GetParameters().Length ?? 0}");
            
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
            
            // Initialize ParameterValues if needed
            if (methodEntry.ParameterValues == null || methodEntry.ParameterValues.Length != parameters.Length)
            {
                Debug.Log($"[MethodEntryDrawer] 初始化 ParameterValues for {method.Name}: " +
                         $"was null={methodEntry.ParameterValues == null}, " +
                         $"expected length={parameters.Length}, " +
                         $"actual length={methodEntry.ParameterValues?.Length ?? 0}");
                
                methodEntry.ParameterValues = new object[parameters.Length];
                for (int j = 0; j < parameters.Length; j++)
                {
                    methodEntry.ParameterValues[j] = GetDefaultValue(parameters[j].ParameterType);
                    Debug.Log($"  [MethodEntryDrawer] 初始化 param[{j}] ({parameters[j].Name}): " +
                             $"type={parameters[j].ParameterType.Name}, " +
                             $"value={methodEntry.ParameterValues[j] ?? "null"}");
                }
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                string parameterPath = $"{property.propertyPath}_param_{i}";
                float paramHeight = GetParameterFieldHeight(param.ParameterType, methodEntry.ParameterValues[i], parameterPath);
                var paramRect = new Rect(position.x + 10, currentY, position.width - 20, paramHeight);

                EditorGUI.BeginChangeCheck();
                var newValue = DrawParameterFieldInternal(paramRect, param.Name, methodEntry.ParameterValues[i], param.ParameterType, parameterPath);
                if (EditorGUI.EndChangeCheck())
                {
                    methodEntry.ParameterValues[i] = newValue;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }

                currentY += paramHeight + Spacing;
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
                        var field = obj.GetType().GetField(elementName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                        var field = obj.GetType().GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
            catch (Exception e)
            {
                // Silently catch reflection errors to prevent Unity crash
                Debug.LogWarning($"[MethodInvoker] Failed to get MethodEntry: {e.Message}");
                return null;
            }
        }
        
        private bool IsUnsupportedType(Type type)
        {
            // Check for Dictionary and other problematic generic types
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(Dictionary<,>) ||
                    genericTypeDef == typeof(System.Collections.Generic.HashSet<>) ||
                    genericTypeDef == typeof(System.Collections.Generic.Queue<>) ||
                    genericTypeDef == typeof(System.Collections.Generic.Stack<>))
                {
                    return true;
                }
            }
            return false;
        }
        
        private object DrawParameterFieldInternal(Rect rect, string label, object value, Type type, string parameterPath)
        {
            // Handle by-reference types (e.g., Vector3&)
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }
            
            // Check for arrays
            if (type.IsArray)
            {
                return DrawArrayField(rect, label, value, type, parameterPath);
            }
            
            // Check for complex types (classes/structs that aren't primitives or Unity types)
            if ((type.IsClass || (type.IsValueType && !type.IsPrimitive)) &&
                type != typeof(string) && type != typeof(Vector2) && type != typeof(Vector3) &&
                type != typeof(Vector4) && type != typeof(Color) && !type.IsEnum &&
                !typeof(Object).IsAssignableFrom(type))
            {
                return DrawComplexTypeField(rect, label, value, type, parameterPath);
            }
            
            // Default to simple field drawing
            return DrawParameterField(rect, label, value, type);
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
        
        private object DrawArrayField(Rect rect, string label, object value, Type arrayType, string parameterPath)
        {
            Type elementType = arrayType.GetElementType();
            Array array = value as Array;
            
            // Create array if null
            if (array == null)
            {
                array = Array.CreateInstance(elementType, 0);
            }
            
            string foldoutKey = parameterPath + "_array";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;
            
            // Foldout header
            var labelWidth = EditorGUIUtility.labelWidth;
            var foldoutRect = new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight);
            foldoutStates[foldoutKey] = EditorGUI.Foldout(foldoutRect, foldoutStates[foldoutKey], $"{label} [{array.Length}]");
            
            float currentY = rect.y + EditorGUIUtility.singleLineHeight + Spacing;
            
            if (foldoutStates[foldoutKey])
            {
                // Size field
                var sizeRect = new Rect(rect.x + 20, currentY, rect.width - 20, EditorGUIUtility.singleLineHeight);
                int newSize = EditorGUI.IntField(sizeRect, "Size", array.Length);
                currentY += EditorGUIUtility.singleLineHeight + Spacing;
                
                if (newSize != array.Length && newSize >= 0)
                {
                    // Resize array
                    Array newArray = Array.CreateInstance(elementType, newSize);
                    int copyLength = Math.Min(array.Length, newSize);
                    Array.Copy(array, newArray, copyLength);
                    
                    // Initialize new elements with default values
                    for (int i = array.Length; i < newSize; i++)
                    {
                        newArray.SetValue(GetDefaultValue(elementType), i);
                    }
                    
                    array = newArray;
                }
                
                // Draw elements
                for (int i = 0; i < array.Length; i++)
                {
                    string elementPath = $"{parameterPath}_array_{i}";
                    float elementHeight = GetParameterFieldHeight(elementType, array.GetValue(i), elementPath);
                    var elementRect = new Rect(rect.x + 20, currentY, rect.width - 20, elementHeight);
                    var elementValue = array.GetValue(i);
                    
                    EditorGUI.BeginChangeCheck();
                    var newValue = DrawParameterFieldInternal(elementRect, $"Element {i}", elementValue, elementType, elementPath);
                    if (EditorGUI.EndChangeCheck())
                    {
                        array.SetValue(newValue, i);
                    }
                    
                    currentY += elementHeight + Spacing;
                }
            }
            
            return array;
        }
        
        private object DrawComplexTypeField(Rect rect, string label, object value, Type type, string parameterPath)
        {
            string foldoutKey = parameterPath + "_complex";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;
            
            float currentY = rect.y;
            
            if (value == null)
            {
                // Show '+' button and constructor dropdown
                var labelWidth = EditorGUIUtility.labelWidth;
                var labelRect = new Rect(rect.x, currentY, labelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, label);
                
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                
                if (constructors.Length == 0)
                {
                    var nullRect = new Rect(rect.x + labelWidth, currentY, rect.width - labelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(nullRect, "No public constructors");
                    return null;
                }
                
                string ctorKey = parameterPath + "_ctor";
                if (!constructorSelectionIndices.ContainsKey(ctorKey))
                    constructorSelectionIndices[ctorKey] = 0;
                
                int selectedCtorIndex = constructorSelectionIndices[ctorKey];
                if (selectedCtorIndex >= constructors.Length)
                    selectedCtorIndex = 0;
                
                var buttonWidth = 30f;
                var buttonRect = new Rect(rect.x + labelWidth, currentY, buttonWidth, EditorGUIUtility.singleLineHeight);
                
                bool createInstance = GUI.Button(buttonRect, "+");
                
                if (constructors.Length > 1)
                {
                    // Show constructor dropdown
                    var dropdownRect = new Rect(rect.x + labelWidth + buttonWidth + 5, currentY, rect.width - labelWidth - buttonWidth - 5, EditorGUIUtility.singleLineHeight);
                    
                    string[] ctorNames = new string[constructors.Length];
                    for (int i = 0; i < constructors.Length; i++)
                    {
                        var parameters = constructors[i].GetParameters();
                        ctorNames[i] = $"Constructor ({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})";
                    }
                    
                    EditorGUI.BeginChangeCheck();
                    selectedCtorIndex = EditorGUI.Popup(dropdownRect, selectedCtorIndex, ctorNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        constructorSelectionIndices[ctorKey] = selectedCtorIndex;
                    }
                }
                
                currentY += EditorGUIUtility.singleLineHeight + Spacing;
                
                // Draw constructor parameters
                var selectedCtor = constructors[selectedCtorIndex];
                var ctorParams = selectedCtor.GetParameters();
                
                string ctorParamsKey = parameterPath + "_ctorparams";
                if (!constructorParameterValues.ContainsKey(ctorParamsKey) || constructorParameterValues[ctorParamsKey].Length != ctorParams.Length)
                {
                    constructorParameterValues[ctorParamsKey] = new object[ctorParams.Length];
                    for (int i = 0; i < ctorParams.Length; i++)
                    {
                        constructorParameterValues[ctorParamsKey][i] = GetDefaultValue(ctorParams[i].ParameterType);
                    }
                }
                
                for (int i = 0; i < ctorParams.Length; i++)
                {
                    string ctorParamPath = $"{parameterPath}_ctorparam_{i}";
                    float ctorParamHeight = GetParameterFieldHeight(ctorParams[i].ParameterType, constructorParameterValues[ctorParamsKey][i], ctorParamPath);
                    var paramRect = new Rect(rect.x + 20, currentY, rect.width - 20, ctorParamHeight);
                    
                    EditorGUI.BeginChangeCheck();
                    var paramValue = DrawParameterFieldInternal(paramRect, ctorParams[i].Name, constructorParameterValues[ctorParamsKey][i], ctorParams[i].ParameterType, ctorParamPath);
                    if (EditorGUI.EndChangeCheck())
                    {
                        constructorParameterValues[ctorParamsKey][i] = paramValue;
                    }
                    
                    currentY += ctorParamHeight + Spacing;
                }
                
                if (createInstance)
                {
                    try
                    {
                        value = selectedCtor.Invoke(constructorParameterValues[ctorParamsKey]);
                        constructorParameterValues.Remove(ctorParamsKey);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to create instance: {e.Message}");
                    }
                }
            }
            else
            {
                // Show foldout with fields
                var labelWidth = EditorGUIUtility.labelWidth;
                var foldoutRect = new Rect(rect.x, currentY, rect.width, EditorGUIUtility.singleLineHeight);
                foldoutStates[foldoutKey] = EditorGUI.Foldout(foldoutRect, foldoutStates[foldoutKey], $"{label} ({type.Name})");
                
                currentY += EditorGUIUtility.singleLineHeight + Spacing;
                
                if (foldoutStates[foldoutKey])
                {
                    var fields = GetSerializableFields(type);
                    
                    foreach (var field in fields)
                    {
                        string fieldPath = $"{parameterPath}_field_{field.Name}";
                        var fieldValue = field.GetValue(value);
                        float fieldHeight = GetParameterFieldHeight(field.FieldType, fieldValue, fieldPath);
                        var fieldRect = new Rect(rect.x + 20, currentY, rect.width - 20, fieldHeight);
                        
                        EditorGUI.BeginChangeCheck();
                        var newFieldValue = DrawParameterFieldInternal(fieldRect, field.Name, fieldValue, field.FieldType, fieldPath);
                        if (EditorGUI.EndChangeCheck())
                        {
                            field.SetValue(value, newFieldValue);
                        }
                        
                        currentY += fieldHeight + Spacing;
                    }
                }
            }
            
            return value;
        }
        
        private FieldInfo[] GetSerializableFields(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return fields.Where(f =>
                (f.IsPublic && !f.IsNotSerialized) ||
                f.GetCustomAttribute<SerializeField>() != null).ToArray();
        }
        
        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        
        private float GetParameterFieldHeight(Type type, object value, string parameterPath)
        {
            // Handle by-reference types
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }
            
            // Primitive and Unity built-in types
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                type == typeof(string) || type == typeof(bool) ||
                type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Color) || type.IsEnum || typeof(Object).IsAssignableFrom(type))
            {
                return EditorGUIUtility.singleLineHeight;
            }
            
            // Arrays
            if (type.IsArray)
            {
                float height = EditorGUIUtility.singleLineHeight; // Foldout header
                
                string foldoutKey = parameterPath + "_array";
                if (foldoutStates.ContainsKey(foldoutKey) && foldoutStates[foldoutKey])
                {
                    Array array = value as Array;
                    if (array != null)
                    {
                        height += EditorGUIUtility.singleLineHeight + Spacing; // Size field
                        
                        Type elementType = type.GetElementType();
                        for (int i = 0; i < array.Length; i++)
                        {
                            height += GetParameterFieldHeight(elementType, array.GetValue(i), $"{parameterPath}_array_{i}") + Spacing;
                        }
                    }
                }
                
                return height;
            }
            
            // Complex types (classes/structs)
            if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
            {
                float height = EditorGUIUtility.singleLineHeight; // Label or foldout
                
                if (value == null)
                {
                    // '+' button and constructor dropdown
                    var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                    if (constructors.Length > 0)
                    {
                        string ctorKey = parameterPath + "_ctor";
                        int selectedCtorIndex = constructorSelectionIndices.ContainsKey(ctorKey) ? constructorSelectionIndices[ctorKey] : 0;
                        if (selectedCtorIndex >= constructors.Length)
                            selectedCtorIndex = 0;
                        
                        var ctorParams = constructors[selectedCtorIndex].GetParameters();
                        for (int i = 0; i < ctorParams.Length; i++)
                        {
                            string ctorParamsKey = parameterPath + "_ctorparams";
                            object paramValue = constructorParameterValues.ContainsKey(ctorParamsKey) && i < constructorParameterValues[ctorParamsKey].Length
                                ? constructorParameterValues[ctorParamsKey][i]
                                : null;
                            height += GetParameterFieldHeight(ctorParams[i].ParameterType, paramValue, $"{parameterPath}_ctorparam_{i}") + Spacing;
                        }
                    }
                }
                else
                {
                    string foldoutKey = parameterPath + "_complex";
                    if (foldoutStates.ContainsKey(foldoutKey) && foldoutStates[foldoutKey])
                    {
                        var fields = GetSerializableFields(type);
                        foreach (var field in fields)
                        {
                            var fieldValue = field.GetValue(value);
                            height += GetParameterFieldHeight(field.FieldType, fieldValue, $"{parameterPath}_field_{field.Name}") + Spacing;
                        }
                    }
                }
                
                return height;
            }
            
            return EditorGUIUtility.singleLineHeight;
        }

    #endregion
    }
#endif
}