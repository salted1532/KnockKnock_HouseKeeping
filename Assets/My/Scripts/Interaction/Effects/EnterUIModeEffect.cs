using UnityEngine;

// 상호작용 시 UI 모드 진입 (책상 접객 등). anchor 포즈로 카메라 고정 + 마우스 표시.
// 저녁에만 쓰려면 같은 오브젝트에 PhaseCondition 추가.
public class EnterUIModeEffect : InteractionEffect
{
    [Tooltip("카메라가 이동할 위치/방향 (앉은 시점)")]
    [SerializeField] private Transform anchor;

    public override void Play(in InteractionContext ctx)
    {
        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.Enter(anchor != null ? anchor : transform);
    }
}
