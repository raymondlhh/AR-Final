using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGM : MonoBehaviour
{
    public AudioSource bgmSource;

    void Awake()
    {
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
    }

    // Called when image target is detected
    public void OnTargetFound()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
            Debug.Log("bgm played");
        }
    }

    // Called when image target is lost
    public void OnTargetLost()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }
}
