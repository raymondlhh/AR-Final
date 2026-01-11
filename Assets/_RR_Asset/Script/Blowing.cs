using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blowing : MonoBehaviour
{
    [Header("Stretch Settings")]
    public float stretchAmount = 0.25f; // how strong the wind
    public float stretchSpeed = 8f;
    public float duration = 1.2f;
    public bool squashRight = true; // true = pivot simulated on left, squish to right

    private Vector3 originalScale;
    private Vector3 originalPosition;

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition; // for local pivot offset
        Blow();
    }

    public void Blow()
    {
        StartCoroutine(SquishRoutine());
    }

    IEnumerator SquishRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            float wave = Mathf.Sin(Time.time * stretchSpeed) * stretchAmount;

            Vector3 targetScale = originalScale;
            Vector3 targetPosition = originalPosition;

            // X-axis squash/stretch
            if (squashRight)
            {
                targetScale.x = originalScale.x + wave;
                targetScale.y = originalScale.y - wave * 0.5f;
                targetPosition.x = originalPosition.x - wave * 0.5f; // offset left so it squishes right
            }
            else
            {
                targetScale.x = originalScale.x + wave;
                targetScale.y = originalScale.y - wave * 0.5f;
                targetPosition.x = originalPosition.x + wave * 0.1f; // squish left
            }

            // Smoothly apply
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * stretchSpeed);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * stretchSpeed);

            timer += Time.deltaTime;
            yield return null;
        }

        // Return to normal
        float t = 0f;
        while (t < 1f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * stretchSpeed);
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * stretchSpeed);
            t += Time.deltaTime;
            yield return null;
        }
    }
}
