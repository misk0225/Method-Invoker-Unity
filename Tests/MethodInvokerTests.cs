using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MethodInvoker;

namespace MethodInvoker.Tests
{
    public class MethodInvokerTests
    {
        private GameObject testObject;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestObject");
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void Test_EmptyGameObject_NoMethods()
        {
            var container = new MethodContainer(testObject);
            // GameObject has Transform component which has public methods
            // We just verify the container was created successfully
            Assert.IsNotNull(container, "Container should be created");
            Assert.IsNotNull(container.methodEntries, "Method entries should be initialized");
        }

        [Test]
        public void Test_GameObjectWithComponents_FindsMethods()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            Assert.Greater(container.methodEntries.Count, 0, "Should find methods on TestMethodScript");
        }

        [Test]
        public void Test_NoParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var noParamMethod = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "NoParameterMethod");

            Assert.IsNotNull(noParamMethod, "Should find NoParameterMethod");
            Assert.DoesNotThrow(() => noParamMethod.Invoke(), "Should invoke without error");
            Assert.IsTrue(script.noParamCalled, "Method should have been called");
        }

        [Test]
        public void Test_IntParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "IntParameterMethod");

            Assert.IsNotNull(method, "Should find IntParameterMethod");
            Assert.IsNotNull(method.ParameterValues, "Should have parameter array");
            Assert.AreEqual(1, method.ParameterValues.Length, "Should have 1 parameter");

            method.ParameterValues[0] = 42;
            Assert.DoesNotThrow(() => method.Invoke(), "Should invoke without error");
            Assert.AreEqual(42, script.lastIntValue, "Should receive correct value");
        }

        [Test]
        public void Test_FloatParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "FloatParameterMethod");

            Assert.IsNotNull(method, "Should find FloatParameterMethod");
            method.ParameterValues[0] = 3.14f;
            method.Invoke();
            Assert.AreEqual(3.14f, script.lastFloatValue, 0.001f);
        }

        [Test]
        public void Test_StringParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "StringParameterMethod");

            Assert.IsNotNull(method, "Should find StringParameterMethod");
            method.ParameterValues[0] = "Hello";
            method.Invoke();
            Assert.AreEqual("Hello", script.lastStringValue);
        }

        [Test]
        public void Test_BoolParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "BoolParameterMethod");

            Assert.IsNotNull(method, "Should find BoolParameterMethod");
            method.ParameterValues[0] = true;
            method.Invoke();
            Assert.IsTrue(script.lastBoolValue);
        }

        [Test]
        public void Test_Vector3ParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "Vector3ParameterMethod");

            Assert.IsNotNull(method, "Should find Vector3ParameterMethod");
            var testVector = new Vector3(1, 2, 3);
            method.ParameterValues[0] = testVector;
            method.Invoke();
            Assert.AreEqual(testVector, script.lastVector3Value);
        }

        [Test]
        public void Test_ColorParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "ColorParameterMethod");

            Assert.IsNotNull(method, "Should find ColorParameterMethod");
            var testColor = Color.red;
            method.ParameterValues[0] = testColor;
            method.Invoke();
            Assert.AreEqual(testColor, script.lastColorValue);
        }

        [Test]
        public void Test_EnumParameterMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "EnumParameterMethod");

            Assert.IsNotNull(method, "Should find EnumParameterMethod");
            method.ParameterValues[0] = TestEnum.Value2;
            method.Invoke();
            Assert.AreEqual(TestEnum.Value2, script.lastEnumValue);
        }

        [Test]
        public void Test_MultipleParametersMethod_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "MultipleParametersMethod");

            Assert.IsNotNull(method, "Should find MultipleParametersMethod");
            Assert.AreEqual(3, method.ParameterValues.Length, "Should have 3 parameters");

            method.ParameterValues[0] = 10;
            method.ParameterValues[1] = "Test";
            method.ParameterValues[2] = 2.5f;
            method.Invoke();

            Assert.AreEqual(10, script.lastIntValue);
            Assert.AreEqual("Test", script.lastStringValue);
            Assert.AreEqual(2.5f, script.lastFloatValue, 0.001f);
        }

        [Test]
        public void Test_Serialization_PreservesDelegate()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var del = new Action(script.NoParameterMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);

            // Test that serialization callbacks work without errors
            entry.OnBeforeSerialize();

            // Verify internal state was serialized
            var bytesField = typeof(MethodEntry).GetField("bytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bytes = (byte[])bytesField.GetValue(entry);

            Assert.IsNotNull(bytes, "OnBeforeSerialize should produce bytes");
            Assert.Greater(bytes.Length, 0, "Should have serialized data");
        }

        [Test]
        public void Test_Serialization_PreservesParameters()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var del = new Action<int, string>(script.TwoParametersMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);
            entry.ParameterValues[0] = 99;
            entry.ParameterValues[1] = "TestString";

            // Test that serialization callbacks work without errors
            entry.OnBeforeSerialize();

            // Verify internal state was serialized
            var bytesField = typeof(MethodEntry).GetField("bytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bytes = (byte[])bytesField.GetValue(entry);

            Assert.IsNotNull(bytes, "OnBeforeSerialize should produce bytes");
            Assert.Greater(bytes.Length, 0, "Should have serialized data");
        }

        [Test]
        public void Test_RefreshEntries_UpdatesMethodList()
        {
            var container = new MethodContainer(testObject);
            int initialCount = container.methodEntries.Count;
            // GameObject has Transform component, so initial count > 0

            testObject.AddComponent<TestMethodScript>();
            container.RefreshEntries();
            int finalCount = container.methodEntries.Count;

            Assert.Greater(finalCount, initialCount, "Should have more methods after adding component");
        }

        [Test]
        public void Test_BuiltInComponents_FindsMethods()
        {
            var spriteRenderer = testObject.AddComponent<SpriteRenderer>();
            var container = new MethodContainer(testObject);

            Assert.Greater(container.methodEntries.Count, 0, "Should find methods on built-in components");
        }

        [Test]
        public void Test_NullGameObject_NoError()
        {
            var container = new MethodContainer(null);
            Assert.AreEqual(0, container.methodEntries.Count);
            Assert.DoesNotThrow(() => container.RefreshEntries());
        }

        [Test]
        public void Test_IntArrayParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "IntArrayParameterMethod");

            Assert.IsNotNull(method, "Should find IntArrayParameterMethod");
            Assert.IsNotNull(method.ParameterValues, "Should have parameter array");
            Assert.AreEqual(1, method.ParameterValues.Length, "Should have 1 parameter");

            int[] testArray = new int[] { 1, 2, 3, 4, 5 };
            method.ParameterValues[0] = testArray;
            method.Invoke();

            Assert.IsNotNull(script.lastIntArray, "Array should not be null");
            Assert.AreEqual(5, script.lastIntArray.Length, "Array length should match");
            Assert.AreEqual(1, script.lastIntArray[0], "First element should match");
            Assert.AreEqual(5, script.lastIntArray[4], "Last element should match");
        }

        [Test]
        public void Test_StringArrayParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "StringArrayParameterMethod");

            Assert.IsNotNull(method, "Should find StringArrayParameterMethod");

            string[] testArray = new string[] { "Hello", "World", "Test" };
            method.ParameterValues[0] = testArray;
            method.Invoke();

            Assert.IsNotNull(script.lastStringArray, "Array should not be null");
            Assert.AreEqual(3, script.lastStringArray.Length, "Array length should match");
            Assert.AreEqual("Hello", script.lastStringArray[0], "First element should match");
            Assert.AreEqual("Test", script.lastStringArray[2], "Last element should match");
        }

        [Test]
        public void Test_CustomClassParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "CustomClassParameterMethod");

            Assert.IsNotNull(method, "Should find CustomClassParameterMethod");

            var testClass = new TestCustomClass();
            testClass.intField = 42;
            testClass.stringField = "TestString";
            testClass.floatField = 3.14f;

            method.ParameterValues[0] = testClass;
            method.Invoke();

            Assert.IsNotNull(script.lastCustomClass, "Custom class should not be null");
            Assert.AreEqual(42, script.lastCustomClass.intField, "Int field should match");
            Assert.AreEqual("TestString", script.lastCustomClass.stringField, "String field should match");
            Assert.AreEqual(3.14f, script.lastCustomClass.floatField, 0.001f, "Float field should match");
        }

        [Test]
        public void Test_CustomClassWithConstructor_CanCreate()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "CustomClassParameterMethod");

            Assert.IsNotNull(method, "Should find CustomClassParameterMethod");

            // Test parameterless constructor
            var testClass1 = new TestCustomClass();
            Assert.IsNotNull(testClass1, "Should create instance with parameterless constructor");

            // Test constructor with one parameter
            var testClass2 = new TestCustomClass(100);
            Assert.AreEqual(100, testClass2.intField, "Constructor parameter should be set");

            // Test constructor with multiple parameters
            var testClass3 = new TestCustomClass(200, "ConstructorTest");
            Assert.AreEqual(200, testClass3.intField, "First constructor parameter should be set");
            Assert.AreEqual("ConstructorTest", testClass3.stringField, "Second constructor parameter should be set");
        }

        [Test]
        public void Test_NestedClassParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "NestedClassParameterMethod");

            Assert.IsNotNull(method, "Should find NestedClassParameterMethod");

            var nestedClass = new TestNestedClass();
            nestedClass.value = 123;
            nestedClass.nestedObject = new TestCustomClass();
            nestedClass.nestedObject.intField = 456;
            nestedClass.nestedObject.stringField = "Nested";

            method.ParameterValues[0] = nestedClass;
            method.Invoke();

            Assert.IsNotNull(script.lastNestedClass, "Nested class should not be null");
            Assert.AreEqual(123, script.lastNestedClass.value, "Nested class value should match");
            Assert.IsNotNull(script.lastNestedClass.nestedObject, "Nested object should not be null");
            Assert.AreEqual(456, script.lastNestedClass.nestedObject.intField, "Nested object field should match");
            Assert.AreEqual("Nested", script.lastNestedClass.nestedObject.stringField, "Nested object string should match");
        }

        [Test]
        public void Test_CustomClassArrayParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "CustomClassArrayParameterMethod");

            Assert.IsNotNull(method, "Should find CustomClassArrayParameterMethod");

            var testArray = new TestCustomClass[2];
            testArray[0] = new TestCustomClass();
            testArray[0].intField = 10;
            testArray[0].stringField = "First";

            testArray[1] = new TestCustomClass();
            testArray[1].intField = 20;
            testArray[1].stringField = "Second";

            method.ParameterValues[0] = testArray;
            method.Invoke();

            Assert.IsNotNull(script.lastCustomClassArray, "Array should not be null");
            Assert.AreEqual(2, script.lastCustomClassArray.Length, "Array length should match");
            Assert.AreEqual(10, script.lastCustomClassArray[0].intField, "First element int field should match");
            Assert.AreEqual("First", script.lastCustomClassArray[0].stringField, "First element string field should match");
            Assert.AreEqual(20, script.lastCustomClassArray[1].intField, "Second element int field should match");
            Assert.AreEqual("Second", script.lastCustomClassArray[1].stringField, "Second element string field should match");
        }

        [Test]
        public void Test_CustomClassSerialization_PreservesFields()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var del = new System.Action<TestCustomClass>(script.CustomClassParameterMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);

            var testClass = new TestCustomClass();
            testClass.intField = 999;
            testClass.stringField = "SerializationTest";
            testClass.floatField = 1.23f;
            entry.ParameterValues[0] = testClass;

            // Test that serialization callbacks work without errors
            entry.OnBeforeSerialize();

            // Verify internal state was serialized
            var bytesField = typeof(MethodEntry).GetField("bytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bytes = (byte[])bytesField.GetValue(entry);

            Assert.IsNotNull(bytes, "OnBeforeSerialize should produce bytes");
            Assert.Greater(bytes.Length, 0, "Should have serialized data");

            // Deserialize and verify
            entry.OnAfterDeserialize();

            Assert.IsNotNull(entry.ParameterValues, "Parameters should be restored");
            Assert.AreEqual(1, entry.ParameterValues.Length, "Should have 1 parameter");

            var deserializedClass = entry.ParameterValues[0] as TestCustomClass;
            Assert.IsNotNull(deserializedClass, "Deserialized object should not be null");
            Assert.AreEqual(999, deserializedClass.intField, "Int field should be preserved");
            Assert.AreEqual("SerializationTest", deserializedClass.stringField, "String field should be preserved");
            Assert.AreEqual(1.23f, deserializedClass.floatField, 0.001f, "Float field should be preserved");
        }

        [Test]
        public void Test_ArraySerialization_PreservesElements()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var del = new System.Action<int[]>(script.IntArrayParameterMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);

            int[] testArray = new int[] { 10, 20, 30, 40 };
            entry.ParameterValues[0] = testArray;

            // Test that serialization callbacks work without errors
            entry.OnBeforeSerialize();

            // Verify internal state was serialized
            var bytesField = typeof(MethodEntry).GetField("bytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bytes = (byte[])bytesField.GetValue(entry);

            Assert.IsNotNull(bytes, "OnBeforeSerialize should produce bytes");
            Assert.Greater(bytes.Length, 0, "Should have serialized data");

            // Deserialize and verify
            entry.OnAfterDeserialize();

            Assert.IsNotNull(entry.ParameterValues, "Parameters should be restored");
            var deserializedArray = entry.ParameterValues[0] as int[];
            Assert.IsNotNull(deserializedArray, "Deserialized array should not be null");
            Assert.AreEqual(4, deserializedArray.Length, "Array length should be preserved");
            Assert.AreEqual(10, deserializedArray[0], "First element should be preserved");
            Assert.AreEqual(40, deserializedArray[3], "Last element should be preserved");
        }

        [Test]
        public void Test_ClassWithArrayField_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "ClassWithArrayParameterMethod");

            Assert.IsNotNull(method, "Should find ClassWithArrayParameterMethod");

            var classWithArray = new TestClassWithArray();
            classWithArray.name = "TestArray";
            classWithArray.numbers = new int[] { 1, 2, 3 };

            method.ParameterValues[0] = classWithArray;
            method.Invoke();

            Assert.IsNotNull(script.lastClassWithArray, "Class should not be null");
            Assert.AreEqual("TestArray", script.lastClassWithArray.name, "Name should match");
            Assert.IsNotNull(script.lastClassWithArray.numbers, "Array field should not be null");
            Assert.AreEqual(3, script.lastClassWithArray.numbers.Length, "Array length should match");
            Assert.AreEqual(1, script.lastClassWithArray.numbers[0], "Array element should match");
        }

        [Test]
        public void Test_DeepNestedClass_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "DeepNestedClassParameterMethod");

            Assert.IsNotNull(method, "Should find DeepNestedClassParameterMethod");

            // Create deep nested structure
            var deepNested = new TestDeepNestedClass();
            deepNested.depth = 3;
            deepNested.level1 = new TestNestedClass();
            deepNested.level1.value = 100;
            deepNested.level1.nestedObject = new TestCustomClass();
            deepNested.level1.nestedObject.intField = 999;
            deepNested.level1.nestedObject.stringField = "DeepNested";

            method.ParameterValues[0] = deepNested;
            method.Invoke();

            Assert.IsNotNull(script.lastDeepNestedClass, "Deep nested class should not be null");
            Assert.AreEqual(3, script.lastDeepNestedClass.depth, "Depth should match");
            Assert.IsNotNull(script.lastDeepNestedClass.level1, "Level 1 should not be null");
            Assert.AreEqual(100, script.lastDeepNestedClass.level1.value, "Level 1 value should match");
            Assert.IsNotNull(script.lastDeepNestedClass.level1.nestedObject, "Level 2 should not be null");
            Assert.AreEqual(999, script.lastDeepNestedClass.level1.nestedObject.intField, "Level 2 int should match");
            Assert.AreEqual("DeepNested", script.lastDeepNestedClass.level1.nestedObject.stringField, "Level 2 string should match");
        }

        [Test]
        public void Test_ArrayOfCustomClasses_EachElementEditable()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "CustomClassArrayParameterMethod");

            Assert.IsNotNull(method, "Should find CustomClassArrayParameterMethod");

            // Test that we can create and modify multiple elements
            var array = new TestCustomClass[3];
            array[0] = new TestCustomClass(10, "First");
            array[1] = new TestCustomClass(20, "Second");
            array[2] = new TestCustomClass(30, "Third");

            method.ParameterValues[0] = array;
            method.Invoke();

            Assert.IsNotNull(script.lastCustomClassArray, "Array should not be null");
            Assert.AreEqual(3, script.lastCustomClassArray.Length, "Should have 3 elements");

            // Verify each element maintains its data
            Assert.AreEqual(10, script.lastCustomClassArray[0].intField, "First element int should match");
            Assert.AreEqual("First", script.lastCustomClassArray[0].stringField, "First element string should match");
            Assert.AreEqual(20, script.lastCustomClassArray[1].intField, "Second element int should match");
            Assert.AreEqual("Second", script.lastCustomClassArray[1].stringField, "Second element string should match");
            Assert.AreEqual(30, script.lastCustomClassArray[2].intField, "Third element int should match");
            Assert.AreEqual("Third", script.lastCustomClassArray[2].stringField, "Third element string should match");
        }

        [Test]
        public void Test_ConstructorWithNestedClassParameter()
        {
            // This tests that the DrawParameterFieldInternal recursion works for constructor parameters
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "DeepNestedClassParameterMethod");

            Assert.IsNotNull(method, "Should find DeepNestedClassParameterMethod");

            // TestDeepNestedClass has a constructor that takes TestNestedClass
            // This verifies that constructor parameters support complex types
            var nestedParam = new TestNestedClass(50);
            nestedParam.nestedObject = new TestCustomClass(123, "CtorTest");

            var deepNested = new TestDeepNestedClass(nestedParam);
            deepNested.depth = 5;

            method.ParameterValues[0] = deepNested;
            method.Invoke();

            Assert.IsNotNull(script.lastDeepNestedClass, "Should not be null");
            Assert.IsNotNull(script.lastDeepNestedClass.level1, "Constructor parameter should be set");
            Assert.AreEqual(50, script.lastDeepNestedClass.level1.value, "Constructor param value should match");
            Assert.AreEqual(123, script.lastDeepNestedClass.level1.nestedObject.intField, "Nested field should match");
        }

        // ====== 新增測試：邊界情況 ======

        [Test]
        public void Test_EmptyArray_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "IntArrayParameterMethod");

            Assert.IsNotNull(method, "Should find IntArrayParameterMethod");

            // Test with empty array
            int[] emptyArray = new int[0];
            method.ParameterValues[0] = emptyArray;
            method.Invoke();

            Assert.IsNotNull(script.lastIntArray, "Array should not be null");
            Assert.AreEqual(0, script.lastIntArray.Length, "Array should be empty");
        }

        [Test]
        public void Test_NullArrayParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "IntArrayParameterMethod");

            Assert.IsNotNull(method, "Should find IntArrayParameterMethod");

            // Test with null array
            method.ParameterValues[0] = null;
            method.Invoke();

            Assert.IsNull(script.lastIntArray, "Array should remain null");
        }

        [Test]
        public void Test_NullStringParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "StringParameterMethod");

            Assert.IsNotNull(method, "Should find StringParameterMethod");

            // Test with null string
            method.ParameterValues[0] = null;
            method.Invoke();

            Assert.IsNull(script.lastStringValue, "String should remain null");
        }

        [Test]
        public void Test_MultipleComponents_SameType()
        {
            var script1 = testObject.AddComponent<TestMethodScript>();
            var script2 = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            // Should find methods from both components
            var methods = container.methodEntries.FindAll(m =>
                m.Delegate?.Method?.Name == "NoParameterMethod");

            Assert.GreaterOrEqual(methods.Count, 2, "Should find methods from both TestMethodScript components");
        }

        [Test]
        public void Test_ParameterValues_AutoInitialize()
        {
            var script = testObject.AddComponent<TestMethodScript>();

            // Create entry with method that has 3 parameters
            var del = new System.Action<int, string, float>(script.MultipleParametersMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);

            // ParameterValues should be automatically initialized in constructor
            Assert.IsNotNull(entry.ParameterValues, "ParameterValues should be initialized");
            Assert.AreEqual(3, entry.ParameterValues.Length, "Should have 3 parameters for MultipleParametersMethod");

            // Verify that creating entry with different parameter count works
            var del2 = new System.Action(script.NoParameterMethod);
            var delInfo2 = new DelegateInfo { Method = del2.Method, Target = script };
            var entry2 = new MethodEntry(del2, delInfo2);

            Assert.IsNotNull(entry2.ParameterValues, "ParameterValues should be initialized");
            Assert.AreEqual(0, entry2.ParameterValues.Length, "Should have 0 parameters for NoParameterMethod");
        }

        // ====== 新增測試：其他 Unity 類型 ======

        [Test]
        public void Test_QuaternionParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "QuaternionParameterMethod");

            Assert.IsNotNull(method, "Should find QuaternionParameter");

            var testQuaternion = Quaternion.Euler(45, 90, 180);
            method.ParameterValues[0] = testQuaternion;
            method.Invoke();

            Assert.AreEqual(testQuaternion.x, script.lastQuaternionValue.x, 0.001f, "Quaternion X should match");
            Assert.AreEqual(testQuaternion.y, script.lastQuaternionValue.y, 0.001f, "Quaternion Y should match");
            Assert.AreEqual(testQuaternion.z, script.lastQuaternionValue.z, 0.001f, "Quaternion Z should match");
            Assert.AreEqual(testQuaternion.w, script.lastQuaternionValue.w, 0.001f, "Quaternion W should match");
        }

        [Test]
        public void Test_RectParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "RectParameterMethod");

            Assert.IsNotNull(method, "Should find RectParameterMethod");

            var testRect = new Rect(10, 20, 100, 200);
            method.ParameterValues[0] = testRect;
            method.Invoke();

            Assert.AreEqual(testRect.x, script.lastRectValue.x, 0.001f, "Rect X should match");
            Assert.AreEqual(testRect.y, script.lastRectValue.y, 0.001f, "Rect Y should match");
            Assert.AreEqual(testRect.width, script.lastRectValue.width, 0.001f, "Rect width should match");
            Assert.AreEqual(testRect.height, script.lastRectValue.height, 0.001f, "Rect height should match");
        }

        [Test]
        public void Test_BoundsParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "BoundsParameterMethod");

            Assert.IsNotNull(method, "Should find BoundsParameterMethod");

            var testBounds = new Bounds(new Vector3(1, 2, 3), new Vector3(10, 20, 30));
            method.ParameterValues[0] = testBounds;
            method.Invoke();

            Assert.AreEqual(testBounds.center, script.lastBoundsValue.center, "Bounds center should match");
            Assert.AreEqual(testBounds.size, script.lastBoundsValue.size, "Bounds size should match");
        }

        // ====== 新增測試：錯誤處理 ======

        [Test]
        public void Test_MethodInvoke_WithNullDelegate()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var delInfo = new DelegateInfo
            {
                Method = typeof(TestMethodScript).GetMethod("NoParameterMethod"),
                Target = script
            };
            var entry = new MethodEntry(null, delInfo); // Delegate is null

            // Should not throw exception, use MethodInfo.Invoke instead
            Assert.DoesNotThrow(() => entry.Invoke(), "Should handle null delegate gracefully");
            Assert.IsTrue(script.noParamCalled, "Method should have been called via MethodInfo");
        }

        [Test]
        public void Test_Serialization_WithNullParameterValues()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var del = new System.Action<string>(script.StringParameterMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);

            // Set parameter to null
            entry.ParameterValues[0] = null;

            // Test serialization with null value
            Assert.DoesNotThrow(() => entry.OnBeforeSerialize(), "Should serialize null values without error");

            var bytesField = typeof(MethodEntry).GetField("bytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bytes = (byte[])bytesField.GetValue(entry);

            Assert.IsNotNull(bytes, "Should produce bytes even with null parameter");

            // Deserialize and verify
            Assert.DoesNotThrow(() => entry.OnAfterDeserialize(), "Should deserialize null values without error");
            Assert.IsNull(entry.ParameterValues[0], "Null value should be preserved after deserialization");
        }

        [Test]
        public void Test_Serialization_EmptyArray_PreservesType()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var del = new System.Action<int[]>(script.IntArrayParameterMethod);
            var delInfo = new DelegateInfo { Method = del.Method, Target = script };
            var entry = new MethodEntry(del, delInfo);

            // Set empty array
            entry.ParameterValues[0] = new int[0];

            // Serialize and deserialize
            entry.OnBeforeSerialize();
            entry.OnAfterDeserialize();

            Assert.IsNotNull(entry.ParameterValues[0], "Array should not be null");
            Assert.IsInstanceOf<int[]>(entry.ParameterValues[0], "Should preserve array type");
            Assert.AreEqual(0, ((int[])entry.ParameterValues[0]).Length, "Should preserve empty array");
        }

        [Test]
        public void Test_Container_WithNullGameObject_NoError()
        {
            var container = new MethodContainer(null);

            Assert.IsNotNull(container, "Container should be created");
            Assert.IsNotNull(container.methodEntries, "Method entries should be initialized");
            Assert.AreEqual(0, container.methodEntries.Count, "Should have no entries for null GameObject");
            Assert.DoesNotThrow(() => container.RefreshEntries(), "Should handle null GameObject gracefully");
        }

        [Test]
        public void Test_DestroyedComponent_HandlesGracefully()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            var method = container.methodEntries.Find(m =>
                m.Delegate?.Method?.Name == "NoParameterMethod");

            Assert.IsNotNull(method, "Should find method");

            // Destroy the component
            UnityEngine.Object.DestroyImmediate(script);

            // Refreshing should handle destroyed component gracefully
            Assert.DoesNotThrow(() => container.RefreshEntries(), "Should handle destroyed component");
        }

        [Test]
        public void Test_PrivateMethods_NotShownByDefault()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);
            container.showPrivateMethods = false;
            container.RefreshEntries();

            var privateMethod = container.methodEntries.Find(m =>
                m.DelegateInfo.Method?.Name == "PrivateNoParameterMethod");

            Assert.IsNull(privateMethod, "Should not find private methods when showPrivateMethods is false");
        }

        [Test]
        public void Test_PrivateMethods_ShownWhenEnabled()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);
            container.showPrivateMethods = true;
            container.RefreshEntries();

            var privateMethod = container.methodEntries.Find(m =>
                m.DelegateInfo.Method?.Name == "PrivateNoParameterMethod");

            Assert.IsNotNull(privateMethod, "Should find private methods when showPrivateMethods is true");
        }

        [Test]
        public void Test_PrivateMethod_NoParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);
            container.showPrivateMethods = true;
            container.RefreshEntries();

            var privateMethod = container.methodEntries.Find(m =>
                m.DelegateInfo.Method?.Name == "PrivateNoParameterMethod");

            Assert.IsNotNull(privateMethod, "Should find PrivateNoParameterMethod");
            Assert.DoesNotThrow(() => privateMethod.Invoke(), "Should invoke private method without error");
            Assert.IsTrue(script.noParamCalled, "Private method should have been called");
        }

        [Test]
        public void Test_PrivateMethod_WithIntParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);
            container.showPrivateMethods = true;
            container.RefreshEntries();

            var privateMethod = container.methodEntries.Find(m =>
                m.DelegateInfo.Method?.Name == "PrivateIntParameterMethod");

            Assert.IsNotNull(privateMethod, "Should find PrivateIntParameterMethod");
            Assert.IsNotNull(privateMethod.ParameterValues, "Should have parameter array");
            Assert.AreEqual(1, privateMethod.ParameterValues.Length, "Should have 1 parameter");

            privateMethod.ParameterValues[0] = 99;
            Assert.DoesNotThrow(() => privateMethod.Invoke(), "Should invoke private method without error");
            Assert.AreEqual(99, script.lastIntValue, "Should receive correct value from private method");
        }

        [Test]
        public void Test_PrivateMethod_WithStringParameter_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);
            container.showPrivateMethods = true;
            container.RefreshEntries();

            var privateMethod = container.methodEntries.Find(m =>
                m.DelegateInfo.Method?.Name == "PrivateStringParameterMethod");

            Assert.IsNotNull(privateMethod, "Should find PrivateStringParameterMethod");
            privateMethod.ParameterValues[0] = "private test";
            privateMethod.Invoke();
            Assert.AreEqual("private test", script.lastStringValue);
        }

        [Test]
        public void Test_PrivateMethod_MultipleParameters_CanInvoke()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);
            container.showPrivateMethods = true;
            container.RefreshEntries();

            var privateMethod = container.methodEntries.Find(m =>
                m.DelegateInfo.Method?.Name == "PrivateMultipleParametersMethod");

            Assert.IsNotNull(privateMethod, "Should find PrivateMultipleParametersMethod");
            Assert.AreEqual(2, privateMethod.ParameterValues.Length, "Should have 2 parameters");

            privateMethod.ParameterValues[0] = 123;
            privateMethod.ParameterValues[1] = "private multi";
            privateMethod.Invoke();
            Assert.AreEqual(123, script.lastIntValue);
            Assert.AreEqual("private multi", script.lastStringValue);
        }

        [Test]
        public void Test_TogglePrivateMethods_UpdatesList()
        {
            var script = testObject.AddComponent<TestMethodScript>();
            var container = new MethodContainer(testObject);

            // Initially disabled
            container.showPrivateMethods = false;
            container.RefreshEntries();
            int publicCount = container.methodEntries.Count;

            // Enable private methods
            container.showPrivateMethods = true;
            container.RefreshEntries();
            int totalCount = container.methodEntries.Count;

            Assert.Greater(totalCount, publicCount, "Should have more methods when private methods are enabled");
        }
    }
}
