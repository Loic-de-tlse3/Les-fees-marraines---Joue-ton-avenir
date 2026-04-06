using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;  // ← nouveau namespace

public class CamerController : MonoBehaviour {
    public float speed;
    public float clampLeft;
    public float clampRight;
    private float cameraX;

    void Start () {
        cameraX = transform.position.x;
    }

    void Update () {
        if (Keyboard.current.rightArrowKey.isPressed && transform.position.x < clampRight)
        {
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        }
        if (Keyboard.current.leftArrowKey.isPressed && transform.position.x > clampLeft)
        {
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log(cameraX);
        }
    }
}