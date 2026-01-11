using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderGame : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public RectTransform greenZone;

    [Header("Settings")]
    public float speed = 1.5f;
    public float successThreshold = 0.1f;

    private bool isPlaying = true;

    void Update()
    {
        if (!isPlaying) return;

        // Move slider automatically
        slider.value = Mathf.PingPong(Time.time * speed, 1f);
    }

    public void CheckResult()
    {
        float sliderValue = slider.value;

        // Convert green zone position to slider value
        float greenMin = greenZone.anchorMin.y;
        float greenMax = greenZone.anchorMax.y;

        if (sliderValue >= greenMin && sliderValue <= greenMax)
        {
            Success();
        }
        else
        {
            Fail();
        }
    }

    void Success()
    {
        isPlaying = false;
        Debug.Log("Success!");
        // TODO: Play animation, sound, strengthen house
    }

    void Fail()
    {
        Debug.Log("Fail!");
        // TODO: Weaker block, house cracks, wolf stronger
    }
}
