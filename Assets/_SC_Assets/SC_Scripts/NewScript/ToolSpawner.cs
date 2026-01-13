using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;

public class ToolSpawner : MonoBehaviour
{
    public static event Action OnHayCrafted;
    [Header("Tool Type")]
    public bool isRopeSpawner; // tick this ONLY on rope spawner

    [Header("References")]
    public Camera cam;
    public GameObject dragPrefab;
    public LayerMask toolLayer;
    public LayerMask dragSurface;
    public Transform[] milletSlots;

    [Header("Crafting")]
    public GameObject hayPrefab;          // hay to spawn
    public Transform haySpawnPoint;        // center of table


    [Header("SFX")]
    public AudioSource audioSource;

    public AudioClip takeToolSFX;
    public AudioClip placeMilletSFX;
    public AudioClip dropFailSFX;
    public AudioClip ropeLockedSFX;
    public AudioClip hayCraftedSFX;

    private Rigidbody currentRb;
    private bool isDragging = false;
    private bool isOverTable = false;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Update()
    {
        if (PressStarted())
            TrySpawnAndStartDrag();

        if (isDragging)
            DragObject();

        if (PressReleased())
            ReleaseObject();
    }

    bool PressStarted()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    bool PressReleased()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            return true;

        return false;
    }

    Vector2 GetPointerPosition()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return Vector2.zero;
    }

    // ================= SPAWN =================
    void TrySpawnAndStartDrag()
    {
        Debug.Log("Detected");

        if (currentRb != null) return;

        // Rope locked until all millet placed
        if (isRopeSpawner && !AllSlotsFilled())
        {
            Debug.Log("Rope locked: place 3 millet first");
            PlaySFX(ropeLockedSFX);
            return;
        }

        Vector2 mousePos = GetPointerPosition()
;
        Ray ray = cam.ScreenPointToRay(mousePos);

        // Must click on tool icon
        RaycastHit hitTool;
        if (!Physics.Raycast(ray, out hitTool, 100f, toolLayer))
        {
            Debug.Log("Tool raycast MISS");
            return;
        }

        Debug.Log("Tool raycast HIT: " + hitTool.collider.name);

        if (hitTool.collider.gameObject != gameObject)
        {
            Debug.Log("Hit wrong object: " + hitTool.collider.name);
            return;
        }


        // Must have valid drag surface
        if (!Physics.Raycast(ray, out RaycastHit hitSurface, 100f, dragSurface))
            return;

        GameObject obj = Instantiate(dragPrefab, hitSurface.point, Quaternion.identity);

        currentRb = obj.GetComponent<Rigidbody>();
        currentRb.useGravity = false;
        currentRb.isKinematic = true;

        isDragging = true;
        PlaySFX(takeToolSFX);

        Debug.Log("Spawn + Drag started");
    }

    // ================= DRAG =================
    void DragObject()
    {
        Vector2 mousePos = GetPointerPosition()
;
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, dragSurface))
        {
            isOverTable = true;
            currentRb.MovePosition(hit.point);
        }
        else
        {
            isOverTable = false;
        }
    }

    // ================= RELEASE =================
    void ReleaseObject()
    {
        if (currentRb == null) return;

        GameObject obj = currentRb.gameObject;

        // ---------- MILLET ----------
        if (obj.CompareTag("Millet"))
        {
            Transform freeSlot = GetFreeSlot();

            if (freeSlot != null)
            {
                // ✅ SNAP AND KEEP
                SnapToSlot(freeSlot);
                Debug.Log("Millet snapped and kept");

                ResetDrag();
                return;
            }
            else
            {
                // ❌ NO SLOT → DESTROY
                Debug.Log("No free slot → destroy millet");
                DropAndDestroy(obj);

                ResetDrag();
                return;
            }
        }

        // ---------- ROPE ----------
        if (obj.CompareTag("Rope"))
        {
            if (AllSlotsFilled())
            {
                Debug.Log("Rope used → crafting hay");

                CraftHay();      // destroys millet inside
                Destroy(obj);    // destroy rope
            }
            else
            {
                Debug.Log("Rope without 3 millet → destroy rope");
                Destroy(obj, 1f);
            }

            ResetDrag();
            return;
        }
    }

    // ================= HELPERS =================
    void ResetDrag()
    {
        currentRb = null;
        isDragging = false;
        isOverTable = false;
    }

    void DropAndDestroy(GameObject obj)
    {
        currentRb.isKinematic = false;
        currentRb.useGravity = true;
        PlaySFX(dropFailSFX);
        Destroy(obj, 1f);
    }

    Transform GetFreeSlot()
    {
        foreach (Transform slot in milletSlots)
        {
            if (slot.childCount == 0)
                return slot;
        }
        return null;
    }

    void SnapToSlot(Transform slot)
    {
        Debug.Log("Snapped");

        PlaySFX(placeMilletSFX);

        currentRb.transform.position = slot.position;
        currentRb.transform.rotation = slot.rotation;
        currentRb.transform.SetParent(slot);

        currentRb.isKinematic = true;
        currentRb.useGravity = false;

    }

    bool AllSlotsFilled()
    {
        foreach (Transform slot in milletSlots)
        {
            if (slot.childCount == 0)
                return false;
        }
        return true;
    }

    // ================= CRAFT =================
    public void CraftHay()
    {
        PlaySFX(hayCraftedSFX);

        Debug.Log("HAY CREATED!");

        // remove all millet
        foreach (Transform slot in milletSlots)
        {
            if (slot.childCount > 0)
                Destroy(slot.GetChild(0).gameObject);
        }

        // spawn hay
        if (hayPrefab != null && haySpawnPoint != null)
        {
            GameObject hayInstance = Instantiate(
                hayPrefab,
                haySpawnPoint.position,
                haySpawnPoint.rotation,
                haySpawnPoint.parent
            );

            Destroy(hayInstance, 2f);
        }

        OnHayCrafted?.Invoke();
    }
    void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

}