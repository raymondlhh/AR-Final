using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles Pig 3's interaction with zones (collecting, processing, building)
/// Pig 3 Logic: 10 raw → 10 processed (sequential, 10 raw per processed, scale 0.1→1.0) → 64 build (4 visible per press, consumes 1 processed)
/// Features: Sequential processed material scaling (0.1 increments), 4 build materials per press
/// NOTE: This script requires the player GameObject to have a Rigidbody component for trigger detection
/// </summary>
public class Pig3InteractionController : MonoBehaviour
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
    private ZoneType previousZone = ZoneType.None; // Track previous zone for debugging
    public bool IsAutoProcessing { get; private set; }
    public System.Action OnRawCollected;

    // Zone detection - track colliders we're currently inside
    private List<Collider> currentZoneColliders = new List<Collider>(); // Track all zones we're currently in

    // Action button reference
    public Button actionButton;

    [Header("Action Button UI")]
    public Image actionButtonImage;

    [Header("Zone Icons")]
    public Sprite collectIcon;
    public Sprite processIcon;
    public Sprite buildIcon;

    public Sprite defaultIcon;

    [Header("Animation")]
    public Animator pigAnimator;
    private static readonly int s_Eat = Animator.StringToHash("Eat"); // Use for valid action
    private static readonly int s_Damaged = Animator.StringToHash("Damaged"); // Use for invalid action

    [Header("SFX - Action Sounds")]
    public AudioSource audioSource;
    public AudioClip collectValidSFX;
    public AudioClip processValidSFX;
    public AudioClip buildValidSFX;
    public AudioClip actionInvalidSFX; // Shared invalid SFX for all invalid actions
    public AudioClip houseCompleteSFX;

    [Header("Material Arrays - Pig 3")]
    [Header("Raw Materials (10 total, initially hidden at Processing Zone)")]
    public GameObject[] rawMaterials = new GameObject[10];

    [Header("Processed Materials (10 total, initially hidden at Processing Zone)")]
    public GameObject[] processedMaterials = new GameObject[10];

    [Header("Build Materials (64 total, initially hidden at Building Zone)")]
    public GameObject[] buildMaterials = new GameObject[64];
    
    [Header("Auto-Assignment Helpers")]
    [Tooltip("Parent GameObject containing all raw materials (for auto-assignment). Leave empty to search entire scene.")]
    public GameObject rawMaterialsParent;
    [Tooltip("Parent GameObject containing all processed materials (for auto-assignment). Leave empty to search entire scene.")]
    public GameObject processedMaterialsParent;
    [Tooltip("Parent GameObject containing all build materials (for auto-assignment). Leave empty to search entire scene.")]
    public GameObject buildMaterialsParent;

    [Header("Final Objects")]
    public GameObject house;
    public GameObject baseObject; // Base object to hide when house appears

    // Material tracking - track visibility
    private bool[] rawMaterialVisible = new bool[10];
    private bool[] processedMaterialVisible = new bool[10];
    private bool[] buildMaterialVisible = new bool[64];
    
    // Processed material scale tracking: 0 = hidden (scale 0), 1-10 = scale 0.1 to 1.0 (in 0.1 increments)
    // Scale state 1 = 0.1, state 2 = 0.2, ..., state 10 = 1.0 (complete)
    private int[] processedMaterialScaleState = new int[10]; // 0-10 for each processed material
    
    // Store original scales for each processed material (for relative scaling)
    private Vector3[] originalProcessedMaterialScales = new Vector3[10];
    
    // Track which processed material is currently being worked on (for sequential processing)
    private int currentProcessedMaterialIndex = 0; // Index of the processed material currently being scaled
    
    // Counters for quick checks
    private int visibleRawMaterialCount = 0;
    private int visibleProcessedMaterialCount = 0; // How many processed materials are at full scale (state 10)
    private int visibleBuildMaterialCount = 0;

    // Scale constants
    private const float SCALE_STEP = 0.1f; // Each press increases scale by 0.1
    private const float SCALE_MIN = 0.1f;  // Minimum scale (first press)
    private const float SCALE_MAX = 1.0f;  // Maximum scale (complete, 10 presses)
    private const float SCALE_HIDDEN = 0.0f; // Hidden scale

    void Start()
    {
        // Check for Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Pig3InteractionController: No Rigidbody found. Please ensure the player has a Rigidbody component.");
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
                PlayerController pc = GetComponent<PlayerController>();
                if (pc != null && pc.audioSource != null)
                {
                    audioSource = pc.audioSource;
                }
                else
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.loop = false;
                }
            }
            else
            {
                audioSource.loop = false;
            }
        }

        // Store original scales of processed materials (for relative scaling)
        StoreOriginalProcessedMaterialScales();
        
        // Initialize all materials as hidden
        HideAllMaterials();

        // Setup action button listener if button is assigned
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonPressed);
        }
        UpdateActionButtonIcon();
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

    // Public methods that can be called by ZoneDetectionHelper script
    public void OnZoneTriggerEnter(Collider other)
    {
        // Detect which zone the player entered (matching Pig2InteractionController logic)
        if (other.gameObject.name == "PlayerDetectZone")
        {
            // Add to list of current zone colliders (for tracking)
            if (!currentZoneColliders.Contains(other))
            {
                currentZoneColliders.Add(other);
            }
            
            Transform zoneParent = other.transform.parent;
            if (zoneParent != null)
            {
                string zoneName = zoneParent.gameObject.name;

                if (zoneName == "Collecting_Zone")
                {
                    previousZone = currentZone;
                    currentZone = ZoneType.Collecting;
                    Debug.Log($"Pig 3: Entered Collecting Zone (Total zones tracked: {currentZoneColliders.Count})");
                }
                else if (zoneName == "Processing_Zone")
                {
                    previousZone = currentZone;
                    currentZone = ZoneType.Processing;
                    Debug.Log($"Pig 3: Entered Processing Zone (Total zones tracked: {currentZoneColliders.Count})");
                }
                else if (zoneName == "Building_Zone")
                {
                    previousZone = currentZone;
                    currentZone = ZoneType.Building;
                    Debug.Log($"Pig 3: Entered Building Zone (Total zones tracked: {currentZoneColliders.Count})");
                }
            }
            UpdateActionButtonIcon();

        }
    }

    public void OnZoneTriggerExit(Collider other)
    {
        // Reset zone when leaving (matching Pig2InteractionController logic)
        if (other.gameObject.name == "PlayerDetectZone")
        {
            // Remove from tracking list
            currentZoneColliders.Remove(other);
            
            // Check if we're leaving the current zone
            Transform zoneParent = other.transform.parent;
            if (zoneParent != null)
            {
                string zoneName = zoneParent.gameObject.name;
                ZoneType exitedZone = ZoneType.None;
                
                if (zoneName == "Collecting_Zone")
                {
                    exitedZone = ZoneType.Collecting;
                }
                else if (zoneName == "Processing_Zone")
                {
                    exitedZone = ZoneType.Processing;
                }
                else if (zoneName == "Building_Zone")
                {
                    exitedZone = ZoneType.Building;
                }
                
                // If we're leaving the current zone, clear it
                if (exitedZone == currentZone)
                {
                    previousZone = currentZone;
                    currentZone = ZoneType.None;
                    Debug.Log($"Pig 3: Left {exitedZone} Zone (Remaining zones tracked: {currentZoneColliders.Count})");
                    UpdateActionButtonIcon();

                }
            }
        }
    }

    public void OnActionButtonPressed()
    {
        HandleActionPress();
    }

    private void HandleActionPress()
    {
        // Don't validate zone here - trust the trigger events (matching Pig2InteractionController)
        Debug.Log($"Pig 3: Action button pressed. Current zone: {currentZone}, Previous: {previousZone}, Zone colliders tracked: {currentZoneColliders.Count}");
        
        if (currentZone == ZoneType.None)
        {
            Debug.LogWarning("Pig 3: ⚠️ Action pressed but NOT in any zone! Action button does nothing (no SFX).");
            // Don't play invalid SFX when not in zone - just do nothing (matching Pig2InteractionController behavior)
            return;
        }

        Debug.Log($"Pig 3: Processing action in {currentZone} zone...");
        
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

    public void ProcessAllRawFromMiniGame()
    {
        //if (currentZone != ZoneType.Processing)
        //{
        //    Debug.Log("Pig 2: Mini-game completed but NOT in Processing Zone.");
        //    return;
        //}
        StartCoroutine(ProcessAllRawSequence());
    }
    private IEnumerator ProcessAllRawSequence()
    {
        IsAutoProcessing = true;

        Debug.Log("Pig 2: Mini-game SEQUENTIAL processing started");

        while (true)
        {
            int beforeProcessed = visibleProcessedMaterialCount;

            HandleProcessingZone();

            if (visibleProcessedMaterialCount == beforeProcessed)
            {
                break;
            }

            yield return new WaitForSeconds(0.4f);
        }
        IsAutoProcessing = false;

        Debug.Log("Pig 2: Mini-game finished. No raw material left to process.");
    }

    public bool HasWorkToProcess()
    {
        // Can process if:
        // - has raw
        // OR
        // - has incomplete processed material
        if (visibleRawMaterialCount > 0)
            return true;

        for (int i = 0; i < processedMaterialScaleState.Length; i++)
        {
            if (processedMaterialScaleState[i] > 0 &&
                processedMaterialScaleState[i] < 10)
                return true;
        }

        return false;
    }

    private void HandleCollectingZone()
    {
        // Collect one raw material (make it visible)
        // Find the first hidden raw material (starting from index 0)
        int materialIndex = FindFirstHiddenRawMaterial();
        
        if (materialIndex != -1)
        {
            // Valid action - collect material
            rawMaterials[materialIndex].SetActive(true);
            rawMaterialVisible[materialIndex] = true;
            visibleRawMaterialCount++;
            Debug.Log($"Pig 3: ✅ VALID ACTION - Collected raw material {materialIndex + 1}. Total: {visibleRawMaterialCount}/{rawMaterials.Length}");
            
            // Play valid animation
            PlayValidActionAnimation();
            
            // Play valid collect SFX
            PlaySFX(collectValidSFX);
            NotifyRawCollected();

        }
        else
        {
            // Invalid action - already at max
            Debug.Log("Pig 3: ❌ INVALID ACTION - All raw materials already collected!");
            
            // Play invalid animation
            PlayInvalidActionAnimation();
            
            // Play shared invalid SFX
            PlaySFX(actionInvalidSFX);
        }
    }
    private void NotifyRawCollected()
    {
        OnRawCollected?.Invoke();
    }
    private void HandleProcessingZone()
    {
        // Process: Sequential processing - consume 1 raw material per press, scale up current processed material by 0.1
        // Press 1-10 on processed material #0: Scale from 0.1 to 1.0 (consumes 10 raw materials)
        // Press 11-20 on processed material #1: Scale from 0.1 to 1.0 (consumes 10 more raw materials)
        // And so on...
        
        // Check if we have any raw materials to consume
        if (visibleRawMaterialCount <= 0)
        {
            Debug.Log("Pig 3: ❌ INVALID ACTION - No raw materials to process! Collect some first.");
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
            return;
        }
        
        // Find the current processed material to work on (sequential processing)
        // Check if current processed material is complete (scale state 10), if so move to next
        if (currentProcessedMaterialIndex < processedMaterials.Length)
        {
            // If current processed material is complete (state 10), move to next one
            if (processedMaterialScaleState[currentProcessedMaterialIndex] >= 10)
            {
                // Find next incomplete processed material
                int nextIndex = -1;
                for (int i = currentProcessedMaterialIndex + 1; i < processedMaterials.Length; i++)
                {
                    if (processedMaterialScaleState[i] < 10)
                    {
                        nextIndex = i;
                        break;
                    }
                }
                
                if (nextIndex != -1)
                {
                    currentProcessedMaterialIndex = nextIndex;
                }
                else
                {
                    // All processed materials are complete
                    Debug.Log("Pig 3: ❌ INVALID ACTION - All processed materials are complete! Build some first.");
                    PlayInvalidActionAnimation();
                    PlaySFX(actionInvalidSFX);
                    return;
                }
            }
        }
        else
        {
            // Current index is out of bounds, find first incomplete
            currentProcessedMaterialIndex = FindFirstIncompleteProcessedMaterial();
            if (currentProcessedMaterialIndex == -1)
            {
                Debug.Log("Pig 3: ❌ INVALID ACTION - All processed materials are complete!");
                PlayInvalidActionAnimation();
                PlaySFX(actionInvalidSFX);
                return;
            }
        }
        
        // Process the current processed material
        int processedIndex = currentProcessedMaterialIndex;
        
        // Consume 1 raw material
        int rawIndexToConsume = FindFirstVisibleRawMaterial();
        if (rawIndexToConsume == -1)
        {
            Debug.Log("Pig 3: ❌ INVALID ACTION - No raw materials to consume! This shouldn't happen.");
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
            return;
        }
        
        // Hide the raw material
        rawMaterials[rawIndexToConsume].SetActive(false);
        rawMaterialVisible[rawIndexToConsume] = false;
        visibleRawMaterialCount--;
        
        // Increase processed material scale state (1-10, where 10 = complete/scale 1.0)
        processedMaterialScaleState[processedIndex]++;
        
        // Show processed material if it's the first press (was hidden/state 0)
        if (processedMaterialScaleState[processedIndex] == 1)
        {
            processedMaterials[processedIndex].SetActive(true);
            processedMaterialVisible[processedIndex] = true;
        }
        
        // Update scale based on state (state 1 = 0.1, state 2 = 0.2, ..., state 10 = 1.0)
        float scaleRatio = GetScaleRatioForState(processedMaterialScaleState[processedIndex]);
        SetMaterialScale(processedMaterials[processedIndex], processedIndex, scaleRatio);
        
        // Check if processed material is now complete (state 10 = scale 1.0)
        if (processedMaterialScaleState[processedIndex] >= 10)
        {
            visibleProcessedMaterialCount++;
            Debug.Log($"Pig 3: ✅ VALID ACTION - Processed material {processedIndex + 1} COMPLETE (scale 1.0). Consumed raw material {rawIndexToConsume + 1}. Total complete: {visibleProcessedMaterialCount}/10");
        }
        else
        {
            Debug.Log($"Pig 3: ✅ VALID ACTION - Processed material {processedIndex + 1} scaled to {scaleRatio} (state {processedMaterialScaleState[processedIndex]}/10). Consumed raw material {rawIndexToConsume + 1}.");
        }
        
        // Play valid animation and SFX
        PlayValidActionAnimation();
        PlaySFX(processValidSFX);
    }

    private void HandleBuildingZone()
    {
        // Build: Show 4 build materials per press, consume 1 processed material
        // Check if we have at least 1 complete processed material and haven't shown all 64 build materials
        if (visibleProcessedMaterialCount <= 0)
        {
            Debug.Log("Pig 3: ❌ INVALID ACTION - No complete processed materials to build! Process some first.");
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
            return;
        }
        
        // Check if we can show 4 more build materials
        int remainingBuildSlots = buildMaterials.Length - visibleBuildMaterialCount;
        if (remainingBuildSlots < 4)
        {
            Debug.Log($"Pig 3: ❌ INVALID ACTION - Not enough build material slots! Need 4, have {remainingBuildSlots} remaining.");
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
            return;
        }
        
        // Find 1 complete processed material to consume (from the first complete one)
        int processedIndexToConsume = FindFirstCompleteProcessedMaterial();
        if (processedIndexToConsume == -1)
        {
            Debug.Log("Pig 3: ❌ INVALID ACTION - No complete processed materials found!");
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
            return;
        }
        
        // Consume 1 processed material (hide it and reset its state)
        processedMaterials[processedIndexToConsume].SetActive(false);
        processedMaterialVisible[processedIndexToConsume] = false;
        processedMaterialScaleState[processedIndexToConsume] = 0;
        SetMaterialScale(processedMaterials[processedIndexToConsume], processedIndexToConsume, SCALE_HIDDEN);
        visibleProcessedMaterialCount--;
        
        // If this was the current processed material being worked on, reset current index
        if (currentProcessedMaterialIndex == processedIndexToConsume)
        {
            currentProcessedMaterialIndex = FindFirstIncompleteProcessedMaterial();
            if (currentProcessedMaterialIndex == -1)
            {
                currentProcessedMaterialIndex = 0; // Reset to start if all are complete
            }
        }
        
        // Show 4 build materials (sequential order)
        int buildMaterialsShown = 0;
        for (int i = 0; i < buildMaterials.Length && buildMaterialsShown < 4; i++)
        {
            if (!buildMaterialVisible[i] && buildMaterials[i] != null)
            {
                buildMaterials[i].SetActive(true);
                buildMaterialVisible[i] = true;
                visibleBuildMaterialCount++;
                buildMaterialsShown++;
            }
        }
        
        Debug.Log($"Pig 3: ✅ VALID ACTION - Built 4 materials (showed build materials). Consumed processed material {processedIndexToConsume + 1}. Processed remaining: {visibleProcessedMaterialCount}/10, Build: {visibleBuildMaterialCount}/64");
        
        // Check if all 64 build materials are visible
        if (visibleBuildMaterialCount >= buildMaterials.Length)
        {
            CompleteHouseBuilding();
        }
        else
        {
            // Play valid animation and SFX
            PlayValidActionAnimation();
            PlaySFX(buildValidSFX);
        }
    }

    private void CompleteHouseBuilding()
    {
        Debug.Log("Pig 3: House building complete!");

        // Hide all 64 build materials
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

        StartCoroutine(LoadScene());
    }

    private IEnumerator LoadScene()
    {
        yield return new WaitForSeconds(2f); // wait for SFX / animation
        SceneManager.LoadScene("Wolf Come Scene 3");
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

        // Hide all processed materials and reset scale states
        for (int i = 0; i < processedMaterials.Length; i++)
        {
            if (processedMaterials[i] != null)
            {
                processedMaterials[i].SetActive(false);
                processedMaterialScaleState[i] = 0;
                SetMaterialScale(processedMaterials[i], i, SCALE_HIDDEN);
            }
            processedMaterialVisible[i] = false;
            processedMaterialScaleState[i] = 0;
        }
        visibleProcessedMaterialCount = 0;
        currentProcessedMaterialIndex = 0;

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
    }

    // Helper methods for material management
    
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
    /// Finds the first visible raw material (for consuming)
    /// Returns -1 if none are visible
    /// </summary>
    private int FindFirstVisibleRawMaterial()
    {
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterialVisible[i] && rawMaterials[i] != null)
            {
                return i;
            }
        }
        return -1; // No visible raw materials
    }
    
    /// <summary>
    /// Finds the first incomplete processed material (scale state < 10)
    /// Returns -1 if all are complete
    /// </summary>
    private int FindFirstIncompleteProcessedMaterial()
    {
        for (int i = 0; i < processedMaterials.Length; i++)
        {
            if (processedMaterialScaleState[i] < 10 && processedMaterials[i] != null)
            {
                return i;
            }
        }
        return -1; // All processed materials are complete
    }
    
    /// <summary>
    /// Finds the first complete processed material (scale state >= 10)
    /// Returns -1 if none are complete
    /// </summary>
    private int FindFirstCompleteProcessedMaterial()
    {
        for (int i = 0; i < processedMaterials.Length; i++)
        {
            if (processedMaterialScaleState[i] >= 10 && processedMaterialVisible[i] && processedMaterials[i] != null)
            {
                return i;
            }
        }
        return -1; // No complete processed materials
    }
    
    /// <summary>
    /// Stores the original scales of all processed materials at Start (for relative scaling)
    /// </summary>
    private void StoreOriginalProcessedMaterialScales()
    {
        for (int i = 0; i < processedMaterials.Length; i++)
        {
            if (processedMaterials[i] != null)
            {
                originalProcessedMaterialScales[i] = processedMaterials[i].transform.localScale;
                if (originalProcessedMaterialScales[i] == Vector3.zero)
                {
                    originalProcessedMaterialScales[i] = Vector3.one;
                }
            }
            else
            {
                originalProcessedMaterialScales[i] = Vector3.one; // Default fallback
            }
        }
    }
    
    /// <summary>
    /// Gets the scale ratio based on state (1 = 0.1, 2 = 0.2, ..., 10 = 1.0, 0 = hidden/0.0)
    /// </summary>
    private float GetScaleRatioForState(int state)
    {
        if (state <= 0)
        {
            return SCALE_HIDDEN; // 0.0 (hidden)
        }
        else if (state >= 10)
        {
            return SCALE_MAX; // 1.0 (complete)
        }
        else
        {
            return SCALE_MIN + (state - 1) * SCALE_STEP; // 0.1, 0.2, 0.3, ..., 0.9
        }
    }

    /// <summary>
    /// Sets the scale of a processed material GameObject (all axes XYZ) relative to its original size
    /// </summary>
    private void SetMaterialScale(GameObject material, int materialIndex, float scaleRatio)
    {
        if (material == null) 
        {
            Debug.LogWarning($"Pig 3: SetMaterialScale called with null material at index {materialIndex}");
            return;
        }
        
        // Get the original scale for this material
        Vector3 originalScale = originalProcessedMaterialScales[materialIndex];
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
            Debug.LogWarning($"Pig 3: Original scale for material {materialIndex} was zero, using fallback Vector3.one. Material name: {material.name}");
        }
        
        // Apply scale ratio to all axes (uniform scaling)
        Vector3 newScale = originalScale * scaleRatio;
        material.transform.localScale = newScale;
    }

    // Animation helper methods
    private void PlayValidActionAnimation()
    {
        if (pigAnimator != null)
        {
            pigAnimator.SetTrigger(s_Eat);
        }
    }

    private void PlayInvalidActionAnimation()
    {
        if (pigAnimator != null)
        {
            pigAnimator.SetTrigger(s_Damaged);
        }
    }

    // SFX helper method
    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Auto-assignment helper method
    /// <summary>
    /// Public method for auto-assignment. Can be called from custom editor button or context menu.
    /// Automatically assigns materials from parent GameObjects or searches by naming patterns.
    /// </summary>
    [ContextMenu("Auto Assign Materials")]
    public void AutoAssignMaterials()
    {
        int rawCount = 0, processedCount = 0, buildCount = 0;
        
        // Auto-assign raw materials
        if (rawMaterialsParent != null)
        {
            List<GameObject> foundRawMaterials = new List<GameObject>();
            
            // Try finding by name pattern first
            FindMaterialsInChildren(rawMaterialsParent.transform, "Raw", foundRawMaterials);
            
            // If no materials found by pattern, get ALL direct children
            if (foundRawMaterials.Count == 0)
            {
                foreach (Transform child in rawMaterialsParent.transform)
                {
                    foundRawMaterials.Add(child.gameObject);
                }
                Debug.Log($"Pig3InteractionController: No materials found with 'Raw' in name. Using all children of {rawMaterialsParent.name}.");
            }
            
            // Sort by name using natural/numeric sorting
            foundRawMaterials.Sort((a, b) => NaturalCompare(a.name, b.name));
            
            // Assign to array (up to 10)
            rawCount = Mathf.Min(foundRawMaterials.Count, rawMaterials.Length);
            for (int i = 0; i < rawCount; i++)
            {
                rawMaterials[i] = foundRawMaterials[i];
            }
            
            Debug.Log($"Pig3InteractionController: Auto-assigned {rawCount}/{rawMaterials.Length} raw materials from '{rawMaterialsParent.name}'. Found {foundRawMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig3InteractionController: Raw Materials Parent not assigned! Cannot auto-assign raw materials.");
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
                Debug.Log($"Pig3InteractionController: No materials found with 'Processed' in name. Using all children of {processedMaterialsParent.name}.");
            }
            
            // Sort by name using natural/numeric sorting
            foundProcessedMaterials.Sort((a, b) => NaturalCompare(a.name, b.name));
            
            // Assign to array (up to 10)
            processedCount = Mathf.Min(foundProcessedMaterials.Count, processedMaterials.Length);
            for (int i = 0; i < processedCount; i++)
            {
                processedMaterials[i] = foundProcessedMaterials[i];
            }
            
            Debug.Log($"Pig3InteractionController: Auto-assigned {processedCount}/{processedMaterials.Length} processed materials from '{processedMaterialsParent.name}'. Found {foundProcessedMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig3InteractionController: Processed Materials Parent not assigned! Cannot auto-assign processed materials.");
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
                Debug.Log($"Pig3InteractionController: No materials found with 'Build' in name. Using all children of {buildMaterialsParent.name}.");
            }
            
            // Sort by name using natural/numeric sorting
            foundBuildMaterials.Sort((a, b) => NaturalCompare(a.name, b.name));
            
            // Assign to array (up to 64)
            buildCount = Mathf.Min(foundBuildMaterials.Count, buildMaterials.Length);
            for (int i = 0; i < buildCount; i++)
            {
                buildMaterials[i] = foundBuildMaterials[i];
            }
            
            Debug.Log($"Pig3InteractionController: Auto-assigned {buildCount}/{buildMaterials.Length} build materials from '{buildMaterialsParent.name}'. Found {foundBuildMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig3InteractionController: Build Materials Parent not assigned! Cannot auto-assign build materials.");
        }
        
        if (rawCount > 0 || processedCount > 0 || buildCount > 0)
        {
            Debug.Log($"Pig3InteractionController: Auto-assignment complete! Raw: {rawCount}, Processed: {processedCount}, Build: {buildCount}");
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
    
    /// <summary>
    /// Natural string comparison that handles numbers correctly.
    /// "Material (1)" < "Material (2)" < "Material (10)" (not alphabetical: "Material (10)" < "Material (2)")
    /// </summary>
    int NaturalCompare(string a, string b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;
        
        int aIndex = 0, bIndex = 0;
        
        while (aIndex < a.Length && bIndex < b.Length)
        {
            // Check if both characters are digits
            if (char.IsDigit(a[aIndex]) && char.IsDigit(b[bIndex]))
            {
                // Extract the full number from both strings
                int aNumber = 0;
                int bNumber = 0;
                
                // Parse number from a
                while (aIndex < a.Length && char.IsDigit(a[aIndex]))
                {
                    aNumber = aNumber * 10 + (a[aIndex] - '0');
                    aIndex++;
                }
                
                // Parse number from b
                while (bIndex < b.Length && char.IsDigit(b[bIndex]))
                {
                    bNumber = bNumber * 10 + (b[bIndex] - '0');
                    bIndex++;
                }
                
                // Compare numbers numerically
                if (aNumber != bNumber)
                {
                    return aNumber.CompareTo(bNumber);
                }
            }
            else
            {
                // Compare characters alphabetically (case-insensitive)
                int comparison = char.ToLowerInvariant(a[aIndex]).CompareTo(char.ToLowerInvariant(b[bIndex]));
                if (comparison != 0)
                {
                    return comparison;
                }
                aIndex++;
                bIndex++;
            }
        }
        
        // If we've reached the end of one string, the shorter one comes first
        return a.Length.CompareTo(b.Length);
    }

    private void UpdateActionButtonIcon()
    {
        if (actionButtonImage == null)
        {
            Debug.LogWarning("[ActionButton] Image reference is NULL");
            return;
        }

        // ZONE NONE → hide icon completely
        if (currentZone == ZoneType.None)
        {
            actionButtonImage.enabled = false;
            Debug.Log("[ActionButton] No zone → icon hidden");
            return;
        }

        // ZONE ACTIVE → show icon
        actionButtonImage.enabled = true;

        switch (currentZone)
        {
            case ZoneType.Collecting:
                actionButtonImage.sprite = collectIcon;
                Debug.Log("[ActionButton] Collecting icon applied");
                break;

            case ZoneType.Processing:
                actionButtonImage.sprite = processIcon;
                Debug.Log("[ActionButton] Processing icon applied");
                break;

            case ZoneType.Building:
                actionButtonImage.sprite = buildIcon;
                Debug.Log("[ActionButton] Building icon applied");
                break;
        }
    }
}
