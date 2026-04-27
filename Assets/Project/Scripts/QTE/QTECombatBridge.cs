using UnityEngine;
using UnityEngine.Events;
using System;

public class QTECombatBridge : MonoBehaviour
{
    public event Action SequenceSucceeded;
    public event Action SequenceFailed;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = true;

    [Header("Events combat")]
    [SerializeField] private UnityEvent onPhase1Started;
    [SerializeField] private UnityEvent onPhase2Started;
    [SerializeField] private UnityEvent onQTESucceeded;
    [SerializeField] private UnityEvent onQTEFailed;

    public void HandleStepChanged(int stepIndex)
    {
        if (logTransitions)
        {
            Debug.Log("QTE step index: " + stepIndex, this);
        }

        if (stepIndex == 0)
        {
            onPhase1Started?.Invoke();
            return;
        }

        if (stepIndex == 1)
        {
            onPhase2Started?.Invoke();
        }
    }

    public void HandleSequenceSucceeded()
    {
        if (logTransitions)
        {
            Debug.Log("QTE sequence succeeded", this);
        }

        SequenceSucceeded?.Invoke();
        onQTESucceeded?.Invoke();
    }

    public void HandleSequenceFailed()
    {
        if (logTransitions)
        {
            Debug.Log("QTE sequence failed", this);
        }

        SequenceFailed?.Invoke();
        onQTEFailed?.Invoke();
    }
}
