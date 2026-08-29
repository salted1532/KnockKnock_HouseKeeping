using System;
using UnityEngine;

// NPC 1명의 정체성 — CSV 에 담을 수 없는 것(스프라이트 참조, 게임 로직 플래그)만.
// 대사는 여기 없음. DialogueDatabase 가 id(숫자) 로 매칭한다. NpcData 조회는 NpcCatalog.
[CreateAssetMenu(menuName = "KnockKnock/Npc Data", fileName = "Npc_")]
public class NpcData : ScriptableObject
{
    [Tooltip("CSV 의 npcId 와 일치하는 번호. 1~60 계획")]
    public int id;
    public string displayName;

    [Header("초상화 (말풍선 표정)")]
    public Sprite neutralPortrait;
    public Sprite angryPortrait;

    [Header("외형 (후속: 손님 모델 스왑용 — 현재 미사용)")]
    [Tooltip("NPC별 3D 모델. 지금은 씬의 공용 손님 오브젝트 하나를 재사용하므로 미사용. 모델 스왑 붙일 때 사용")]
    public GameObject modelPrefab;

    [Header("게임 로직 (밤 판정·신분증 — 플레이어에게 안 보임)")]
    public bool isSleepwalker;   // 내부 정답: 몽유병 환자인가
    public bool visitorOnly;     // 숙박 안 하고 대화만 (메시지 전달 인물)
    public IdCard idCard;

    public Sprite Portrait(Expression e) =>
        e == Expression.Angry && angryPortrait != null ? angryPortrait : neutralPortrait;

    private void OnValidate()
    {
        if (id < 1 || id > 60)
            Debug.LogWarning($"[NpcData] '{name}' id={id} — 1~60 범위 밖", this);
    }
}

[Serializable]
public struct IdCard
{
    public string name;
    public string birthDate;
    public Sprite photo;
    public bool forged;   // 위조 여부(내부 정답) — SYS-04
}
