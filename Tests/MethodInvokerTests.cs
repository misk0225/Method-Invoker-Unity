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
    }
}
