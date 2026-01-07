using UnityEngine;

// Test custom class for complex parameter testing
[System.Serializable]
public class TestCustomClass
{
    public int intField = 0;
    public string stringField = "";
    public float floatField = 0f;

    public TestCustomClass()
    {
    }

    public TestCustomClass(int value)
    {
        intField = value;
    }

    public TestCustomClass(int intValue, string strValue)
    {
        intField = intValue;
        stringField = strValue;
    }
}

// Test nested custom class
[System.Serializable]
public class TestNestedClass
{
    public TestCustomClass nestedObject;
    public int value;

    public TestNestedClass()
    {
    }

    public TestNestedClass(int val)
    {
        value = val;
    }
}

// Test class with array field (for nested recursion testing)
[System.Serializable]
public class TestClassWithArray
{
    public int[] numbers;
    public string name;

    public TestClassWithArray()
    {
    }
}

// Test class with nested class field (for deep recursion testing)
[System.Serializable]
public class TestDeepNestedClass
{
    public TestNestedClass level1;
    public int depth;

    public TestDeepNestedClass()
    {
    }

    public TestDeepNestedClass(TestNestedClass nested)
    {
        level1 = nested;
    }
}

public class TestMethodScript : MonoBehaviour
{
    public bool noParamCalled = false;
    public int lastIntValue = 0;
    public float lastFloatValue = 0f;
    public string lastStringValue = "";
    public bool lastBoolValue = false;
    public Vector3 lastVector3Value = Vector3.zero;
    public Color lastColorValue = Color.white;
    public TestEnum lastEnumValue = TestEnum.Value1;
    public int[] lastIntArray = null;
    public string[] lastStringArray = null;
    public TestCustomClass lastCustomClass = null;
    public TestNestedClass lastNestedClass = null;
    public TestCustomClass[] lastCustomClassArray = null;
    public TestClassWithArray lastClassWithArray = null;
    public TestDeepNestedClass lastDeepNestedClass = null;

    public void NoParameterMethod()
    {
        noParamCalled = true;
    }

    public void IntParameterMethod(int value)
    {
        lastIntValue = value;
    }

    public void FloatParameterMethod(float value)
    {
        lastFloatValue = value;
    }

    public void StringParameterMethod(string value)
    {
        lastStringValue = value;
    }

    public void BoolParameterMethod(bool value)
    {
        lastBoolValue = value;
    }

    public void Vector3ParameterMethod(Vector3 value)
    {
        lastVector3Value = value;
    }

    public void ColorParameterMethod(Color value)
    {
        lastColorValue = value;
    }

    public void EnumParameterMethod(TestEnum value)
    {
        lastEnumValue = value;
    }

    public void MultipleParametersMethod(int intVal, string strVal, float floatVal)
    {
        lastIntValue = intVal;
        lastStringValue = strVal;
        lastFloatValue = floatVal;
    }

    public void TwoParametersMethod(int intVal, string strVal)
    {
        lastIntValue = intVal;
        lastStringValue = strVal;
    }

    public void IntArrayParameterMethod(int[] array)
    {
        lastIntArray = array;
    }

    public void StringArrayParameterMethod(string[] array)
    {
        lastStringArray = array;
    }

    public void CustomClassParameterMethod(TestCustomClass customClass)
    {
        lastCustomClass = customClass;
    }

    public void NestedClassParameterMethod(TestNestedClass nestedClass)
    {
        lastNestedClass = nestedClass;
    }

    public void CustomClassArrayParameterMethod(TestCustomClass[] array)
    {
        lastCustomClassArray = array;
    }

    public void ClassWithArrayParameterMethod(TestClassWithArray classWithArray)
    {
        lastClassWithArray = classWithArray;
    }

    public void DeepNestedClassParameterMethod(TestDeepNestedClass deepNested)
    {
        lastDeepNestedClass = deepNested;
    }
}

public enum TestEnum
{
    Value1,
    Value2,
    Value3
}
