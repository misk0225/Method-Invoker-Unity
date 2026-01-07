#region

using System.Reflection;
using UnityEngine;

#endregion

namespace MethodInvoker
{
    public struct DelegateInfo
    {
        public Object Target;
        public MethodInfo Method;
    }
}