using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MixBucketTrigger : MonoBehaviour
{
    [Header("Bucket Visuals")]
    public GameObject clayVisual;
    public GameObject sandVisual;
    public GameObject waterVisual;
    public event Action OnMixSuccess;
    public event Action OnMixFail;

    private List<string> inputSequence = new List<string>();

    private readonly List<string> correctSequence = new List<string>
    {
        "Clay",
        "Sand",
        "Water"
    };

    private bool isProcessing;

    private void OnTriggerEnter(Collider other)
    {
        if (isProcessing) return;

        string tag = other.tag;

        // Only accept these ingredients
        if (!correctSequence.Contains(tag)) return;

        // Ignore duplicates
        if (inputSequence.Contains(tag))
        {
            Destroy(other.gameObject);
            return;
        }

        // Save order
        inputSequence.Add(tag);

        // Activate visual
        if (tag == "Clay") clayVisual.SetActive(true);
        if (tag == "Sand") sandVisual.SetActive(true);
        if (tag == "Water") waterVisual.SetActive(true);

        // Consume ingredient
        Destroy(other.gameObject);

        // ONLY check when all 3 are added
        if (inputSequence.Count == 3)
        {
            StartCoroutine(CheckSequence());
        }
    }

    IEnumerator CheckSequence()
    {
        isProcessing = true;

        yield return new WaitForSeconds(0.5f); // small pause for feedback

        bool isCorrect = true;

        for (int i = 0; i < 3; i++)
        {
            if (inputSequence[i] != correctSequence[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            Debug.Log("Correct Sequence!");
            OnMixSuccess?.Invoke();
            //play correct sfx
            // trigger mix / brick creation here
        }
        else
        {
            Debug.Log("Wrong Sequence!");
            OnMixFail?.Invoke();

            //play sfx wrong audio
        }

        yield return new WaitForSeconds(0.8f);

        ResetBucket();
    }

    void ResetBucket()
    {
        inputSequence.Clear();

        clayVisual.SetActive(false);
        sandVisual.SetActive(false);
        waterVisual.SetActive(false);

        isProcessing = false;
    }
}