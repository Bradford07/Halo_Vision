using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
public class HeadsetSyncTests
{
    private GameObject testObject;
    private HeadsetSync headsetSync;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestHeadsetSync");
        headsetSync = testObject.AddComponent<HeadsetSync>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }

    [Test]
    public void InitialConnectionStatus_IsNotConnected()
    {
        Assert.IsFalse(headsetSync.IsConnected());
        Assert.AreEqual("Backend: Not connected", headsetSync.GetConnectionStatus());
    }

    // Check that trying to connect without a URL gives error status
    [UnityTest]
    public IEnumerator CheckConnection_WithMissingUrl_ReportsError()
    {
        // Start the connection check without configuring a server URL
        headsetSync.CheckConnection();

        // Allow Unity to execute the coroutine.
        yield return null;

        Assert.IsFalse(headsetSync.IsConnected());
        Assert.AreEqual("Backend: URL not configured", headsetSync.GetConnectionStatus());
    }

    // Check pairing without a code gives error status
    [UnityTest]
    public IEnumerator ClaimPairingCode_WithMissingCode_ReportsError()
    {
        // Attempt pairing without configuring a pairing code.
        headsetSync.ClaimPairingCode();

        // Allow the pairing coroutine to execute.
        yield return null;

        Assert.IsFalse(headsetSync.IsConnected());

        Assert.AreEqual("Pairing: Missing URL or code", headsetSync.GetConnectionStatus());
    }

}
