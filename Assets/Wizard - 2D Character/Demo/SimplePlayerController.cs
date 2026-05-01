using UnityEngine;
using UnityEngine.InputSystem;

namespace ClearSky
{
    public class SimplePlayerController : MonoBehaviour
    {
        public float movePower = 10f;
        public float jumpPower = 15f; //Set Gravity Scale in Rigidbody2D Component to 5

        private Rigidbody2D rb;
        private Animator anim;
        Vector3 movement;
        private int direction = 1;
        bool isJumping = false;
        private bool alive = true;


        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
        }

        private void Update()
        {
            Restart();
            if (alive)
            {
                Hurt();
                Die();
                Attack();
                Jump();
                Run();

            }
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            anim.SetBool("isJump", false);
        }


        void Run()
        {
            Vector3 moveVelocity = Vector3.zero;
            anim.SetBool("isRun", false);

            float h = GetHorizontal();
            if (h < 0)
            {
                direction = -1;
                moveVelocity = Vector3.left;

                transform.localScale = new Vector3(direction, 1, 1);
                if (!anim.GetBool("isJump"))
                    anim.SetBool("isRun", true);

            }
            if (h > 0)
            {
                direction = 1;
                moveVelocity = Vector3.right;

                transform.localScale = new Vector3(direction, 1, 1);
                if (!anim.GetBool("isJump"))
                    anim.SetBool("isRun", true);

            }
            transform.position += moveVelocity * movePower * Time.deltaTime;
        }
        void Jump()
        {
            if ((GetJumpPressed() || GetVertical() > 0) && !anim.GetBool("isJump"))
            {
                isJumping = true;
                anim.SetBool("isJump", true);
            }
            if (!isJumping)
            {
                return;
            }

            rb.linearVelocity = Vector2.zero;

            Vector2 jumpVelocity = new Vector2(0, jumpPower);
            rb.AddForce(jumpVelocity, ForceMode2D.Impulse);

            isJumping = false;
        }
        void Attack()
        {
            if (GetAlphaPressed(1))
            {
                anim.SetTrigger("attack");
            }
        }
        void Hurt()
        {
            if (GetAlphaPressed(2))
            {
                anim.SetTrigger("hurt");
                if (direction == 1)
                    rb.AddForce(new Vector2(-5f, 1f), ForceMode2D.Impulse);
                else
                    rb.AddForce(new Vector2(5f, 1f), ForceMode2D.Impulse);
            }
        }
        void Die()
        {
            if (GetAlphaPressed(3))
            {
                anim.SetTrigger("die");
                alive = false;
            }
        }
        void Restart()
        {
            if (GetAlphaPressed(0))
            {
                anim.SetTrigger("idle");
                alive = true;
            }
        }

        // Helper: prefer new Input System, fallback to legacy Input
        private float GetHorizontal()
        {
            // Keyboard
            if (Keyboard.current != null)
            {
                float left = (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) ? -1f : 0f;
                float right = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) ? 1f : 0f;
                float val = left + right;
                if (val != 0f) return val;
            }

            // Gamepad
            if (Gamepad.current != null)
            {
                float x = Gamepad.current.leftStick.x.ReadValue();
                if (Mathf.Abs(x) > 0.1f) return x;
            }

            // Fallback to legacy
            return Input.GetAxisRaw("Horizontal");
        }

        private float GetVertical()
        {
            if (Keyboard.current != null)
            {
                float down = (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) ? -1f : 0f;
                float up = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) ? 1f : 0f;
                float val = up + down;
                if (val != 0f) return val;
            }

            if (Gamepad.current != null)
            {
                float y = Gamepad.current.leftStick.y.ReadValue();
                if (Mathf.Abs(y) > 0.1f) return y;
            }

            return Input.GetAxisRaw("Vertical");
        }

        private bool GetJumpPressed()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
            return Input.GetButtonDown("Jump");
        }

        private bool GetAlphaPressed(int n)
        {
            // 0-9 keys on keyboard
            if (Keyboard.current != null)
            {
                switch (n)
                {
                    case 0: if (Keyboard.current.digit0Key.wasPressedThisFrame) return true; break;
                    case 1: if (Keyboard.current.digit1Key.wasPressedThisFrame) return true; break;
                    case 2: if (Keyboard.current.digit2Key.wasPressedThisFrame) return true; break;
                    case 3: if (Keyboard.current.digit3Key.wasPressedThisFrame) return true; break;
                }
            }

            // fallback legacy
            switch (n)
            {
                case 0: return Input.GetKeyDown(KeyCode.Alpha0);
                case 1: return Input.GetKeyDown(KeyCode.Alpha1);
                case 2: return Input.GetKeyDown(KeyCode.Alpha2);
                case 3: return Input.GetKeyDown(KeyCode.Alpha3);
            }

            return false;
        }
    }
}