// Author: Cal Peters
// Syncronize the headset with the backend for the guide app in order to control
// displays on the headset remotely. Handles the network and requests to backend.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class HeadsetSync : MonoBehaviour
{
    // [SerializeField] allows private variable to be viewable inside inspector for quick access/changes for demo
    // Azure backend address
    [SerializeField]
    private string serverUrl = "";

    // Temporary pairing code entered throught the inspector
    // For the prototype, the headset claims this code at startup
    [SerializeField]
    private string pairingCode = "";

    // Connection states
    private bool isPaired = false;
    private string deviceToken = "";
    private bool isConnected = false;
    private string connectionStatus = "Backend: Not connected";

    // Display backend and pairing status in the headset
    public TextMeshProUGUI connectionStatusText;

    //Control the standby and camera preview panels
    public SessionController sessionController;

    // Public Accessors ----------------------------------------------------------------------------------------------------------
    public string GetConnectionStatus()
    {
        return connectionStatus;
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    // Initializations -----------------------------------------------------------------------------------------------------------
    void Start()
    {
        // Display initial status 
        UpdateStatusDisplay();

        // Begin backend connection and pairing
        StartCoroutine(InitializeConnection());
    }

    private IEnumerator InitializeConnection()
    {
        // Check backend availability through health check
        yield return StartCoroutine(CheckServerHealth());

        //Temporary prototype behaviour:
        // Automatically pair using code entered in the inspector.
        if (isConnected && !string.IsNullOrEmpty(pairingCode))
        { 
            yield return StartCoroutine(SendPairingRequest());
        }
    }
    
    // Backend Connections -----------------------------------------------------------------------------------------------------------
    // Public method to check backend availability
    public void CheckConnection()
    {
        StartCoroutine(CheckServerHealth());
    }

    // Public method to request a pairing token based on the input code
    public void ClaimPairingCode()
    {
        StartCoroutine(SendPairingRequest());
    }

    private IEnumerator SendPairingRequest()
    {
        // Check to see if server URL or pairing code is missing
        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(pairingCode))
        {
            // Display missing inputs
            connectionStatus = "Pairing: Missing URL or code";
            UpdateStatusDisplay();
            yield break;
        }

        // URL and pairing code exist so begin connecting
        connectionStatus = "Pairing: Connecting...";
        UpdateStatusDisplay();

        // Append pairing path request to URL
        string url = serverUrl.TrimEnd('/') + "/api/pairing/claim";

        // Create the JSON request body.
        PairingClaimRequest pairingRequest = new PairingClaimRequest
        {
            code = pairingCode.Trim().ToUpperInvariant()
        };

        // Convert request object inot JSON text
        string json = JsonUtility.ToJson(pairingRequest);

        // Create POST request using Unity's Web request call
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // Encode the JSON as bytes for the HTTP request body
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Tell ASP.NET that the request body is JSON
            request.SetRequestHeader("Content-Type", "application/json");

            // Time to wait for reply until timing out
            request.timeout = 30;

            // Send request and wait for response
            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                // Deserialize Azure's JSON response
                PairingClaimResponse response = JsonUtility.FromJson<PairingClaimResponse>(request.downloadHandler.text);

                // A valid token confirms the pairing succeeded
                if(response != null && !string.IsNullOrEmpty(response.token))
                {
                    deviceToken = response.token;
                    isPaired = true;

                    connectionStatus = "Pairing: Successful!";
                    
                    // Start checking Azure for guidance commands
                    StartCoroutine(PollGuidanceState());
                }
                else
                {
                    isPaired = false;
                    connectionStatus = "Pairing: Invalid response";
                }
            }
            else
            {
                connectionStatus = "Pairing: " + request.error;
            }

            UpdateStatusDisplay();
        }
    }

    private IEnumerator CheckServerHealth()
    {
        // Must have a server URL before sending requests
        if(string.IsNullOrWhiteSpace(serverUrl))
        {
            isConnected = false;
            connectionStatus = "Backend: URL not configured";
            UpdateStatusDisplay();
            yield break;
        }

        connectionStatus = "Backend: Connecting...";
        UpdateStatusDisplay();

        // Check health endpoint to confirm Azure is reachable
        string url = serverUrl.TrimEnd('/') + "/health";

        using(UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 20;

            // Wait for the HTTP request without blocking Unity
            yield return request.SendWebRequest();

            // Check request result
            if(request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
                connectionStatus = "Backend: Connected";
            }
            else
            {
                isConnected = false;
                connectionStatus = "Backend: " + request.error;
            }
            UpdateStatusDisplay();
        }
    }

    // Guidance State Synchronization ------------------------------------------------------------------------------------------------------
    private IEnumerator PollGuidanceState()
    {
        // Continue polling while the headset is paired and has a valid device token
        while(isPaired && !string.IsNullOrEmpty(deviceToken))
        {
            string url = serverUrl.TrimEnd('/') + "/api/devices/guidance";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Authenticate using the token received during pairing
                request.SetRequestHeader("Authorization", "Bearer " + deviceToken);

                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GuidanceResponse response = JsonUtility.FromJson<GuidanceResponse>(request.downloadHandler.text);

                    if (response != null && sessionController != null)
                    {
                        // Change panels when teh local state differs from the stored Azure state
                        if (response.active && !sessionController.IsGuidanceActive())
                        {
                            sessionController.StartGuidance();
                        }
                        else if (!response.active && sessionController.IsGuidanceActive())
                        {
                            sessionController.StopGuidance();
                        }
                    }
                }
            }
            // Wait two seconds before checking again
            yield return new WaitForSeconds(2f);
        }
    }


    // Status Display --------------------------------------------------------------------------------------------------------------------
    private void UpdateStatusDisplay()
    {
        if(connectionStatusText != null)
        {
            connectionStatusText.text = connectionStatus;
        }
    }
}

// API Data Classes ----------------------------------------------------------------------------------------------------------------------
// Request body sent when claiming a pairing code
[System.Serializable]
public class PairingClaimRequest
{
    public string code;
}

// Response returned by Azure after a successful pairing
[System.Serializable]
public class PairingClaimResponse
{
    public string token;
    public int deviceId;
    public int profileId;
}

// Current guidance state returned by Azure
[System.Serializable]
public class GuidanceResponse
{
    public bool active;
}