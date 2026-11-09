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
        StopGuidance();
    }

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

    public void StartGuidance()
    {
        guidanceActive = true;

        standbyPanel.SetActive(false);
        cameraPreview.SetActive(true);
        cameraStatus.SetActive(true);

        toggleButtonText.text = "Stop Camera";
    }

    public void StopGuidance()
    {
        guidanceActive = false;

        standbyPanel.SetActive(true);
        cameraPreview.SetActive(false);
        cameraStatus.SetActive(false);

        toggleButtonText.text = "Start Camera";
    }

    public bool IsGuidanceActive()
    {
        return guidanceActive;
    }

}
