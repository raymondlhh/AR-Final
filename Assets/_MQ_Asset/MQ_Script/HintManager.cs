using UnityEngine;
using UnityEngine.UI;

public class HintManager : MonoBehaviour
{
    [Header("Manager Reference")]
    public MultiImageTargetManager targetManager;

    [Header("Hint Images (UI)")]
    public GameObject pig1HintImage;
    public GameObject pig2HintImage;
    public GameObject pig3HintImage;

    void Start()
    {
        Debug.Log("[HintManager] Start → Hiding all hints");
        HideAllHints();
    }

    // Called by Hint Button
    public void OnHintButtonClicked()
    {
        Debug.Log("[HintManager] Hint button clicked");

        HideAllHints();

        if (targetManager == null)
        {
            Debug.LogWarning("[HintManager] ❌ TargetManager NOT assigned!");
            return;
        }

        int activePig = targetManager.GetCurrentActivePigIndex();
        Debug.Log("[HintManager] Active Pig Index = " + activePig);

        switch (activePig)
        {
            case 0:
                Debug.Log("[HintManager] Showing Pig 1 hint");
                if (pig1HintImage != null)
                    pig1HintImage.SetActive(true);
                else
                    Debug.LogWarning("[HintManager] Pig 1 hint image missing!");
                break;

            case 1:
                Debug.Log("[HintManager] Showing Pig 2 hint");
                if (pig2HintImage != null)
                {
                    pig2HintImage.SetActive(true);
                    Debug.Log("[HintManager] Pig 2 hint set ACTIVE");
                }

                else
                    Debug.LogWarning("[HintManager] Pig 2 hint image missing!");
                break;

            case 2:
                Debug.Log("[HintManager] Showing Pig 3 hint");
                if (pig3HintImage != null)
                    pig3HintImage.SetActive(true);
                else
                    Debug.LogWarning("[HintManager] Pig 3 hint image missing!");
                break;

            default:
                Debug.Log("[HintManager] ⚠ No active image target detected");
                break;
        }
    }

    void HideAllHints()
    {
        Debug.Log("[HintManager] Hiding all hint images");

        if (pig1HintImage != null) pig1HintImage.SetActive(false);
        if (pig2HintImage != null) pig2HintImage.SetActive(false);
        if (pig3HintImage != null) pig3HintImage.SetActive(false);
    }


}
