using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Spawn3Bobj : MonoBehaviour
{
    public Camera cam;

    [Header("Prefabs")]
    public GameObject waterPrefab;
    public GameObject clayPrefab;
    public GameObject sandPrefab;

    bool hasSpawned;

    void Update()
    {
        if (PressStarted())
        {
            if (hasSpawned) return;
            hasSpawned = true;

            TrySpawn();
        }

        if (PressReleased())
            hasSpawned = false;
    }

    void TrySpawn()
    {
        Vector2 pointerPos = GetPointerPos();
        Debug.Log("Pointer Position: " + pointerPos);

        Ray ray = cam.ScreenPointToRay(pointerPos);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log("Raycast HIT NOTHING");
            return;
        }

        Debug.Log("Raycast HIT: " + hit.collider.name);
        Debug.Log("Hit TAG: " + hit.collider.tag);

        GameObject prefab = GetPrefabFromHit(hit.collider);
        if (prefab == null)
        {
            Debug.Log("No prefab matched for this tag");
            return;
        }

        GameObject obj = Instantiate(prefab, hit.point, Quaternion.identity);

        DragController drag = obj.GetComponent<DragController>();
        if (drag != null)
            drag.BeginDrag(cam);
    }

    GameObject GetPrefabFromHit(Collider col)
    {
        // OPTION 1: Use TAG (recommended)
        if (col.CompareTag("Water")) return waterPrefab;
        if (col.CompareTag("Clay")) return clayPrefab;
        if (col.CompareTag("Sand")) return sandPrefab;

        return null;
    }

    // -------- INPUT --------

    bool PressStarted()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        return Mouse.current.leftButton.wasPressedThisFrame;
    }

    bool PressReleased()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;

        return Mouse.current.leftButton.wasReleasedThisFrame;
    }

    Vector2 GetPointerPos()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return Mouse.current.position.ReadValue();
    }
}