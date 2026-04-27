using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QTEHudPresenter : MonoBehaviour
{
    [SerializeField] private MobileQTESequence sequence;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text attemptText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider progressSlider;

    private void Update()
    {
        if (sequence == null)
        {
            return;
        }

        if (instructionText != null)
        {
            instructionText.text = sequence.IsRunning ? sequence.CurrentInstruction : "QTE inactif";
        }

        if (attemptText != null)
        {
            attemptText.text = sequence.IsRunning ? "Tentative " + sequence.CurrentAttempt : string.Empty;
        }

        if (timerText != null)
        {
            timerText.text = sequence.IsRunning ? sequence.CurrentTimeRemaining.ToString("0.0") + " s" : string.Empty;
        }

        if (progressSlider != null)
        {
            progressSlider.value = sequence.GetCurrentProgress01();
        }
    }
}
