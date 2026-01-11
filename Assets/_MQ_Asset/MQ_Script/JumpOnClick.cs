using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class JumpOnClick : MonoBehaviour
{
    private Vector3 originalPos;
    private bool isJumping;
    private Camera mainCam;

    void Start()
    {
        originalPos = transform.localPosition;
        mainCam = Camera.main;
    }

    void Update()
    {
        // NEW Input System click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform && !isJumping)
                {
                    Debug.Log("[Pig] Clicked → Jump!");
                    StartCoroutine(Jump());
                }
            }
        }
    }

    IEnumerator Jump()
    {
        isJumping = true;

        float height = 0.2f;
        float duration = 0.15f;

        Vector3 up = originalPos + Vector3.up * height;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            transform.localPosition = Vector3.Lerp(originalPos, up, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            transform.localPosition = Vector3.Lerp(up, originalPos, t);
            yield return null;
        }

        isJumping = false;
    }

}
