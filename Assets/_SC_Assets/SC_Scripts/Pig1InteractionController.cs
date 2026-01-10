using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles Pig 1's interaction with zones (collecting, processing, building)
/// Pig 1 Logic: 16 raw → 8 processed (specific pairs) → 8 build (flexible)
/// NOTE: This script requires the player GameObject to have:
/// 1. A Rigidbody component (now used by PlayerController for movement) - will auto-detect triggers, OR
/// 2. A separate child GameObject with a trigger collider and ZoneDetectionHelper script
/// The Rigidbody component (from PlayerController) should handle trigger detection automatically.
/// </summary>
public class Pig1InteractionController : MonoBehaviour
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

    [Header("Auto-Assignment Helpers")]
    [Tooltip("Parent GameObject containing all raw materials (for auto-assignment). Leave empty to search entire scene.")]
    public GameObject rawMaterialsParent;
    [Tooltip("Parent GameObject containing all processed materials (for auto-assignment). Leave empty to search entire scene.")]
    public GameObject processedMaterialsParent;
    [Tooltip("Parent GameObject containing all build materials (for auto-assignment). Leave empty to search entire scene.")]
    public GameObject buildMaterialsParent;

    // Material tracking - track which materials are visible
    // Arrays to track visibility: true = visible, false = hidden
    private bool[] rawMaterialVisible = new bool[16];
    private bool[] processedMaterialVisible = new bool[8];
    private bool[] buildMaterialVisible = new bool[8];
    
    // Counters for quick checks
    private int visibleRawMaterialCount = 0;
    private int visibleProcessedMaterialCount = 0;
    private int visibleBuildMaterialCount = 0;
    
    // Processing pairs: [processedMaterialIndex] = (rawMaterialIndex1, rawMaterialIndex2)
    // Pattern: Raw 1+5→Processed1, Raw 2+6→Processed2, Raw 3+7→Processed3, Raw 4+8→Processed4,
    //          Raw 9+13→Processed5, Raw 10+14→Processed6, Raw 11+15→Processed7, Raw 12+16→Processed8
    private int[][] processingPairs = new int[8][]
    {
        new int[] {0, 4},   // Processed 0: Raw 1 (0) + Raw 5 (4)
        new int[] {1, 5},   // Processed 1: Raw 2 (1) + Raw 6 (5)
        new int[] {2, 6},   // Processed 2: Raw 3 (2) + Raw 7 (6)
        new int[] {3, 7},   // Processed 3: Raw 4 (3) + Raw 8 (7)
        new int[] {8, 12},  // Processed 4: Raw 9 (8) + Raw 13 (12)
        new int[] {9, 13},  // Processed 5: Raw 10 (9) + Raw 14 (13)
        new int[] {10, 14}, // Processed 6: Raw 11 (10) + Raw 15 (14)
        new int[] {11, 15}  // Processed 7: Raw 12 (11) + Raw 16 (15)
    };

    void Start()
    {
        // Check for Rigidbody (now required for movement and trigger detection)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Pig1InteractionController: No Rigidbody found. " +
                "PlayerController now uses Rigidbody for movement. Please ensure the player has a Rigidbody component. " +
                "For trigger detection, make sure the Rigidbody is set to IsKinematic = false (for physics-based movement) " +
                "or use a child GameObject with a trigger collider for zone detection.");
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

    // Unity's built-in trigger detection (works with Rigidbody-based movement)
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
        // Process 2 raw materials into 1 processed material using specific predefined pairs
        // Find the next available processing pair (check all 8 pairs in order)
        int processedMaterialIndex = FindNextAvailableProcessingPair();
        
        if (processedMaterialIndex != -1)
        {
            // Valid action - process materials using the specific pair
            int rawIndex1 = processingPairs[processedMaterialIndex][0];  // First raw material of pair
            int rawIndex2 = processingPairs[processedMaterialIndex][1];  // Second raw material of pair
            
            // Hide the two raw materials
            rawMaterials[rawIndex1].SetActive(false);
            rawMaterials[rawIndex2].SetActive(false);
            rawMaterialVisible[rawIndex1] = false;
            rawMaterialVisible[rawIndex2] = false;
            visibleRawMaterialCount -= 2;

            // Show the corresponding processed material
            processedMaterials[processedMaterialIndex].SetActive(true);
            processedMaterialVisible[processedMaterialIndex] = true;
            visibleProcessedMaterialCount++;
            
            Debug.Log($"Processed raw materials {rawIndex1 + 1} & {rawIndex2 + 1} into processed material {processedMaterialIndex + 1}. Raw: {visibleRawMaterialCount}, Processed: {visibleProcessedMaterialCount}");
            
            // Play valid animation
            PlayValidActionAnimation();
            
            // Play valid process SFX
            PlaySFX(processValidSFX);
        }
        else
        {
            // Invalid action - no complete pair available or all processed materials already created
            if (visibleRawMaterialCount < 2)
            {
                Debug.Log("Not enough raw materials to process! Need 2 raw materials in a specific pair.");
            }
            else
            {
                Debug.Log("No complete processing pairs available or all processed materials already created!");
            }
            
            // Play invalid animation
            PlayInvalidActionAnimation();
            
            // Play shared invalid SFX
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleBuildingZone()
    {
        // Build: Take any 2 visible processed materials and create next build material sequentially
        // Check if we have at least 2 visible processed materials and haven't created all 8 build materials
        if (visibleProcessedMaterialCount >= 2 && visibleBuildMaterialCount < buildMaterials.Length)
        {
            // Find any two visible processed materials
            int processedIndex1 = -1;
            int processedIndex2 = -1;
            
            // Find first visible processed material
            for (int i = 0; i < processedMaterials.Length; i++)
            {
                if (processedMaterialVisible[i])
                {
                    processedIndex1 = i;
                    break;
                }
            }
            
            // Find second visible processed material
            if (processedIndex1 != -1)
            {
                for (int i = processedIndex1 + 1; i < processedMaterials.Length; i++)
                {
                    if (processedMaterialVisible[i])
                    {
                        processedIndex2 = i;
                        break;
                    }
                }
            }
            
            // If we found two visible processed materials, create build material
            if (processedIndex1 != -1 && processedIndex2 != -1)
            {
                // Hide the two processed materials (any two visible ones)
                processedMaterials[processedIndex1].SetActive(false);
                processedMaterials[processedIndex2].SetActive(false);
                processedMaterialVisible[processedIndex1] = false;
                processedMaterialVisible[processedIndex2] = false;
                visibleProcessedMaterialCount -= 2;

                // Show the next build material sequentially (Build 1, then 2, then 3... up to 8)
                int nextBuildMaterialIndex = visibleBuildMaterialCount; // This will be 0, 1, 2... 7
                buildMaterials[nextBuildMaterialIndex].SetActive(true);
                buildMaterialVisible[nextBuildMaterialIndex] = true;
                visibleBuildMaterialCount++;
                
                Debug.Log($"Built processed materials {processedIndex1 + 1} & {processedIndex2 + 1} into build material {nextBuildMaterialIndex + 1}. Processed: {visibleProcessedMaterialCount}, Build: {visibleBuildMaterialCount}");

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
                // Shouldn't happen, but safety check
                Debug.Log("Could not find two visible processed materials!");
                PlayInvalidActionAnimation();
                PlaySFX(actionInvalidSFX);
            }
        }
        else
        {
            // Invalid action - not enough processed materials or all build materials already created
            if (visibleProcessedMaterialCount < 2)
            {
                Debug.Log("Not enough processed materials to build! Need at least 2 visible processed materials.");
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
    /// Finds the next available processing pair using predefined pairs
    /// Returns the processed material index (0-7) if a complete pair is found, -1 otherwise
    /// Uses specific pairs: Raw 1+5→Processed1, Raw 2+6→Processed2, Raw 3+7→Processed3, Raw 4+8→Processed4,
    ///                      Raw 9+13→Processed5, Raw 10+14→Processed6, Raw 11+15→Processed7, Raw 12+16→Processed8
    /// </summary>
    private int FindNextAvailableProcessingPair()
    {
        // Check each processing pair in order
        for (int processedIndex = 0; processedIndex < processingPairs.Length; processedIndex++)
        {
            // Check if this processed material is already created
            if (processedMaterialVisible[processedIndex])
            {
                continue; // Skip to next pair
            }
            
            // Get the raw material indices for this processed material
            int rawIndex1 = processingPairs[processedIndex][0];
            int rawIndex2 = processingPairs[processedIndex][1];
            
            // Check if both raw materials in this specific pair are visible and exist
            if (rawIndex1 < rawMaterials.Length && rawIndex2 < rawMaterials.Length &&
                rawMaterialVisible[rawIndex1] && rawMaterialVisible[rawIndex2] &&
                rawMaterials[rawIndex1] != null && rawMaterials[rawIndex2] != null)
            {
                return processedIndex; // Found a valid complete pair
            }
        }
        return -1; // No complete pair found
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
    
    // Auto-assignment helper method
    /// <summary>
    /// Public method for auto-assignment. Can be called from custom editor button or context menu.
    /// Automatically assigns materials from parent GameObjects or searches by naming patterns.
    /// Flexible search: Tries exact name patterns first, then falls back to finding all direct children.
    /// </summary>
    [ContextMenu("Auto Assign Materials")]
    public void AutoAssignMaterials()
    {
        int rawCount = 0, processedCount = 0, buildCount = 0;
        
        // Auto-assign raw materials
        if (rawMaterialsParent != null)
        {
            List<GameObject> foundRawMaterials = new List<GameObject>();
            
            // Try finding by name pattern first (more specific)
            FindMaterialsInChildren(rawMaterialsParent.transform, "Raw", foundRawMaterials);
            
            // If no materials found by pattern, get ALL direct children
            if (foundRawMaterials.Count == 0)
            {
                foreach (Transform child in rawMaterialsParent.transform)
                {
                    if (child.gameObject.activeInHierarchy || !child.gameObject.activeInHierarchy) // Include all, even inactive
                    {
                        foundRawMaterials.Add(child.gameObject);
                    }
                }
                Debug.Log($"Pig1InteractionController: No materials found with 'Raw' in name. Using all children of {rawMaterialsParent.name}.");
            }
            
            // Sort by name to ensure consistent order
            foundRawMaterials.Sort((a, b) => string.Compare(a.name, b.name));
            
            // Assign to array (up to 16)
            rawCount = Mathf.Min(foundRawMaterials.Count, rawMaterials.Length);
            for (int i = 0; i < rawCount; i++)
            {
                rawMaterials[i] = foundRawMaterials[i];
            }
            
            Debug.Log($"Pig1InteractionController: Auto-assigned {rawCount}/{rawMaterials.Length} raw materials from '{rawMaterialsParent.name}'. Found {foundRawMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig1InteractionController: Raw Materials Parent not assigned! Cannot auto-assign raw materials.");
        }
        
        // Auto-assign processed materials
        if (processedMaterialsParent != null)
        {
            List<GameObject> foundProcessedMaterials = new List<GameObject>();
            
            // Try finding by name pattern first
            FindMaterialsInChildren(processedMaterialsParent.transform, "Processed", foundProcessedMaterials);
            
            // If no materials found by pattern, get ALL direct children
            if (foundProcessedMaterials.Count == 0)
            {
                foreach (Transform child in processedMaterialsParent.transform)
                {
                    foundProcessedMaterials.Add(child.gameObject);
                }
                Debug.Log($"Pig1InteractionController: No materials found with 'Processed' in name. Using all children of {processedMaterialsParent.name}.");
            }
            
            // Sort by name
            foundProcessedMaterials.Sort((a, b) => string.Compare(a.name, b.name));
            
            // Assign to array (up to 8)
            processedCount = Mathf.Min(foundProcessedMaterials.Count, processedMaterials.Length);
            for (int i = 0; i < processedCount; i++)
            {
                processedMaterials[i] = foundProcessedMaterials[i];
            }
            
            Debug.Log($"Pig1InteractionController: Auto-assigned {processedCount}/{processedMaterials.Length} processed materials from '{processedMaterialsParent.name}'. Found {foundProcessedMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig1InteractionController: Processed Materials Parent not assigned! Cannot auto-assign processed materials.");
        }
        
        // Auto-assign build materials
        if (buildMaterialsParent != null)
        {
            List<GameObject> foundBuildMaterials = new List<GameObject>();
            
            // Try finding by name pattern first
            FindMaterialsInChildren(buildMaterialsParent.transform, "Build", foundBuildMaterials);
            
            // If no materials found by pattern, get ALL direct children
            if (foundBuildMaterials.Count == 0)
            {
                foreach (Transform child in buildMaterialsParent.transform)
                {
                    foundBuildMaterials.Add(child.gameObject);
                }
                Debug.Log($"Pig1InteractionController: No materials found with 'Build' in name. Using all children of {buildMaterialsParent.name}.");
            }
            
            // Sort by name
            foundBuildMaterials.Sort((a, b) => string.Compare(a.name, b.name));
            
            // Assign to array (up to 8)
            buildCount = Mathf.Min(foundBuildMaterials.Count, buildMaterials.Length);
            for (int i = 0; i < buildCount; i++)
            {
                buildMaterials[i] = foundBuildMaterials[i];
            }
            
            Debug.Log($"Pig1InteractionController: Auto-assigned {buildCount}/{buildMaterials.Length} build materials from '{buildMaterialsParent.name}'. Found {foundBuildMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig1InteractionController: Build Materials Parent not assigned! Cannot auto-assign build materials.");
        }
        
        if (rawCount > 0 || processedCount > 0 || buildCount > 0)
        {
            Debug.Log($"Pig1InteractionController: Auto-assignment complete! Raw: {rawCount}, Processed: {processedCount}, Build: {buildCount}");
        }
    }
    
    /// <summary>
    /// Recursively finds GameObjects with names containing the search term (case-insensitive) in children
    /// </summary>
    void FindMaterialsInChildren(Transform parent, string searchTerm, List<GameObject> results)
    {
        if (parent == null) return;
        
        foreach (Transform child in parent)
        {
            // Case-insensitive search
            if (child.name.Contains(searchTerm) || child.name.ToLower().Contains(searchTerm.ToLower()))
            {
                results.Add(child.gameObject);
            }
            // Recursively search children
            FindMaterialsInChildren(child, searchTerm, results);
        }
    }
}
