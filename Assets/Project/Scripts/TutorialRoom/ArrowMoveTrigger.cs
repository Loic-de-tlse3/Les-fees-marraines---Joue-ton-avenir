using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ArrowMoveTrigger : MonoBehaviour, IPointerDownHandler
{
    public PlayerMovement2D player;
    public bool moveRight = true;

    private Vector3 initialScale;
    private bool isAnimating = false;
    private Collider2D cachedCollider;

    void Start()
    {
        initialScale = transform.localScale;
        cachedCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // Fallback: detect touches or mouse presses via new Input System and physics raycast
        // so arrows work even without EventSystem UI setup.
        // Touchs
        #if ENABLE_INPUT_SYSTEM
        try
        {
            var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            foreach (var t in activeTouches)
            {
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector2 screenPos = t.screenPosition;
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                    RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                    if (hit.collider == cachedCollider)
                    {
                        TriggerMove();
                        return;
                    }
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider == cachedCollider)
                {
                    TriggerMove();
                    return;
                }
            }
        }
        catch { }
        #endif
    }

    // Support both EventSystem pointer and legacy OnMouseDown
    public void OnPointerDown(PointerEventData eventData)
    {
        TriggerMove();
    }

    void OnMouseDown()
    {
        TriggerMove();
    }

    private void TriggerMove()
    {
        if (player == null) return;

        if (!isAnimating)
            StartCoroutine(ClickEffect());

        if (moveRight)
            player.MoveRight();
        else
            player.MoveLeft();
    }

    private IEnumerator ClickEffect()
    {
        isAnimating = true;

        transform.localScale = initialScale * 0.85f;
        yield return new WaitForSeconds(0.08f);
        transform.localScale = initialScale;

        isAnimating = false;
    }
}