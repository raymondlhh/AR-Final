using UnityEngine;

namespace Ricimi
{
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance;

        private AudioSource audioSource;
        private float sfxVolume = 1f;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;

            sfxVolume = PlayerPrefs.GetFloat("sfx_volume", 1f);
            audioSource.volume = sfxVolume;

            Debug.Log("[SFX] Initialized");
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            audioSource.PlayOneShot(clip, sfxVolume);
        }

        public void SetVolume(float volume)
        {
            sfxVolume = volume;
            audioSource.volume = volume;
            PlayerPrefs.SetFloat("sfx_volume", volume);
        }
    }
}
