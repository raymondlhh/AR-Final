using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Spawn3Bobj : MonoBehaviour
{
    public Camera cam;
    public GameObject prefab;
    public LayerMask uiLayer;

    private GameObject currentObj;
    private Rigidbody currentRb;
    private bool isDragging;
    private float dragDepth; // auto calculated

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySpawn();

        if (isDragging)
            Drag();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            Release();
    }

    void TrySpawn()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, uiLayer))
            return;

        if (hit.collider.gameObject != gameObject)
            return;

        // use hit point depth
        dragDepth = Vector3.Distance(cam.transform.position, hit.point);

        Vector3 spawnPos = cam.ScreenToWorldPoint(
            new Vector3(Mouse.current.position.ReadValue().x,
                        Mouse.current.position.ReadValue().y,
                        dragDepth));

        currentObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        currentRb = currentObj.GetComponent<Rigidbody>();

        currentRb.isKinematic = true;
        currentRb.useGravity = false;

        isDragging = true;
    }

    void Drag()
    {
        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(Mouse.current.position.ReadValue().x,
                        Mouse.current.position.ReadValue().y,
                        dragDepth));

        currentObj.transform.position = worldPos;
    }

    void Release()
    {
        if (!isDragging) return;

        currentRb.isKinematic = false;
        currentRb.useGravity = true;

        isDragging = false;
        currentObj = null;
        currentRb = null;
    }
}