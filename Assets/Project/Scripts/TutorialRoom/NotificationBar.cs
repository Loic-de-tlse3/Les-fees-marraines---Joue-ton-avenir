using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationBar : MonoBehaviour
{
    public GameObject barObject;
    public TMP_Text messageText;
    public Image backgroundImage;
    public float displayTime = 2f;

    private Coroutine currentRoutine;

    void Start()
    {
        if (barObject != null)
            barObject.SetActive(false);
    }

    public void ShowMessage(string message, bool success)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (barObject != null)
            barObject.SetActive(true);

        if (messageText != null)
            messageText.text = message;

        if (backgroundImage != null)
        {
            backgroundImage.color = success
                ? new Color32(46, 204, 113, 255)
                : new Color32(231, 76, 60, 255);
        }

        currentRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);

        if (barObject != null)
            barObject.SetActive(false);

        currentRoutine = null;
    }
}