using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public GameObject introDialogue;
    public GameObject endingDialogue;
    public GameObject questionsParent;

    void Start()
    {
        introDialogue.SetActive(true);
        endingDialogue.SetActive(false);
        questionsParent.SetActive(false);
    }

    // Called by QuizManager
    public void OnQuizFinished()
    {
        questionsParent.SetActive(false);
        endingDialogue.SetActive(true);
    }
}
