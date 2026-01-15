#region

using System;
using System.Reflection;
using UnityEngine;

#endregion

namespace MethodInvoker
{
    [Serializable]
    public struct DelegateInfo
    {
        public UnityEngine.Object Target;

        [NonSerialized]
        public MethodInfo Method;
    }
}