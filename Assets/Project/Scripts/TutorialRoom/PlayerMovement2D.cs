using System.Collections;
using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    public float speed = 3f;
    public float stopDistance = 0.05f;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Animator animator;

    private System.Action onReachTarget;

    void Start()
    {
        targetPosition = transform.position;
        animator = GetComponent<Animator>();
    }

    public void MoveLeft()
    {
        transform.localScale = new Vector3(-1, 1, 1);
        MoveToPosition(new Vector3(transform.position.x - 2f, transform.position.y, transform.position.z), null);
    }

    public void MoveRight()
    {
        transform.localScale = new Vector3(1, 1, 1);
        MoveToPosition(new Vector3(transform.position.x + 2f, transform.position.y, transform.position.z), null);
    }

    public void MoveToPosition(Vector3 destination, System.Action callback)
    {
        targetPosition = new Vector3(destination.x, transform.position.y, transform.position.z);
        onReachTarget = callback;
        isMoving = true;

        if (animator != null)
            animator.SetBool("isWalking", true);
    }

    void Update()
    {
        if (!isMoving) 
        {
            CheckBoundaries();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Mathf.Abs(transform.position.x - targetPosition.x) < stopDistance)
        {
            transform.position = new Vector3(
                targetPosition.x,
                transform.position.y,
                transform.position.z
            );

            isMoving = false;

            if (animator != null)
                animator.SetBool("isWalking", false);

            if (onReachTarget != null)
            {
                System.Action callback = onReachTarget;
                onReachTarget = null;
                callback.Invoke();
            }
        }

        CheckBoundaries();
    }

    void CheckBoundaries()
    {
        // LIMITE GAUCHE
        if (transform.position.x < -25f)
        {
            transform.position = new Vector2(-25f, transform.position.y);
            if (targetPosition.x < -25f) 
            {
                StopMovementAtLimit(-25f);
            }
        }

        // LIMITE DROITE
        if (transform.position.x > 12f)
        {
            transform.position = new Vector2(12f, transform.position.y);
            if (targetPosition.x > 12f) 
            {
                StopMovementAtLimit(12f);
            }
        }
    }

    // CETTE FONCTION ÉTAIT MANQUANTE :
    void StopMovementAtLimit(float limitX)
    {
        targetPosition.x = limitX;
        isMoving = false;
        if (animator != null) 
            animator.SetBool("isWalking", false);
    }

    public void PlayInteractAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Interact");
    }
}