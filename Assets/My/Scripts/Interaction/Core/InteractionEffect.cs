using UnityEngine;

// 상호작용 1회의 정보. 효과들이 읽는다.
public readonly struct InteractionContext
{
    public readonly Interactable Interactable;
    public readonly GameObject Source;   // 상호작용한 주체(플레이어 등). null 가능
    public readonly bool IsOn;            // 토글 상호작용이면 토글 후 상태, 아니면 항상 true
    public readonly Vector3 Point;        // 레이 히트 지점

    public InteractionContext(Interactable interactable, GameObject source, bool isOn, Vector3 point)
    {
        Interactable = interactable;
        Source = source;
        IsOn = isOn;
        Point = point;
    }
}

// 한 GameObject에 여러 개를 붙여 조합한다. Interactable이 상호작용 시 순서대로 Play 호출.
[RequireComponent(typeof(Interactable))]
public abstract class InteractionEffect : MonoBehaviour
{
    public abstract void Play(in InteractionContext ctx);
}
