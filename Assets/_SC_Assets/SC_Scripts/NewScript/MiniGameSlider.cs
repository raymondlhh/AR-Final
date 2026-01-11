using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class MiniGameSlider : MonoBehaviour
{
    [Header("Slider")]
    public Slider slider;
    public Action OnMiniGamePerfectComplete;

    [Header("Perfect Zone UI (Orange Fill)")]
    public RectTransform perfectFill; // Slider > Fill Area > Fill

    [Header("Perfect Zone Settings")]
    [Range(0.05f, 0.3f)]
    public float perfectWidth = 0.12f;

    public float minStart = 0.05f;
    public float maxEnd = 0.95f;

    [Header("Slider Movement")]
    public float speed = 1.2f;

    [Header("Retry Settings")]
    public float retryDelay = 0.5f;

    [Header("Wood UI (3 needed)")]
    public GameObject[] woodUI;

    private bool moveRight = true;
    private bool isStopped = false;
    private int perfectCount = 0;

    private float perfectMin;
    private float perfectMax;

    void Start()
    {
        foreach (var wood in woodUI)
            wood.SetActive(false);

        slider.value = 0f;
        RandomizePerfectZone();
    }

    void Update()
    {
        if (!isStopped)
            MoveSlider();

        // Mouse
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !isStopped)
        {
            StopAndCheck();
        }

        // Touch
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame &&
            !isStopped)
        {
            StopAndCheck();
        }
    }

    void MoveSlider()
    {
        if (moveRight)
            slider.value += speed * Time.deltaTime;
        else
            slider.value -= speed * Time.deltaTime;

        if (slider.value >= 1f)
            moveRight = false;
        else if (slider.value <= 0f)
            moveRight = true;
    }

    void StopAndCheck()
    {
        isStopped = true;
        CheckPerfect();
    }

    void CheckPerfect()
    {
        if (slider.value >= perfectMin && slider.value <= perfectMax)
        {
            //Show Perfect UI
            Debug.Log("PERFECT");

            if (perfectCount < woodUI.Length)
            {
                woodUI[perfectCount].SetActive(true);
                perfectCount++;
            }

            if (perfectCount >= woodUI.Length)
            {
                OnMiniGamePerfectComplete?.Invoke();
                gameObject.SetActive(false);
                Debug.Log("ALL PERFECT - GAME COMPLETE");
                return;
            }

            StartCoroutine(NextRound());
        }
        else
        {
            //Show Miss UI
            Debug.Log("MISS");
            StartCoroutine(Retry());
        }
    }

    IEnumerator Retry()
    {
        yield return new WaitForSeconds(retryDelay);
        ResetSlider();
    }

    IEnumerator NextRound()
    {
        yield return new WaitForSeconds(0.5f);
        ResetSlider();
    }

    void ResetSlider()
    {
        slider.value = 0f;
        moveRight = true;
        isStopped = false;

        RandomizePerfectZone();
    }


    void RandomizePerfectZone()
    {
        float center = UnityEngine.Random.Range(
                    minStart + perfectWidth / 2f,
            maxEnd - perfectWidth / 2f
        );

        perfectMin = center - perfectWidth / 2f;
        perfectMax = center + perfectWidth / 2f;

        ApplyPerfectZoneToUI();
    }

    void ApplyPerfectZoneToUI()
    {
        // Anchors control fill area perfectly (0–1)
        Vector2 min = perfectFill.anchorMin;
        Vector2 max = perfectFill.anchorMax;

        min.x = perfectMin;
        max.x = perfectMax;

        perfectFill.anchorMin = min;
        perfectFill.anchorMax = max;

        // Reset offsets (IMPORTANT)
        perfectFill.offsetMin = Vector2.zero;
        perfectFill.offsetMax = Vector2.zero;
    }
    public void ResetMiniGame()
    {
        // Reset logic state
        perfectCount = 0;
        isStopped = false;
        moveRight = true;

        // Reset slider
        slider.value = 0f;

        // Hide wood UI
        foreach (var wood in woodUI)
            wood.SetActive(false);

        // New perfect zone
        RandomizePerfectZone();
    }
}
