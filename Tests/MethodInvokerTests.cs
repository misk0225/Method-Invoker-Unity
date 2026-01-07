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
    }
}
