using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public GameObject moveIcon;
    public GameObject hotspotIcon;
    public GameObject pickupIcon;

    private int currentStep = 0;

    private void Start()
    {
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (moveIcon != null) moveIcon.SetActive(currentStep == 0);
        if (hotspotIcon != null) hotspotIcon.SetActive(currentStep == 1);
        if (pickupIcon != null) pickupIcon.SetActive(currentStep == 2);
    }

    public void ValidateMoveStep()
    {
        if (currentStep == 0)
        {
            currentStep = 1;
            ShowCurrentStep();
            Debug.Log("Déplacement validé");
        }
    }

    public void ValidateHotspotStep()
    {
        if (currentStep == 1)
        {
            currentStep = 2;
            ShowCurrentStep();
            Debug.Log("Hotspot validé");
        }
    }

    public void ValidatePickupStep()
    {
        if (currentStep == 2)
        {
            currentStep = 3;

            if (moveIcon != null) moveIcon.SetActive(false);
            if (hotspotIcon != null) hotspotIcon.SetActive(false);
            if (pickupIcon != null) pickupIcon.SetActive(false);

            Debug.Log("Tutoriel terminé");
        }
    }
}