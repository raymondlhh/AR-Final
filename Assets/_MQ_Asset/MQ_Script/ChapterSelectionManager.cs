using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterSelectionManager : MonoBehaviour
{
    public static ChapterSelectionManager Instance;

    private ChapterItem selectedChapter;

    void Awake()
    {
        Instance = this;
    }

    // Called by CHAPTER BUTTONS
    public void SelectChapter(ChapterItem chapter)
    {
        if (chapter == null)
        {
            Debug.LogError("[Chapter] SelectChapter received NULL");
            return;
        }

        // Remove previous outline
        if (selectedChapter != null)
            selectedChapter.Deselect();

        // Select new one
        selectedChapter = chapter;
        selectedChapter.Select();

        Debug.Log("[Chapter] Selected: " + chapter.sceneName);
    }

    // Called by CONFIRM BUTTON ONLY
    public void ConfirmSelection()
    {
        if (selectedChapter == null)
        {
            Debug.Log("[Chapter] No chapter selected");
            return;
        }

        Debug.Log("[Chapter] Loading scene: " + selectedChapter.sceneName);
        SceneManager.LoadScene(selectedChapter.sceneName);
    }
}
