#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace MethodInvoker
{
#if UNITY_EDITOR
    public class MethodInvokerWindow : EditorWindow
    {
    #region Public Variables

        [MenuItem("Tools/Method Invoker")]
        public static void ShowMenu()
        {
            instance = GetWindow<MethodInvokerWindow>("Method Invoker");
            instance.Show();
        }

        public MethodContainer container;

    #endregion

    #region Private Variables

        private static MethodInvokerWindow instance;
        private GameObject target;
        private Vector2 scrollPosition;
        private SerializedObject serializedObject;
        private SerializedProperty containerProperty;
        private bool showPrivateMethods = false;
        
        // State management for foldouts and constructor selection
        private static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private static Dictionary<string, int> constructorSelectionIndices = new Dictionary<string, int>();
        private static Dictionary<string, object[]> constructorParameterValues = new Dictionary<string, object[]>();
        
        // State management for component method list foldouts
        private static Dictionary<int, bool> componentFoldoutStates = new Dictionary<int, bool>();

    #endregion

    #region Protected Methods

        protected virtual void OnEnable()
        {
            instance = GetWindow<MethodInvokerWindow>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Selection.selectionChanged += OnSelectionChanged;
            
            if (container == null)
                container = new MethodContainer(null);
                
            serializedObject = new SerializedObject(this);
            containerProperty = serializedObject.FindProperty("container");
            
            // Initialize with current selection
            OnSelectionChanged();
        }

        protected virtual void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        protected virtual void OnGUI()
        {
            if (serializedObject == null || !serializedObject.targetObject)
            {
                serializedObject = new SerializedObject(this);
                containerProperty = serializedObject.FindProperty("container");
            }

            serializedObject.Update();
            
            // Sync target with container
            if (container != null && target != container.targetGameObject)
            {
                target = container.targetGameObject;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // If no target selected, show hint message
            if (target == null)
            {
                GUILayout.FlexibleSpace();
                
                var hintStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    wordWrap = true,
                    normal = { textColor = Color.gray }
                };
                
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("+ Please Click GameObject in your Hierarchy or Scene view.", hintStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.FlexibleSpace();
            }
            else
            {
                // Show private methods checkbox when target is selected
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(3);
                
                EditorGUI.BeginChangeCheck();
                showPrivateMethods = EditorGUILayout.Toggle("Show Private Methods", showPrivateMethods);
                if (EditorGUI.EndChangeCheck())
                {
                    if (container != null)
                    {
                        container.showPrivateMethods = showPrivateMethods;
                        container.RefreshEntries();
                        serializedObject.Update();
                    }
                }
                
                GUILayout.Space(3);
                GUILayout.EndVertical();
                
                // Draw methods section
                GUILayout.Space(8);
                
                if (container != null && container.methodEntries != null)
                {
                    if (container.methodEntries.Count == 0)
                    {
                        var oldColor2 = GUI.color;
                        GUI.color = new Color(1f, 1f, 0.5f, 0.3f);
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUI.color = oldColor2;
                        
                        GUILayout.Space(10);
                        var labelStyle = new GUIStyle(EditorStyles.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true
                        };
                        EditorGUILayout.LabelField("No public void methods found on this GameObject.\nAdd components with public methods.", labelStyle);
                        GUILayout.Space(10);
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        // Group methods by component
                        var methodsByComponent = new Dictionary<Component, List<(MethodEntry entry, int index)>>();
                        
                        for (int i = 0; i < container.methodEntries.Count; i++)
                        {
                            var methodEntry = container.methodEntries[i];
                            
                            // Get component
                            Component comp = null;
                            if (methodEntry.Delegate?.Target is Component c1)
                            {
                                comp = c1;
                            }
                            else if (methodEntry.DelegateInfo.Target is Component c2)
                            {
                                comp = c2;
                            }
                            
                            if (comp != null)
                            {
                                if (!methodsByComponent.ContainsKey(comp))
                                {
                                    methodsByComponent[comp] = new List<(MethodEntry, int)>();
                                }
                                methodsByComponent[comp].Add((methodEntry, i));
                            }
                        }
                        
                        // Draw each component with foldout
                        bool isFirst = true;
                        foreach (var kvp in methodsByComponent)
                        {
                            var comp = kvp.Key;
                            var methods = kvp.Value;
                            int instanceID = comp.GetInstanceID();
                            
                            if (!isFirst)
                            {
                                GUILayout.Space(5);
                                DrawDivider(Color.red);
                                GUILayout.Space(5);
                            }
                            isFirst = false;
                            
                            // Component header with foldout
                            GUILayout.BeginHorizontal();
                            
                            // Initialize foldout state (default: collapsed)
                            if (!componentFoldoutStates.ContainsKey(instanceID))
                            {
                                componentFoldoutStates[instanceID] = false;
                            }
                            
                            // Foldout with component name and method count
                            var foldoutLabel = $"{comp.GetType().Name} ({methods.Count} methods)";
                            componentFoldoutStates[instanceID] = EditorGUILayout.Foldout(
                                componentFoldoutStates[instanceID], 
                                foldoutLabel, 
                                true, 
                                EditorStyles.foldoutHeader
                            );
                            
                            // Component reference field (read-only)
                            GUI.enabled = false;
                            EditorGUILayout.ObjectField(comp, comp.GetType(), true, GUILayout.Width(150));
                            GUI.enabled = true;
                            
                            GUILayout.EndHorizontal();
                            GUILayout.Space(3);
                            
                            // Draw methods if expanded
                            if (componentFoldoutStates[instanceID])
                            {
                                EditorGUI.indentLevel++;
                                
                                foreach (var methodData in methods)
                                {
                                    DrawMethodEntry(methodData.entry, methodData.index);
                                    GUILayout.Space(5);
                                }
                                
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawDivider(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            s = 0.5f;
            var adjustedColor = Color.HSVToRGB(h, s, v);
            
            var rect = EditorGUILayout.GetControlRect(false, 3);
            EditorGUI.DrawRect(rect, adjustedColor);
        }
        
        private void DrawMethodEntry(MethodEntry methodEntry, int index)
        {
            if (methodEntry == null) return;
            
            var method = methodEntry.Delegate?.Method ?? methodEntry.DelegateInfo.Method;
            if (method == null) return;
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            
            // Left side: Method name and parameters
            GUILayout.BeginVertical();
            
            // Method name
            var methodFullName = method.ToString();
            if (methodFullName.StartsWith("Void "))
                methodFullName = methodFullName.Remove(0, 5);
            EditorGUILayout.LabelField(methodFullName, EditorStyles.boldLabel);
            
            // Parameters
            var parameters = method.GetParameters();
            if (methodEntry.ParameterValues == null || methodEntry.ParameterValues.Length != parameters.Length)
            {
                methodEntry.ParameterValues = new object[parameters.Length];
                for (int j = 0; j < parameters.Length; j++)
                {
                    methodEntry.ParameterValues[j] = GetDefaultValue(parameters[j].ParameterType);
                }
            }
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                string paramPath = $"window_method_{index}_param_{i}";
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(param.Name, GUILayout.Width(120));
                methodEntry.ParameterValues[i] = DrawParameterField(
                    methodEntry.ParameterValues[i], 
                    param.ParameterType, 
                    paramPath);
                EditorGUILayout.EndHorizontal();
            }
            
            GUILayout.EndVertical();
            
            // Right side: Invoke button
            if (GUILayout.Button("Invoke", GUILayout.Width(80), GUILayout.Height(parameters.Length > 0 ? 60 : 30)))
            {
                methodEntry.Invoke();
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        
        private object DrawParameterField(object value, Type type, string parameterPath)
        {
            // Handle by-reference types
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            // Handle arrays
            if (type.IsArray)
            {
                return DrawArrayField(value, type, parameterPath);
            }

            // Handle List<T>
            if (IsListType(type))
            {
                return DrawListField(value, type, parameterPath);
            }

            // Handle Dictionary<TKey, TValue>
            if (IsDictionaryType(type))
            {
                return DrawDictionaryField(value, type, parameterPath);
            }
            
            // Handle complex types (classes/structs)
            if ((type.IsClass || (type.IsValueType && !type.IsPrimitive)) &&
                type != typeof(string) && type != typeof(Vector2) && type != typeof(Vector3) &&
                type != typeof(Vector4) && type != typeof(Color) && !type.IsEnum &&
                !typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return DrawComplexTypeField(value, type, parameterPath);
            }
            
            // Handle different primitive types
            if (type == typeof(int))
                return EditorGUILayout.IntField(value != null ? (int)value : 0);
            else if (type == typeof(float))
                return EditorGUILayout.FloatField(value != null ? (float)value : 0f);
            else if (type == typeof(double))
                return EditorGUILayout.DoubleField(value != null ? (double)value : 0.0);
            else if (type == typeof(string))
                return EditorGUILayout.TextField(value as string ?? "");
            else if (type == typeof(bool))
                return EditorGUILayout.Toggle(value != null ? (bool)value : false);
            else if (type == typeof(Vector2))
                return EditorGUILayout.Vector2Field(GUIContent.none, value != null ? (Vector2)value : Vector2.zero);
            else if (type == typeof(Vector3))
                return EditorGUILayout.Vector3Field(GUIContent.none, value != null ? (Vector3)value : Vector3.zero);
            else if (type == typeof(Vector4))
                return EditorGUILayout.Vector4Field(GUIContent.none, value != null ? (Vector4)value : Vector4.zero);
            else if (type == typeof(Color))
                return EditorGUILayout.ColorField(value != null ? (Color)value : Color.white);
            else if (type.IsEnum)
                return EditorGUILayout.EnumPopup(value as Enum ?? (Enum)Enum.GetValues(type).GetValue(0));
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return EditorGUILayout.ObjectField(value as UnityEngine.Object, type, true);
            else
            {
                EditorGUILayout.LabelField(value?.ToString() ?? "null");
                return value;
            }
        }
        
        private object DrawArrayField(object value, Type arrayType, string parameterPath)
        {
            Type elementType = arrayType.GetElementType();
            Array array = value as Array;
            
            if (array == null)
            {
                array = Array.CreateInstance(elementType, 0);
            }
            
            string foldoutKey = parameterPath + "_array";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], $"Array [{array.Length}]");
            
            if (foldoutStates[foldoutKey])
            {
                EditorGUI.indentLevel++;
                
                int newSize = EditorGUILayout.IntField("Size", array.Length);
                if (newSize != array.Length && newSize >= 0)
                {
                    Array newArray = Array.CreateInstance(elementType, newSize);
                    int copyLength = Math.Min(array.Length, newSize);
                    Array.Copy(array, newArray, copyLength);
                    
                    for (int i = array.Length; i < newSize; i++)
                    {
                        newArray.SetValue(GetDefaultValue(elementType), i);
                    }
                    
                    array = newArray;
                }
                
                for (int i = 0; i < array.Length; i++)
                {
                    string elementPath = $"{parameterPath}_array_{i}";
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Element {i}", GUILayout.Width(80));
                    var newValue = DrawParameterField(array.GetValue(i), elementType, elementPath);
                    array.SetValue(newValue, i);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            return array;
        }

        private object DrawListField(object value, Type listType, string parameterPath)
        {
            Type elementType = listType.GetGenericArguments()[0];
            IList list = value as IList;

            if (list == null)
            {
                list = Activator.CreateInstance(listType) as IList;
            }

            string foldoutKey = parameterPath + "_list";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], $"List<{elementType.Name}> [{list.Count}]");

            if (foldoutStates[foldoutKey])
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                int newSize = EditorGUILayout.IntField("Size", list.Count);
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    list.Add(GetDefaultValue(elementType));
                }
                if (GUILayout.Button("-", GUILayout.Width(30)) && list.Count > 0)
                {
                    list.RemoveAt(list.Count - 1);
                }
                EditorGUILayout.EndHorizontal();

                if (newSize != list.Count && newSize >= 0)
                {
                    while (list.Count < newSize)
                    {
                        list.Add(GetDefaultValue(elementType));
                    }

                    while (list.Count > newSize)
                    {
                        list.RemoveAt(list.Count - 1);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    string elementPath = $"{parameterPath}_list_{i}";
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Element {i}", GUILayout.Width(80));
                    list[i] = DrawParameterField(list[i], elementType, elementPath);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return list;
        }

        private object DrawDictionaryField(object value, Type dictionaryType, string parameterPath)
        {
            var genericArgs = dictionaryType.GetGenericArguments();
            var keyType = genericArgs[0];
            var valueType = genericArgs[1];
            IDictionary dictionary = value as IDictionary;

            if (dictionary == null)
            {
                dictionary = Activator.CreateInstance(dictionaryType) as IDictionary;
            }

            string foldoutKey = parameterPath + "_dict";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], $"Dictionary<{keyType.Name}, {valueType.Name}> [{dictionary.Count}]");

            if (foldoutStates[foldoutKey])
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                int newSize = EditorGUILayout.IntField("Size", dictionary.Count);
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    var newKey = CreateDefaultDictionaryKey(keyType, dictionary);
                    if ((newKey != null || keyType.IsValueType) && !dictionary.Contains(newKey))
                    {
                        dictionary.Add(newKey, GetDefaultValue(valueType));
                    }
                }
                if (GUILayout.Button("-", GUILayout.Width(30)) && dictionary.Count > 0)
                {
                    var keys = dictionary.Keys.Cast<object>().ToList();
                    dictionary.Remove(keys[keys.Count - 1]);
                }
                EditorGUILayout.EndHorizontal();

                if (newSize != dictionary.Count && newSize >= 0)
                {
                    while (dictionary.Count > newSize)
                    {
                        var keys = dictionary.Keys.Cast<object>().ToList();
                        if (keys.Count == 0) break;
                        dictionary.Remove(keys[keys.Count - 1]);
                    }

                    while (dictionary.Count < newSize)
                    {
                        var newKey = CreateDefaultDictionaryKey(keyType, dictionary);
                        if (newKey == null && !keyType.IsValueType) break;
                        if (dictionary.Contains(newKey)) break;
                        dictionary.Add(newKey, GetDefaultValue(valueType));
                    }
                }

                var keySnapshot = dictionary.Keys.Cast<object>().ToList();
                for (int i = 0; i < keySnapshot.Count; i++)
                {
                    var oldKey = keySnapshot[i];
                    var oldValue = dictionary[oldKey];

                    string keyPath = $"{parameterPath}_dict_{i}_key";
                    string valuePath = $"{parameterPath}_dict_{i}_value";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Key {i}", GUILayout.Width(80));
                    var newKey = DrawParameterField(oldKey, keyType, keyPath);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Value {i}", GUILayout.Width(80));
                    var newValue = DrawParameterField(oldValue, valueType, valuePath);
                    EditorGUILayout.EndHorizontal();

                    bool keyChanged = !Equals(newKey, oldKey);
                    if (keyChanged)
                    {
                        if (newKey == null && !keyType.IsValueType)
                        {
                            dictionary[oldKey] = newValue;
                            continue;
                        }

                        if (dictionary.Contains(newKey))
                        {
                            dictionary[oldKey] = newValue;
                            continue;
                        }

                        dictionary.Remove(oldKey);
                        dictionary[newKey] = newValue;
                    }
                    else
                    {
                        dictionary[oldKey] = newValue;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return dictionary;
        }

        private static bool IsListType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool IsDictionaryType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        private object CreateDefaultDictionaryKey(Type keyType, IDictionary dictionary)
        {
            if (keyType == typeof(string))
            {
                int index = 1;
                string candidate;
                do
                {
                    candidate = $"Key{index}";
                    index++;
                } while (dictionary.Contains(candidate));

                return candidate;
            }

            if (keyType == typeof(int))
            {
                int candidate = 0;
                while (dictionary.Contains(candidate))
                {
                    candidate++;
                }

                return candidate;
            }

            if (keyType.IsEnum)
            {
                foreach (var enumValue in Enum.GetValues(keyType))
                {
                    if (!dictionary.Contains(enumValue))
                    {
                        return enumValue;
                    }
                }

                return null;
            }

            var defaultKey = GetDefaultValue(keyType);
            return dictionary.Contains(defaultKey) ? null : defaultKey;
        }
        
        private object DrawComplexTypeField(object value, Type type, string parameterPath)
        {
            string foldoutKey = parameterPath + "_complex";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (value == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(type.Name, GUILayout.Width(120));
                
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                
                if (constructors.Length == 0)
                {
                    EditorGUILayout.LabelField("No public constructors");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return null;
                }
                
                // Check if all constructors have recursive parameters
                bool allConstructorsRecursive = constructors.All(ctor =>
                {
                    var parameters = ctor.GetParameters();
                    return parameters.Length > 0 && parameters.Any(p => p.ParameterType == type || p.ParameterType == type.MakeByRefType());
                });
                
                if (allConstructorsRecursive)
                {
                    EditorGUILayout.LabelField("(Recursive constructor - cannot instantiate)");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return null;
                }
                
                string ctorKey = parameterPath + "_ctor";
                if (!constructorSelectionIndices.ContainsKey(ctorKey))
                    constructorSelectionIndices[ctorKey] = 0;
                
                int selectedCtorIndex = constructorSelectionIndices[ctorKey];
                if (selectedCtorIndex >= constructors.Length)
                    selectedCtorIndex = 0;
                
                bool createInstance = GUILayout.Button("+", GUILayout.Width(30));
                
                if (constructors.Length > 1)
                {
                    string[] ctorNames = new string[constructors.Length];
                    for (int i = 0; i < constructors.Length; i++)
                    {
                        var parameters = constructors[i].GetParameters();
                        ctorNames[i] = $"Constructor ({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})";
                    }
                    
                    selectedCtorIndex = EditorGUILayout.Popup(selectedCtorIndex, ctorNames);
                    constructorSelectionIndices[ctorKey] = selectedCtorIndex;
                }
                
                EditorGUILayout.EndHorizontal();
                
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
                
                if (ctorParams.Length > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < ctorParams.Length; i++)
                    {
                        string ctorParamPath = $"{parameterPath}_ctorparam_{i}";
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(ctorParams[i].Name, GUILayout.Width(120));
                        constructorParameterValues[ctorParamsKey][i] = DrawParameterField(
                            constructorParameterValues[ctorParamsKey][i], 
                            ctorParams[i].ParameterType, 
                            ctorParamPath);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
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
                foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], $"{type.Name}");
                
                if (foldoutStates[foldoutKey])
                {
                    EditorGUI.indentLevel++;
                    var fields = GetSerializableFields(type);
                    
                    foreach (var field in fields)
                    {
                        string fieldPath = $"{parameterPath}_field_{field.Name}";
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(field.Name, GUILayout.Width(120));
                        var fieldValue = field.GetValue(value);
                        var newFieldValue = DrawParameterField(fieldValue, field.FieldType, fieldPath);
                        field.SetValue(value, newFieldValue);
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
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
            
            // For reference types, try to create instance with parameterless constructor
            try
            {
                // Check if type has a parameterless constructor
                var constructor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);
                    
                if (constructor != null)
                {
                    return Activator.CreateInstance(type);
                }
            }
            catch (Exception ex)
            {
                // If creation fails (e.g., recursive constructor), log and return null
                Debug.LogWarning($"Cannot create default instance of type {type.Name}: {ex.Message}");
            }
            
            return null;
        }

    #endregion

    #region Private Methods

        private void OnSelectionChanged()
        {
            GameObject selectedObject = Selection.activeGameObject;
            
            // Update target (can be null)
            if (selectedObject != target)
            {
                target = selectedObject;
                
                if (target != null)
                {
                    if (container == null)
                        container = new MethodContainer(target);
                    else
                        container.targetGameObject = target;
                        
                    container.showPrivateMethods = showPrivateMethods;
                    container.RefreshEntries();
                }
                else
                {
                    // Clear container when no selection
                    if (container != null)
                    {
                        container.targetGameObject = null;
                        container.methodEntries.Clear();
                    }
                }
                
                if (serializedObject != null)
                    serializedObject.Update();
                    
                Repaint();
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                if (container != null)
                {
                    container.RefreshEntries();
                    Repaint();
                }
            }
        }

    #endregion
    }
#endif
}