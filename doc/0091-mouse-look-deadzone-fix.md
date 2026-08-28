# 0091 - 마우스 미세 움직임 씹힘 (카메라 룩 데드존) 수정

## 요청
> 마우스가 미세한 움직임에 대해서 잘 안움직이는데 카메라 관련된것도 좀 확인해줄 수 있어?

## 조사
`Assets/AssetsFolder/StarterAssets/FirstPersonController/Scripts/FirstPersonController.cs`(Unity 공식 Starter Assets, 카메라/캐릭터 회전 담당) — `CameraRotation()`:

```csharp
private const float _threshold = 0.01f;
...
private void CameraRotation()
{
    if (_input.look.sqrMagnitude >= _threshold)
    {
        ...
    }
}
```

`_input.look`은 `StarterAssetsInputs.LookInput()`에 그대로 들어오는 **가공 안 된 마우스 델타(픽셀)** — 여기엔 감도 스케일링이 전혀 없고, 감도는 `RotationSpeed`(기본 1.0) 하나로 다 처리됨.

문제: `sqrMagnitude >= 0.01f` 조건은 **magnitude 기준 0.1** 데드존인데, 이건 원래 게임패드 스틱 미세 흔들림(drift)을 걸러내려고 넣은 값. 근데 마우스 입력에도 똑같이 적용돼서, 프레임당 마우스 델타가 0.1 픽셀보다 작으면(정밀하게 살짝 움직일 때 흔함, 특히 고주사율 모니터/트랙패드) **그 프레임 입력이 통째로 버려짐** — 이게 "미세한 움직임에 잘 안 움직이는" 증상의 원인. `IsCurrentDeviceMouse`는 `deltaTimeMultiplier`에만 쓰이고 이 데드존 체크엔 반영이 안 돼 있음.

Unity 커뮤니티에서도 잘 알려진 Starter Assets 결함이고, 표준적인 수정은 "데드존은 마우스가 아닐 때만 적용".

## 계획
```csharp
private void CameraRotation()
{
    // 마우스는 델타값이라 데드존 불필요 — 게임패드 스틱 흔들림 방지용 데드존은 마우스가 아닐 때만 적용
    if (_input.look.sqrMagnitude >= _threshold || IsCurrentDeviceMouse)
    {
        ...
    }
}
```
한 줄 조건 추가. 게임패드 데드존 동작은 그대로 유지, 마우스만 프레임마다 델타 그대로 반영.

## 리스크
- 낮음. 조건 완화라 기존에 통과되던 입력(큰 움직임)은 그대로 통과, 마우스의 작은 델타만 추가로 통과됨.
- **주의**: 이 스크립트는 Unity 패키지 매니저의 Starter Assets 패키지에서 가져온 벤더 코드. `Tools/Starter Assets/Reinstall Dependencies`를 다시 돌리면 이 수정이 덮어써질 수 있음([[project_quickoutline-local-patch]]와 같은 종류의 로컬 패치 주의사항).

## 결과 (2026-08-28, 승인 후 적용)
`CameraRotation()` 조건을 `_input.look.sqrMagnitude >= _threshold || IsCurrentDeviceMouse`로 변경. 게임패드는 기존 데드존 그대로, 마우스는 조건 자체를 항상 통과해서 매 프레임 델타가 그대로 반영됨.

## 검증
- 정적 확인만 완료. Unity Play 모드에서 마우스를 아주 천천히/미세하게 움직였을 때 카메라가 끊김 없이 따라오는지 확인 필요.

## 상태
2026-08-28 완료.
