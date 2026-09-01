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
    [Tooltip("한글 이름. 비면 displayName 으로 폴백")]
    public string displayNameKo;

    // 현재 언어에 맞는 표시 이름.
    public string DisplayName =>
        LocalizationManager.Korean && !string.IsNullOrEmpty(displayNameKo) ? displayNameKo : displayName;

    [Header("초상화 (말풍선 표정)")]
    public Sprite neutralPortrait;
    public Sprite angryPortrait;
    [Tooltip("퇴장(카운터를 떠날 때) 뒷모습. 비면 정면 유지")]
    public Sprite backPortrait;
    [Tooltip("옆모습 (걸어서 입·퇴장 시 수평 이동). 화면 왼쪽 향한 그림 기준 — 오른쪽 이동 시 자동 좌우반전. 비면 정면/뒷모습 유지")]
    public Sprite sidePortrait;

    [Header("외형 (후속: 손님 모델 스왑용 — 현재 미사용)")]
    [Tooltip("NPC별 3D 모델. 지금은 씬의 공용 손님 오브젝트 하나를 재사용하므로 미사용. 모델 스왑 붙일 때 사용")]
    public GameObject modelPrefab;

    [Header("게임 로직 (밤 판정·신분증 — 플레이어에게 안 보임)")]
    public bool isSleepwalker;   // 내부 정답: 몽유병 환자인가
    public bool visitorOnly;     // 숙박 안 하고 대화만 (메시지 전달 인물)
    public bool refusesDawnKnock; // 새벽에 노크해도 문을 안 열어줌 (doc/0118)

    [Tooltip("숙박 박수. 체크인 다음날부터 카운트 — 1 = 다음날 아침 체크아웃, 2 = 이틀 묵고 셋째날 아침 체크아웃 (doc/0132)")]
    [Min(1)] public int stayNights = 1;
    [Tooltip("숙박 중 매일 아침 하우스키핑을 위해 방을 열어줌. 끄면 체크아웃 아침에만 개방 (doc/0132)")]
    public bool allowsMorningCleaning;

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
