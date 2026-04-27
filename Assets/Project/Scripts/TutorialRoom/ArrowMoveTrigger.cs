using System.Collections;
using UnityEngine;

public class ArrowMoveTrigger : MonoBehaviour
{
    public PlayerMovement2D player;
    public bool moveRight = true;

    private Vector3 initialScale;
    private bool isAnimating = false;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void OnMouseDown()
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