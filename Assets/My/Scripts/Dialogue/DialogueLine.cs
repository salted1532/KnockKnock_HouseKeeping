using System;
using System.Collections.Generic;
using UnityEngine;

// 대사 한 줄: 표정 + 텍스트(영어/한글). (후속 훅 자리: 음성 클립, 애니 트리거, 홀드 시간 등 — 지금은 안 만듦)
// Text 는 현재 언어에 맞는 문자열을 돌려준다. 한글이 비면 영어로 폴백.
[Serializable]
public struct DialogueLine
{
    public Expression expression;
    [TextArea(1, 4)] public string textEn;
    [TextArea(1, 4)] public string textKo;

    public string Text => LocalizationManager.Korean && !string.IsNullOrEmpty(textKo) ? textKo : textEn;
}

// 대사 묶음의 역할 (= 노드 종류).
public enum EntryRole
{
    Greeting,   // 입장/대면 인사 — 대화 시작 시 자동 재생
    Question,   // QuestionPanel 허브에 버튼으로 나열
    Ambient,    // 그 외 (미사용, 확장용)
    Node,       // goto 로만 도달하는 중간 노드 (허브에 안 뜸)
}

// 선택지 하나: 버튼 문구(영어/한글) + 목표 노드.
[Serializable]
public struct Choice
{
    public string labelEn;
    public string labelKo;
    public string goToNode;

    public string Label => LocalizationManager.Korean && !string.IsNullOrEmpty(labelKo) ? labelKo : labelEn;
}

// 노드 하나 = (npcId, situation, day, role, nodeKey). DialogueDatabase 가 리스트로 보관.
// CSV 임포터가 채우며 손으로 편집하지 않는다.
//   재생: lines 읽기 → choices 있으면 플레이어 선택 → 없으면 goToNode 자동 → 둘 다 없으면 이 가지 종료.
//   outcome == Rejected 인 노드를 다 읽으면 대화 전체가 끝나며 거절로 판정된다.
[Serializable]
public class DialogueEntry
{
    public int npcId;
    public Situation situation;
    public int day;                        // 0 = 모든 일차 공통(폴백), N = N일차 전용
    public EntryRole role;
    public string nodeKey;                 // (npcId, situation) 안에서 유일. Greeting 은 보통 빈 문자열
    public string labelEn;                 // 질문/선택지 버튼 문구 (영어)
    public string labelKo;                 // 질문/선택지 버튼 문구 (한글)
    public List<DialogueLine> lines = new();
    public List<Choice> choices = new();   // 비면 선형
    public string goToNode;                // choices 없을 때 자동으로 이어질 노드 (비면 이 가지 종료)
    public Verdict outcome;                // 기본 None. Rejected 면 대화 종료 + 거절 판정

    public string Label => LocalizationManager.Korean && !string.IsNullOrEmpty(labelKo) ? labelKo : labelEn;
}
