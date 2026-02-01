using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    void Start()
    {
        float master = PlayerPrefs.GetFloat("Master", 0f);
        audioMixer.SetFloat("Master", master);
        masterSlider.value = master;

        audioMixer.GetFloat("BGM", out float bgm);
        bgmSlider.value = bgm;

        audioMixer.GetFloat("SE", out float se);
        seSlider.value = se;
    }

    public void OnBGMVolumeChanged(float value)
    {
        audioMixer.SetFloat("BGM", value);
    }

    public void OnSEVolumeChanged(float value)
    {
        audioMixer.SetFloat("SE", value);
    }

    // ÉvÉåÉCÉÑÅ[Ç≤Ç∆ÇÃâπó ê›íËèÓïÒ.
    public void OnMasterVolumeChanged(float value)
    {
        audioMixer.SetFloat("Master", value);
        PlayerPrefs.SetFloat("Master", value);
    }
}