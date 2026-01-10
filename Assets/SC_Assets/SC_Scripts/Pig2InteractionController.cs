using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles Pig 2's interaction with zones (collecting, processing, building)
/// Pig 2 Logic: 6 raw (progressive growth) → 36 processed (1 raw → 3 processed) → 27 build (4 processed → 1 build)
/// Features: Progressive height growth/shrinking, flexible cycles, max 36 processed visible
/// NOTE: This script requires the player GameObject to have a Rigidbody component for trigger detection
/// </summary>
public class Pig2InteractionController : MonoBehaviour
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
    private static readonly int s_Eat = Animator.StringToHash("Eat"); // Use for valid action
    private static readonly int s_Damaged = Animator.StringToHash("Damaged"); // Use for invalid action

    [Header("SFX - Action Sounds")]
    public AudioSource audioSource;
    public AudioClip collectValidSFX;
    public AudioClip processValidSFX;
    public AudioClip buildValidSFX;
    public AudioClip actionInvalidSFX; // Shared invalid SFX for all invalid actions
    public AudioClip houseCompleteSFX;

    [Header("Material Arrays - Pig 2")]
    [Header("Raw Materials (6 total, initially hidden at Processing Zone)")]
    public GameObject[] rawMaterials = new GameObject[6];

    [Header("Processed Materials (36 total, initially hidden at Processing Zone)")]
    public GameObject[] processedMaterials = new GameObject[36];

    [Header("Build Materials (27 total, initially hidden at Building Zone)")]
    public GameObject[] buildMaterials = new GameObject[27];

    [Header("Final Objects")]
    public GameObject house;
    public GameObject baseObject; // Base object to hide when house appears

    [Header("Height Settings")]
    [Tooltip("Height values for progressive growth/shrinking (Y position or scale)")]
    public float heightOneThird = 0.02f;   // 1/3 height
    public float heightTwoThirds = 0.04f;  // 2/3 height
    public float heightFull = 0.06f;       // Full height
    [Tooltip("If true, modify Y scale. If false, modify Y position (recommended: false for position-based)")]
    public bool useScaleForHeight = false;  // Default to position-based (Y position)
    [Tooltip("Original Y scale of raw materials (only used if useScaleForHeight is true)")]
    public float originalRawMaterialScaleY = 1f;  // Original scale, used for ratio calculation

    // Material tracking - track visibility and height state
    private bool[] rawMaterialVisible = new bool[6];
    private bool[] processedMaterialVisible = new bool[36];
    private bool[] buildMaterialVisible = new bool[27];
    
    // Track raw material height state: 0 = hidden, 1 = 1/3, 2 = 2/3, 3 = full
    private int[] rawMaterialHeightState = new int[6]; // 0-3 for each raw material
    
    
    // Counters for quick checks
    private int visibleProcessedMaterialCount = 0; // How many processed materials are currently visible (max 36)
    private int visibleBuildMaterialCount = 0;

    void Start()
    {
        // Check for Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Pig2InteractionController: No Rigidbody found. Please ensure the player has a Rigidbody component.");
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

    // Public methods that can be called by ZoneDetectionHelper script
    public void OnZoneTriggerEnter(Collider other)
    {
        // Detect which zone the player entered
        if (other.gameObject.name == "PlayerDetectZone")
        {
            Transform zoneParent = other.transform.parent;
            if (zoneParent != null)
            {
                string zoneName = zoneParent.gameObject.name;

                if (zoneName == "Collecting_Zone")
                {
                    currentZone = ZoneType.Collecting;
                    Debug.Log("Pig 2: Entered Collecting Zone");
                }
                else if (zoneName == "Processing_Zone")
                {
                    currentZone = ZoneType.Processing;
                    Debug.Log("Pig 2: Entered Processing Zone");
                }
                else if (zoneName == "Building_Zone")
                {
                    currentZone = ZoneType.Building;
                    Debug.Log("Pig 2: Entered Building Zone");
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
            Debug.Log("Pig 2: Left Zone");
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
            Debug.Log("Pig 2: Not in any zone. Action button does nothing.");
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
        // Progressive growth: Each raw material needs 3 presses to reach full height
        // Find the next raw material to grow (in order: 0, 1, 2, 3, 4, 5)
        
        // Find the first raw material that's not at full height
        int rawIndex = -1;
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterialHeightState[i] < 3) // Not at full height yet
            {
                rawIndex = i;
                break;
            }
        }
        
        if (rawIndex != -1)
        {
            // Grow this raw material
            rawMaterialHeightState[rawIndex]++;
            
            // Show material if it's the first press (was hidden)
            if (rawMaterialHeightState[rawIndex] == 1)
            {
                rawMaterials[rawIndex].SetActive(true);
                rawMaterialVisible[rawIndex] = true;
            }
            
            // Update height based on state
            float targetHeight = GetHeightForState(rawMaterialHeightState[rawIndex]);
            SetMaterialHeight(rawMaterials[rawIndex], targetHeight);
            
            Debug.Log($"Pig 2: Raw material {rawIndex + 1} at height state {rawMaterialHeightState[rawIndex]}/3 (height: {targetHeight})");
            
            // Play valid animation and SFX
            PlayValidActionAnimation();
            PlaySFX(collectValidSFX);
        }
        else
        {
            // All raw materials are at full height
            Debug.Log("Pig 2: All raw materials are at full height!");
            
            // Play invalid animation and SFX
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleProcessingZone()
    {
        // Process: 1 raw material (at full height) → 3 processed materials (by pressing 3 times, shrinking raw 1/3 each time)
        // Find a raw material at full height (state 3)
        int rawIndex = -1;
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterialHeightState[i] == 3) // At full height
            {
                rawIndex = i;
                break;
            }
        }
        
        // Check if we can show more processed materials (max 36 visible)
        if (rawIndex != -1 && visibleProcessedMaterialCount < 36)
        {
            // Shrink raw material and show processed material
            rawMaterialHeightState[rawIndex]--; // Decrease height state (3 → 2 → 1 → 0)
            
            float targetHeight = GetHeightForState(rawMaterialHeightState[rawIndex]);
            SetMaterialHeight(rawMaterials[rawIndex], targetHeight);
            
            // If raw material reached 0, hide it
            if (rawMaterialHeightState[rawIndex] == 0)
            {
                rawMaterials[rawIndex].SetActive(false);
                rawMaterialVisible[rawIndex] = false;
            }
            
            // Find next available processed material slot (can reuse consumed slots)
            int processedSlotIndex = FindNextAvailableProcessedSlot();
            if (processedSlotIndex != -1)
            {
                processedMaterials[processedSlotIndex].SetActive(true);
                processedMaterialVisible[processedSlotIndex] = true;
                visibleProcessedMaterialCount++;
                
                Debug.Log($"Pig 2: Processed raw material {rawIndex + 1} (now at state {rawMaterialHeightState[rawIndex]}). Created processed material at slot {processedSlotIndex + 1}. Total visible: {visibleProcessedMaterialCount}/36");
            }
            else
            {
                // All 36 slots are already visible (shouldn't happen due to check above, but safety)
                Debug.LogWarning("Pig 2: All processed material slots are visible!");
                // Revert the raw material state change
                rawMaterialHeightState[rawIndex]++;
                SetMaterialHeight(rawMaterials[rawIndex], GetHeightForState(rawMaterialHeightState[rawIndex]));
                PlayInvalidActionAnimation();
                PlaySFX(actionInvalidSFX);
                return;
            }
            
            // Play valid animation and SFX
            PlayValidActionAnimation();
            PlaySFX(processValidSFX);
        }
        else
        {
            // No raw material at full height, or max processed materials reached
            if (rawIndex == -1)
            {
                Debug.Log("Pig 2: No raw material at full height to process!");
            }
            else if (visibleProcessedMaterialCount >= 36)
            {
                Debug.Log("Pig 2: Maximum processed materials (36) already visible! Build some first.");
            }
            
            // Play invalid animation and SFX
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleBuildingZone()
    {
        // Build: 4 processed materials → 1 build material (flexible - consume any 4 visible processed materials)
        // Check if we have at least 4 visible processed materials and haven't created all 27 build materials
        if (visibleProcessedMaterialCount >= 4 && visibleBuildMaterialCount < buildMaterials.Length)
        {
            // Find any 4 visible processed materials
            List<int> processedIndicesToConsume = new List<int>();
            for (int i = 0; i < processedMaterials.Length && processedIndicesToConsume.Count < 4; i++)
            {
                if (processedMaterialVisible[i])
                {
                    processedIndicesToConsume.Add(i);
                }
            }
            
            // If we found 4 processed materials, consume them and create build material
            if (processedIndicesToConsume.Count == 4)
            {
                // Hide the 4 processed materials
                foreach (int index in processedIndicesToConsume)
                {
                    processedMaterials[index].SetActive(false);
                    processedMaterialVisible[index] = false;
                }
                visibleProcessedMaterialCount -= 4;

                // Show the next build material sequentially (Build 1, then 2, then 3... up to 27)
                int nextBuildSlot = FindNextHiddenBuildMaterial();
                if (nextBuildSlot != -1)
                {
                    buildMaterials[nextBuildSlot].SetActive(true);
                    buildMaterialVisible[nextBuildSlot] = true;
                    visibleBuildMaterialCount++;
                    
                    Debug.Log($"Pig 2: Built 4 processed materials into build material {nextBuildSlot + 1}. Processed: {visibleProcessedMaterialCount}/36, Build: {visibleBuildMaterialCount}/27");
                    
                    // Check if all 27 build materials are visible
                    if (visibleBuildMaterialCount >= buildMaterials.Length)
                    {
                        CompleteHouseBuilding();
                    }
                }
                else
                {
                    // Shouldn't happen since we checked visibleBuildMaterialCount < buildMaterials.Length
                    Debug.LogWarning("Pig 2: Could not find hidden build material slot!");
                }

                // Play valid animation and SFX
                PlayValidActionAnimation();
                PlaySFX(buildValidSFX);
            }
            else
            {
                // Shouldn't happen, but safety check
                Debug.Log($"Pig 2: Could not find 4 visible processed materials! Found: {processedIndicesToConsume.Count}");
                PlayInvalidActionAnimation();
                PlaySFX(actionInvalidSFX);
            }
        }
        else
        {
            // Invalid action - not enough processed materials or all build materials already created
            if (visibleProcessedMaterialCount < 4)
            {
                Debug.Log($"Pig 2: Not enough processed materials to build! Need 4, have {visibleProcessedMaterialCount}.");
            }
            else
            {
                Debug.Log("Pig 2: All build materials already created!");
            }
            
            // Play invalid animation and SFX
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
        }
    }

    private void CompleteHouseBuilding()
    {
        Debug.Log("Pig 2: House building complete!");

        // Hide all 27 build materials
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
        // Hide all raw materials and reset height states
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterials[i] != null)
            {
                rawMaterials[i].SetActive(false);
                // Reset height to 0 (hidden state)
                rawMaterialHeightState[i] = 0;
                SetMaterialHeight(rawMaterials[i], 0f);
            }
            rawMaterialVisible[i] = false;
            rawMaterialHeightState[i] = 0; // Reset height state
        }

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

    // Helper methods for material management
    
    /// <summary>
    /// Finds the next available processed material slot (can reuse consumed slots)
    /// Returns slot index (0-35) if found, -1 if all 36 are visible
    /// </summary>
    private int FindNextAvailableProcessedSlot()
    {
        // Find first hidden processed material slot (can be anywhere in the 36 slots)
        for (int i = 0; i < processedMaterials.Length; i++)
        {
            if (!processedMaterialVisible[i] && processedMaterials[i] != null)
            {
                return i;
            }
        }
        return -1; // All slots are visible (max 36 reached)
    }
    
    /// <summary>
    /// Finds the next hidden build material slot (sequential: 0, 1, 2... 26)
    /// Returns slot index (0-26) if found, -1 if all 27 are visible
    /// </summary>
    private int FindNextHiddenBuildMaterial()
    {
        // Find first hidden build material slot (sequential order)
        for (int i = 0; i < buildMaterials.Length; i++)
        {
            if (!buildMaterialVisible[i] && buildMaterials[i] != null)
            {
                return i;
            }
        }
        return -1; // All build materials are visible
    }
    
    /// <summary>
    /// Gets the height value based on state (0 = hidden, 1 = 1/3, 2 = 2/3, 3 = full)
    /// </summary>
    private float GetHeightForState(int state)
    {
        switch (state)
        {
            case 0: return 0f; // Hidden (shouldn't be called, but safety)
            case 1: return heightOneThird;   // 0.02
            case 2: return heightTwoThirds;  // 0.04
            case 3: return heightFull;       // 0.06
            default: return 0f;
        }
    }

    /// <summary>
    /// Sets the height of a material GameObject by modifying either Y scale or Y position
    /// </summary>
    private void SetMaterialHeight(GameObject material, float height)
    {
        if (material == null) return;

        if (useScaleForHeight)
        {
            // Modify Y scale (scale based on ratio of height to full height)
            Vector3 scale = material.transform.localScale;
            float scaleRatio = (heightFull > 0) ? height / heightFull : 0f;
            material.transform.localScale = new Vector3(scale.x, originalRawMaterialScaleY * scaleRatio, scale.z);
        }
        else
        {
            // Modify Y position (moves the object up/down) - RECOMMENDED
            Vector3 pos = material.transform.localPosition;
            material.transform.localPosition = new Vector3(pos.x, height, pos.z);
        }
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
}
