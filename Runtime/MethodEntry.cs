#region

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace MethodInvoker
{
    [Serializable]
    public class MethodEntry : ISerializationCallbackReceiver
    {
        #region Public Variables

        [NonSerialized]
        [HideInInspector]
        public Delegate Delegate;

        [NonSerialized]
        [HideInInspector]
        public object[] ParameterValues;

        [NonSerialized]
        [HideInInspector]
        public DelegateInfo DelegateInfo;

        #endregion

        #region Private Variables

        [SerializeField]
        [HideInInspector]
        private List<Object> unityReferences;

        [SerializeField]
        [HideInInspector]
        private byte[] bytes;

        #endregion

        #region Constructor

        public MethodEntry()
        {
            // Default constructor for serialization
        }

        public MethodEntry(Delegate del, DelegateInfo delegateInfo)
        {
            Delegate = del;
            DelegateInfo = delegateInfo;

            if (delegateInfo.Method != null)
            {
                ParameterValues = new object[delegateInfo.Method.GetParameters().Length];
            }
        }

        #endregion

        #region Public Methods

        public void Invoke()
        {
            if (ParameterValues == null) return;

            // If Delegate is available, use it (faster)
            if (Delegate != null)
            {
                Delegate.Method.Invoke(Delegate.Target, ParameterValues);
            }
            // Otherwise use MethodInfo directly (for ref/out parameters)
            else if (DelegateInfo.Method != null && DelegateInfo.Target != null)
            {
                DelegateInfo.Method.Invoke(DelegateInfo.Target, ParameterValues);
            }
        }

        public void OnAfterDeserialize()
        {
            if (bytes == null || bytes.Length == 0) return;
            var val = CustomSerializationUtility.DeserializeValue<SerializedData>(bytes, unityReferences);
            Delegate = val.Delegate;
            ParameterValues = val.ParameterValues;

            // Rebuild DelegateInfo from Delegate
            if (Delegate != null)
            {
                DelegateInfo = new DelegateInfo
                {
                    Method = Delegate.Method,
                    Target = Delegate.Target as Object
                };
            }
            else
            {
                // Try to restore from serialized DelegateInfo
                DelegateInfo = val.DelegateInfo;
            }
        }

        public void OnBeforeSerialize()
        {
            var val = new SerializedData()
            {
                Delegate = Delegate,
                DelegateInfo = DelegateInfo,
                ParameterValues = ParameterValues
            };
            bytes = CustomSerializationUtility.SerializeValue(val, out unityReferences);
        }

        #endregion

        #region Nested Types

        private struct SerializedData
        {
            public Delegate Delegate;
            public DelegateInfo DelegateInfo;
            public object[] ParameterValues;
        }

        #endregion
    }
}