using System.Collections.Generic;
using UnityEngine;

// 모든 NPC 대사의 단일 저장소. Tools > Dialogue > Import CSV 가 통째로 재생성한다 — 손으로 편집 금지.
[CreateAssetMenu(menuName = "KnockKnock/Dialogue Database", fileName = "DialogueDatabase")]
public class DialogueDatabase : ScriptableObject
{
    public List<DialogueEntry> entries = new();

    // (npcId, situation) → 그 NPC/상황의 엔트리들
    private Dictionary<(int, Situation), List<DialogueEntry>> byNpc;
    // (npcId, situation) → nodeKey → 엔트리들 (day 변형이 있으면 여러 개)
    private Dictionary<(int, Situation), Dictionary<string, List<DialogueEntry>>> byNode;

    private void BuildIndex()
    {
        byNpc = new Dictionary<(int, Situation), List<DialogueEntry>>();
        byNode = new Dictionary<(int, Situation), Dictionary<string, List<DialogueEntry>>>();

        foreach (var e in entries)
        {
            var k = (e.npcId, e.situation);

            if (!byNpc.TryGetValue(k, out var list))
                byNpc[k] = list = new List<DialogueEntry>();
            list.Add(e);

            if (string.IsNullOrEmpty(e.nodeKey)) continue;
            if (!byNode.TryGetValue(k, out var nodeMap))
                byNode[k] = nodeMap = new Dictionary<string, List<DialogueEntry>>();
            if (!nodeMap.TryGetValue(e.nodeKey, out var nodeList))
                nodeMap[e.nodeKey] = nodeList = new List<DialogueEntry>();
            nodeList.Add(e);
        }
    }

    public void RebuildIndex() => BuildIndex();

    // 진입점(Greeting/Question) 조회. 오늘 일차 전용이 있으면 그것, 없으면 day == 0 폴백.
    public List<DialogueEntry> Query(int npcId, Situation situation, int day, EntryRole role)
    {
        var result = new List<DialogueEntry>();
        if (byNpc == null) BuildIndex();
        if (!byNpc.TryGetValue((npcId, situation), out var pool)) return result;

        foreach (var e in pool)
            if (e.role == role && e.day == day) result.Add(e);
        if (result.Count > 0) return result;

        foreach (var e in pool)
            if (e.role == role && e.day == 0) result.Add(e);
        return result;
    }

    // goto 대상 노드 조회. day 전용 → day 0 폴백 → 아무거나.
    public DialogueEntry GetNode(int npcId, Situation situation, string nodeKey, int day)
    {
        if (string.IsNullOrEmpty(nodeKey)) return null;
        if (byNode == null) BuildIndex();
        if (!byNode.TryGetValue((npcId, situation), out var map) ||
            !map.TryGetValue(nodeKey, out var list) || list.Count == 0)
            return null;

        DialogueEntry common = null;
        foreach (var e in list)
        {
            if (e.day == day) return e;
            if (e.day == 0) common = e;
        }
        return common ?? list[0];
    }
}
