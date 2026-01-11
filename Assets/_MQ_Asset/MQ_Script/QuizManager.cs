using UnityEngine;

public class QuizManager : MonoBehaviour
{
    public GameObject[] questions;

    private int currentQuestionIndex = 0;

    public GameFlowManager flowManager;


    void Start()
    {
        ShowQuestion(0);
    }

    public void CorrectAnswer()
    {
        AnswerButton.ResetLock();
        NextQuestion();
    }


    void NextQuestion()
    {
        questions[currentQuestionIndex].SetActive(false);
        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Length)
        {
            questions[currentQuestionIndex].SetActive(true);
        }
        else
        {
            Debug.Log("Quiz Finished");

            // ✅ Tell GameFlowManager quiz is done
            flowManager.OnQuizFinished();
        }
    }

        void ShowQuestion(int index)
    {
        for (int i = 0; i < questions.Length; i++)
        {
            questions[i].SetActive(i == index);
        }
    }
}
