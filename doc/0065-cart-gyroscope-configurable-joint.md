# 0065 - CartGroundAlign 제거 후 ConfigurableJoint 기반 자이로스코프로 교체

## 요청 내용
> 현재 적용시킨건 다 뺴주고 자이로스코프 기능 넣어줄수 있나?
> x값이 완전히 0으로 가는거만 막으면 되는데 나머지 값은 괜찮은데 -120에서 -60사이에서만 기울어진
> Quaternion(-0.683013022,0.183012322,-0.183012366,0.683012664), Quaternion(-0.683012843,-0.183012635,0.183012873,0.683012605),
> Quaternion(-0.500001132,-1.92000115e-07,5.19336368e-07,0.866024792),
> Quaternion(-0.866025805,-4.25945018e-07,3.53758594e-07,0.499999404)
> 이렇게 까지만 기울어지도록 하는거야

## 조사 내용 - 쿼터니언 4개 해석
- 카트 베이크 기준 자세(요 0, X -90°)의 쿼터니언은 `(-0.7071068, 0, 0, 0.7071068)`
- 이 기준과 제시된 4개 쿼터니언 사이의 각도(`angle = 2*acos(|dot|)`)를 계산:
  - Q1 `(-0.683013, 0.183012, -0.183012, 0.683013)` → **30.06°**
  - Q2 `(-0.683013, -0.183013, 0.183013, 0.683013)` → **30.06°**
  - Q3 `(-0.500001, ~0, ~0, 0.866025)` → 순수 X축 회전, 오일러 X = **-60°** (기준에서 +30°) → **30.0°**
  - Q4 `(-0.866026, ~0, ~0, 0.499999)` → 순수 X축 회전, 오일러 X = **-120°** (기준에서 -30°) → **30.05°**
- 4개 전부 기준 자세로부터 **정확히 약 30° 떨어진 지점**. Q1/Q2는 피치+롤이 섞인 대각선 방향인데도 똑같이 30° → "X 오일러만 -60~-120으로 제한"이 아니라 **기준 자세에서 어느 방향으로든 최대 30°까지 자유롭게 기울고, 그 이상은 못 넘어가는 원뿔(cone) 제한**이 실제 요구사항
- 조향(Z축 회전, `나머지 값은 괜찮은데`)은 완전히 자유로워야 함

## 왜 ConfigurableJoint인가
- 이건 Unity 물리 엔진에 이미 내장된 **swing-twist 관절 제한** 기능과 정확히 같은 문제(하나의 축은 자유 회전(twist)=조향, 나머지 두 축은 원뿔로 제한(swing)=기울기)
- 커스텀 수학(스윙-트위스트 분해)을 직접 짜는 것보다 `ConfigurableJoint`를 코드로 설정해서 물리 엔진이 직접 제한을 계산하게 하는 게 훨씬 안정적이고 코드도 짧음 (관절 자체가 물리 스텝에 맞춰 정확하게 처리)

## 계획된 변경

**1. `CartGroundAlign.cs` 삭제** (스크립트 + `.meta`), 씬에서 컴포넌트 참조 제거
- Carro_su에 붙어있던 `CartGroundAlign` 컴포넌트(현재 비활성화된 상태) 블록과 `m_AddedComponents` 목록 항목 삭제
- (참고: 앞서 만들어뒀던 FrontWheel/RearWheel 빈 오브젝트는 더는 안 쓰지만 삭제 요청은 없었으니 씬에 그대로 둠)

**2. Carro_su Rigidbody 회전 제약 해제**
```diff
-  m_Constraints: 48
+  m_Constraints: 0
```
(Freeze Rotation X/Y를 풀어서 물리가 자유롭게 회전시킬 수 있게 함 — 대신 아래 Joint가 한계를 잡아줌)

**3. 새 스크립트 `Assets/My/Scripts/Interaction/CartStabilizerJoint.cs`**
```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartStabilizerJoint : MonoBehaviour
{
    [SerializeField] private float maxTiltAngle = 30f;

    private void Awake()
    {
        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = null;
        joint.axis = Vector3.forward;    // local Z = 조향(요) 축 -> twist로 자유롭게
        joint.secondaryAxis = Vector3.right;

        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

        joint.angularXMotion = ConfigurableJointMotion.Free;     // twist = 조향, 무제한
        joint.angularYMotion = ConfigurableJointMotion.Limited;  // swing1
        joint.angularZMotion = ConfigurableJointMotion.Limited;  // swing2

        SoftJointLimit swing = new SoftJointLimit { limit = maxTiltAngle };
        joint.angularYLimit = swing;
        joint.angularZLimit = swing;
    }
}
```
- `connectedBody = null`이면 Joint를 붙이는 시점(Awake, 즉 카트의 원래 베이크 자세)이 자동으로 "기준(0°)" 자세가 됨 — 우리가 계산한 30° 기준점과 정확히 일치
- Position(X/Y/Z)은 다 Free라 카트 이동 자체는 전혀 제약 없음, 오직 회전만 관여
- `axis = local Z`로 지정해서 Angular X Motion(twist)이 조향 축이 되고, 나머지 두 축(swing1/swing2)이 합쳐져서 "기준에서 30° 원뿔" 제한을 만듦

## 사용자가 씬/에셋에서 직접 해야 하는 일
1. Carro_su에서 (이미 비활성화되어 있던) **CartGroundAlign 컴포넌트 삭제** 확인 (스크립트가 없어졌으니 Missing Script로 뜰 수 있음 → 있으면 지우기)
2. Carro_su에 **CartStabilizerJoint** 컴포넌트 추가 (Max Tilt Angle 기본 30, 필요하면 조절)

## 동작 요약
- 평소엔 중력/미는 힘으로 자유롭게 흔들리다가, 기준 자세에서 30°를 넘어가려 하면 Joint가 자연스럽게 밀어냄 → 완전히 뒤집히거나 눕는 일 없이 "말랑하게 흔들리는" 자이로스코프 느낌
- 조향(옆으로 미는 토크)은 그대로 자유롭게 작동

## 적용 결과
계획대로 적용함. `CartGroundAlign.cs`(+ .meta) 삭제, 씬에서 컴포넌트/목록 참조 제거. Rigidbody `m_Constraints`는 확인해보니 이미 0(사용자가 테스트 중 직접 풀어둔 상태)이라 별도 변경 없음. `Assets/My/Scripts/Interaction/CartStabilizerJoint.cs` 생성.
