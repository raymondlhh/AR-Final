using System.Collections;
using UnityEngine;

namespace Ricimi
{
    public class BackgroundMusic : MonoBehaviour
    {
        public static BackgroundMusic Instance;

        private AudioSource audioSource;
        private Coroutine fadeRoutine;
        private bool isPlaying = false;

        private float targetVolume = 1f;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;

            targetVolume = PlayerPrefs.GetFloat("music_volume", 1f);
            audioSource.volume = 0f;
            audioSource.Stop();

            Debug.Log("[BGM] Initialized");
        }

        // =====================
        // PUBLIC API
        // =====================

        public void PlayBGM()
        {
            if (isPlaying) return;
            isPlaying = true;

            Debug.Log($"[BGM] Play → targetVolume={targetVolume}");

            StopFade();

            audioSource.volume = 0f;
            audioSource.Play();

            fadeRoutine = StartCoroutine(FadeTo(targetVolume));
        }

        public void StopBGM()
        {
            if (!isPlaying) return;
            isPlaying = false;

            Debug.Log("[BGM] Stop");

            StopFade();
            fadeRoutine = StartCoroutine(FadeTo(0f, stopAfter: true));
        }

        public void SetVolume(float volume)
        {
            targetVolume = volume;
            PlayerPrefs.SetFloat("music_volume", volume);

            // If currently playing, update volume immediately
            if (isPlaying)
            {
                audioSource.volume = volume;
            }
        }

        // =====================
        // FADE
        // =====================

        IEnumerator FadeTo(float target, bool stopAfter = false)
        {
            float start = audioSource.volume;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(start, target, t);
                yield return null;
            }

            audioSource.volume = target;

            if (stopAfter)
                audioSource.Stop();
        }

        void StopFade()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
        }
    }
}
