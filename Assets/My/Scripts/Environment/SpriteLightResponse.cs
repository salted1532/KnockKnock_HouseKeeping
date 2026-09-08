using System.Collections.Generic;
using UnityEngine;

// URP 3D 에선 SpriteRenderer 가 씬 라이트를 안 받아 스프라이트가 늘 균일하게 밝다.
// 이 컴포넌트가 주변 광원(방 point light, 가로등, 태양 등)을 샘플해 SpriteRenderer.color 에
// 밝기를 곱해줘서, 어두운 복도에선 어둡고 불 켜진 방에 들어가면 밝아지게 한다.
// baseColor(보통 흰색)는 Awake 에 캡처하고, 비활성화되면 원본으로 되돌린다.
// 손님(Guest.prefab)의 Square 스프라이트에 붙이는 게 주 용도지만 어떤 스프라이트에도 쓸 수 있다.
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class SpriteLightResponse : MonoBehaviour
{
    [Tooltip("가장 어두운 곳에서 남는 밝기 (0 = 완전 검정)")]
    [SerializeField, Range(0f, 1f)] private float minBrightness = 0.15f;
    [Tooltip("밝기 상한 (1 = 원본 그대로)")]
    [SerializeField, Range(0.5f, 3f)] private float maxBrightness = 1f;
    [Tooltip("샘플한 광량 → 밝기 변환 배율. 방이 전체적으로 너무 어두우면 올린다")]
    [SerializeField] private float sensitivity = 1f;
    [Tooltip("밝기 재계산 간격(초). 손님이 느려서 촘촘할 필요 없음. 0 = 매 프레임")]
    [SerializeField] private float updateInterval = 0.15f;
    [Tooltip("색이 목표 밝기로 수렴하는 속도 (방 넘나들 때 팍 튀는 것 방지)")]
    [SerializeField] private float lerpSpeed = 6f;

    private SpriteRenderer sr;
    private Color baseColor;
    private Color target;
    private float sampleTimer;

    // 씬의 Light 목록 — 여러 스프라이트가 공유, 주기적으로만 갱신 (라이트가 켜졌다 꺼짐).
    private static readonly List<Light> lights = new();
    private static float lightsRefreshedAt = -999f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    private void OnEnable()
    {
        target = Sample();
        if (sr != null) sr.color = target;
    }

    private void OnDisable()
    {
        if (sr != null) sr.color = baseColor;   // 표정/스프라이트 교체 로직이 기대하는 원본으로 복구
    }

    private void LateUpdate()
    {
        sampleTimer -= Time.deltaTime;
        if (sampleTimer <= 0f)
        {
            sampleTimer = updateInterval;
            target = Sample();
        }
        sr.color = Color.Lerp(sr.color, target, Time.deltaTime * Mathf.Max(0f, lerpSpeed));
    }

    private static IReadOnlyList<Light> SceneLights()
    {
        if (Time.time - lightsRefreshedAt > 0.5f)
        {
            lightsRefreshedAt = Time.time;
            lights.Clear();
            lights.AddRange(FindObjectsByType<Light>(FindObjectsInactive.Exclude));
        }
        return lights;
    }

    private Color Sample()
    {
        Vector3 p = transform.position;
        Color acc = RenderSettings.ambientLight;

        var scene = SceneLights();
        for (int i = 0; i < scene.Count; i++)
        {
            var l = scene[i];
            if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;

            float contrib;
            switch (l.type)
            {
                case LightType.Directional:
                    contrib = l.intensity;
                    break;
                case LightType.Point:
                case LightType.Spot:
                {
                    float d = Vector3.Distance(p, l.transform.position);
                    if (d >= l.range) continue;
                    if (l.type == LightType.Spot &&
                        Vector3.Angle(l.transform.forward, p - l.transform.position) > l.spotAngle * 0.5f)
                        continue;
                    float atten = 1f - d / l.range;
                    contrib = l.intensity * atten * atten;
                    break;
                }
                default: continue;
            }
            acc += l.color * contrib;
        }

        float b = Brightness(acc, sensitivity, minBrightness, maxBrightness);
        var lit = baseColor * b;
        lit.a = baseColor.a;
        return lit;
    }

    // ponytail: 러프한 휘도 근사(Rec.601) + 클램프. 물리적으로 정확할 필요 없음, 눈대중 톤 매칭용.
    private static float Brightness(Color c, float sensitivity, float min, float max)
        => Mathf.Clamp((c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) * sensitivity, min, max);

#if UNITY_EDITOR
    [ContextMenu("Self Check")]
    private void SelfCheck()
    {
        Debug.Assert(Mathf.Approximately(Brightness(Color.black, 1f, 0.15f, 1f), 0.15f), "검정 → min");
        Debug.Assert(Mathf.Approximately(Brightness(Color.white * 5f, 1f, 0.15f, 1f), 1f), "과노출 → max");
        float mid = Brightness(new Color(0.5f, 0.5f, 0.5f), 1f, 0.15f, 1f);
        Debug.Assert(mid > 0.15f && mid < 1f, $"중간 밝기 범위 안: {mid}");
        Debug.Assert(Brightness(Color.gray, 2f, 0.15f, 1f) > Brightness(Color.gray, 1f, 0.15f, 1f), "sensitivity 높으면 더 밝음");
        Debug.Log("[SpriteLightResponse] SelfCheck OK");
    }
#endif
}
