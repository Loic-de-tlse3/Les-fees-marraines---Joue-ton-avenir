using System.Collections;
using UnityEngine;

public class QTEAudioSurgeEffect : MonoBehaviour
{
    [Header("Cible")]
    [SerializeField] private Camera targetCamera;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float duration = 0.8f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Shake")]
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.12f;
    [SerializeField, Min(0f)] private float shakeFrequency = 28f;

    [Header("Zoom Pulse")]
    [SerializeField, Min(0f)] private float orthographicSizeDelta = 0.35f;
    [SerializeField, Min(0f)] private float fieldOfViewDelta = 4f;
    [SerializeField, Min(1)] private int pulseCount = 2;

    private Coroutine runningEffect;
    private Vector3 baseLocalPosition;
    private float baseOrthoSize;
    private float baseFov;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        CacheBaseState();
    }

    private void OnDisable()
    {
        RestoreBaseState();
    }

    public void PlayAudioSurge()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("QTEAudioSurgeEffect: aucune caméra assignée.", this);
            return;
        }

        CacheBaseState();

        if (runningEffect != null)
        {
            StopCoroutine(runningEffect);
            RestoreBaseState();
        }

        runningEffect = StartCoroutine(AudioSurgeRoutine());
    }

    private IEnumerator AudioSurgeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float normalized = Mathf.Clamp01(elapsed / duration);
            float fade = 1f - normalized;

            ApplyShake(fade);
            ApplyZoomPulse(normalized, fade);

            yield return null;
        }

        RestoreBaseState();
        runningEffect = null;
    }

    private void ApplyShake(float fade)
    {
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float x = Mathf.PerlinNoise(time * shakeFrequency, 0f) - 0.5f;
        float y = Mathf.PerlinNoise(0f, time * shakeFrequency) - 0.5f;
        Vector3 offset = new Vector3(x, y, 0f) * (shakeAmplitude * 2f * fade);
        targetCamera.transform.localPosition = baseLocalPosition + offset;
    }

    private void ApplyZoomPulse(float normalized, float fade)
    {
        float wave = Mathf.Sin(normalized * pulseCount * Mathf.PI * 2f);
        float pulse = Mathf.Abs(wave) * fade;

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Max(0.01f, baseOrthoSize - orthographicSizeDelta * pulse);
            return;
        }

        targetCamera.fieldOfView = baseFov + fieldOfViewDelta * pulse;
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void CacheBaseState()
    {
        if (targetCamera == null)
        {
            return;
        }

        baseLocalPosition = targetCamera.transform.localPosition;
        baseOrthoSize = targetCamera.orthographicSize;
        baseFov = targetCamera.fieldOfView;
    }

    private void RestoreBaseState()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.transform.localPosition = baseLocalPosition;

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = baseOrthoSize;
        }
        else
        {
            targetCamera.fieldOfView = baseFov;
        }
    }
}
