using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private ZoneType previousZone = ZoneType.None; // Track previous zone for debugging
    public bool IsAutoProcessing { get; private set; }

    // Zone detection settings
    [Header("Zone Detection Settings")]
    [Tooltip("Enable continuous zone checking for more reliable detection")]
    public bool useContinuousZoneCheck = true;
    [Tooltip("How often to check for zone presence (in seconds)")]
    public float zoneCheckInterval = 0.1f;
    [Tooltip("Radius to check for zone colliders around the player")]
    public float zoneCheckRadius = 1.5f;
    
    private float lastZoneCheckTime = 0f;
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

    [Header("Material Arrays - Pig 2")]
    [Header("Raw Materials (6 total, initially hidden at Processing Zone)")]
    public GameObject[] rawMaterials = new GameObject[6];

    [Header("Processed Materials (36 total, initially hidden at Processing Zone)")]
    public GameObject[] processedMaterials = new GameObject[36];

    [Header("Build Materials (27 total, initially hidden at Building Zone)")]
    public GameObject[] buildMaterials = new GameObject[27];
    
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

    [Header("Scale Settings")]
    [Tooltip("Original scale of raw materials (stored at Start, used for relative scaling)")]
    public Vector3 originalRawMaterialScale = Vector3.one;  // Default (1,1,1), will be stored from first raw material
    
    // Scale ratios (relative to original size)
    private const float SCALE_ONE_THIRD = 0.33f;    // 1/3 of original size
    private const float SCALE_TWO_THIRDS = 0.67f;   // 2/3 of original size
    private const float SCALE_FULL = 1.0f;          // Full original size
    private const float SCALE_HIDDEN = 0.0f;        // Hidden (scale to 0)

    // Material tracking - track visibility and scale state
    private bool[] rawMaterialVisible = new bool[6];
    private bool[] processedMaterialVisible = new bool[36];
    private bool[] buildMaterialVisible = new bool[27];
    
    // Track raw material scale state: 0 = hidden (scale 0), 1 = 1/3 scale, 2 = 2/3 scale, 3 = full scale
    private int[] rawMaterialScaleState = new int[6]; // 0-3 for each raw material
    
    // Store original scales for each raw material (in case they differ)
    private Vector3[] originalRawMaterialScales = new Vector3[6];
    
    
    // Counters for quick checks
    private int visibleProcessedMaterialCount = 0; // How many processed materials are currently visible (max 36)
    private int visibleBuildMaterialCount = 0;


    void Start()
    {
        // Check for Rigidbody and validate trigger detection setup
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Pig2InteractionController: No Rigidbody found. Please ensure the player has a Rigidbody component.");
        }
        else
        {
            // Ensure Rigidbody is configured for trigger detection
            // Non-kinematic Rigidbodies detect triggers automatically in Unity
            if (rb.isKinematic)
            {
                Debug.LogWarning("Pig2InteractionController: Rigidbody is kinematic. " +
                    "Kinematic Rigidbodies may not detect triggers reliably in some Unity versions. " +
                    "Consider setting IsKinematic = false for better trigger detection.");
            }
        }
        
        // Ensure player has a collider for trigger detection
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            Debug.LogWarning("Pig2InteractionController: Player GameObject has no Collider! " +
                "Trigger detection requires a Collider on the player GameObject. " +
                "Zone detection may not work without a collider. Please add a Collider component.");
        }
        else
        {
            // Ensure player collider is NOT a trigger (the zone colliders should be triggers)
            if (playerCollider.isTrigger)
            {
                Debug.LogWarning("Pig2InteractionController: Player Collider is set as a trigger! " +
                    "The player's collider should NOT be a trigger. Only the zone colliders (PlayerDetectZone) should be triggers. " +
                    "Fixing automatically...");
                playerCollider.isTrigger = false;
            }
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

        // Store original scales of raw materials (for relative scaling)
        StoreOriginalRawMaterialScales();
        
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
        // Continuous zone checking only as a backup/validation (don't override trigger-based detection)
        // Only run if we think we're in a zone but want to verify, OR if we're not in a zone and want to check
        if (useContinuousZoneCheck && Time.time - lastZoneCheckTime >= zoneCheckInterval)
        {
            // Only validate if we're not already in a zone (as a fallback detection)
            // OR validate periodically to ensure we haven't lost tracking
            if (currentZone == ZoneType.None || currentZoneColliders.Count == 0)
            {
                // Only use continuous check as a fallback when triggers fail
                ValidateCurrentZone();
            }
            lastZoneCheckTime = Time.time;
        }
        
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
        ZoneTrigger zoneTrigger = other.GetComponent<ZoneTrigger>();
        if (zoneTrigger != null)
        {
            if (!currentZoneColliders.Contains(other))
                currentZoneColliders.Add(other);

            previousZone = currentZone;
            currentZone = zoneTrigger.zoneType;

            Debug.Log($"Pig 2: Entered {currentZone} zone (via ZoneTrigger)");
            UpdateActionButtonIcon();
            return; 
        }

        // Detect which zone the player entered (matching PlayerInteractionController logic)
        // The zone detection boxes are named "PlayerDetectZone" and are children of the zone GameObjects
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
                    Debug.Log($"Pig 2: Entered Collecting Zone (Total zones tracked: {currentZoneColliders.Count})");
                }
                else if (zoneName == "Processing_Zone")
                {
                    previousZone = currentZone;
                    currentZone = ZoneType.Processing;
                    Debug.Log($"Pig 2: Entered Processing Zone (Total zones tracked: {currentZoneColliders.Count})");
                }
                else if (zoneName == "Building_Zone")
                {
                    previousZone = currentZone;
                    currentZone = ZoneType.Building;
                    Debug.Log($"Pig 2: Entered Building Zone (Total zones tracked: {currentZoneColliders.Count})");
                }
                
            }
        }
    }

    public void OnZoneTriggerExit(Collider other)
    {
        if (other.GetComponent<ZoneTrigger>() != null)
        {
            currentZoneColliders.Remove(other);

            if (currentZoneColliders.Count == 0)
            {
                previousZone = currentZone;
                currentZone = ZoneType.None;
                Debug.Log("Pig 2: Left all zones (ZoneTrigger)");

                UpdateActionButtonIcon();
            }
            return;
        }

        // Reset zone when leaving (matching PlayerInteractionController logic)
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
                    Debug.Log($"Pig 2: Left {exitedZone} Zone (Remaining zones tracked: {currentZoneColliders.Count})");

                    UpdateActionButtonIcon();
                }
                UpdateActionButtonIcon();
            }
        }
    }
    
    /// <summary>
    /// Validates the current zone by checking which zone colliders the player is currently inside.
    /// This is used as a FALLBACK only when trigger-based detection fails (when currentZone is None).
    /// Uses physics overlap to find all zone colliders near the player, then checks bounds.
    /// </summary>
    void ValidateCurrentZone()
    {
        if (!useContinuousZoneCheck) return;

        // Only use this as a fallback when we're not already in a zone
        // Don't override trigger-based detection!
        if (currentZone != ZoneType.None)
        {
            return; // Trust the trigger-based detection
        }
        
        // Use physics overlap to find all zone colliders near the player
        // Only check for trigger colliders with the name "PlayerDetectZone"
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, zoneCheckRadius);
        
        ZoneType highestPriorityZone = ZoneType.None;
        List<Collider> foundZoneColliders = new List<Collider>();
        
        foreach (Collider col in nearbyColliders)
        {
            if (col == null || !col.isTrigger) continue;
            if (col.gameObject.name != "PlayerDetectZone") continue;
            if (!col.gameObject.activeInHierarchy) continue;
            
            // Check if the player's position is inside the collider bounds
            bool isInside = false;
            
            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
            {
                // Check if our position is within the bounds
                if (col.bounds.Contains(transform.position))
                {
                    isInside = true;
                }
                else
                {
                    // Check closest point as fallback
                    Vector3 closestPoint = col.ClosestPoint(transform.position);
                    float distance = Vector3.Distance(transform.position, closestPoint);
                    if (distance < 0.2f)
                    {
                        isInside = true;
                    }
                }
            }
            else
            {
                // For other collider types, use closest point check
                Vector3 closestPoint = col.ClosestPoint(transform.position);
                float distance = Vector3.Distance(transform.position, closestPoint);
                isInside = distance < 0.2f;
            }
            
            if (isInside)
            {
                foundZoneColliders.Add(col);
                
                Transform zoneParent = col.transform.parent;
                if (zoneParent != null)
                {
                    string zoneName = zoneParent.gameObject.name;
                    ZoneType detectedZone = ZoneType.None;

                    if (zoneName == "Collecting_Zone")
                    {
                        detectedZone = ZoneType.Collecting;
                    }
                    else if (zoneName == "Processing_Zone")
                    {
                        detectedZone = ZoneType.Processing;
                    }
                    else if (zoneName == "Building_Zone")
                    {
                        detectedZone = ZoneType.Building;
                    }
                    
                    // Priority: Building > Processing > Collecting > None
                    if (detectedZone == ZoneType.Building)
                    {
                        highestPriorityZone = ZoneType.Building;
                    }
                    else if (detectedZone == ZoneType.Processing && highestPriorityZone != ZoneType.Building)
                    {
                        highestPriorityZone = ZoneType.Processing;
                    }
                    else if (detectedZone == ZoneType.Collecting && highestPriorityZone == ZoneType.None)
                    {
                        highestPriorityZone = ZoneType.Collecting;
                    }
                }
            }
        }
        
        // Only update if we found a zone and we weren't in one before (fallback detection)
        if (highestPriorityZone != ZoneType.None && currentZone == ZoneType.None)
        {
            previousZone = currentZone;
            currentZone = highestPriorityZone;
            currentZoneColliders.Clear();
            currentZoneColliders.AddRange(foundZoneColliders);
            Debug.Log($"Pig 2: Fallback validation detected {currentZone} zone (found {foundZoneColliders.Count} zone collider(s))");

            UpdateActionButtonIcon();

        }
    }

    public void OnActionButtonPressed()
    {
        HandleActionPress();
    }

    private void HandleActionPress()
    {
        // Don't validate zone here - trust the trigger events (like PlayerInteractionController)
        // Only use continuous validation as a backup, not to override trigger-based detection
        
        Debug.Log($"Pig 2: Action button pressed. Current zone: {currentZone}, Previous: {previousZone}, Zone colliders tracked: {currentZoneColliders.Count}");
        
        if (currentZone == ZoneType.None)
        {
            Debug.LogWarning("Pig 2: ⚠️ Action pressed but NOT in any zone! Action button does nothing (no SFX).");
            // Don't play invalid SFX when not in zone - just do nothing (matching PlayerInteractionController behavior)
            return;
        }

        Debug.Log($"Pig 2: Processing action in {currentZone} zone...");
        
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

    public bool HasRawReadyForProcessing()
    {
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterialScaleState[i] > 0)
                return true;
        }
        return false;
    }

    private void HandleCollectingZone()
    {
        // Progressive growth: Each raw material needs 3 presses to reach full size (scale)
        // Press 1: Scale to 1/3 (0.33), Press 2: Scale to 2/3 (0.67), Press 3: Scale to full (1.0)
        // Find the first raw material that's not at full scale

        int rawIndex = -1;
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterials[i] == null)
            {
                Debug.LogWarning($"Pig 2: Raw material at index {i} is null! Check material assignment.");
                continue;
            }
            
            if (rawMaterialScaleState[i] < 3) // Not at full scale yet (0, 1, or 2)
            {
                rawIndex = i;
                break;
            }
        }
        
        if (rawIndex != -1)
        {
            // Grow this raw material (increase scale state)
            rawMaterialScaleState[rawIndex]++;
            
            // Show material if it's the first press (was hidden/scale 0)
            if (rawMaterialScaleState[rawIndex] == 1)
            {
                if (rawMaterials[rawIndex] != null)
                {
                    rawMaterials[rawIndex].SetActive(true);
                    rawMaterialVisible[rawIndex] = true;
                }
                else
                {
                    Debug.LogError($"Pig 2: Raw material at index {rawIndex} is null! Cannot show.");
                    PlayInvalidActionAnimation();
                    PlaySFX(actionInvalidSFX);
                    return;
                }
            }
            
            // Update scale based on state (relative to original size)
            float scaleRatio = GetScaleRatioForState(rawMaterialScaleState[rawIndex]);
            SetMaterialScale(rawMaterials[rawIndex], rawIndex, scaleRatio);
            
            Debug.Log($"Pig 2: ✅ VALID ACTION - Raw material {rawIndex + 1} grown to scale state {rawMaterialScaleState[rawIndex]}/3 (scale ratio: {scaleRatio})");
            
            // Play valid animation and SFX
            PlayValidActionAnimation();
            PlaySFX(collectValidSFX);
        }
        else
        {
            // All raw materials are at full scale
            Debug.Log("Pig 2: ❌ INVALID ACTION - All raw materials are at full scale! (All 6 materials at state 3/3)");
            
            // Play invalid animation and SFX
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleProcessingZone()
    {
        // Process: 1 raw material (at full scale) → 3 processed materials (by pressing 3 times)
        // Press 1: Shrink raw to 2/3 scale (0.67), show processed material 1
        // Press 2: Shrink raw to 1/3 scale (0.33), show processed material 2
        // Press 3: Shrink raw to 0 scale (hidden), show processed material 3
        // 
        // IMPORTANT: Continue processing the SAME raw material until it reaches state 0
        // Find a raw material that's ready to process (state > 0) - prioritize state 3, but continue with partially processed ones
        int rawIndex = -1;
        
        // First, check if there's a raw material that's partially processed (state 1 or 2)
        // This means we're in the middle of processing one - continue with that one
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterials[i] != null && rawMaterialScaleState[i] > 0 && rawMaterialScaleState[i] < 3)
            {
                // Found a partially processed raw material - continue processing this one
                rawIndex = i;
                break;
            }
        }
        
        // If no partially processed material, find one at full scale (state 3)
        if (rawIndex == -1)
        {
            for (int i = 0; i < rawMaterials.Length; i++)
            {
                if (rawMaterials[i] != null && rawMaterialScaleState[i] == 3) // At full scale
                {
                    rawIndex = i;
                    break;
                }
            }
        }
        
        // Check if we can show more processed materials (max 36 visible)
        if (rawIndex != -1 && visibleProcessedMaterialCount < 36)
        {
            // Shrink raw material (decrease scale state: 3 → 2 → 1 → 0)
            int previousState = rawMaterialScaleState[rawIndex];
            rawMaterialScaleState[rawIndex]--;
            int newState = rawMaterialScaleState[rawIndex];
            
            float scaleRatio = GetScaleRatioForState(newState);
            SetMaterialScale(rawMaterials[rawIndex], rawIndex, scaleRatio);
            
            // If raw material reached state 0 (scale 0), hide it completely
            if (newState == 0)
            {
                rawMaterialVisible[rawIndex] = false;
                // Optionally also SetActive(false) to ensure it's completely hidden
                // But since scale is 0, it should be invisible anyway
                Debug.Log($"Pig 2: Raw material {rawIndex + 1} fully consumed (scale = 0)");
            }
            
            // Find next available processed material slot (sequential: 0, 1, 2... 35)
            int processedSlotIndex = FindNextAvailableProcessedSlot();
            if (processedSlotIndex != -1)
            {
                processedMaterials[processedSlotIndex].SetActive(true);
                processedMaterialVisible[processedSlotIndex] = true;
                visibleProcessedMaterialCount++;
                
                Debug.Log($"Pig 2: ✅ VALID ACTION - Processed raw material {rawIndex + 1} (state {previousState} → {newState}, scale ratio: {scaleRatio}). Created processed material at slot {processedSlotIndex + 1}. Total visible: {visibleProcessedMaterialCount}/36");
            }
            else
            {
                // All 36 slots are already visible (shouldn't happen due to check above, but safety)
                Debug.LogWarning("Pig 2: All processed material slots are visible!");
                // Revert the raw material state change
                rawMaterialScaleState[rawIndex] = previousState;
                float revertScale = GetScaleRatioForState(previousState);
                SetMaterialScale(rawMaterials[rawIndex], rawIndex, revertScale);
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
            // No raw material ready to process, or max processed materials reached
            if (rawIndex == -1)
            {
                Debug.Log("Pig 2: ❌ INVALID ACTION - No raw material ready to process! (Need at least one at state 1, 2, or 3)");
            }
            else if (visibleProcessedMaterialCount >= 36)
            {
                Debug.Log("Pig 2: ❌ INVALID ACTION - Maximum processed materials (36) already visible! Build some first.");
            }
            
            // Play invalid animation and SFX
            PlayInvalidActionAnimation();
            PlaySFX(actionInvalidSFX);
        }
    }

    private void HandleBuildingZone()
    {
        // Build: 4 processed materials → 1 build material
        // Processed materials are consumed from the END of the array (highest indices first)
        // Example: Build 1 consumes processed materials 32-35, Build 2 consumes 28-31, etc.
        // Check if we have at least 4 visible processed materials and haven't created all 27 build materials
        if (visibleProcessedMaterialCount >= 4 && visibleBuildMaterialCount < buildMaterials.Length)
        {
            // Find 4 visible processed materials starting from the END of the array (highest indices first)
            List<int> processedIndicesToConsume = new List<int>();
            for (int i = processedMaterials.Length - 1; i >= 0 && processedIndicesToConsume.Count < 4; i--)
            {
                if (processedMaterialVisible[i])
                {
                    processedIndicesToConsume.Add(i);
                }
            }
            
            // If we found 4 processed materials, consume them and create build material
            if (processedIndicesToConsume.Count == 4)
            {
                // Sort indices descending for logging clarity
                processedIndicesToConsume.Sort((a, b) => b.CompareTo(a));
                
                // Hide the 4 processed materials (from end of array)
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
                    
                    Debug.Log($"Pig 2: Built 4 processed materials (indices {processedIndicesToConsume[0]+1}, {processedIndicesToConsume[1]+1}, {processedIndicesToConsume[2]+1}, {processedIndicesToConsume[3]+1}) into build material {nextBuildSlot + 1}. Processed: {visibleProcessedMaterialCount}/36, Build: {visibleBuildMaterialCount}/27");
                    
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

        StartCoroutine(LoadScene());
    }


private IEnumerator LoadScene()
{
    yield return new WaitForSeconds(2f); // wait for SFX / animation
    SceneManager.LoadScene("Wolf Come Scene 1");
}
private void HideAllMaterials()
    {
        // Hide all raw materials and reset scale states
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterials[i] != null)
            {
                rawMaterials[i].SetActive(false);
                // Reset scale to 0 (hidden state)
                rawMaterialScaleState[i] = 0;
                SetMaterialScale(rawMaterials[i], i, SCALE_HIDDEN);
            }
            rawMaterialVisible[i] = false;
            rawMaterialScaleState[i] = 0; // Reset scale state
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
    /// Stores the original scales of all raw materials at Start (for relative scaling)
    /// </summary>
    private void StoreOriginalRawMaterialScales()
    {
        for (int i = 0; i < rawMaterials.Length; i++)
        {
            if (rawMaterials[i] != null)
            {
                originalRawMaterialScales[i] = rawMaterials[i].transform.localScale;
                // If no material has been assigned yet, use default Vector3.one
                if (originalRawMaterialScales[i] == Vector3.zero)
                {
                    originalRawMaterialScales[i] = Vector3.one;
                }
            }
            else
            {
                originalRawMaterialScales[i] = Vector3.one; // Default fallback
            }
        }
        
        // Also store in the public field for reference (use first non-null material's scale)
        if (rawMaterials.Length > 0 && rawMaterials[0] != null)
        {
            originalRawMaterialScale = originalRawMaterialScales[0];
        }
    }
    
    /// <summary>
    /// Gets the scale ratio based on state (0 = hidden/0.0, 1 = 1/3/0.33, 2 = 2/3/0.67, 3 = full/1.0)
    /// </summary>
    private float GetScaleRatioForState(int state)
    {
        switch (state)
        {
            case 0: return SCALE_HIDDEN;    // 0.0 (hidden)
            case 1: return SCALE_ONE_THIRD; // 0.33 (1/3 size)
            case 2: return SCALE_TWO_THIRDS;// 0.67 (2/3 size)
            case 3: return SCALE_FULL;      // 1.0 (full size)
            default: return SCALE_HIDDEN;
        }
    }

    /// <summary>
    /// Sets the scale of a material GameObject (all axes XYZ) relative to its original size
    /// </summary>
    private void SetMaterialScale(GameObject material, int materialIndex, float scaleRatio)
    {
        if (material == null) 
        {
            Debug.LogWarning($"Pig 2: SetMaterialScale called with null material at index {materialIndex}");
            return;
        }
        
        // Get the original scale for this material (or use stored default)
        Vector3 originalScale = originalRawMaterialScales[materialIndex];
        if (originalScale == Vector3.zero)
        {
            // If original scale wasn't stored properly, try to use the current scale divided by current ratio
            // Or use Vector3.one as fallback
            originalScale = Vector3.one;
            Debug.LogWarning($"Pig 2: Original scale for material {materialIndex} was zero, using fallback Vector3.one. Material name: {material.name}");
        }
        
        // Apply scale ratio to all axes (uniform scaling)
        Vector3 newScale = originalScale * scaleRatio;
        material.transform.localScale = newScale;
        
        // Debug log for troubleshooting scale changes
        if (scaleRatio == SCALE_HIDDEN)
        {
            Debug.Log($"Pig 2: Set material {materialIndex} ({material.name}) scale to 0 (HIDDEN). Original: {originalScale}, New: {newScale}, Ratio: {scaleRatio}");
        }
        else
        {
            Debug.Log($"Pig 2: Set material {materialIndex} ({material.name}) scale. Original: {originalScale}, New: {newScale}, Ratio: {scaleRatio}");
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
                    foundRawMaterials.Add(child.gameObject);
                }
                Debug.Log($"Pig2InteractionController: No materials found with 'Raw' in name. Using all children of {rawMaterialsParent.name}.");
            }
            
            // Sort by name using natural/numeric sorting (handles numbers correctly)
            foundRawMaterials.Sort((a, b) => NaturalCompare(a.name, b.name));
            
            // Assign to array (up to 6)
            rawCount = Mathf.Min(foundRawMaterials.Count, rawMaterials.Length);
            for (int i = 0; i < rawCount; i++)
            {
                rawMaterials[i] = foundRawMaterials[i];
            }
            
            Debug.Log($"Pig2InteractionController: Auto-assigned {rawCount}/{rawMaterials.Length} raw materials from '{rawMaterialsParent.name}'. Found {foundRawMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig2InteractionController: Raw Materials Parent not assigned! Cannot auto-assign raw materials.");
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
                Debug.Log($"Pig2InteractionController: No materials found with 'Processed' in name. Using all children of {processedMaterialsParent.name}.");
            }
            
            // Sort by name using natural/numeric sorting (handles numbers correctly)
            foundProcessedMaterials.Sort((a, b) => NaturalCompare(a.name, b.name));
            
            // Assign to array (up to 36)
            processedCount = Mathf.Min(foundProcessedMaterials.Count, processedMaterials.Length);
            for (int i = 0; i < processedCount; i++)
            {
                processedMaterials[i] = foundProcessedMaterials[i];
            }
            
            Debug.Log($"Pig2InteractionController: Auto-assigned {processedCount}/{processedMaterials.Length} processed materials from '{processedMaterialsParent.name}'. Found {foundProcessedMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig2InteractionController: Processed Materials Parent not assigned! Cannot auto-assign processed materials.");
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
                Debug.Log($"Pig2InteractionController: No materials found with 'Build' in name. Using all children of {buildMaterialsParent.name}.");
            }
            
            // Sort by name using natural/numeric sorting (handles numbers correctly)
            foundBuildMaterials.Sort((a, b) => NaturalCompare(a.name, b.name));
            
            // Assign to array (up to 27)
            buildCount = Mathf.Min(foundBuildMaterials.Count, buildMaterials.Length);
            for (int i = 0; i < buildCount; i++)
            {
                buildMaterials[i] = foundBuildMaterials[i];
            }
            
            Debug.Log($"Pig2InteractionController: Auto-assigned {buildCount}/{buildMaterials.Length} build materials from '{buildMaterialsParent.name}'. Found {foundBuildMaterials.Count} total children.");
        }
        else
        {
            Debug.LogWarning("Pig2InteractionController: Build Materials Parent not assigned! Cannot auto-assign build materials.");
        }
        
        if (rawCount > 0 || processedCount > 0 || buildCount > 0)
        {
            Debug.Log($"Pig2InteractionController: Auto-assignment complete! Raw: {rawCount}, Processed: {processedCount}, Build: {buildCount}");
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
                int aStart = aIndex;
                int bStart = bIndex;
                
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
