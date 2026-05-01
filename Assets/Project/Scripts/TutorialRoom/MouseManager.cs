using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class MouseManager : MonoBehaviour
{
    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }
    void Update()
    {
        Vector3 inputPosition;

        // Check enhanced touch first (touchscreen)
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (activeTouches.Count > 0)
        {
            // find a touch that began this frame
            foreach (var t in activeTouches)
            {
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector2 screenPos = t.screenPosition;
                    if (IsPointerOverUI(screenPos))
                        return;

                    inputPosition = new Vector3(screenPos.x, screenPos.y, 0f);
                    ProcessInputPosition(inputPosition);
                    return;
                }
            }
        }

        // Fallback to mouse via new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (IsPointerOverUI(mousePos))
                return;

            inputPosition = new Vector3(mousePos.x, mousePos.y, 0f);
            ProcessInputPosition(inputPosition);
            return;
        }

        // nothing to do this frame
        return;
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = screenPosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }

    private void ProcessInputPosition(Vector3 inputPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPosition);
        worldPos.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();

            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }
}