using UnityEngine;
using UnityEngine.UI;

namespace Ricimi
{
    public class MusicManager : MonoBehaviour
    {
        private Slider slider;
        private BackgroundMusic bgm;

        void Start()
        {
            slider = GetComponent<Slider>();
            bgm = BackgroundMusic.Instance;

            // Slider setup
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            float savedVolume = PlayerPrefs.GetFloat("music_volume", 1f);
            slider.value = savedVolume;

            slider.onValueChanged.AddListener(OnSliderChanged);
        }

        void OnSliderChanged(float value)
        {
            Debug.Log($"[MusicManager] Volume = {value}");
            bgm.SetVolume(value);
        }
    }
}
