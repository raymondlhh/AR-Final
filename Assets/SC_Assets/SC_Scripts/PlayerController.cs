using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Joystick movementJoystick;
    public float playerSpeed = 2f;
    public float gravity = -9.8f;

    [Header("Animation")]
    public Animator pigAnimator;
    private static readonly int s_Idle = Animator.StringToHash("Idle");
    private static readonly int s_Walk = Animator.StringToHash("Walk");
    private bool isMoving = false;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip walkingSFX;
    private bool isWalkingSFXPlaying = false;

    private CharacterController charCon;
    private Vector3 velocity;
   
    void Start()
    {
        charCon = GetComponent<CharacterController>();
        
        // Auto-find Animator if not assigned
        if (pigAnimator == null)
        {
            pigAnimator = GetComponent<Animator>();
        }

        // Auto-find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Setup walking SFX to loop
        if (audioSource != null && walkingSFX != null)
        {
            audioSource.loop = true;
        }

        // Start with idle animation
        if (pigAnimator != null)
        {
            pigAnimator.SetTrigger(s_Idle);
        }
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

        // Check if moving
        bool currentlyMoving = move.magnitude > 0.1f;

        // Handle rotation when moving
        if (currentlyMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
             );
        }

        // Handle walking/idle animation
        if (pigAnimator != null)
        {
            if (currentlyMoving && !isMoving)
            {
                // Started moving - play walk animation
                isMoving = true;
                pigAnimator.SetTrigger(s_Walk);
            }
            else if (!currentlyMoving && isMoving)
            {
                // Stopped moving - play idle animation
                isMoving = false;
                pigAnimator.SetTrigger(s_Idle);
            }
        }

        // Handle walking SFX
        if (audioSource != null && walkingSFX != null)
        {
            if (currentlyMoving && !isWalkingSFXPlaying)
            {
                // Start walking SFX
                audioSource.clip = walkingSFX;
                audioSource.Play();
                isWalkingSFXPlaying = true;
            }
            else if (!currentlyMoving && isWalkingSFXPlaying)
            {
                // Stop walking SFX
                audioSource.Stop();
                isWalkingSFXPlaying = false;
            }
        }
    }
}
