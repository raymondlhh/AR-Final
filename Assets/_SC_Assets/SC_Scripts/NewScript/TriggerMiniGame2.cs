using UnityEngine;

public class TriggerMiniGame2 : MonoBehaviour
{
    [Header("References")]
    public Pig3InteractionController pig3;
    public MixBucketTrigger mixBucket;

    [Header("Gameplay UI")]
    public GameObject joystickUI;
    public GameObject actionButtonUI;
    public GameObject miniGameParent;

    private bool isMiniGameActive;

    private void Start()
    {
        if (miniGameParent != null)
            miniGameParent.SetActive(false);
    }

    private void OnEnable()
    {
        mixBucket.OnMixSuccess += OnMiniGameCompleted;
    }

    private void OnDisable()
    {
        mixBucket.OnMixSuccess -= OnMiniGameCompleted;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isMiniGameActive) return;

        if (pig3.IsAutoProcessing)
        {
            Debug.Log("Mini-game locked: auto processing");
            return;
        }

        if (!pig3.HasWorkToProcess())
        {
            Debug.Log("Mini-game blocked: no work to process");
            return;
        }

        OpenMiniGame();
    }

    private void OpenMiniGame()
    {
        isMiniGameActive = true;

        miniGameParent.SetActive(true);
        joystickUI.SetActive(false);
        actionButtonUI.SetActive(false);

        Debug.Log("Pig3 Mini-game OPENED");
    }

    private void OnMiniGameCompleted()
    {
        if (!isMiniGameActive) return;

        Debug.Log("Pig3 Mini-game COMPLETED  auto processing");

        pig3.ProcessAllRawFromMiniGame();
        CloseMiniGame();
    }

    private void CloseMiniGame()
    {
        miniGameParent.SetActive(false);
        joystickUI.SetActive(true);
        actionButtonUI.SetActive(true);

        isMiniGameActive = false;

        Debug.Log("Pig3 Mini-game CLOSED");
    }
}