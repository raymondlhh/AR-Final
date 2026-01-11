using UnityEngine;
using UnityEngine.InputSystem;

public class DragController : MonoBehaviour
{
    Camera cam;
    bool dragging;
    float depth;

    public void BeginDrag(Camera camera)
    {
        cam = camera;
        dragging = true;

        // Keep same distance from camera
        depth = Vector3.Distance(cam.transform.position, transform.position);
    }

    void Update()
    {
        if (!dragging) return;

        if (!IsHeld())
        {
            dragging = false;
            return;
        }

        Vector3 screenPos = new Vector3(
            GetPointerPos().x,
            GetPointerPos().y,
            depth
        );

        transform.position = cam.ScreenToWorldPoint(screenPos);
    }

    bool IsHeld()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.isPressed;

        return Mouse.current.leftButton.isPressed;
    }

    Vector2 GetPointerPos()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return Mouse.current.position.ReadValue();
    }
}
