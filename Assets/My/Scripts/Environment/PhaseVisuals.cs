using System;
using UnityEngine;
using UnityEngine.Rendering;

// 시간대별 조명/스카이박스/볼륨/fog 를 스왑한다. (구 DayNightSwitcher 대체)
// DayPhaseManager.OnPhaseChanged (암전 시점) 를 구독해 즉시 적용 — 페이드에 가려짐.
public class PhaseVisuals : MonoBehaviour
{
    [Serializable]
    public struct PhaseLook
    {
        public Material skybox;
        public GameObject lightRoot;   // 이 단계에서 켤 라이트 묶음 (나머지 단계 것은 꺼짐)
        public VolumeProfile volume;
        public bool fog;
    }

    [SerializeField] private Volume globalVolume;
    [Tooltip("Morning, Noon, Evening, Dawn 순서 4개. 아침/점심이 같은 값을 가리켜도 됨")]
    [SerializeField] private PhaseLook[] looks = new PhaseLook[4];

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
        {
            DayPhaseManager.Instance.OnPhaseChanged += Apply;
            Apply(DayPhaseManager.Instance.Current);
        }
        else Debug.LogWarning("[PhaseVisuals] DayPhaseManager 없음 — 시간대 비주얼 안 바뀜", this);
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= Apply;
    }

    private void Apply(DayPhase phase)
    {
        int i = (int)phase;
        if (looks == null || i >= looks.Length)
        {
            Debug.LogWarning($"[PhaseVisuals] looks 배열이 4개 미만 — {phase} 적용 못 함", this);
            return;
        }

        var look = looks[i];

        if (look.skybox != null) RenderSettings.skybox = look.skybox;
        RenderSettings.fog = look.fog;
        if (globalVolume != null && look.volume != null) globalVolume.sharedProfile = look.volume;

        // 모든 단계의 lightRoot 를 끄고 현재 것만 켠다 (중복 참조는 마지막 승자)
        foreach (var l in looks)
            if (l.lightRoot != null) l.lightRoot.SetActive(false);
        if (look.lightRoot != null) look.lightRoot.SetActive(true);
    }
}
