using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Joystick movementJoystick;
    public float playerSpeed = 2f;
    
    [Header("AR Settings")]
    [Tooltip("Lock Y position to keep pig on image target plane")]
    public bool lockYPosition = true;
    [Tooltip("Y offset from image target plane (0 = on plane, positive = above plane)")]
    public float yOffsetFromGround = 0.1f;
    [Tooltip("Ground detection method")]
    public GroundDetectionMethod groundDetection = GroundDetectionMethod.ImageTargetParent;
    [Tooltip("Maximum raycast distance to find ground (if using raycast)")]
    public float groundRaycastDistance = 5f;
    
    public enum GroundDetectionMethod
    {
        ImageTargetParent,  // Use parent ImageTarget Y position (best for AR)
        Raycast,            // Use raycast to find ground
        FixedYZero          // Use Y=0 (simple fallback)
    }
    
    private Transform imageTargetParent;

    [Header("Animation")]
    public Animator pigAnimator;
    private static readonly int s_Idle = Animator.StringToHash("Idle");
    private static readonly int s_Walk = Animator.StringToHash("Walk");
    private bool isMoving = false;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip walkingSFX;
    private bool isWalkingSFXPlaying = false;

    private Rigidbody rb;
   
    void Start()
    {
        // Find ImageTarget parent (for AR positioning)
        FindImageTargetParent();
        
        // Get or add Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody for AR movement (no gravity, kinematic-like but with physics)
        rb.freezeRotation = true; // Prevent physics rotation (we handle rotation manually)
        rb.useGravity = false; // No gravity in AR
        rb.drag = 10f; // Higher drag to prevent sliding on AR plane
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        
        // Lock Y position if enabled
        if (lockYPosition)
        {
            float groundY = FindGroundLevel();
            
            // Freeze Y position in constraints
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            
            // Set the Y position to ground level + offset
            Vector3 currentPos = transform.position;
            transform.position = new Vector3(currentPos.x, groundY + yOffsetFromGround, currentPos.z);
        }
        
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
        // Get input
        float horizontal = movementJoystick.Horizontal;
        float vertical = movementJoystick.Vertical;

        // Calculate movement direction (only X and Z for AR plane movement)
        Vector3 move = new Vector3(horizontal, 0f, vertical).normalized;
        
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

    void FixedUpdate()
    {
        // Get input
        float horizontal = movementJoystick.Horizontal;
        float vertical = movementJoystick.Vertical;

        // Calculate movement direction (only X and Z, no Y)
        Vector3 move = new Vector3(horizontal, 0f, vertical).normalized;

        // Apply movement using Rigidbody (only X and Z, Y is frozen)
        Vector3 moveVelocity = move * playerSpeed;
        if (lockYPosition)
        {
            // Only set X and Z velocity, Y stays at 0 (frozen)
            rb.velocity = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        }
        else
        {
            rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
        }

        // Ensure Y position stays locked if enabled (safety check)
        if (lockYPosition)
        {
            float groundY = FindGroundLevel();
            float targetY = groundY + yOffsetFromGround;
            Vector3 currentPos = transform.position;
            
            if (Mathf.Abs(currentPos.y - targetY) > 0.01f)
            {
                transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            }
        }
    }

    void FindImageTargetParent()
    {
        // Try to find the ImageTarget parent GameObject
        // In Vuforia, the ImageTarget is usually a parent of game objects placed on it
        Transform current = transform.parent;
        
        while (current != null)
        {
            // Check if this is likely an ImageTarget (has ObserverBehaviour or ImageTargetBehaviour)
            if (current.name.Contains("Image_Target") || 
                current.name.Contains("ImageTarget") ||
                current.GetComponent("ObserverBehaviour") != null ||
                current.GetComponent("ImageTargetBehaviour") != null)
            {
                imageTargetParent = current;
                break;
            }
            current = current.parent;
        }
        
        // If not found, try to find in scene
        if (imageTargetParent == null)
        {
            GameObject imageTarget = GameObject.Find("Image_Target");
            if (imageTarget != null)
            {
                imageTargetParent = imageTarget.transform;
            }
        }
    }

    float FindGroundLevel()
    {
        switch (groundDetection)
        {
            case GroundDetectionMethod.ImageTargetParent:
                if (imageTargetParent != null)
                {
                    // Use the ImageTarget's Y position as ground level
                    return imageTargetParent.position.y;
                }
                // Fallback to Y=0 if parent not found
                return 0f;
                
            case GroundDetectionMethod.Raycast:
                // Cast a ray downward to find the image target plane (ground)
                RaycastHit hit;
                Vector3 rayStart = transform.position + Vector3.up * 2f; // Start raycast from above
                
                // Try casting downward first
                if (Physics.Raycast(rayStart, Vector3.down, out hit, groundRaycastDistance))
                {
                    return hit.point.y;
                }
                
                // If raycast fails, try casting from current position
                if (Physics.Raycast(transform.position, Vector3.down, out hit, groundRaycastDistance))
                {
                    return hit.point.y;
                }
                
                // If still no hit, try upward raycast (in case we're below the plane)
                if (Physics.Raycast(transform.position, Vector3.up, out hit, groundRaycastDistance))
                {
                    return hit.point.y;
                }
                
                // Fallback to Y=0
                return 0f;
                
            case GroundDetectionMethod.FixedYZero:
            default:
                // Simple: Use Y=0 (image target is typically at world Y=0 in AR)
                return 0f;
        }
    }
}
