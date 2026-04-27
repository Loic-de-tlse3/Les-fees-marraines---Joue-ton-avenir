using UnityEngine;

public class QTEHandHintAnimator : MonoBehaviour
{
    [SerializeField] private MobileQTESequence sequence;
    [SerializeField] private RectTransform handRect;
    [SerializeField] private CanvasGroup handCanvasGroup;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Hold Hint")]
    [SerializeField, Min(0.05f)] private float holdRotateDuration = 0.25f;
    [SerializeField, Min(0.05f)] private float holdMoveDuration = 0.45f;
    [SerializeField, Min(0f)] private float holdBottomPauseDuration = 2f;
    [SerializeField, Min(0.05f)] private float holdReturnDuration = 0.35f;
    [SerializeField, Min(0f)] private float holdLoopPauseDuration = 0.25f;
    [SerializeField] private Vector2 holdDownLeftOffset = new Vector2(-40f, -40f);
    [SerializeField, Range(0f, 180f)] private float holdRotateLeftDegrees = 90f;
    [SerializeField, Range(0f, 180f)] private float holdRotateRightDegrees = 90f;

    [Header("Swipe Hint")]
    [SerializeField] private Vector2 swipeStartOffset = new Vector2(0f, -90f);
    [SerializeField] private Vector2 swipeEndOffset = new Vector2(0f, 110f);
    [SerializeField, Min(0.1f)] private float swipeDuration = 0.65f;
    [SerializeField, Min(0f)] private float swipePauseDuration = 0.15f;
    [SerializeField, Range(0f, 1f)] private float swipeStartAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float swipeEndAlpha = 1f;

    private enum HintMode
    {
        None,
        Hold,
        Swipe
    }

    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private HintMode currentMode = HintMode.None;
    private float modeTime;
    private bool swipePause;
    private float swipePauseTimer;

