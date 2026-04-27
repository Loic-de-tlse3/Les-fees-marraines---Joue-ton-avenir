using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[Serializable]
public class QTEStepEvent : UnityEvent<int> { }

[Serializable]
public class QTEStepDefinition
{
    public enum QTEType
    {
        Hold,
        Swipe
    }

    [Header("Meta")]
    public string stepName = "Nouveau QTE";
    [TextArea]
    public string instruction = "Réalise le QTE";
    public QTEType type = QTEType.Hold;

    [Header("Temps / Tentatives")]
    [Min(0.2f)] public float timeLimit = 3f;
    [Min(1)] public int maxAttempts = 3;
    [Tooltip("Si activé, une tentative ratée relance toujours le QTE sans état d'échec final.")]
    public bool infiniteRetry = true;

    [Header("Hold")]
    [Min(0.1f)] public float holdDuration = 1.2f;

    [Header("Swipe")]
    [Min(20f)] public float swipeMinDistance = 180f;
    [Min(0.05f)] public float swipeMaxDuration = 0.8f;
    public Vector2 swipeDirection = Vector2.up;
    [Range(0.1f, 1f)] public float directionDotMin = 0.75f;

    [Header("Zone tactile (optionnel)")]
    public RectTransform touchZone;

    [Header("Events")]
    public UnityEvent onStepStarted;
    public UnityEvent onAttemptFailed;
    public UnityEvent onStepSucceeded;
    public UnityEvent onStepFailedFinal;
}

public class MobileQTESequence : MonoBehaviour
{
    [SerializeField] private List<QTEStepDefinition> steps = new List<QTEStepDefinition>();
    [SerializeField] private bool autoStartOnEnable;
    [Min(0f)]
    [SerializeField] private float autoStartDelay = 5f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float retryDelay = 0.35f;

    [Header("Sequence Events")]
    [SerializeField] private QTEStepEvent onStepIndexChanged;
    [SerializeField] private UnityEvent onSequenceSucceeded;
    [SerializeField] private UnityEvent onSequenceFailed;

    public int CurrentStepIndex => currentStepIndex;
    public bool IsRunning => isRunning;
    public bool HasActiveStep => isRunning && HasCurrentStep;
    public QTEStepDefinition.QTEType CurrentStepType => HasCurrentStep ? steps[currentStepIndex].type : QTEStepDefinition.QTEType.Hold;
    public string CurrentInstruction => HasCurrentStep ? steps[currentStepIndex].instruction : string.Empty;
    public float CurrentTimeRemaining => Mathf.Max(0f, currentStepTimeRemaining);
    public int CurrentAttempt => attemptIndex + 1;

    private bool HasCurrentStep => currentStepIndex >= 0 && currentStepIndex < steps.Count;

    private int currentStepIndex = -1;
    private int attemptIndex;
    private float currentStepTimeRemaining;
    private float holdProgress;
    private bool isRunning;

    private int activePointerId = int.MinValue;
    private Vector2 pointerStartPosition;
    private float pointerStartTime;
    private bool pointerStartedInsideZone;
    private bool retryQueued;
    private bool waitForFreshInput;

    private void OnEnable()
    {
        if (autoStartOnEnable)
        {
            StartCoroutine(AutoStartSequence());
        }
    }

    private IEnumerator AutoStartSequence()
    {
        if (autoStartDelay > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(autoStartDelay);
            }
            else
            {
                yield return new WaitForSeconds(autoStartDelay);
            }
        }

