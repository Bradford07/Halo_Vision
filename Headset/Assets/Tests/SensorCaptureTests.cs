using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using System.Collections;
using System.Reflection;

public class SensorCaptureTests
{
    private GameObject testObject;
    private SensorCapture sensorCapture;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestSensorCapture");
        sensorCapture = testObject.AddComponent<SensorCapture>();
    }

    private void InvokePrivateMethod(string methodName)
    {
        MethodInfo method = typeof(SensorCapture).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method, $"Method {methodName} was not found.");

        method.Invoke(sensorCapture, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }

    [Test]
    public void Start_WithMissingPreview_DoesNotThrow()
    {
        Assert.IsNull(sensorCapture.cameraPreview);

        Assert.DoesNotThrow(() => InvokePrivateMethod("Start"));
    }

    [Test]
    public void Update_WithMissingStatusText_DoesNotThrow()
    {
        Assert.IsNull(sensorCapture.cameraStatus);

        Assert.DoesNotThrow(() => InvokePrivateMethod("Update"));
    }

    [UnityTest]
    public IEnumerator Start_OutsideUWP_DisplaysUnsupportedStatus()
    {
        // Create the UI objects required by SensorCapture.
        GameObject previewObject = new GameObject("CameraPreview", typeof(RectTransform), typeof(RawImage));

        previewObject.transform.SetParent(testObject.transform);

        sensorCapture.cameraPreview = previewObject.GetComponent<RawImage>();

        GameObject statusObject = new GameObject("CameraStatus",typeof(RectTransform), typeof(TextMeshProUGUI));

        statusObject.transform.SetParent(testObject.transform);

        TextMeshProUGUI statusText = statusObject.GetComponent<TextMeshProUGUI>();

        sensorCapture.cameraStatus = statusText;

        // Invoke initialization and status update.
        InvokePrivateMethod("Start");
        InvokePrivateMethod("Update");

        yield return null;

        Assert.AreEqual("Camera Status: UWP required", statusText.text);
    }
}
