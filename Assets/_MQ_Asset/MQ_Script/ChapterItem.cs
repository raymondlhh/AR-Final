using UnityEngine;
using UnityEngine.UI;

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
}
