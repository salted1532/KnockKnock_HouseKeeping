# 0083 - QuickOutline excludeRoots (자식 렌더러 제외)

## 요청
조명 스위치: `Interactable`+`Outline`+`ChangeObjectEffect`(자식 on/off 램프 스왑). 스위치 모델만 외곽선 뜨고 안의 램프는 안 뜨게.
QuickOutline 은 `GetComponentsInChildren<Renderer>` 로 자식 전부 잡음 → 제외 기능 필요.

## 변경: `Assets/AssetsFolder/QuickOutline/Scripts/Outline.cs` (LOCAL PATCH 2)

```
[Header("Optional")]
+ [SerializeField] private Transform[] excludeRoots;   // 이 Transform 및 자식의 Renderer 제외

void Awake() {
    renderers = GetComponentsInChildren<Renderer>(true);
+   if (excludeRoots != null && excludeRoots.Length > 0)
+       renderers = System.Array.FindAll(renderers, r => !IsExcluded(r.transform));
    ...
}

+ bool IsExcluded(Transform t) {
+     foreach (var root in excludeRoots)
+         if (root != null && t.IsChildOf(root)) return true; // IsChildOf: 자기 자신도 true
+     return false;
+ }
```

- `renderers` 만 필터하면 `OnEnable`/`OnDisable` 의 외곽선 머티리얼 부착 대상에서 빠짐 → 충분.
- `LoadSmoothNormals`/`Bake` 의 MeshFilter 순회는 그대로 (제외 메쉬에 UV3 스무스노멀을 써도 외곽선 머티리얼이 안 붙으므로 무해).

## 사용
`light_switch` 의 `Outline` 인스펙터 `Exclude Roots` 에 램프 부모 Transform(또는 각 램프) 드래그.
스위치 메시가 자식이고 램프도 자식인 구조에서, 램프 쪽만 지정하면 스위치 메시는 외곽선 유지.

## 상태
2026-08-27 완료. 서드파티 로컬 패치 — 재임포트 시 날아감 ([[project_quickoutline-local-patch]], `grep "LOCAL PATCH"`).
