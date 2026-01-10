using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TypeWriting : MonoBehaviour
{
    public Text textDisplay;
    [TextArea] public string[] sentences;
    public float typingSpeed = 0.05f;
    public GameObject[] playerUI;
    public GameObject dialogueUI;
    public AudioSource sound;

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

        foreach (char letter in sentences[index])
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    public void ChangeText()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            textDisplay.text = sentences[index];
            isTyping = false;
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
        
        dialogueUI.SetActive(false);
        
    }
}
