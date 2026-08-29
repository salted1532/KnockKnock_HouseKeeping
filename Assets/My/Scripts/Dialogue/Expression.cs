// NPC 대사 한 줄의 표정. 줄별로 지정 → SpeechBubble 이 해당 초상화로 교체.
// 정수값으로 직렬화/CSV 파싱됨 — 사이에 삽입 금지, 새 값은 끝에만 추가.
public enum Expression
{
    Neutral,
    Angry,
}
