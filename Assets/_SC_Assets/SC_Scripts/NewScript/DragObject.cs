using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragObject : MonoBehaviour
{
    private bool isDragging;
    private Camera arCamera;

    void Start()
    {
        arCamera = Camera.main;
    }

    public void StartDragging()
    {
        isDragging = true;
    }

    public void StopDragging()
    {
        isDragging = false;
    }

    void Update()
    {
        if (!isDragging) return;

        // Ignore UI touches
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(0))
            return;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Drag(t.position);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            Drag(Input.mousePosition);
        }
#endif
    }

    void Drag(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 20f))
        {
            transform.position = hit.point;
        }
    }
}
