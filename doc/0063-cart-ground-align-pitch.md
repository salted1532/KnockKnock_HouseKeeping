# 0063 - 카트 경사로 대응 레이캐스트 피치 정렬 스크립트

## 요청 내용
> 현재 쇼핑카트에서 진짜 쇼핑카트 미는거처럼 하려면 어떤식으로 해야할까 rigidbody에서 Freeze rotation x,y값 고정을 푸니깐 미는 각도에따라서 막 굴러다니고 그러는데 바퀴쪽으로 미는 느낌이였으면 좋겠고 경사로에도 x 회전값이 바뀌면서 경사로에 바퀴쪽 바닥이 붙어있었으면 좋겠는데
>
> 카트가 그냥 box콜리더기 때문에 레이캐스트로 피치를 부드럽게 정렬하는 스크립트를 추가해보자

## 조사 내용
- Carro_su Rigidbody(`InGame.unity`, fileID 1079720608): `m_Constraints: 48` = Freeze Rotation X + Y (Local Z만 물리 회전 자유 = 좌우 조향, 0057에서 확인된 내용)
- 카트 prefab은 로컬 X축 -90° 베이크 회전이 있어서: **Local X축(transform.right) = 바퀴 축(좌우, 변하지 않음)**, **Local Y축(transform.up) = 카트가 미는 진행 방향**, **Local Z축(transform.forward) = 평지에서 월드 업과 거의 일치**. X축 회전(피치)은 이 바퀴 축을 힌지로 진행방향-업벡터가 함께 기울어지는 것 → "경사로에서 X 회전값이 바뀐다"는 요청과 정확히 일치하는 축
- Rigidbody의 Freeze Rotation X/Y는 **물리 엔진(힘/토크)에 의한 회전만 막음** — 스크립트에서 `Rigidbody.MoveRotation()`으로 직접 회전을 지정하는 건 이 제약과 무관하게 항상 가능. 그래서 X,Y를 다시 풀지 않고 그대로 얼린 채로, 피치만 스크립트가 매 프레임 계산해서 강제로 넣어주면 물리 토크발 텀블링 없이 안정적으로 경사에 붙는 효과를 낼 수 있음
- 방식: 카트 앞/뒤 바퀴 위치에 두 개의 빈 Transform(`frontWheel`, `rearWheel`)을 두고, 각각에서 아래로 레이캐스트해서 바닥 히트 지점을 구함 → 두 히트 지점을 잇는 방향이 "지금 서 있어야 할 진행방향 기울기" → 이 방향으로 `transform.up`(현재 진행방향)을 서서히 회전시킴 (`Quaternion.FromToRotation` + `Slerp`로 부드럽게)
- 조향(Z, 물리 토크)은 기존 Push 로직 그대로 유지, 이 스크립트는 피치만 건드림

## 계획된 변경

**새 스크립트 `Assets/My/Scripts/Interaction/CartGroundAlign.cs`**
```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartGroundAlign : MonoBehaviour
{
    [SerializeField] private Transform frontWheel;
    [SerializeField] private Transform rearWheel;
    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float alignSpeed = 8f;

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void FixedUpdate()
    {
        if (frontWheel == null || rearWheel == null)
            return;

        bool frontHit = Physics.Raycast(frontWheel.position, Vector3.down, out RaycastHit fHit, rayDistance, groundMask, QueryTriggerInteraction.Ignore);
        bool rearHit = Physics.Raycast(rearWheel.position, Vector3.down, out RaycastHit rHit, rayDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (!frontHit || !rearHit)
            return;

        Vector3 targetForward = (fHit.point - rHit.point).normalized;
        // ponytail: FromToRotation은 axle(local X)이 아닌 최단 회전축을 쓰므로 아주 미세한 요/롤 드리프트가 섞일 수 있음 - 눈에 띄면 axle 축으로 프로젝션해서 보정
        Quaternion delta = Quaternion.FromToRotation(transform.up, targetForward);
        Quaternion targetRot = delta * rb.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, alignSpeed * Time.fixedDeltaTime));
    }
}
```

## 사용자가 씬/에셋에서 직접 해야 하는 일
1. Carro_su 하위에 빈 자식 오브젝트 2개 생성: 앞바퀴 위치에 `FrontWheel`, 뒷바퀴 위치에 `RearWheel` (Transform만 있으면 됨, 실제 바퀴 메쉬 위치에 맞춰서)
2. Carro_su에 `CartGroundAlign` 컴포넌트 추가, `Front Wheel`/`Rear Wheel`에 위 두 오브젝트 드래그
3. Ray Distance는 바퀴에서 바닥까지 거리보다 넉넉하게(기본 1) 조절
4. Rigidbody의 Freeze Rotation X/Y는 **그대로 켜진 상태 유지** (풀지 않음) — 스크립트가 MoveRotation으로 직접 X를 조절하기 때문

## 동작 요약
- 평지에서는 두 레이 히트 지점이 수평이라 `targetForward`가 거의 `transform.up`과 같아서 회전 변화 없음
- 경사로에 올라가면 앞/뒤 바퀴 히트 높이차가 생겨서 그 기울기만큼 부드럽게(`alignSpeed`) 피치가 따라감 — 바퀴 쪽 바닥이 항상 지면에 붙어있는 느낌
- 좌우 조향은 기존 물리 토크(Push) 그대로라 안 건드림, 롤(Y)은 계속 완전히 얼려있어서 옆으로 굴러 넘어가는 일 없음

## 별개로 남아있는 이슈
- Carro_su에 붙은 BoxCollider가 아직 실제 크기(`0.0118 x 0.0201 x 0.0188`)로 사실상 무의미함 — 이전 대화에서 논의했던 "5개 박스로 바구니 모양 만들기" 작업은 이번 스크립트와 별개로 아직 미해결 상태

## 적용 결과
계획대로 적용함. `Assets/My/Scripts/Interaction/CartGroundAlign.cs` 생성. 씬 작업(빈 오브젝트 2개 생성, 컴포넌트 추가/연결)은 사용자가 직접.
