using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Joystick movementJoystick;
    public float playerSpeed = 2f;
    public float gravity = -9.8f;

    private CharacterController charCon;
    private Vector3 velocity;
   
    void Start()
    {
        charCon = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = movementJoystick.Horizontal;
        float vertical = movementJoystick.Vertical;

        Vector3 move = new Vector3(horizontal, 0f, vertical);

        charCon.Move(move * playerSpeed * Time.deltaTime);

        if (charCon.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if(move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
             );
        }
    }
}
