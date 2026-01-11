using UnityEngine;

public class TriggerMiniGame : MonoBehaviour
{
    [Header("References")]
    public MiniGameSlider miniGame;              // Mini-game UI
    public Pig2InteractionController pig2;        // Pig controller


    [Header("Gameplay UI")]
    public GameObject joystickUI;
    public GameObject actionButtonUI;
    public GameObject miniGameUI;
    public GameObject woodLog;

    private bool isMiniGameActive = false;

    private void Start()
    {
        if (miniGame != null)
            miniGame.gameObject.SetActive(false);
            miniGameUI.SetActive(false);
        woodLog.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (pig2.IsAutoProcessing)
        {
            Debug.Log("Mini-game locked: processing in progress");
            return;
        }

        if (!pig2.HasRawReadyForProcessing())
        {
            Debug.Log("Mini-game locked: no raw material");
            return;
        }

        if (isMiniGameActive) return;

        OpenMiniGame();
    }

    private void OpenMiniGame()
    {
        miniGame.ResetMiniGame();

        isMiniGameActive = true;

        miniGame.gameObject.SetActive(true);
        miniGameUI.SetActive(true);
        woodLog.SetActive(true);

        joystickUI.SetActive(false);
        actionButtonUI.SetActive(false);

        miniGame.OnMiniGamePerfectComplete -= OnMiniGameComplete;
        miniGame.OnMiniGamePerfectComplete += OnMiniGameComplete;

        Debug.Log("Mini-game started");
    }

    //private void OnTriggerExit(Collider other)
    //{
    //    if (!other.CompareTag("Player")) return;

    //    // Optional: close mini-game when leaving
    //    miniGame.gameObject.SetActive(false);

    //    // Clean up subscription
    //    miniGame.OnMiniGamePerfectComplete -= OnMiniGameComplete;
    //}

    private void OnMiniGameComplete()
    {
        Debug.Log("Mini-game PERFECT Processing raw material");

        // Tell Pig2 to process ONE raw automatically
        pig2.ProcessAllRawFromMiniGame();
        CloseMiniGame();
    }

    private void CloseMiniGame()
    {
        miniGame.gameObject.SetActive(false);
        miniGameUI.SetActive(false);
        woodLog.SetActive(false);

        joystickUI.SetActive(true);
        actionButtonUI.SetActive(true);

        isMiniGameActive = false;

        miniGame.OnMiniGamePerfectComplete -= OnMiniGameComplete;
    }
}
