using UnityEngine;
using UnityEngine.InputSystem;

public class ToolSpawner : MonoBehaviour
{
    [Header("Tool Types")]
    public bool isRopeSpawner;

    public Camera cam;
    public GameObject dragPrefab;
    public LayerMask toolLayer;
    public LayerMask dragSurface;
    public Transform[] milletSlots;

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

    void TrySpawnAndStartDrag()
    {
        if (isRopeSpawner && !AllSlotsFilled())
        {
            Debug.Log("Rope locked: place all millet first");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        RaycastHit hitTool;
        if (!Physics.Raycast(ray, out hitTool, 100f, toolLayer))
            return;

        // Raycast to drag surface immediately
        RaycastHit hitSurface;
        if (!Physics.Raycast(ray, out hitSurface, 100f, dragSurface))
            return;

        // Spawn EXACTLY at finger/mouse position
        GameObject obj = Instantiate(dragPrefab, hitSurface.point, Quaternion.identity);

        currentRb = obj.GetComponent<Rigidbody>();
        currentRb.useGravity = false;
        currentRb.isKinematic = true;
        currentRb.interpolation = RigidbodyInterpolation.Interpolate;

        isDragging = true;

        Debug.Log("Spawn + Drag started");
    }

    void DragObject()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, dragSurface))
        {
            isOverTable = true;
            currentRb.MovePosition(hit.point);
        }
        else
        {
            isOverTable = false;

        }
    }

    void ReleaseObject()
    {
        if (currentRb == null) return;
        if (isOverTable)
        {
            Transform freeSlot = GetFreeSlot();

            if (freeSlot != null)
            {
                SnapToSlot(freeSlot);

                if (AllSlotsFilled())
                {
                    Debug.Log("All millet placed – rope unlocked");
                }
            }
            else
            {
                Destroy(currentRb.gameObject, 1f);
                //play error sfx (full)
            }

        }
        else
        {
            currentRb.isKinematic = false;
            currentRb.useGravity = true;
            GameObject objToDestroy = currentRb.gameObject;
            Destroy(objToDestroy, 1f);
        }

        currentRb = null;
        isDragging = false;
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

        currentRb.isKinematic = true;   // stay fixed on table
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
}
