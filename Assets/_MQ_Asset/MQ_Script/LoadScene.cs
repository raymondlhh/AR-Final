using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class LoadScene : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneName;
    public float delay = 2f;

    public void Load()
    {
        StartCoroutine(LoadSceneRoutine());
    }

    public IEnumerator LoadSceneRoutine()
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
