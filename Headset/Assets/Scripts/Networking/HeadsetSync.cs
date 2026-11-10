using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class HeadsetSync : MonoBehaviour
{

    [SerializeField]
    private string serverUrl = "";

    // Viewable inside inspector for quick access/changes for demo
    [SerializeField]
    private string pairingCode = "";

    private bool isPaired = false;
    private string deviceToken = "";

    public TextMeshProUGUI connectionStatusText;

    public SessionController sessionController;

    private bool isConnected = false;
    private string connectionStatus = "Backend: Not connected";

    public string GetConnectionStatus()
    {
        return connectionStatus;
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    void Start()
    {
        UpdateStatusDisplay();
        CheckConnection();

        // Temporary prototype pairing (will change later):
        // Only attempt pairing when a code has been configured
        if(!string.IsNullOrWhiteSpace(pairingCode))
        {
            ClaimPairingCode();
        }

    }

    public void CheckConnection()
    {
        StartCoroutine(CheckServerHealth());
    }

    public void ClaimPairingCode()
    {
        StartCoroutine(SendPairingRequest());
    }

    private IEnumerator SendPairingRequest()
    {
        // Make sure we have a server address and pairing code.
        if (string.IsNullOrWhiteSpace(serverUrl) ||
            string.IsNullOrWhiteSpace(pairingCode))
        {
            connectionStatus = "Pairing: Missing URL or code";
            UpdateStatusDisplay();
            yield break;
        }

        connectionStatus = "Pairing: Connecting...";
        UpdateStatusDisplay();

        string url = serverUrl.TrimEnd('/') + "/api/pairing/claim";

        // Create the JSON request body.
        PairingClaimRequest pairingRequest = new PairingClaimRequest
        {
            code = pairingCode.Trim().ToUpperInvariant()
        };

        string json = JsonUtility.ToJson(pairingRequest);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                PairingClaimResponse response =
                    JsonUtility.FromJson<PairingClaimResponse>(
                        request.downloadHandler.text
                    );

                if (response != null &&
                    !string.IsNullOrEmpty(response.token))
                {
                    deviceToken = response.token;
                    isPaired = true;

                    connectionStatus = "Pairing: Successful!";
                    Debug.Log("Headset paired successfully.");
                    
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
                Debug.LogWarning(connectionStatus);
            }

            UpdateStatusDisplay();
        }

    }

    private IEnumerator CheckServerHealth()
    {
        if(string.IsNullOrWhiteSpace(serverUrl))
        {
            isConnected = false;
            connectionStatus = "Backend: URL not configured";
            UpdateStatusDisplay();
            yield break;
        }

        connectionStatus = "Backend: Connecting...";
        UpdateStatusDisplay();

        string url = serverUrl.TrimEnd('/') + "/health";

        using(UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
                connectionStatus = "Backend: Connected";
                Debug.Log(connectionStatus);
            }
            else
            {
                isConnected = false;
                connectionStatus = "Backend: " + request.error;
                Debug.LogError(connectionStatus);
            }
            UpdateStatusDisplay();
        }
    }

    private IEnumerator PollGuidanceState()
    {
        while (isPaired && !string.IsNullOrEmpty(deviceToken))
        {
            string url = serverUrl.TrimEnd('/') + "/api/devices/guidance";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader(
                    "Authorization",
                    "Bearer " + deviceToken
                );

                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GuidanceResponse response =
                        JsonUtility.FromJson<GuidanceResponse>(
                            request.downloadHandler.text
                        );

                    if (response != null && sessionController != null)
                    {
                        if (response.active &&
                            !sessionController.IsGuidanceActive())
                        {
                            sessionController.StartGuidance();
                        }
                        else if (!response.active &&
                                sessionController.IsGuidanceActive())
                        {
                            sessionController.StopGuidance();
                        }
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "Guidance polling failed: " + request.error
                    );
                }
            }

            yield return new WaitForSeconds(2f);
        }
    }

    private void UpdateStatusDisplay()
    {
        if(connectionStatusText != null)
        {
            connectionStatusText.text = connectionStatus;
        }
    }
}

[System.Serializable]
public class PairingClaimRequest
{
    public string code;
}

[System.Serializable]
public class PairingClaimResponse
{
    public string token;
    public int deviceId;
    public int profileId;
}

[System.Serializable]
public class GuidanceResponse
{
    public bool active;
}