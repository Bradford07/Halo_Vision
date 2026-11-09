using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using TMPro;

public class SessionControllerTests
{
    [Test]
    public void StartGuidance_ActivatesCameraUI()
    {
        // Create the controller and test UI objects
        GameObject controllerObject = new GameObject("TestController");
        SessionController controller = controllerObject.AddComponent<SessionController>();

        GameObject standby = new GameObject("Standby");
        GameObject preview = new GameObject("CameraPreview");
        GameObject status = new GameObject("CameraStatus");

        GameObject buttonTextObject = new GameObject("ButtonText");
        TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();

        // Connect the objects to SessionController
        controller.standbyPanel = standby;
        controller.cameraPreview = preview;
        controller.cameraStatus = status;
        controller.toggleButtonText = buttonText;

        // Run the method we're testing
        controller.StartGuidance();

        // Verify the expected state
        Assert.IsTrue(controller.IsGuidanceActive());
        Assert.IsFalse(standby.activeSelf);
        Assert.IsTrue(preview.activeSelf);
        Assert.IsTrue(status.activeSelf);
        Assert.AreEqual("Stop Camera", buttonText.text);

        // Clean up test objects
        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(standby);
        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(status);
        Object.DestroyImmediate(buttonTextObject);
    }

    [Test]
    public void StopGuidance_ActivatesStandbyUI()
    {
        GameObject controllerObject = new GameObject("TestController");
        SessionController controller = controllerObject.AddComponent<SessionController>();

        GameObject standby = new GameObject("Standby");
        GameObject preview = new GameObject("CameraPreview");
        GameObject status = new GameObject("CameraStatus");

        GameObject buttonObject = new GameObject("ButtonText");
        TextMeshProUGUI buttonText = buttonObject.AddComponent<TextMeshProUGUI>();

        controller.standbyPanel = standby;
        controller.cameraPreview = preview;
        controller.cameraStatus = status;
        controller.toggleButtonText = buttonText;

        controller.StartGuidance();
        controller.StopGuidance();

        Assert.IsFalse(controller.IsGuidanceActive());
        Assert.IsTrue(standby.activeSelf);
        Assert.IsFalse(preview.activeSelf);
        Assert.IsFalse(status.activeSelf);
        Assert.AreEqual("Start Camera", buttonText.text);

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(standby);
        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(status);
        Object.DestroyImmediate(buttonObject);
    }

    [Test]
    public void ToggleGuidance_SwitchesBetweenStates()
    {
        GameObject controllerObject = new GameObject("TestController");
        SessionController controller = controllerObject.AddComponent<SessionController>();

        GameObject standby = new GameObject("Standby");
        GameObject preview = new GameObject("CameraPreview");
        GameObject status = new GameObject("CameraStatus");

        GameObject buttonObject = new GameObject("ButtonText");
        TextMeshProUGUI buttonText = buttonObject.AddComponent<TextMeshProUGUI>();

        controller.standbyPanel = standby;
        controller.cameraPreview = preview;
        controller.cameraStatus = status;
        controller.toggleButtonText = buttonText;

        controller.StopGuidance();

        // First toggle: Activate
        controller.ToggleGuidance();

        Assert.IsTrue(controller.IsGuidanceActive());
        Assert.IsFalse(standby.activeSelf);
        Assert.IsTrue(preview.activeSelf);
        Assert.IsTrue(status.activeSelf);
        Assert.AreEqual("Stop Camera", buttonText.text);

        // Second toggle: Return to standby
        controller.ToggleGuidance();

        Assert.IsFalse(controller.IsGuidanceActive());
        Assert.IsTrue(standby.activeSelf);
        Assert.IsFalse(preview.activeSelf);
        Assert.IsFalse(status.activeSelf);
        Assert.AreEqual("Start Camera", buttonText.text);

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(standby);
        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(status);
        Object.DestroyImmediate(buttonObject);
    }

}
