using UnityEngine;
using UnityEngine.InputSystem;
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

    private Rigidbody currentRb;
    private bool isDragging = false;
    private bool isOverTable = false;

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySpawnAndStartDrag();

        if (isDragging)
            DragObject();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            ReleaseObject();
    }

    // ================= SPAWN =================
    void TrySpawnAndStartDrag()
    {
        if (currentRb != null) return;

        // Rope locked until all millet placed
        if (isRopeSpawner && !AllSlotsFilled())
        {
            Debug.Log("Rope locked: place 3 millet first");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        // Must click on tool icon
        RaycastHit hitTool;
        if (!Physics.Raycast(ray, out hitTool, 100f))
            return;

        if (hitTool.collider.gameObject != gameObject)
            return;

        // Must have valid drag surface
        if (!Physics.Raycast(ray, out RaycastHit hitSurface, 100f, dragSurface))
            return;

        GameObject obj = Instantiate(dragPrefab, hitSurface.point, Quaternion.identity);

        currentRb = obj.GetComponent<Rigidbody>();
        currentRb.useGravity = false;
        currentRb.isKinematic = true;

        isDragging = true;
        Debug.Log("Spawn + Drag started");
    }

    // ================= DRAG =================
    void DragObject()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
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
}
