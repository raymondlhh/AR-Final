using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages multiple image targets and switches between different pigs based on which image target is currently tracked.
/// Detects which image target (pig1build, pig2build, pig3build) is active and activates/deactivates corresponding pig GameObjects.
/// Uses DefaultObserverEventHandler UnityEvents for tracking detection.
/// </summary>
public class MultiImageTargetManager : MonoBehaviour
{
    [Header("Image Target References")]
    [Tooltip("Image Target for Pig 1 (pig1build) - GameObject with ImageTargetBehaviour")]
    public GameObject imageTargetPig1;
    
    [Tooltip("Image Target for Pig 2 (pig2build) - GameObject with ImageTargetBehaviour")]
    public GameObject imageTargetPig2;
    
    [Tooltip("Image Target for Pig 3 (pig3build) - GameObject with ImageTargetBehaviour")]
    public GameObject imageTargetPig3;

    [Header("Pig GameObjects")]
    [Tooltip("Pig 1 GameObject (player with Pig1InteractionController)")]
    public GameObject pig1;
    
    [Tooltip("Pig 2 GameObject (player with Pig2InteractionController)")]
    public GameObject pig2;
    
    [Tooltip("Pig 3 GameObject (player with Pig3InteractionController - if implemented)")]
    public GameObject pig3;

    [Header("UI References")]
    [Tooltip("Joystick reference (shared by all pigs)")]
    public Joystick joystick;
    
    [Tooltip("Action Button reference (shared by all pigs)")]
    public Button actionButton;

    // Track which pig is currently active
    private GameObject currentActivePig;
    private int currentActivePigIndex = -1; // 0 = Pig1, 1 = Pig2, 2 = Pig3

    // Track last known tracked state to detect changes
    private bool pig1WasTracked = false;
    private bool pig2WasTracked = false;
    private bool pig3WasTracked = false;
    

    void Start()
    {
        // Initially deactivate all pigs
        if (pig1 != null) pig1.SetActive(false);
        if (pig2 != null) pig2.SetActive(false);
        if (pig3 != null) pig3.SetActive(false);

        // Setup joystick and action button references for all pigs
        SetupPigReferences();
        
        // Note: Image target tracking should be set up in Inspector:
        // On each ImageTarget's DefaultObserverEventHandler component, connect:
        // - OnTargetFound → MultiImageTargetManager.OnPigXTargetFound() (X = 1, 2, or 3)
        // - OnTargetLost → MultiImageTargetManager.OnPigXTargetLost() (X = 1, 2, or 3)
        // OR the fallback method in Update() will detect active targets
    }
    
    void SetupImageTargetTracking()
    {
        // Note: Image target tracking detection is handled via:
        // 1. Inspector UnityEvents (RECOMMENDED): Connect DefaultObserverEventHandler's OnTargetFound/OnTargetLost
        //    events to the public methods below (OnPig1TargetFound, OnPig1TargetLost, etc.)
        // 2. Fallback method: CheckImageTargetTrackingFallback() runs in Update() to detect active targets
        
        // This method is kept for future enhancements but UnityEvents in Inspector are preferred
    }

    void Update()
    {
        // Check which image target is currently tracked using simpler method
        // (UnityEvents are preferred, but this provides fallback)
        CheckImageTargetTrackingFallback();
    }

    void SetupPigReferences()
    {
        // Setup joystick for all pigs (if they have PlayerController)
        if (joystick != null)
        {
            if (pig1 != null)
            {
                PlayerController pc1 = pig1.GetComponent<PlayerController>();
                if (pc1 != null) pc1.movementJoystick = joystick;
            }
            if (pig2 != null)
            {
                PlayerController pc2 = pig2.GetComponent<PlayerController>();
                if (pc2 != null) pc2.movementJoystick = joystick;
            }
            if (pig3 != null)
            {
                PlayerController pc3 = pig3.GetComponent<PlayerController>();
                if (pc3 != null) pc3.movementJoystick = joystick;
            }
        }

        // Setup action button for all pigs (if they have interaction controllers)
        if (actionButton != null)
        {
            if (pig1 != null)
            {
                Pig1InteractionController ic1 = pig1.GetComponent<Pig1InteractionController>();
                if (ic1 != null) ic1.actionButton = actionButton;
            }
            if (pig2 != null)
            {
                Pig2InteractionController ic2 = pig2.GetComponent<Pig2InteractionController>();
                if (ic2 != null) ic2.actionButton = actionButton;
            }
            // Add Pig3InteractionController when implemented
        }
    }