        StartSequence();
    }

    private void Update()
    {
        if (!isRunning || !HasCurrentStep || retryQueued)
        {
            return;
        }

        var step = steps[currentStepIndex];
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        currentStepTimeRemaining -= dt;

        ProcessPointerInput(step, dt);

        if (currentStepTimeRemaining <= 0f)
        {
            RegisterAttemptFailure();
        }
    }

    public void StartSequence()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("MobileQTESequence: aucune étape configurée.", this);
            return;
        }

        isRunning = true;
        currentStepIndex = 0;
        attemptIndex = 0;
        BeginCurrentStep();
    }

    public void StopSequence()
    {
        isRunning = false;
        retryQueued = false;
        activePointerId = int.MinValue;
        StopAllCoroutines();
    }

    public void ConfigureSingleStep(int stepIndex, bool startNow)
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("MobileQTESequence: impossible de configurer une étape unique, liste vide.", this);
            return;
        }

        int clampedIndex = Mathf.Clamp(stepIndex, 0, steps.Count - 1);
        QTEStepDefinition selectedStep = steps[clampedIndex];
        steps = new List<QTEStepDefinition> { selectedStep };

        StopSequence();

        if (startNow)
        {
            StartSequence();
        }
    }

    public float GetCurrentProgress01()
    {
        if (!HasCurrentStep)
        {
            return 0f;
        }

        var step = steps[currentStepIndex];
        if (step.type == QTEStepDefinition.QTEType.Hold)
        {
            return Mathf.Clamp01(holdProgress / Mathf.Max(0.001f, step.holdDuration));
        }

        return 1f - Mathf.Clamp01(currentStepTimeRemaining / Mathf.Max(0.001f, step.timeLimit));
    }

    private void BeginCurrentStep()
    {
        if (!HasCurrentStep)
        {
            return;
        }

        var step = steps[currentStepIndex];
        currentStepTimeRemaining = step.timeLimit;
        holdProgress = 0f;
        activePointerId = int.MinValue;
        pointerStartedInsideZone = false;
        waitForFreshInput = true;

        onStepIndexChanged?.Invoke(currentStepIndex);
        step.onStepStarted?.Invoke();
    }

    private void ProcessPointerInput(QTEStepDefinition step, float dt)
    {
        if (waitForFreshInput)
        {
            if (IsAnyPointerCurrentlyPressed())
            {
                return;
            }

            waitForFreshInput = false;
        }

        if (TryGetPointerDown(out int downId, out Vector2 downPosition) && activePointerId == int.MinValue)
        {
            activePointerId = downId;
            pointerStartPosition = downPosition;
            pointerStartTime = useUnscaledTime ? Time.unscaledTime : Time.time;
            pointerStartedInsideZone = IsInsideZone(step, downPosition);
        }

        if (activePointerId == int.MinValue)
        {
            return;
        }

        bool isHeld = TryGetPointerHeld(activePointerId, out Vector2 heldPosition);
        bool isReleased = TryGetPointerReleased(activePointerId, out Vector2 releasedPosition);

        if (step.type == QTEStepDefinition.QTEType.Hold)
        {
            if (isHeld)
            {
                bool validHold = pointerStartedInsideZone && IsInsideZone(step, heldPosition);
                holdProgress = validHold ? holdProgress + dt : 0f;

                if (holdProgress >= step.holdDuration)
                {
                    RegisterStepSuccess();
                    return;
                }
            }

            if (isReleased)
            {
                activePointerId = int.MinValue;
                holdProgress = 0f;
            }

            return;
        }

        if (step.type == QTEStepDefinition.QTEType.Swipe && isReleased)
        {
            float endTime = useUnscaledTime ? Time.unscaledTime : Time.time;
            float swipeDuration = endTime - pointerStartTime;
            Vector2 swipeVector = releasedPosition - pointerStartPosition;
            float swipeDistance = swipeVector.magnitude;

            bool inZone = pointerStartedInsideZone;
            bool distanceOk = swipeDistance >= step.swipeMinDistance;
            bool durationOk = swipeDuration <= step.swipeMaxDuration;

            float dot = 0f;
            if (swipeDistance > 0.001f)
            {
                Vector2 wanted = step.swipeDirection.sqrMagnitude < 0.001f ? Vector2.up : step.swipeDirection.normalized;
                dot = Vector2.Dot(swipeVector.normalized, wanted);
            }

            bool directionOk = dot >= step.directionDotMin;

            activePointerId = int.MinValue;

            if (inZone && distanceOk && durationOk && directionOk)
            {
                RegisterStepSuccess();
            }
        }
    }

    private bool IsInsideZone(QTEStepDefinition step, Vector2 screenPosition)
    {
        if (step.touchZone == null)
        {
            return true;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(step.touchZone, screenPosition, null);
    }

    private void RegisterAttemptFailure()
    {
        if (!HasCurrentStep)
        {
            return;
        }

        var step = steps[currentStepIndex];
        step.onAttemptFailed?.Invoke();

        if (step.infiniteRetry)
        {
            StartCoroutine(RetryCurrentStep());
            return;
        }

        attemptIndex++;
        if (attemptIndex >= step.maxAttempts)
        {
            step.onStepFailedFinal?.Invoke();
            isRunning = false;
            onSequenceFailed?.Invoke();
            return;
        }

        StartCoroutine(RetryCurrentStep());
    }

    private IEnumerator RetryCurrentStep()
    {
        retryQueued = true;
        if (retryDelay > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(retryDelay);
            }
            else
            {
                yield return new WaitForSeconds(retryDelay);
            }
        }

        retryQueued = false;
        BeginCurrentStep();
    }

    private void RegisterStepSuccess()
    {
        if (!HasCurrentStep)
        {
            return;
        }

        var step = steps[currentStepIndex];
        step.onStepSucceeded?.Invoke();

        currentStepIndex++;
        attemptIndex = 0;

        if (currentStepIndex >= steps.Count)
        {
            isRunning = false;
            onSequenceSucceeded?.Invoke();
            return;
        }

        BeginCurrentStep();
    }

    private bool TryGetPointerDown(out int id, out Vector2 position)
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                id = (int)touch.touchId.ReadValue();
                position = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            id = -1;
            position = Mouse.current.position.ReadValue();
            return true;
        }

        id = int.MinValue;
        position = default;
        return false;
    }

    private bool TryGetPointerHeld(int pointerId, out Vector2 position)
    {
        if (pointerId == -1 && Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return Mouse.current.leftButton.isPressed;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                int id = (int)touch.touchId.ReadValue();
                if (id != pointerId)
                {
                    continue;
                }

                position = touch.position.ReadValue();
                return touch.press.isPressed;
            }
        }

        position = default;
        return false;
    }

    private bool TryGetPointerReleased(int pointerId, out Vector2 position)
    {
        if (pointerId == -1 && Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return Mouse.current.leftButton.wasReleasedThisFrame;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                int id = (int)touch.touchId.ReadValue();
                if (id != pointerId)
                {
                    continue;
                }

                position = touch.position.ReadValue();
                return touch.press.wasReleasedThisFrame;
            }
        }

        position = default;
        return false;
    }

    private bool IsAnyPointerCurrentlyPressed()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    return true;
                }
            }
        }

        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }
}
