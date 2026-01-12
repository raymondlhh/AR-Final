using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class TypeWriting : MonoBehaviour
{
    public Text textDisplay;
    [TextArea] public string[] sentences;
    public float typingSpeed = 0.05f;
    public GameObject[] playerUI;
    public GameObject dialogueUI;
    public AudioSource sound;

    [Header("Optional Scene Load")]
    public bool loadSceneAfterDialogue = false;
    public string sceneName;
    public float sceneLoadDelay = 1.5f;


    int index;
    bool isTyping;
    Coroutine typingCoroutine;

    void OnEnable()
    {
        index = 0;
        textDisplay.text = "";
        isTyping = false;

        foreach (GameObject ui in playerUI)
        {
            ui.SetActive(false);
        }

        PlaySentence();
    }

    void PlaySentence()
    {
        if (sound != null)
            sound.Play();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeSentence());
    }

    IEnumerator TypeSentence()
    {
        isTyping = true;
        textDisplay.text = "";

        string sentence = sentences[index];
        int i = 0;

        while (i < sentence.Length)
        {
            // If this is a rich text tag
            if (sentence[i] == '<')
            {
                int tagEnd = sentence.IndexOf('>', i);

                if (tagEnd != -1)
                {
                    // Add the entire tag instantly
                    textDisplay.text += sentence.Substring(i, tagEnd - i + 1);
                    i = tagEnd + 1;
                }
            }
            else
            {
                // Normal visible character
                textDisplay.text += sentence[i];
                i++;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
        sound.Stop();
    }

    public void ChangeText()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            textDisplay.text = sentences[index];
            isTyping = false;
            sound.Stop();
            return;
        }
        if (index < sentences.Length - 1)
        {
            index++;
            PlaySentence();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        foreach (GameObject ui in playerUI)
        {
            ui.SetActive(true);
        }

        // ✅ START coroutine FIRST (while object is active)
        if (loadSceneAfterDialogue)
        {
            StartCoroutine(LoadSceneRoutine());
        }

        // ❌ Disable dialogue AFTER coroutine started
        dialogueUI.SetActive(false);
    }

    private IEnumerator LoadSceneRoutine()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(sceneName);
    }

}
