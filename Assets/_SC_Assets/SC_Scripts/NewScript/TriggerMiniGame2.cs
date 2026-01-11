using UnityEngine;

public class TriggerMiniGame2 : MonoBehaviour
{
    [Header("References")]
    public Pig3InteractionController pig3;
    public MixBucketTrigger mixBucket;

    [Header("Gameplay UI")]
    public GameObject joystickUI;
    public GameObject actionButtonUI;
    public GameObject miniGameUI;
    public GameObject miniGameParent;

    private bool isProcessing;
    private bool playerInZone;

    private void Start()
    {
        if (miniGameParent != null)
            miniGameParent.SetActive(false);

        miniGameUI.SetActive(false);

        // Listen to mix result
        mixBucket.OnMixSuccess += OnMixSuccess;
    }

    private void OnDestroy()
    {
        mixBucket.OnMixSuccess -= OnMixSuccess;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = true;

        TryOpenMiniGame();
    }


    private void TryOpenMiniGame()
    {
        if (!playerInZone) return;
        if (isProcessing) return;
        if (pig3.IsAutoProcessing) return;
        if (!pig3.HasRawReadyForProcessing()) return;

        OpenMiniGame();
    }

    private void OpenMiniGame()
    {
        if (miniGameParent != null)
            miniGameParent.SetActive(true);

        miniGameUI.SetActive(true);
        joystickUI.SetActive(false);
        actionButtonUI.SetActive(false);

        Debug.Log("MiniGame OPENED");
    }

    private void OnMixSuccess()
    {
        if (isProcessing) return;

        Debug.Log("Mix SUCCESS Auto Processing");

        isProcessing = true;

        pig3.ProcessAllRawFromMiniGame();

        // Close mini game immediately after success
        CloseMiniGameUI();
    }

    private void CloseMiniGameUI()
    {
        if (miniGameParent != null)
            miniGameParent.SetActive(false);

        miniGameUI.SetActive(false);
        joystickUI.SetActive(true);
        actionButtonUI.SetActive(true);
    }

    public void OnProcessingFinished()
    {
        isProcessing = false;

        // Mini-game will only reopen if:
        // player is still in zone AND has new raw
        TryOpenMiniGame();
    }
}