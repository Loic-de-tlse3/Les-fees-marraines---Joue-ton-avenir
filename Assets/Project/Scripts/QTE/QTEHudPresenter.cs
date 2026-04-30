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

    private void Awake()
    {
        EnsureSequenceReference();
    }

    private void OnEnable()
    {
        EnsureSequenceReference();
    }

    private void EnsureSequenceReference()
    {
        if (sequence != null)
        {
            return;
        }

        sequence = GetComponent<MobileQTESequence>();
        if (sequence == null)
        {
            sequence = GetComponentInParent<MobileQTESequence>(true);
        }
    }

    private void Update()
    {
        EnsureSequenceReference();

        if (sequence == null)
        {
            return;
        }

        if (progressSlider != null)
        {
            progressSlider.value = sequence.GetCurrentProgress01();
        }
    }
}
