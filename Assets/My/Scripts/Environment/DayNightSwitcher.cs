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

    private bool isNight = true;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame)
        {
            isNight = !isNight;
            if (isNight) SetNight();
            else SetMorning();
        }
    }

    private void SetNight()
    {
        RenderSettings.skybox = nightSkybox;
        nightLight.SetActive(true);
        morningLight.SetActive(false);
        globalVolume.sharedProfile = nightProfile;
        RenderSettings.fog = true;
    }

    private void SetMorning()
    {
        RenderSettings.skybox = morningSkybox;
        morningLight.SetActive(true);
        nightLight.SetActive(false);
        globalVolume.sharedProfile = morningProfile;
        RenderSettings.fog = false;
    }
}
