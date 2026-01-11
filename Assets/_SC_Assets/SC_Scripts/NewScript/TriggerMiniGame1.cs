using UnityEngine;

public class TriggerMiniGame1 : MonoBehaviour
{
    [Header("References")]
    public ToolSpawner toolspawner;
    public PlayerInteractionController pig1;

    [Header("Gameplay UI")]
    public GameObject joystickUI;
    public GameObject actionButtonUI;
    //public GameObject miniGameUI;
    public GameObject miniGameParent;

    private bool isMiniGameActive = false;

    private void Start()
    {
        if (miniGameParent != null)
            miniGameParent.SetActive(false);

        //if (miniGameUI != null)
        //    miniGameUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isMiniGameActive) return;

        if (pig1.IsAutoProcessing)
        {
            Debug.Log("Mini-game locked: processing in progress");
            return;
        }

        if (!pig1.HasRawReadyForProcessing())
        {
            Debug.Log("Mini-game blocked: no raw material left");
            return;
        }

        OpenMiniGame();
    }
    private void OnEnable()
    {
        ToolSpawner.OnHayCrafted += OnMiniGameCompleted;
    }

    private void OnDisable()
    {
        ToolSpawner.OnHayCrafted -= OnMiniGameCompleted;
    }
    private void OpenMiniGame()
    {
        isMiniGameActive = true;

        miniGameParent.SetActive(true);
        //miniGameUI.SetActive(true);

        joystickUI.SetActive(false);
        actionButtonUI.SetActive(false);

        Debug.Log("Mini-game started");
    }

    public void OnMiniGameCompleted()
    {
        if (!isMiniGameActive) return;

        Debug.Log("Mini-game completed");

        pig1.StartAutoProcessing();
        CloseMiniGame();
    }

    private void CloseMiniGame()
    {
        miniGameParent.SetActive(false);
        //miniGameUI.SetActive(false);

        joystickUI.SetActive(true);
        actionButtonUI.SetActive(true);

        isMiniGameActive = false;

        Debug.Log("Mini-game closed");

    }

}
