using System;
using System.Collections.Generic;
using UnityEngine;

public enum Verdict { None, Approved, Rejected, Killed }

// 한 손님의 이번 판 상태.
[Serializable]
public class GuestState
{
    public NpcData npc;
    public int room = -1;
    public Verdict verdict = Verdict.None;
    public int checkInDay;
}

// 이번 플레이의 손님 상태 저장소. 접객에서 판정/배정을 기록, 밤 판정 로직이 읽는다.
// 밤에 누가 죽는지 계산하는 로직은 별도 시스템 — 여기선 npc.isSleepwalker(정답) vs verdict(플레이어) 데이터만 보관.
public class GuestManager : MonoBehaviour
{
    public static GuestManager Instance { get; private set; }

    private readonly List<GuestState> active = new();
    public IReadOnlyList<GuestState> Active => active;

    private void Awake() => Instance = this;

    public GuestState Get(NpcData npc) => active.Find(g => g.npc == npc);

    public GuestState CheckIn(NpcData npc, int room, int day)
    {
        var s = Get(npc) ?? AddNew(npc);
        s.room = room;
        s.verdict = Verdict.Approved;
        s.checkInDay = day;
        return s;
    }

    public void SetVerdict(NpcData npc, Verdict v, int day)
    {
        var s = Get(npc) ?? AddNew(npc);
        s.verdict = v;
        if (v == Verdict.Approved) s.checkInDay = day;
    }

    public void CheckOut(NpcData npc)
    {
        var s = Get(npc);
        if (s != null) active.Remove(s);
    }

    private GuestState AddNew(NpcData npc)
    {
        var s = new GuestState { npc = npc };
        active.Add(s);
        return s;
    }
}
