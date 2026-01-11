using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public GameObject introDialogue;
    public GameObject endingDialogue;
    public GameObject questionsParent;

    void Start()
    {
        introDialogue.SetActive(false);
        endingDialogue.SetActive(false);
        questionsParent.SetActive(false);
    }

    public void OnTargetReached()
    {
        introDialogue.SetActive(true);
    }


    // Called by QuizManager
    public void OnQuizFinished()
    {
        questionsParent.SetActive(false);
        endingDialogue.SetActive(true);
    }
}
