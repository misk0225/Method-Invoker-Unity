#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#endregion

namespace MethodInvoker
{
    [Serializable]
    public class MethodContainer
    {
        #region Public Variables

        [HideInInspector]
        public GameObject targetGameObject;

        public List<MethodEntry> methodEntries = new List<MethodEntry>();

        #endregion

        #region Constructor

        public MethodContainer(GameObject target)
        {
            targetGameObject = target;
            RefreshEntries();
        }

        #endregion

        #region Public Methods

        public void RefreshEntries()
        {
            methodEntries.Clear();
            if (targetGameObject == null) return;
            var components = targetGameObject.GetComponents<Component>();
            if (components.Length > 0)
                foreach (var component in components)
                {
                    if (component == null) continue;
                    var type = component.GetType();
                    var methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var methodInfo in methodInfos)
                    {
                        if (methodInfo.ReturnType != typeof(void))
                            continue;
                        var info = new DelegateInfo { Method = methodInfo, Target = component };
                        var newDelegate = CreateAndAssignNewDelegate(info);
                        methodEntries.Add(new MethodEntry(newDelegate, info));
                    }
                }
        }

        #endregion

        #region Private Methods

        private Delegate CreateAndAssignNewDelegate(DelegateInfo delInfo)
        {
            var method = delInfo.Method;
            var target = delInfo.Target;
            var parameters = method.GetParameters();

            // Check if any parameter is by-reference (ref/out)
            // Action/Func delegates don't support ref/out, so return null
            // The method will be invoked using MethodInfo.Invoke instead
            if (parameters.Any(p => p.ParameterType.IsByRef))
            {
                return null;
            }

            var pTypes = parameters.Select(x => x.ParameterType).ToArray();
            var args = new object[pTypes.Length];

            Type delegateType = null;

            if (method.ReturnType == typeof(void))
            {
                if (args.Length == 0) delegateType = typeof(Action);
                else if (args.Length == 1) delegateType = typeof(Action<>).MakeGenericType(pTypes);
                else if (args.Length == 2) delegateType = typeof(Action<,>).MakeGenericType(pTypes);
                else if (args.Length == 3) delegateType = typeof(Action<,,>).MakeGenericType(pTypes);
                else if (args.Length == 4) delegateType = typeof(Action<,,,>).MakeGenericType(pTypes);
                else if (args.Length == 5) delegateType = typeof(Action<,,,,>).MakeGenericType(pTypes);
            }
            else
            {
                pTypes = pTypes.Append(method.ReturnType).ToArray();
                if (args.Length == 0) delegateType = typeof(Func<>).MakeGenericType(new[] { method.ReturnType });
                else if (args.Length == 1) delegateType = typeof(Func<,>).MakeGenericType(pTypes);
                else if (args.Length == 2) delegateType = typeof(Func<,,>).MakeGenericType(pTypes);
                else if (args.Length == 3) delegateType = typeof(Func<,,,>).MakeGenericType(pTypes);
                else if (args.Length == 4) delegateType = typeof(Func<,,,,>).MakeGenericType(pTypes);
                else if (args.Length == 5) delegateType = typeof(Func<,,,,,>).MakeGenericType(pTypes);
            }

            if (delegateType == null)
            {
                Debug.LogError("Unsupported Method Type");
                return null;
            }

            var del = Delegate.CreateDelegate(delegateType, target, method);

            return del;
        }

        #endregion
    }
}