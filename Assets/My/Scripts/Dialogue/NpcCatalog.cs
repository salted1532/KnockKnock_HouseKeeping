using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

// 번호(id) → NpcData 조회 단일 창구. NpcData 에셋은 NPC당 1개, 여기에 리스트로 모은다.
// DayData(CampaignData) 와 DialogueDatabase 가 숫자 id 를 NpcData 로 바꿀 때 이걸 쓴다.
[CreateAssetMenu(menuName = "KnockKnock/Npc Catalog", fileName = "NpcCatalog")]
public class NpcCatalog : ScriptableObject
{
    public List<NpcData> npcs = new();

    private Dictionary<int, NpcData> byId;

    private void BuildIndex()
    {
        byId = new Dictionary<int, NpcData>();
        foreach (var n in npcs)
        {
            if (n == null) continue;
            if (byId.ContainsKey(n.id))
                Debug.LogWarning($"[NpcCatalog] id 중복: {n.id} ('{n.name}')", this);
            else
                byId[n.id] = n;
        }
    }

    public NpcData Get(int id)
    {
        if (byId == null) BuildIndex();
        if (byId.TryGetValue(id, out var npc)) return npc;
        Debug.LogWarning($"[NpcCatalog] id {id} 에 해당하는 NpcData 없음", this);
        return null;
    }

    public void RebuildIndex() => BuildIndex();

#if UNITY_EDITOR
    [ContextMenu("프로젝트의 NpcData 전부 수집")]
    private void CollectAll()
    {
        npcs = AssetDatabase.FindAssets("t:NpcData")
            .Select(g => AssetDatabase.LoadAssetAtPath<NpcData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(n => n != null)
            .OrderBy(n => n.id)
            .ToList();
        BuildIndex();
        EditorUtility.SetDirty(this);
        Debug.Log($"[NpcCatalog] NpcData {npcs.Count}개 수집", this);
    }
#endif
}
