using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QTEScreenFlash : MonoBehaviour
{
    [SerializeField] private Image overlayImage;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.7f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.08f;
    [SerializeField, Min(0f)] private float holdDuration = 0.25f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1.2f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (overlayImage != null)
        {
            SetAlpha(0f);
        }
    }

    public void PlayFlash()
    {
        if (overlayImage == null)
        {
            Debug.LogWarning("QTEScreenFlash: overlayImage n'est pas assignée.", this);
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return FadeTo(maxAlpha, fadeInDuration);

        if (holdDuration > 0f)
        {
            yield return Wait(holdDuration);
        }

        yield return FadeTo(0f, fadeOutDuration);
        flashRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = overlayImage.color.a;

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private IEnumerator Wait(float duration)
    {
        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(duration);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void SetAlpha(float alpha)
    {
        Color color = flashColor;
        color.a = Mathf.Clamp01(alpha);
        overlayImage.color = color;
    }
}
