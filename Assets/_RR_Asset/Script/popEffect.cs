using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popEffect : MonoBehaviour
{
    public float popDuration = 0.5f; // how long the animation takes
    private Vector3 targetScale;      // the original size
    private float timer = 0f;
    private bool isPopping = false;

    void Awake()
    {
        // Store the current/original size when the object is first created
        targetScale = transform.localScale;
    }

    void OnEnable()
    {
        // Start from 0 size
        transform.localScale = Vector3.zero;
        timer = 0f;
        isPopping = true;
    }

    void Update()
    {
        if (isPopping)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / popDuration);

            // Smooth scale from 0 to the original stored size
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, Mathf.SmoothStep(0f, 1f, t));

            if (t >= 1f)
                isPopping = false;
        }
    }
}
