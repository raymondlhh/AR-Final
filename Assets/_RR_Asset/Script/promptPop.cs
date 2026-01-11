using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;   

public class promptPop : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text tmpText;  

    [Header("Settings")]
    public float animationDuration = 0.5f;  // Total pop-in/out duration
    public float displayTime = 1.5f;        // Time to stay at normal scale
    public float scaleAmount = 1.2f;        // Zoom in multiplier

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        ShowPrompt();
    }

    public void ShowPrompt()
    {
        StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        gameObject.SetActive(true);

        // Zoom in/out animation
        float halfDuration = animationDuration / 2f;
        float timer = 0f;

        // Zoom in
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleAmount, t);
            yield return null;
        }

        // Zoom out
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale * scaleAmount, originalScale, t);
            yield return null;
        }

        // Stay at normal scale for displayTime
        yield return new WaitForSeconds(displayTime);

        transform.localScale = originalScale;
        gameObject.SetActive(false);
    }
}
