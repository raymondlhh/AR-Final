using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class ChapterItem : MonoBehaviour
{
    [Header("Scene to load on confirm")]
    public string sceneName;

    private Outline outline;

    void Awake()
    {
        outline = GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false; // start OFF
    }

    public void Select()
    {
        if (outline != null)
            outline.enabled = true;

        Debug.Log("[ChapterItem] Selected " + sceneName);
    }

    public void Deselect()
    {
        if (outline != null)
            outline.enabled = false;
    }

    // ✅ LOAD SCENE
    public void LoadChapter()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log("[ChapterItem] Loading scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is empty!");
        }
    }
}
