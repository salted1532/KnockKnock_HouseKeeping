using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class DayNightSwitcher : MonoBehaviour
{
    [SerializeField] private Material nightSkybox;
    [SerializeField] private Material morningSkybox;
    [SerializeField] private GameObject nightLight;
    [SerializeField] private GameObject morningLight;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private VolumeProfile nightProfile;
    [SerializeField] private VolumeProfile morningProfile;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SetNight();
        else if (keyboard.digit2Key.wasPressedThisFrame)
            SetMorning();
    }

    private void SetNight()
    {
        RenderSettings.skybox = nightSkybox;
        nightLight.SetActive(true);
        morningLight.SetActive(false);
        globalVolume.sharedProfile = nightProfile;
    }

    private void SetMorning()
    {
        RenderSettings.skybox = morningSkybox;
        morningLight.SetActive(true);
        nightLight.SetActive(false);
        globalVolume.sharedProfile = morningProfile;
    }
}
