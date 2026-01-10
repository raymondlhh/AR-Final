using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles player interaction with zones (collecting, processing, building)
/// NOTE: This script requires the player GameObject to have either:
/// 1. A Rigidbody component (set to IsKinematic = true) for trigger detection, OR
/// 2. A separate child GameObject with a trigger collider that represents the player's detection area
/// The CharacterController alone may not reliably trigger OnTriggerEnter events.
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    // Zone detection
    public enum ZoneType
    {
        None,
        Collecting,
        Processing,
        Building
    }

    private ZoneType currentZone = ZoneType.None;
    
    // Reference to CharacterController (optional, for validation)
    private CharacterController characterController;

    // Action button reference
    public Button actionButton;

    [Header("Animation")]
    public Animator pigAnimator;
    // Animation trigger hashes - using existing boar animations
    // Note: You may need to add "ActionValid" and "ActionInvalid" triggers to your Animator Controller
    // For now using Eat for valid and Damaged for invalid (as they exist in BoarAnimations)
    private static readonly int s_Eat = Animator.StringToHash("Eat"); // Use for valid action
    private static readonly int s_Damaged = Animator.StringToHash("Damaged"); // Use for invalid action

    [Header("SFX - Action Sounds")]
    public AudioSource audioSource; // Can use same AudioSource as PlayerController or separate
    public AudioClip collectValidSFX;
    public AudioClip processValidSFX;
    public AudioClip buildValidSFX;
    public AudioClip actionInvalidSFX; // Shared invalid SFX for all invalid actions
    public AudioClip houseCompleteSFX;

    // Material arrays - to be assigned in Inspector
    [Header("Raw Materials (16 total, initially hidden at Processing Zone)")]
    public GameObject[] rawMaterials = new GameObject[16];

    [Header("Processed Materials (8 total, initially hidden at Processing Zone)")]
    public GameObject[] processedMaterials = new GameObject[8];

    [Header("Build Materials (8 total, initially hidden at Building Zone)")]
    public GameObject[] buildMaterials = new GameObject[8];

    [Header("Final Objects")]
    public GameObject house;
    public GameObject baseObject; // Base object to hide when house appears

    // Material tracking - track which materials are visible (for paired processing)
    // Arrays to track visibility: true = visible, false = hidden
    private bool[] rawMaterialVisible = new bool[16];
    private bool[] processedMaterialVisible = new bool[8];
    private bool[] buildMaterialVisible = new bool[8];
    
    // Counters for quick checks
    private int visibleRawMaterialCount = 0;
    private int visibleProcessedMaterialCount = 0;
    private int visibleBuildMaterialCount = 0;

    void Start()
    {
        // Check for CharacterController and warn about trigger detection
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Check if there's a Rigidbody for trigger detection
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("PlayerInteractionController: CharacterController detected but no Rigidbody found. " +
                    "For reliable trigger detection, add a Rigidbody component (set IsKinematic = true) to the player GameObject, " +
                    "or use a child GameObject with a trigger collider for zone detection.");
            }
        }

        // Auto-find Animator if not assigned
        if (pigAnimator == null)
        {
            pigAnimator = GetComponent<Animator>();
        }

        // Auto-find AudioSource if not assigned
        // Note: If PlayerController uses the same AudioSource for walking SFX, 
        // PlayOneShot() will work alongside it without interrupting
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Try to get from PlayerController's AudioSource (can share)
                PlayerController pc = GetComponent<PlayerController>();
                if (pc != null && pc.audioSource != null)
                {
                    audioSource = pc.audioSource;
                }
                else
                {
                    // Create a separate AudioSource for action SFX
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.loop = false; // Action SFX are one-shot
                }
            }
            else
            {
                // If AudioSource exists, make sure it's set to not loop for action SFX
                // (PlayerController will handle looping for walking SFX separately)
                audioSource.loop = false;
            }
        }

        // Initialize all materials as hidden
        HideAllMaterials();

        // Setup action button listener if button is assigned
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonPressed);
        }
    }

    void Update()
    {
        // Check for action button press (handles both UI button and keyboard input)
        if (actionButton != null)
        {
            // UI button is handled via OnActionButtonPressed callback
        }
        else
        {
            // Fallback: Check for keyboard input (for testing)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
            {
                HandleActionPress();
            }
        }
    }

    // Unity's built-in trigger detection (works if GameObject has Rigidbody or is moved by CharacterController)
    void OnTriggerEnter(Collider other)
    {
        OnZoneTriggerEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        OnZoneTriggerExit(other);
    }

    // Public methods that can be called by ZoneDetectionHelper script (for child trigger collider approach)
    public void OnZoneTriggerEnter(Collider other)
    {
        // Detect which zone the player entered
        // The zone detection boxes are named "PlayerDetectZone" and are children of the zone GameObjects
        if (other.gameObject.name == "PlayerDetectZone")
        {
            Transform zoneParent = other.transform.parent;
            if (zoneParent != null)
            {
                string zoneName = zoneParent.gameObject.name;

                if (zoneName == "Collecting_Zone")
                {
                    currentZone = ZoneType.Collecting;
                    Debug.Log("Entered Collecting Zone");
                }
                else if (zoneName == "Processing_Zone")
                {
                    currentZone = ZoneType.Processing;
                    Debug.Log("Entered Processing Zone");
                }
                else if (zoneName == "Building_Zone")
                {
                    currentZone = ZoneType.Building;
                    Debug.Log("Entered Building Zone");
                }
            }
        }
    }

    public void OnZoneTriggerExit(Collider other)
    {
        // Reset zone when leaving
        if (other.gameObject.name == "PlayerDetectZone")
        {
            currentZone = ZoneType.None;
            Debug.Log("Left Zone");
        }
    }

    public void OnActionButtonPressed()
    {
        HandleActionPress();
    }

    private void HandleActionPress()
    {
        if (currentZone == ZoneType.None)
        {
            Debug.Log("Not in any zone. Action button does nothing.");
            return;
        }

        switch (currentZone)
        {
            case ZoneType.Collecting:
                HandleCollectingZone();
                break;
            case ZoneType.Processing:
                HandleProcessingZone();
                break;
            case ZoneType.Building:
                HandleBuildingZone();
                break;
        }
    }

    private void HandleCollectingZone()
    {
        // Collect one raw material (make it visible at Processing Zone)
        // Find the first hidden raw material (starting from index 0)
        int materialIndex = FindFirstHiddenRawMaterial();
        
        if (materialIndex != -1)
        {
            // Valid action - collect material
            rawMaterials[materialIndex].SetActive(true);
            rawMaterialVisible[materialIndex] = true;
            visibleRawMaterialCount++;
            Debug.Log($"Collected raw material {materialIndex + 1}. Total: {visibleRawMaterialCount}/{rawMaterials.Length}");
            
            // Play valid animation
            PlayValidActionAnimation();
            
            // Play valid collect SFX
            PlaySFX(collectValidSFX);
        }
        else
        {
            // Invalid action - already at max
            Debug.Log("All raw materials already collected!");
            
            // Play invalid animation
            PlayInvalidActionAnimation();
            
            // Play shared invalid SFX
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleProcessingZone()
    {
        // Process 2 raw materials into 1 processed material
        // Find the next pair of raw materials (in order: 0&1, 2&3, 4&5, etc.)
        int pairIndex = FindNextRawMaterialPair();
        
        if (pairIndex != -1)
        {
            // Valid action - process materials
            // pairIndex represents which processed material we're creating (0-7)
            int rawIndex1 = pairIndex * 2;      // First raw material of pair
            int rawIndex2 = pairIndex * 2 + 1;  // Second raw material of pair
            
            // Hide the two raw materials
            rawMaterials[rawIndex1].SetActive(false);
            rawMaterials[rawIndex2].SetActive(false);
            rawMaterialVisible[rawIndex1] = false;
            rawMaterialVisible[rawIndex2] = false;
            visibleRawMaterialCount -= 2;

            // Show the corresponding processed material
            processedMaterials[pairIndex].SetActive(true);
            processedMaterialVisible[pairIndex] = true;
            visibleProcessedMaterialCount++;
            
            Debug.Log($"Processed raw materials {rawIndex1 + 1}&{rawIndex2 + 1} into processed material {pairIndex + 1}. Raw: {visibleRawMaterialCount}, Processed: {visibleProcessedMaterialCount}");
            
            // Play valid animation
            PlayValidActionAnimation();
            
            // Play valid process SFX
            PlaySFX(processValidSFX);
        }
        else
        {
            // Invalid action - not enough raw materials or all processed materials already created
            if (visibleRawMaterialCount < 2)
            {
                Debug.Log("Not enough raw materials to process! Need 2 raw materials in a pair.");
            }
            else
            {
                Debug.Log("All processed materials already created!");
            }
            
            // Play invalid animation
            PlayInvalidActionAnimation();
            
            // Play shared invalid SFX
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleBuildingZone()
    {
        // Build: 2 processed materials into 1 build material
        // Find the next pair of processed materials (in order: 0&1, 2&3, 4&5, etc.)
        int pairIndex = FindNextProcessedMaterialPair();
        
        if (pairIndex != -1)
        {
            // Valid action - build material
            // pairIndex represents which build material we're creating (0-7)
            int processedIndex1 = pairIndex * 2;      // First processed material of pair
            int processedIndex2 = pairIndex * 2 + 1;  // Second processed material of pair
            
            // Hide the two processed materials
            processedMaterials[processedIndex1].SetActive(false);
            processedMaterials[processedIndex2].SetActive(false);
            processedMaterialVisible[processedIndex1] = false;
            processedMaterialVisible[processedIndex2] = false;
            visibleProcessedMaterialCount -= 2;

            // Show the corresponding build material
            buildMaterials[pairIndex].SetActive(true);
            buildMaterialVisible[pairIndex] = true;
            visibleBuildMaterialCount++;
            
            Debug.Log($"Built processed materials {processedIndex1 + 1}&{processedIndex2 + 1} into build material {pairIndex + 1}. Processed: {visibleProcessedMaterialCount}, Build: {visibleBuildMaterialCount}");

            // Play valid animation
            PlayValidActionAnimation();
            
            // Play valid build SFX
            PlaySFX(buildValidSFX);

            // Check if all 8 build materials are visible
            if (visibleBuildMaterialCount >= buildMaterials.Length)
            {
                CompleteHouseBuilding();
            }
        }
        else
        {
            // Invalid action - not enough processed materials or all build materials already created
            if (visibleProcessedMaterialCount < 2)
            {
                Debug.Log("Not enough processed materials to build! Need 2 processed materials in a pair.");
            }
            else
            {
                Debug.Log("All build materials already created!");
            }
            
            // Play invalid animation
            PlayInvalidActionAnimation();
            
            // Play shared invalid SFX
            PlaySFX(actionInvalidSFX);
        }
    }

    private void CompleteHouseBuilding()
    {
        Debug.Log("House building complete!");

        // Hide all 8 build materials
        for (int i = 0; i < buildMaterials.Length; i++)
        {
            if (buildMaterials[i] != null)
            {
                buildMaterials[i].SetActive(false);
            }
        }

        // Hide base object
        if (baseObject != null)
        {
            baseObject.SetActive(false);
        }

        // Show house
        if (house != null)
        {
            house.SetActive(true);
        }

        // Play house completion SFX
        PlaySFX(houseCompleteSFX);
    }

    private void HideAllMaterials()
    {
        // Hide all raw materials and reset visibility tracking
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterials[i] != null)
            {
                rawMaterials[i].SetActive(false);
            }
            rawMaterialVisible[i] = false;
        }
        visibleRawMaterialCount = 0;

        // Hide all processed materials and reset visibility tracking
        for (int i = 0; i < processedMaterials.Length; i++)
        {
            if (processedMaterials[i] != null)
            {
                processedMaterials[i].SetActive(false);
            }
            processedMaterialVisible[i] = false;
        }
        visibleProcessedMaterialCount = 0;

        // Hide all build materials and reset visibility tracking
        for (int i = 0; i < buildMaterials.Length; i++)
        {
            if (buildMaterials[i] != null)
            {
                buildMaterials[i].SetActive(false);
            }
            buildMaterialVisible[i] = false;
        }
        visibleBuildMaterialCount = 0;

        // Hide house initially
        if (house != null)
        {
            house.SetActive(false);
        }

        // Base object should be visible initially (will be hidden when house appears)
        // So we don't hide it here
    }

    // Helper methods for finding materials to process
    
    /// <summary>
    /// Finds the first hidden raw material (for collecting)
    /// Returns -1 if all are visible
    /// </summary>
    private int FindFirstHiddenRawMaterial()
    {
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (!rawMaterialVisible[i] && rawMaterials[i] != null)
            {
                return i;
            }
        }
        return -1; // All materials are visible
    }

    /// <summary>
    /// Finds the next pair of visible raw materials to process
    /// Returns the processed material index (0-7) if a pair is found, -1 otherwise
    /// Pairs are: raw 0&1 -> processed 0, raw 2&3 -> processed 1, etc.
    /// </summary>
    private int FindNextRawMaterialPair()
    {
        // Check each possible pair (8 pairs total: 0-1, 2-3, 4-5, 6-7, 8-9, 10-11, 12-13, 14-15)
        for (int pairIndex = 0; pairIndex < processedMaterials.Length; pairIndex++)
        {
            int rawIndex1 = pairIndex * 2;
            int rawIndex2 = pairIndex * 2 + 1;
            
            // Check if this processed material is already created
            if (processedMaterialVisible[pairIndex])
            {
                continue; // Skip to next pair
            }
            
            // Check if both raw materials in this pair are visible
            if (rawIndex1 < rawMaterials.Length && rawIndex2 < rawMaterials.Length &&
                rawMaterialVisible[rawIndex1] && rawMaterialVisible[rawIndex2] &&
                rawMaterials[rawIndex1] != null && rawMaterials[rawIndex2] != null)
            {
                return pairIndex; // Found a valid pair
            }
        }
        return -1; // No valid pair found
    }

    /// <summary>
    /// Finds the next pair of visible processed materials to build
    /// Returns the build material index (0-7) if a pair is found, -1 otherwise
    /// Pairs are: processed 0&1 -> build 0, processed 2&3 -> build 1, etc.
    /// </summary>
    private int FindNextProcessedMaterialPair()
    {
        // Check each possible pair (4 pairs total: 0-1, 2-3, 4-5, 6-7)
        for (int pairIndex = 0; pairIndex < buildMaterials.Length; pairIndex++)
        {
            int processedIndex1 = pairIndex * 2;
            int processedIndex2 = pairIndex * 2 + 1;
            
            // Check if this build material is already created
            if (buildMaterialVisible[pairIndex])
            {
                continue; // Skip to next pair
            }
            
            // Check if both processed materials in this pair are visible
            if (processedIndex1 < processedMaterials.Length && processedIndex2 < processedMaterials.Length &&
                processedMaterialVisible[processedIndex1] && processedMaterialVisible[processedIndex2] &&
                processedMaterials[processedIndex1] != null && processedMaterials[processedIndex2] != null)
            {
                return pairIndex; // Found a valid pair
            }
        }
        return -1; // No valid pair found
    }

    // Animation helper methods
    private void PlayValidActionAnimation()
    {
        if (pigAnimator != null)
        {
            // Using "Eat" animation for valid action (you can change this to a custom "ActionValid" trigger if you add it)
            pigAnimator.SetTrigger(s_Eat);
        }
    }

    private void PlayInvalidActionAnimation()
    {
        if (pigAnimator != null)
        {
            // Using "Damaged" animation for invalid action (you can change this to a custom "ActionInvalid" trigger if you add it)
            pigAnimator.SetTrigger(s_Damaged);
        }
    }

    // SFX helper method
    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            // Play one-shot so it doesn't interrupt walking SFX
            audioSource.PlayOneShot(clip);
        }
    }
}

