#region

using System;
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

    #endregion

    #region Protected Methods

        protected virtual void OnEnable()
        {
            instance = GetWindow<MethodInvokerWindow>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            if (container == null)
                container = new MethodContainer(null);
                
            serializedObject = new SerializedObject(this);
            containerProperty = serializedObject.FindProperty("container");
        }

        protected virtual void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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
            
            // Draw header
            var headerStyle = new GUIStyle(EditorStyles.toolbar);
            GUILayout.BeginVertical(headerStyle);
            GUILayout.Space(5);
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.cyan;
            EditorGUILayout.LabelField("Target GameObject", EditorStyles.boldLabel);
            GUI.backgroundColor = oldColor;
            
            EditorGUI.BeginChangeCheck();
            target = (GameObject)EditorGUILayout.ObjectField(target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (container == null)
                    container = new MethodContainer(target);
                else
                    container.targetGameObject = target;
                container.RefreshEntries();
                serializedObject.Update();
            }
            
            GUILayout.Space(5);
            GUILayout.EndVertical();
            
            if (target != null)
            {
                GUILayout.Space(5);
                DrawDivider(Color.green);
                GUILayout.Space(5);
                
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
                        int lastInstanceId = -9999;
                        
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
                                int instanceID = comp.GetInstanceID();
                                if (instanceID != lastInstanceId)
                                {
                                    if (lastInstanceId != -9999)
                                    {
                                        GUILayout.Space(5);
                                        DrawDivider(Color.red);
                                        GUILayout.Space(5);
                                    }
                                    
                                    EditorGUILayout.ObjectField(comp, comp.GetType(), true);
                                    GUILayout.Space(5);
                                    
                                    lastInstanceId = instanceID;
                                }
                            }
                            
                            // Draw method entry directly
                            DrawMethodEntry(methodEntry, i);
                            GUILayout.Space(5);
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
            }
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(param.Name, GUILayout.Width(EditorGUIUtility.labelWidth));
                methodEntry.ParameterValues[i] = DrawParameterField(methodEntry.ParameterValues[i], param.ParameterType);
                EditorGUILayout.EndHorizontal();
            }
            
            // Invoke button
            if (GUILayout.Button("Invoke", GUILayout.Height(22)))
            {
                methodEntry.Invoke();
            }
            
            GUILayout.EndVertical();
        }
        
        private object DrawParameterField(object value, Type type)
        {
            // Handle by-reference types
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }
            
            // Handle different types
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

    #endregion

    #region Private Methods

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