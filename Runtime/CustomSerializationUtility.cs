#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace MethodInvoker
{
    /// <summary>
    /// Custom serialization utility to replace Odin's serialization system.
    /// Handles arbitrary types including Unity Objects, primitives, and complex types.
    /// </summary>
    public static class CustomSerializationUtility
    {
        #region Public Methods

        public static byte[] SerializeValue<T>(T value, out List<Object> unityReferences)
        {
            unityReferences = new List<Object>();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                SerializeObject(value, typeof(T), writer, unityReferences);
                return stream.ToArray();
            }
        }

        public static T DeserializeValue<T>(byte[] bytes, List<Object> unityReferences)
        {
            if (bytes == null || bytes.Length == 0) return default(T);
            if (unityReferences == null) unityReferences = new List<Object>();

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                return (T)DeserializeObject(typeof(T), reader, unityReferences);
            }
        }

        #endregion

        #region Private Methods

        private static void SerializeObject(object obj, Type type, BinaryWriter writer, List<Object> unityRefs)
        {
            if (obj == null)
            {
                writer.Write((byte)SerializationType.Null);
                return;
            }

            // Unity Object
            if (obj is Object unityObj)
            {
                writer.Write((byte)SerializationType.UnityObject);
                unityRefs.Add(unityObj);
                writer.Write(unityRefs.Count - 1);
                return;
            }

            // Primitive types
            if (type.IsPrimitive)
            {
                SerializePrimitive(obj, type, writer);
                return;
            }

            // String
            if (type == typeof(string))
            {
                writer.Write((byte)SerializationType.String);
                writer.Write((string)obj ?? string.Empty);
                return;
            }

            // Enum
            if (type.IsEnum)
            {
                writer.Write((byte)SerializationType.Enum);
                writer.Write(type.AssemblyQualifiedName);
                writer.Write((int)obj);
                return;
            }

            // Array
            if (type.IsArray)
            {
                writer.Write((byte)SerializationType.Array);
                var array = (Array)obj;
                var elementType = type.GetElementType();
                writer.Write(elementType.AssemblyQualifiedName);
                writer.Write(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    var element = array.GetValue(i);
                    // Use actual element type for object arrays, not declared element type
                    var actualElementType = element != null ? element.GetType() : elementType;
                    SerializeObject(element, actualElementType, writer, unityRefs);
                }
                return;
            }

            // Delegate
            if (typeof(Delegate).IsAssignableFrom(type))
            {
                writer.Write((byte)SerializationType.Delegate);
                var del = (Delegate)obj;

                // Serialize target
                if (del.Target is Object unityTarget)
                {
                    writer.Write(true);
                    unityRefs.Add(unityTarget);
                    writer.Write(unityRefs.Count - 1);
                }
                else
                {
                    writer.Write(false);
                }

                // Serialize method info
                var method = del.Method;
                writer.Write(method.DeclaringType.AssemblyQualifiedName);
                writer.Write(method.Name);

                // Serialize parameter types
                var parameters = method.GetParameters();
                writer.Write(parameters.Length);
                foreach (var param in parameters)
                {
                    var paramType = param.ParameterType;
                    // Handle by-reference types (ref/out parameters)
                    if (paramType.IsByRef)
                    {
                        paramType = paramType.GetElementType();
                    }
                    writer.Write(paramType.AssemblyQualifiedName);
                }
                return;
            }

            // Complex types (class/struct)
            writer.Write((byte)SerializationType.ComplexType);
            writer.Write(type.AssemblyQualifiedName);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var serializableFields = fields.Where(f =>
                f.IsPublic && !f.IsNotSerialized ||
                f.GetCustomAttribute<SerializeField>() != null).ToArray();

            writer.Write(serializableFields.Length);
            foreach (var field in serializableFields)
            {
                writer.Write(field.Name);
                SerializeObject(field.GetValue(obj), field.FieldType, writer, unityRefs);
            }
        }

        private static void SerializePrimitive(object obj, Type type, BinaryWriter writer)
        {
            if (type == typeof(bool))
            {
                writer.Write((byte)SerializationType.Bool);
                writer.Write((bool)obj);
            }
            else if (type == typeof(byte))
            {
                writer.Write((byte)SerializationType.Byte);
                writer.Write((byte)obj);
            }
            else if (type == typeof(sbyte))
            {
                writer.Write((byte)SerializationType.SByte);
                writer.Write((sbyte)obj);
            }
            else if (type == typeof(short))
            {
                writer.Write((byte)SerializationType.Short);
                writer.Write((short)obj);
            }
            else if (type == typeof(ushort))
            {
                writer.Write((byte)SerializationType.UShort);
                writer.Write((ushort)obj);
            }
            else if (type == typeof(int))
            {
                writer.Write((byte)SerializationType.Int);
                writer.Write((int)obj);
            }
            else if (type == typeof(uint))
            {
                writer.Write((byte)SerializationType.UInt);
                writer.Write((uint)obj);
            }
            else if (type == typeof(long))
            {
                writer.Write((byte)SerializationType.Long);
                writer.Write((long)obj);
            }
            else if (type == typeof(ulong))
            {
                writer.Write((byte)SerializationType.ULong);
                writer.Write((ulong)obj);
            }
            else if (type == typeof(float))
            {
                writer.Write((byte)SerializationType.Float);
                writer.Write((float)obj);
            }
            else if (type == typeof(double))
            {
                writer.Write((byte)SerializationType.Double);
                writer.Write((double)obj);
            }
            else if (type == typeof(char))
            {
                writer.Write((byte)SerializationType.Char);
                writer.Write((char)obj);
            }
        }

        private static object DeserializeObject(Type type, BinaryReader reader, List<Object> unityRefs)
        {
            var serializationType = (SerializationType)reader.ReadByte();

            if (serializationType == SerializationType.Null)
                return null;

            if (serializationType == SerializationType.UnityObject)
            {
                int index = reader.ReadInt32();
                return index >= 0 && index < unityRefs.Count ? unityRefs[index] : null;
            }

            if (serializationType == SerializationType.String)
                return reader.ReadString();

            if (serializationType == SerializationType.Enum)
            {
                var enumTypeName = reader.ReadString();
                var enumType = Type.GetType(enumTypeName);
                var value = reader.ReadInt32();
                return Enum.ToObject(enumType, value);
            }

            if (serializationType == SerializationType.Array)
            {
                var elementTypeName = reader.ReadString();
                var elementType = Type.GetType(elementTypeName);
                var length = reader.ReadInt32();
                var array = Array.CreateInstance(elementType, length);
                for (int i = 0; i < length; i++)
                {
                    array.SetValue(DeserializeObject(elementType, reader, unityRefs), i);
                }
                return array;
            }

            if (serializationType == SerializationType.Delegate)
            {
                // Deserialize target
                Object target = null;
                bool hasUnityTarget = reader.ReadBoolean();
                if (hasUnityTarget)
                {
                    int index = reader.ReadInt32();
                    target = index >= 0 && index < unityRefs.Count ? unityRefs[index] : null;
                }

                // Deserialize method info
                var declaringTypeName = reader.ReadString();
                var methodName = reader.ReadString();
                var paramCount = reader.ReadInt32();
                var paramTypes = new Type[paramCount];
                for (int i = 0; i < paramCount; i++)
                {
                    paramTypes[i] = Type.GetType(reader.ReadString());
                }

                var declaringType = Type.GetType(declaringTypeName);
                if (declaringType == null || target == null) return null;

                var method = declaringType.GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null, paramTypes, null);

                if (method == null) return null;

                // Recreate delegate
                Type delegateType;
                if (method.ReturnType == typeof(void))
                {
                    if (paramCount == 0) delegateType = typeof(Action);
                    else if (paramCount == 1) delegateType = typeof(Action<>).MakeGenericType(paramTypes);
                    else if (paramCount == 2) delegateType = typeof(Action<,>).MakeGenericType(paramTypes);
                    else if (paramCount == 3) delegateType = typeof(Action<,,>).MakeGenericType(paramTypes);
                    else if (paramCount == 4) delegateType = typeof(Action<,,,>).MakeGenericType(paramTypes);
                    else if (paramCount == 5) delegateType = typeof(Action<,,,,>).MakeGenericType(paramTypes);
                    else return null;
                }
                else
                {
                    var allTypes = paramTypes.Append(method.ReturnType).ToArray();
                    if (paramCount == 0) delegateType = typeof(Func<>).MakeGenericType(allTypes);
                    else if (paramCount == 1) delegateType = typeof(Func<,>).MakeGenericType(allTypes);
                    else if (paramCount == 2) delegateType = typeof(Func<,,>).MakeGenericType(allTypes);
                    else if (paramCount == 3) delegateType = typeof(Func<,,,>).MakeGenericType(allTypes);
                    else if (paramCount == 4) delegateType = typeof(Func<,,,,>).MakeGenericType(allTypes);
                    else if (paramCount == 5) delegateType = typeof(Func<,,,,,>).MakeGenericType(allTypes);
                    else return null;
                }

                return Delegate.CreateDelegate(delegateType, target, method);
            }

            // Primitive types
            if (serializationType == SerializationType.Bool) return reader.ReadBoolean();
            if (serializationType == SerializationType.Byte) return reader.ReadByte();
            if (serializationType == SerializationType.SByte) return reader.ReadSByte();
            if (serializationType == SerializationType.Short) return reader.ReadInt16();
            if (serializationType == SerializationType.UShort) return reader.ReadUInt16();
            if (serializationType == SerializationType.Int) return reader.ReadInt32();
            if (serializationType == SerializationType.UInt) return reader.ReadUInt32();
            if (serializationType == SerializationType.Long) return reader.ReadInt64();
            if (serializationType == SerializationType.ULong) return reader.ReadUInt64();
            if (serializationType == SerializationType.Float) return reader.ReadSingle();
            if (serializationType == SerializationType.Double) return reader.ReadDouble();
            if (serializationType == SerializationType.Char) return reader.ReadChar();

            // Complex type
            if (serializationType == SerializationType.ComplexType)
            {
                var typeName = reader.ReadString();
                var objType = Type.GetType(typeName);
                if (objType == null) return null;

                var instance = Activator.CreateInstance(objType);
                var fieldCount = reader.ReadInt32();

                for (int i = 0; i < fieldCount; i++)
                {
                    var fieldName = reader.ReadString();
                    var field = objType.GetField(fieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        var value = DeserializeObject(field.FieldType, reader, unityRefs);
                        field.SetValue(instance, value);
                    }
                }
                return instance;
            }

            return null;
        }

        #endregion

        #region Nested Types

        private enum SerializationType : byte
        {
            Null = 0,
            Bool = 1,
            Byte = 2,
            SByte = 3,
            Short = 4,
            UShort = 5,
            Int = 6,
            UInt = 7,
            Long = 8,
            ULong = 9,
            Float = 10,
            Double = 11,
            Char = 12,
            String = 13,
            UnityObject = 14,
            Array = 15,
            Delegate = 16,
            Enum = 17,
            ComplexType = 18
        }

        #endregion
    }
}
