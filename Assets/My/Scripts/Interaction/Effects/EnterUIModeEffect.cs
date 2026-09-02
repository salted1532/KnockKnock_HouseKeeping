using UnityEngine;

// 상호작용 시 화면고정(UI 모드) 진입 — anchor 포즈로 카메라 고정 + 마우스 표시.
// 연출용: 한 번 들어가면 클릭으로 못 나오고, exitKey(Backspace) 홀드로만 나온다 (접객·노크와 동일).
// 저녁에만 쓰려면 같은 오브젝트에 PhaseCondition 추가.
// 가볍게 보다 마는 뷰(클릭/ESC 로 바로 나오는 것)는 MonitorViewEffect 를 쓴다.
public class EnterUIModeEffect : InteractionEffect
{
    [Tooltip("플레이어가 이동해 고정될 위치 + 바라볼 정면 (Player_Anchor). Y 회전만 사용, 비우면 이 오브젝트")]
    [SerializeField] private Transform anchor;

    public override void Play(in InteractionContext ctx)
    {
        var uim = UIInteractionMode.Instance;
        if (uim == null) return;

        var a = anchor != null ? anchor : transform;
        uim.Enter(a, 1f, escExits: false);   // 연출 = 홀드로만 나감
    }
}
