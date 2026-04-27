using UnityEngine;

public class WorldArrowPositioner : MonoBehaviour
{
    public Camera mainCamera;
    public bool isLeftArrow = true;

    [Range(0f, 1f)] public float viewportX = 0.1f;
    [Range(0f, 1f)] public float viewportY = 0.15f;

    public float zDistance = 10f;

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null) return;

        float x = isLeftArrow ? viewportX : 1f - viewportX;

        Vector3 viewportPos = new Vector3(x, viewportY, zDistance);
        Vector3 worldPos = mainCamera.ViewportToWorldPoint(viewportPos);

        worldPos.z = 0f;
        transform.position = worldPos;
    }
}