using UnityEngine;

/// <summary>
/// Helper script to be attached to a child GameObject with a trigger collider.
/// This solves the issue where CharacterController doesn't reliably trigger OnTriggerEnter.
/// Place this on a child GameObject of the player with a trigger collider, and it will
/// forward zone detection events to the PlayerInteractionController on the parent.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneDetectionHelper : MonoBehaviour
{
    private PlayerInteractionController interactionController;

    void Start()
    {
        // Get the PlayerInteractionController from parent
        interactionController = GetComponentInParent<PlayerInteractionController>();
        
        if (interactionController == null)
        {
            Debug.LogError("ZoneDetectionHelper: PlayerInteractionController not found on parent GameObject!");
        }

        // Ensure the collider is set as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("ZoneDetectionHelper: Collider should be set as a trigger!");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (interactionController != null)
        {
            // Forward the trigger event to the parent's OnTriggerEnter method
            // We do this by calling the zone detection logic directly
            interactionController.OnZoneTriggerEnter(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (interactionController != null)
        {
            // Forward the trigger exit event
            interactionController.OnZoneTriggerExit(other);
        }
    }
}