    private void Awake()
    {
        if (handRect == null)
        {
            handRect = transform as RectTransform;
        }

        if (handCanvasGroup == null && handRect != null)
        {
            handCanvasGroup = handRect.GetComponent<CanvasGroup>();
            if (handCanvasGroup == null)
            {
                handCanvasGroup = handRect.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (handRect != null)
        {
            baseAnchoredPosition = handRect.anchoredPosition;
            baseScale = handRect.localScale;
            baseRotation = handRect.localRotation;
        }

        SetVisible(false);
    }

    private void OnDisable()
    {
        ResetHandTransform();
        SetVisible(false);
        currentMode = HintMode.None;
    }

    private void Update()
    {
        if (sequence == null || handRect == null)
        {
            return;
        }

        if (!sequence.HasActiveStep)
        {
            if (currentMode != HintMode.None)
            {
                currentMode = HintMode.None;
                ResetHandTransform();
                SetVisible(false);
            }

            return;
        }

        HintMode desiredMode = GetModeFromStepType(sequence.CurrentStepType);
        if (desiredMode != currentMode)
        {
            EnterMode(desiredMode);
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        modeTime += dt;

        if (currentMode == HintMode.Hold)
        {
            AnimateHold(modeTime);
            return;
        }

        if (currentMode == HintMode.Swipe)
        {
            AnimateSwipe(dt);
        }
    }

    private HintMode GetModeFromStepType(QTEStepDefinition.QTEType stepType)
    {
        if (stepType == QTEStepDefinition.QTEType.Hold)
        {
            return HintMode.Hold;
        }

        if (stepType == QTEStepDefinition.QTEType.Swipe)
        {
            return HintMode.Swipe;
        }

        return HintMode.None;
    }

    private void EnterMode(HintMode mode)
    {
        currentMode = mode;
        modeTime = 0f;
        swipePause = false;
        swipePauseTimer = 0f;

        ResetHandTransform();
        SetVisible(mode != HintMode.None);

        if (mode == HintMode.Swipe)
        {
            handRect.anchoredPosition = baseAnchoredPosition + swipeStartOffset;
            SetAlpha(swipeStartAlpha);
        }
        else if (mode == HintMode.Hold)
        {
            SetAlpha(1f);
        }
    }

    private void AnimateHold(float time)
    {
        float rotateDuration = Mathf.Max(0.001f, holdRotateDuration);
        float moveDuration = Mathf.Max(0.001f, holdMoveDuration);
        float bottomPauseDuration = Mathf.Max(0f, holdBottomPauseDuration);
        float returnDuration = Mathf.Max(0.001f, holdReturnDuration);
        float loopPauseDuration = Mathf.Max(0f, holdLoopPauseDuration);
        float cycleDuration = rotateDuration + moveDuration + bottomPauseDuration + returnDuration + rotateDuration + loopPauseDuration;
        float cycleTime = Mathf.Repeat(time, cycleDuration);

        float leftAngle = holdRotateLeftDegrees;
        float rightAngle = -holdRotateRightDegrees;

        if (cycleTime < rotateDuration)
        {
            float t = cycleTime / rotateDuration;
            float angle = Mathf.Lerp(0f, leftAngle, t);
            handRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
            handRect.anchoredPosition = baseAnchoredPosition;
            handRect.localScale = baseScale;
            return;
        }

        cycleTime -= rotateDuration;

        if (cycleTime < moveDuration)
        {
            float t = cycleTime / moveDuration;
            handRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, leftAngle);
            handRect.anchoredPosition = Vector2.Lerp(baseAnchoredPosition, baseAnchoredPosition + holdDownLeftOffset, t);
            handRect.localScale = baseScale;
            return;
        }

        cycleTime -= moveDuration;

        if (cycleTime < bottomPauseDuration)
        {
            handRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, leftAngle);
            handRect.anchoredPosition = baseAnchoredPosition + holdDownLeftOffset;
            handRect.localScale = baseScale;
            return;
        }

        cycleTime -= bottomPauseDuration;

        if (cycleTime < returnDuration)
        {
            float t = cycleTime / returnDuration;
            handRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, leftAngle);
            handRect.anchoredPosition = Vector2.Lerp(baseAnchoredPosition + holdDownLeftOffset, baseAnchoredPosition, t);
            handRect.localScale = baseScale;
            return;
        }

        cycleTime -= returnDuration;

        if (cycleTime < rotateDuration)
        {
            float rotateRightT = cycleTime / rotateDuration;
            float rotateRightAngle = Mathf.Lerp(leftAngle, rightAngle, rotateRightT);
            handRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotateRightAngle);
            handRect.anchoredPosition = baseAnchoredPosition;
            handRect.localScale = baseScale;
            return;
        }

        handRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rightAngle);
        handRect.anchoredPosition = baseAnchoredPosition;
        handRect.localScale = baseScale;
    }

    private void AnimateSwipe(float dt)
    {
        if (swipePause)
        {
            swipePauseTimer += dt;
            if (swipePauseTimer >= swipePauseDuration)
            {
                swipePause = false;
                swipePauseTimer = 0f;
                modeTime = 0f;
                handRect.anchoredPosition = baseAnchoredPosition + swipeStartOffset;
                SetAlpha(swipeStartAlpha);
            }

            return;
        }

        float t = Mathf.Clamp01(modeTime / Mathf.Max(0.001f, swipeDuration));
        handRect.anchoredPosition = Vector2.Lerp(baseAnchoredPosition + swipeStartOffset, baseAnchoredPosition + swipeEndOffset, t);
        SetAlpha(Mathf.Lerp(swipeStartAlpha, swipeEndAlpha, t));

        if (t >= 1f)
        {
            swipePause = true;
            swipePauseTimer = 0f;
        }
    }

    private void SetVisible(bool visible)
    {
        if (handRect == null)
        {
            return;
        }

        if (handCanvasGroup != null)
        {
            if (visible && handCanvasGroup.alpha <= 0f)
            {
                handCanvasGroup.alpha = 1f;
            }
            else if (!visible)
            {
                handCanvasGroup.alpha = 0f;
            }

            handCanvasGroup.interactable = false;
            handCanvasGroup.blocksRaycasts = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (handCanvasGroup != null)
        {
            handCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private void ResetHandTransform()
    {
        if (handRect == null)
        {
            return;
        }

        handRect.anchoredPosition = baseAnchoredPosition;
        handRect.localScale = baseScale;
        handRect.localRotation = baseRotation;
    }
}
