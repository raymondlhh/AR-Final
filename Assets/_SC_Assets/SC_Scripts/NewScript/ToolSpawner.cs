using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;

public class ToolSpawner : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
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

    //void Update()
    //{
    //    //if (Mouse.current == null) return;

    //    //if (Mouse.current.leftButton.wasPressedThisFrame)
    //    //    TrySpawnAndStartDrag(Mouse.current.position.ReadValue());

    //    //if (isDragging)
    //    //    DragObject(Mouse.current.position.ReadValue());

    //    //if (Mouse.current.leftButton.wasReleasedThisFrame)
    //    //    ReleaseObject();

    //if (Touchscreen.current == null) return;

    //var touch = Touchscreen.current.primaryTouch;

    //if (touch.press.wasPressedThisFrame)
    //    TrySpawnAndStartDrag(touch.position.ReadValue());

    //if (isDragging && touch.press.isPressed)
    //    DragObject(touch.position.ReadValue());

    //if (touch.press.wasReleasedThisFrame)
    //    ReleaseObject();


    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        TrySpawnAndStartDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        DragObject(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseObject();
    }

    // ================= SPAWN =================
    void TrySpawnAndStartDrag(Vector2 screenPos)
    {
        if (currentRb != null) return;

        if (isRopeSpawner && !AllSlotsFilled())
        {
            PlaySFX(ropeLockedSFX);
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hitTool, 100f))
            return;

        if (hitTool.collider.gameObject != gameObject)
            return;

        if (!Physics.Raycast(ray, out RaycastHit hitSurface, 100f, dragSurface))
            return;

        GameObject obj = Instantiate(dragPrefab, hitSurface.point, Quaternion.identity);

        currentRb = obj.GetComponent<Rigidbody>();
        currentRb.useGravity = false;
        currentRb.isKinematic = true;

        isDragging = true;
        PlaySFX(takeToolSFX);
    }

    // ================= DRAG =================
    void DragObject(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);

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

        // Dropped outside table
        if (!isOverTable)
        {
            DropAndDestroy(obj);
            ResetDrag();
            return;
        }

        // ---------- MILLET ----------
        if (obj.CompareTag("Millet"))
        {
            Transform freeSlot = GetFreeSlot();

            if (freeSlot != null)
            {
                SnapToSlot(freeSlot);

                if (AllSlotsFilled())
                    Debug.Log("All millet placed – rope unlocked");
            }
            else
            {
                Destroy(obj, 1f); // slots full
            }
        }
        // ---------- ROPE ----------
        else if (obj.CompareTag("Rope"))
        {
            if (AllSlotsFilled())
            {
                CraftHay();
                Destroy(obj);
            }
            else
            {
                Debug.Log("Rope useless without 3 millet");
                Destroy(obj, 1f);
            }
        }

        ResetDrag();
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
