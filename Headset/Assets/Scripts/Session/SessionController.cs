// Author: Cal Peters
// Provides methods that allow stopping and starting of camera capture (currently just swapping views)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SessionController : MonoBehaviour
{
    public GameObject standbyPanel;
    public GameObject cameraPreview;
    public GameObject cameraStatus;
    public TextMeshProUGUI toggleButtonText;

    private bool guidanceActive = false;

    // Start is called before the first frame update
    void Start()
    {
        // Make sure guidance is not running on startup
        StopGuidance();
    }

    // Checks active guidance state and swaps between them when called
    public void ToggleGuidance()
    {
        if(guidanceActive)
        {
            StopGuidance();
        }
        else
        {
            StartGuidance();
        }
    }

    // Set guidance state to true
    // Hide standby panel while displaying the camera preview panel
    // Swap button text
    public void StartGuidance()
    {
        guidanceActive = true;

        standbyPanel.SetActive(false);
        cameraPreview.SetActive(true);
        cameraStatus.SetActive(true);

        toggleButtonText.text = "Stop Camera";
    }
    // Set guidance state to false
    // Hide camera preview panel while displaying the standby panel
    // Swap button text
    public void StopGuidance()
    {
        guidanceActive = false;

        standbyPanel.SetActive(true);
        cameraPreview.SetActive(false);
        cameraStatus.SetActive(false);

        toggleButtonText.text = "Start Camera";
    }

    // Return private variable
    public bool IsGuidanceActive()
    {
        return guidanceActive;
    }

}
