using UnityEngine;

// Interactable에 붙이면 IsMet가 false인 동안 상호작용이 막힌다. (없으면 항상 가능)
[RequireComponent(typeof(Interactable))]
public abstract class InteractionCondition : MonoBehaviour
{
    public abstract bool IsMet { get; }
}
