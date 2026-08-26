# 0067 - 자이로스코프(Joint) 제거, 4바퀴 레이캐스트 정렬로 복귀

## 요청 내용
> 그럼 레이캐스트 방식으로 한다고 하면 바퀴 부분을 4개의 핀으로 하고(박스를 기준) 각 핀별로 경사로를 만나게 될시 나머지 핀 위치까지 고려해서 각도를 조정하도록 하는거야
> 왼쪽위 오른쪽위 오른쪽아래 왼쪽아래 이런식으로 핀을 4개를 구성하고(실제 바퀴 위치처럼)

## 배경
- [[0065-cart-gyroscope-configurable-joint]]의 ConfigurableJoint 방식은 뒤집히는 건 막지만, 30° 범위 안에서는 미는 힘/접촉 반작용에도 반응해서 기울어짐 — "미는 건 절대 안 기울고 오직 경사로 지형에만 반응"이라는 요청과 안 맞음
- 이전 2점(앞/뒤) 레이캐스트 방식([[0063-cart-ground-align-pitch]])은 Rigidbody X/Y를 완전히 얼려서 물리 힘에 전혀 반응 안 하고 스크립트만 피치를 결정 → 이게 원하는 동작과 일치. 이번엔 앞/뒤 2점이 아니라 **4바퀴 지점**으로 확장해서 피치(앞뒤 기울기)뿐 아니라 롤(좌우 기울기, 대각선 경사 등)까지 지형에 맞게 반영

## 조사 내용
- 4개 지점(FrontLeft, FrontRight, RearLeft, RearRight)에서 각각 바닥으로 레이캐스트 → 4개의 접지 지점을 얻음
- **오른쪽 방향 벡터**: 오른쪽 두 점 평균 − 왼쪽 두 점 평균
- **앞 방향 벡터**: 앞쪽 두 점 평균 − 뒤쪽 두 점 평균
- 이 두 벡터를 외적(Cross)하면 "지금 지형이 만드는 바닥면의 법선(normal)" = 카트가 향해야 할 목표 업(up) 벡터가 나옴 — 앞뒤 차이와 좌우 차이를 한 번에 반영하니까 피치+롤이 동시에 계산됨
- 카트는 로컬 Z축(`transform.forward`)이 평지에서 월드 업과 거의 일치(베이크된 -90° X 회전 때문, 0057/0063에서 확인) → `Quaternion.FromToRotation(transform.forward, targetUp)`으로 정렬 회전 계산
- 지난번 2점 방식에서 `FromToRotation`이 축을 임의로 골라서 드리프트가 생겼던 문제([[0064-cart-align-axle-axis-fix]])는, 그때는 "피치만 원하는데 두 축(Y·Z)이 포함된 벡터"를 정렬시켜서 발생한 것 — 이번엔 애초에 **"업(Z) 축을 목표 노멀에 맞추고 요(twist)는 자동으로 보존"**하는 게 목적이라 `FromToRotation`이 원래 하는 일(요 축 성분은 절대 건드리지 않고 나머지만 정렬)과 정확히 일치함 → 별도 축 고정 없이 그대로 써도 드리프트 없음
- Rigidbody 회전 제약을 다시 X/Y 프리즈로 되돌려서 물리 힘(미는 힘, 충돌 반작용)이 절대 피치/롤에 영향 못 주게 막음 — 오직 이 스크립트의 `MoveRotation`만 회전을 바꿈

## 계획된 변경

**1. `CartStabilizerJoint.cs` 삭제** + 씬에서 컴포넌트 참조 제거

**2. Carro_su Rigidbody 회전 제약 복원**
```diff
-  m_Constraints: 0
+  m_Constraints: 48
```

**3. 새 스크립트 `Assets/My/Scripts/Interaction/CartGroundAlign.cs`**
```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartGroundAlign : MonoBehaviour
{
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform rearLeft;
    [SerializeField] private Transform rearRight;
    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float alignSpeed = 8f;

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void FixedUpdate()
    {
        if (!TryGetGroundPoint(frontLeft, out Vector3 fl) || !TryGetGroundPoint(frontRight, out Vector3 fr) ||
            !TryGetGroundPoint(rearLeft, out Vector3 rl) || !TryGetGroundPoint(rearRight, out Vector3 rr))
            return;

        Vector3 rightEdge = (fr + rr) * 0.5f - (fl + rl) * 0.5f;
        Vector3 forwardEdge = (fl + fr) * 0.5f - (rl + rr) * 0.5f;
        Vector3 targetUp = Vector3.Cross(forwardEdge, rightEdge).normalized;

        Quaternion delta = Quaternion.FromToRotation(transform.forward, targetUp);
        Quaternion targetRot = delta * rb.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, alignSpeed * Time.fixedDeltaTime));
    }

    private bool TryGetGroundPoint(Transform wheel, out Vector3 point)
    {
        point = default;
        if (wheel == null)
            return false;
        if (!Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return false;
        point = hit.point;
        return true;
    }
}
```
- ponytail: `targetUp`이 뒤집힌 방향으로 나오면(카트가 반대로 눕는 것처럼 보이면) `Vector3.Cross(forwardEdge, rightEdge)`를 `Vector3.Cross(rightEdge, forwardEdge)`로 부호만 뒤집으면 됨 — 실제로 돌려보기 전엔 좌표계 방향을 100% 장담 못 함

## 사용자가 씬/에셋에서 직접 해야 하는 일
1. Carro_su에서 CartStabilizerJoint 컴포넌트 삭제 (ConfigurableJoint는 Awake에서 자동으로 붙는 거라 별도로 안 지워도 되지만, Play 모드 아닐 땐 안 붙어있으니 신경 안 써도 됨)
2. Carro_su 하위에 빈 자식 오브젝트 4개 생성해서 실제 바퀴 위치에 배치: `FrontLeft`, `FrontRight`, `RearLeft`, `RearRight` (기존에 만들어뒀던 FrontWheel/RearWheel은 그대로 두거나 지워도 무방, 이번엔 안 씀)
3. Carro_su에 `CartGroundAlign` 컴포넌트 추가, 4개 Transform 연결
4. Play 모드로 돌려서 targetUp 방향이 뒤집혀 나오면 위 ponytail 메모대로 Cross 순서만 바꿔서 재적용 요청

## 동작 요약
- 평지에선 4개 접지 지점이 수평이라 변화 없음
- 경사로에 대각선으로 걸치거나 앞/뒤/좌/우 어느 조합으로 기울어도 4점이 만드는 바닥면에 맞춰 피치+롤이 자연스럽게 따라붙음
- Rigidbody X/Y가 다시 완전히 얼어있어서 미는 힘/충돌로는 절대 기울지 않음, 오직 이 스크립트(지형 감지)만 피치/롤을 바꿈
- 조향(Z, 물리 토크)과 미는 힘은 기존 그대로

## 적용 결과
계획대로 적용함. `CartStabilizerJoint.cs`(+ .meta) 삭제, 씬 컴포넌트/목록 참조 제거. Carro_su Rigidbody `m_Constraints` 48로 복원. `Assets/My/Scripts/Interaction/CartGroundAlign.cs`(4바퀴 버전) 생성. 씬 작업(빈 오브젝트 4개, 컴포넌트 추가/연결)은 사용자가 직접.
