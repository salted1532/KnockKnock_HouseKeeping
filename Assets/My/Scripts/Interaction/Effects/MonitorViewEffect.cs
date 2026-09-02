using UnityEngine;

// 모니터처럼 "가볍게 보다 마는" 화면고정 — 클릭으로 진입, 다시 클릭하거나 ESC 로 바로 해제.
// (접객·노크·연출용 화면고정은 EnterUIModeEffect — 그건 exitKey 홀드로만 나온다)
// 접객 모드 위에 겹쳐 들어가도 됨: ESC/재클릭은 이 뷰만 벗기고 접객으로 복귀.
public class MonitorViewEffect : InteractionEffect
{
    [Tooltip("화면고정 위치 + 정면. 비우면 이 오브젝트")]
    [SerializeField] private Transform anchor;

    public override void Play(in InteractionContext ctx)
    {
        var uim = UIInteractionMode.Instance;
        if (uim == null) return;

        var a = anchor != null ? anchor : transform;
        if (uim.IsTopAnchor(a)) uim.Exit();               // 다시 클릭 = 화면고정 풀기
        else uim.Enter(a, 1f, escExits: true);            // ESC 한 번으로도 나감
    }
}
