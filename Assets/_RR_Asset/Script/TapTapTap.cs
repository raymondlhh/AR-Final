using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TapTapTap : MonoBehaviour
{
    [Header("UI")]
    public Image circularFill;
    public Text tapText;
    public GameObject SuccessUI;
    public bool Successful = false;


    [Header("Settings")]
    public int tapsRequired = 10;
    public float decayRateMultiplier = 0.7f; // < 1 = slower than fill
    public float timeLimit = 10f;             // hidden timer (seconds)

    float currentFill = 0f;
    float fillPerTap;
    float decayRate;

    bool isActive = true;

    public AudioSource winSound;

    void Start()
    {
        fillPerTap = 1f / tapsRequired;
        decayRate = fillPerTap * decayRateMultiplier;
        SuccessUI.SetActive(false);

        UpdateUI();
    }

    void Update()
    {
        if (!isActive) return;

        // Decay only if not full
        if (currentFill < 1f)
        {
            currentFill -= decayRate * Time.deltaTime;
            currentFill = Mathf.Clamp01(currentFill);

            UpdateUI();
        }
    }

    public void RegisterTap()
    {
        if (!isActive) return;

        currentFill += fillPerTap;
        currentFill = Mathf.Clamp01(currentFill);

        UpdateUI();

        // Success check
        if (currentFill >= 1f)
        {
            Success();
        }
    }

    void UpdateUI()
    {
        circularFill.fillAmount = currentFill;

        int remainingTaps = Mathf.CeilToInt((1f - currentFill) / fillPerTap);
        tapText.text = remainingTaps.ToString();
    }

    void Success()
    {
        isActive = false;
        winSound.Play();
        circularFill.fillAmount = 1f;
        tapText.text = "0";

        Debug.Log("House reinforced!");
        SuccessUI.SetActive(true);  
        Successful = true;
        gameObject.SetActive(false);
    }
}
