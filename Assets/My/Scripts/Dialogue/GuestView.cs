using UnityEngine;

// 재활용되는 손님 오브젝트의 겉모습. 접객 큐가 손님마다 Apply(npc) 로 스프라이트를 바꾼다.
// 대화 중에는 DialogueRunner.OnLineShown 을 구독해 줄별 표정(Neutral/Angry)도 반영.
public class GuestView : MonoBehaviour
{
    [Tooltip("손님 몸/얼굴 스프라이트 (2D). NpcData 초상화로 교체됨")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("손님 스프라이트 목표 높이(월드 유닛). 초상화마다 해상도가 달라도 이 높이에 맞춰 스케일. 0 이면 스케일 안 건드림")]
    [SerializeField] private float worldHeight = 2.3f;

    private NpcData npc;

    private void Reset() => body = GetComponentInChildren<SpriteRenderer>();

    public void Apply(NpcData data)
    {
        npc = data;
        SetExpression(Expression.Neutral);
        if (DialogueRunner.Instance != null)
            DialogueRunner.Instance.OnLineShown += OnLine;
    }

    public void Clear()
    {
        if (DialogueRunner.Instance != null)
            DialogueRunner.Instance.OnLineShown -= OnLine;
        npc = null;
    }

    private void OnDisable() => Clear();

    private void OnLine(NpcData shown, DialogueLine line)
    {
        if (shown == npc) SetExpression(line.expression);
    }

    private void SetExpression(Expression e)
    {
        if (body == null || npc == null) return;
        var s = npc.Portrait(e);
        body.sprite = s;

        // 초상화 원본 해상도가 제각각이라 body 트랜스폼을 목표 높이에 맞춰 균일 스케일.
        if (s != null && worldHeight > 0f)
        {
            float h = s.bounds.size.y;   // PPU 반영된 유닛 높이
            if (h > 0.0001f)
            {
                float k = worldHeight / h;
                body.transform.localScale = new Vector3(k, k, body.transform.localScale.z);
            }
        }
    }
}
