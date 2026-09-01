using UnityEngine;

// 상호작용 시 UI 모드(화면고정) 진입 — anchor 포즈로 카메라 고정 + 마우스 표시. 모니터 등.
// 토글: 이미 이 anchor 뷰가 최상위면 다시 상호작용 시 화면고정 해제(한 겹 pop).
// 저녁에만 쓰려면 같은 오브젝트에 PhaseCondition 추가.
public class EnterUIModeEffect : InteractionEffect
{
    [Tooltip("플레이어가 이동해 고정될 위치 + 바라볼 정면 (Player_Anchor). Y 회전만 사용, 비우면 이 오브젝트")]
    [SerializeField] private Transform anchor;
    [Tooltip("이미 이 뷰면 다시 상호작용 시 해제. 끄면 진입만")]
    [SerializeField] private bool toggle = true;

    public override void Play(in InteractionContext ctx)
    {
        var uim = UIInteractionMode.Instance;
        if (uim == null) return;

        var a = anchor != null ? anchor : transform;
        if (toggle && uim.IsTopAnchor(a))
            uim.Exit();          // 모니터 다시 클릭 = 화면고정 풀기
        else
            uim.Enter(a);
    }
}
