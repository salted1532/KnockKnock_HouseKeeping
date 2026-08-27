using UnityEngine;

// 플레이어 손에 드는 오브젝트마다 붙인다. 어떤 ItemId 에 해당하는지 표시.
// HandItemRegistry 가 시작 시 수집한다.
public class HandItem : MonoBehaviour
{
    [SerializeField] private ItemId id;
    public ItemId Id => id;
}
