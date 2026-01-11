using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnswerButton : MonoBehaviour
{
    public bool isCorrect;

    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    private Image image;
    private Color normalColor;

    private static bool isLocked = false;
    private QuizManager quizManager;

    public AudioSource audioSource;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;


    void Awake()
    {
        image = GetComponent<Image>();
        normalColor = image.color;
        quizManager = FindObjectOfType<QuizManager>();

        GetComponent<Button>().onClick.AddListener(OnAnswerClicked);
    }

    void OnAnswerClicked()
    {
        if (isLocked) return;
        StartCoroutine(HandleAnswer());
    }

    IEnumerator HandleAnswer()
    {
        isLocked = true;
        LockAllButtons(true);

        //image.color = isCorrect ? correctColor : wrongColor;
        if (isCorrect)
        {
            image.color = correctColor;
            audioSource.PlayOneShot(correctSFX);
        }
        else
        {
            image.color = wrongColor;
            audioSource.PlayOneShot(wrongSFX);
        }

        yield return new WaitForSeconds(3f);

        if (isCorrect)
        {
            quizManager.CorrectAnswer();
        }
        else
        {
            image.color = normalColor;
            isLocked = false;
            LockAllButtons(false);
        }
    }

    void LockAllButtons(bool lockState)
    {
        AnswerButton[] buttons = FindObjectsOfType<AnswerButton>();
        foreach (AnswerButton btn in buttons)
        {
            btn.GetComponent<Button>().interactable = !lockState;
        }
    }

    public static void ResetLock()
    {
        isLocked = false;
    }

}
