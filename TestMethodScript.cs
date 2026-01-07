using UnityEngine;

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
}

public enum TestEnum
{
    Value1,
    Value2,
    Value3
}