    /// <summary>
    /// Fallback method to check image target tracking if UnityEvents are not properly set up.
    /// This checks if the image target GameObject or its children are active (Vuforia activates children when tracked).
    /// Note: UnityEvents (SetupObserverEvents) is the preferred method.
    /// </summary>
    void CheckImageTargetTrackingFallback()
    {
        // This is a fallback - prefer using UnityEvents from DefaultObserverEventHandler
        // Check if image target has any active children (Vuforia activates children when tracked)
        bool pig1Tracked = IsImageTargetActive(imageTargetPig1);
        bool pig2Tracked = IsImageTargetActive(imageTargetPig2);
        bool pig3Tracked = IsImageTargetActive(imageTargetPig3);
        
        // Handle Pig 1
        if (pig1Tracked && !pig1WasTracked)
        {
            SwitchActivePig(0);
            pig1WasTracked = true;
        }
        else if (!pig1Tracked && pig1WasTracked && currentActivePigIndex == 0)
        {
            DeactivateCurrentPig();
            pig1WasTracked = false;
        }

        // Handle Pig 2 (only if Pig 1 is not tracked)
        if (!pig1Tracked && pig2Tracked && !pig2WasTracked)
        {
            SwitchActivePig(1);
            pig2WasTracked = true;
        }
        else if (!pig2Tracked && pig2WasTracked && currentActivePigIndex == 1)
        {
            DeactivateCurrentPig();
            pig2WasTracked = false;
        }

        // Handle Pig 3 (only if Pig 1 and 2 are not tracked)
        if (!pig1Tracked && !pig2Tracked && pig3Tracked && !pig3WasTracked)
        {
            SwitchActivePig(2);
            pig3WasTracked = true;
        }
        else if (!pig3Tracked && pig3WasTracked && currentActivePigIndex == 2)
        {
            DeactivateCurrentPig();
            pig3WasTracked = false;
        }
    }

    /// <summary>
    /// Checks if an image target is active by checking if it or its children are active.
    /// Vuforia typically activates child GameObjects when the target is tracked.
    /// </summary>
    bool IsImageTargetActive(GameObject imageTarget)
    {
        if (imageTarget == null) return false;

        // Check if the image target itself is active
        if (!imageTarget.activeInHierarchy) return false;

        // Check if it has active children (Vuforia activates children when tracked)
        // If image target has children and they're active, it's likely being tracked
        if (imageTarget.transform.childCount > 0)
        {
            foreach (Transform child in imageTarget.transform)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    return true; // Has active children, likely tracked
                }
            }
        }

        // Alternative: Check if image target is active in hierarchy
        return imageTarget.activeInHierarchy;
    }
    
    // Public methods for manual setup (can be called from DefaultObserverEventHandler UnityEvents in Inspector)
    public void OnPig1TargetFound()
    {
        SwitchActivePig(0);
    }
    
    public void OnPig1TargetLost()
    {
        if (currentActivePigIndex == 0)
        {
            DeactivateCurrentPig();
        }
    }
    
    public void OnPig2TargetFound()
    {
        SwitchActivePig(1);
    }
    
    public void OnPig2TargetLost()
    {
        if (currentActivePigIndex == 1)
        {
            DeactivateCurrentPig();
        }
    }
    
    public void OnPig3TargetFound()
    {
        SwitchActivePig(2);
    }
    
    public void OnPig3TargetLost()
    {
        if (currentActivePigIndex == 2)
        {
            DeactivateCurrentPig();
        }
    }

    void SwitchActivePig(int pigIndex)
    {
        // Deactivate current pig if any
        if (currentActivePig != null)
        {
            currentActivePig.SetActive(false);
        }

        // Activate new pig based on index
        GameObject newPig = null;
        switch (pigIndex)
        {
            case 0:
                newPig = pig1;
                Debug.Log("MultiImageTargetManager: Switched to Pig 1");
                break;
            case 1:
                newPig = pig2;
                Debug.Log("MultiImageTargetManager: Switched to Pig 2");
                break;
            case 2:
                newPig = pig3;
                Debug.Log("MultiImageTargetManager: Switched to Pig 3");
                break;
        }

        if (newPig != null)
        {
            newPig.SetActive(true);
            currentActivePig = newPig;
            currentActivePigIndex = pigIndex;

            // Ensure joystick and action button are connected
            SetupPigReferences();
        }
        else
        {
            Debug.LogWarning($"MultiImageTargetManager: Pig {pigIndex + 1} GameObject is null!");
            currentActivePig = null;
            currentActivePigIndex = -1;
        }
    }

    void DeactivateCurrentPig()
    {
        if (currentActivePig != null)
        {
            currentActivePig.SetActive(false);
            Debug.Log($"MultiImageTargetManager: Deactivated Pig {currentActivePigIndex + 1} (image target lost)");
        }
        currentActivePig = null;
        currentActivePigIndex = -1;
    }
}
