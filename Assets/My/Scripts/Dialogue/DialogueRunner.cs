using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 대화 1회를 오케스트레이션: 인사 → 질문 허브 → (질문/선택지 → 답변, 분기) → 종료.
// 노드 = DialogueEntry. lines 읽기 → outcome 확인 → choices(플레이어 선택) 또는 goToNode(자동) → 재귀.
// 말풍선은 NPC 오브젝트에 붙은 것을 넘겨받는다. 데이터는 DialogueDatabase 에서 id + 오늘 일차로 조회.
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [SerializeField] private DialogueDatabase database;
    [SerializeField] private QuestionPanel questionPanel;
    [Tooltip("한 대화에서 노드를 이 횟수 넘게 방문하면 순환으로 보고 중단")]
    [SerializeField] private int maxNodeVisits = 40;

    public event Action<NpcData> OnDialogueStarted;
    public event Action<NpcData> OnDialogueEnded;
    public event Action<NpcData, DialogueLine> OnLineShown;
    public event Action<NpcData, string> OnNodeReached;   // 노드 도달 (점프스케어 등 훅)

    public bool Running { get; private set; }

    // 접객 일시정지(ESC) — 대화 진행 입력(다음 줄 넘기기)을 막는다. 대화 UI 숨김은 ReceptionManager 가.
    public bool Paused { get; set; }

    private bool rejected;
    private int nodeVisits;
    private SpeechBubble activeBubble;   // 진행 중 대화가 쓰는 말풍선 (Cancel 시 숨김)

    private void Awake() => Instance = this;

    // 진행 중인 대화를 즉시 중단 (새벽 노크 ESC 취소 등). 말풍선·질문패널 정리.
    public void Cancel()
    {
        if (!Running && activeBubble == null) return;
        StopAllCoroutines();
        Running = false;
        if (activeBubble != null) activeBubble.Hide();
        activeBubble = null;
        if (questionPanel != null) questionPanel.Close();
    }

    internal void RaiseLineShown(NpcData npc, DialogueLine line) => OnLineShown?.Invoke(npc, line);

    // onResult: 대화가 그냥 끝나면 None, 거절 노드(outcome=Rejected)로 끝나면 Rejected.
    public void Play(NpcData npc, SpeechBubble bubble, Situation situation, Action<Verdict> onResult = null)
    {
        if (database == null || npc == null || bubble == null)
        {
            Debug.LogWarning("[DialogueRunner] database / npc / bubble 미할당", this);
            onResult?.Invoke(Verdict.None);
            return;
        }
        StartCoroutine(Run(npc, bubble, situation, onResult));
    }

    // 한 노드의 대사 줄만 재생 (허브·분기 없음). 승인 시 "고맙다" 한마디 같은 짧은 대사용.
    public void SayNode(NpcData npc, SpeechBubble bubble, Situation situation, string nodeKey, Action onDone = null)
    {
        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
        var node = database != null && npc != null ? database.GetNode(npc.id, situation, nodeKey, day) : null;
        if (node == null || bubble == null || node.lines.Count == 0) { onDone?.Invoke(); return; }
        StartCoroutine(SayRoutine(npc, bubble, node, onDone));
    }

    private IEnumerator SayRoutine(NpcData npc, SpeechBubble bubble, DialogueEntry node, Action onDone)
    {
        activeBubble = bubble;
        yield return bubble.Show(npc, node.lines);
        bubble.Hide();
        activeBubble = null;
        onDone?.Invoke();
    }

    private IEnumerator Run(NpcData npc, SpeechBubble bubble, Situation situation, Action<Verdict> onResult)
    {
        Running = true;
        activeBubble = bubble;
        rejected = false;
        nodeVisits = 0;
        OnDialogueStarted?.Invoke(npc);

        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;

        foreach (var e in database.Query(npc.id, situation, day, EntryRole.Greeting))
        {
            yield return PlayNode(npc, bubble, situation, day, e);
            if (rejected) break;
        }

        if (!rejected)
            yield return QuestionHub(npc, bubble, situation, day);

        bubble.Hide();
        if (questionPanel != null) questionPanel.Close();
        Running = false;
        activeBubble = null;
        OnDialogueEnded?.Invoke(npc);
        onResult?.Invoke(rejected ? Verdict.Rejected : Verdict.None);
    }

    // 질문 허브: 질문 목록을 반복해서 띄운다. '대화 종료' 나 거절 노드로 빠져나감.
    private IEnumerator QuestionHub(NpcData npc, SpeechBubble bubble, Situation situation, int day)
    {
        if (questionPanel == null) yield break;

        while (!rejected)
        {
            var questions = database.Query(npc.id, situation, day, EntryRole.Question);
            if (questions.Count == 0) yield break;

            var labels = new List<string>(questions.Count);
            foreach (var q in questions)
                labels.Add(string.IsNullOrEmpty(q.Label) ? q.nodeKey : q.Label);

            int pick = -2;
            questionPanel.Show(labels, i => pick = i, showDone: true);
            yield return new WaitUntil(() => pick != -2);

            if (pick == -1) yield break;   // 대화 종료

            questionPanel.Close();
            yield return PlayNode(npc, bubble, situation, day, questions[pick]);
            questionPanel.OnAsked?.Invoke();
        }
    }

    // 노드 하나 재생 (재귀). lines → outcome → choices / goToNode.
    private IEnumerator PlayNode(NpcData npc, SpeechBubble bubble, Situation situation, int day, DialogueEntry entry)
    {
        if (entry == null) yield break;
        if (++nodeVisits > maxNodeVisits)
        {
            Debug.LogWarning($"[DialogueRunner] 노드 방문 {maxNodeVisits} 초과 — 순환 의심, 중단 (npc {npc.id})", this);
            yield break;
        }

        yield return bubble.Show(npc, entry.lines);

        if (!string.IsNullOrEmpty(entry.nodeKey))
            OnNodeReached?.Invoke(npc, entry.nodeKey);

        if (entry.outcome == Verdict.Rejected)
        {
            rejected = true;
            yield break;
        }

        if (entry.choices != null && entry.choices.Count > 0 && questionPanel != null)
        {
            var labels = new List<string>(entry.choices.Count);
            foreach (var c in entry.choices) labels.Add(c.Label);

            int pick = -2;
            questionPanel.Show(labels, i => pick = i, showDone: false);
            yield return new WaitUntil(() => pick != -2);
            questionPanel.Close();

            yield return GoTo(npc, bubble, situation, day, entry.choices[pick].goToNode);
        }
        else if (!string.IsNullOrEmpty(entry.goToNode))
        {
            yield return GoTo(npc, bubble, situation, day, entry.goToNode);
        }
    }

    private IEnumerator GoTo(NpcData npc, SpeechBubble bubble, Situation situation, int day, string nodeKey)
    {
        if (string.IsNullOrEmpty(nodeKey)) yield break;   // 빈 goto = 이 가지 종료(허브로 복귀)

        var next = database.GetNode(npc.id, situation, nodeKey, day);
        if (next == null)
        {
            Debug.LogWarning($"[DialogueRunner] goto 대상 노드 없음: '{nodeKey}' (npc {npc.id})", this);
            yield break;
        }
        yield return PlayNode(npc, bubble, situation, day, next);
    }
}
