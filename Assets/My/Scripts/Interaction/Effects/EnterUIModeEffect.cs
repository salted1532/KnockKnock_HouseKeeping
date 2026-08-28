using UnityEngine;

// 상호작용 시 UI 모드 진입 (책상 접객 등). anchor 포즈로 카메라 고정 + 마우스 표시.
// 저녁에만 쓰려면 같은 오브젝트에 PhaseCondition 추가.
public class EnterUIModeEffect : InteractionEffect
{
    [Tooltip("플레이어가 이동해 고정될 위치 + 바라볼 정면 (Player_Anchor). Y 회전만 사용, 비우면 이 오브젝트")]
    [SerializeField] private Transform anchor;

    public override void Play(in InteractionContext ctx)
    {
        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.Enter(anchor != null ? anchor : transform);
    }
}
