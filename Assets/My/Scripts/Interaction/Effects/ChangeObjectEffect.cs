using UnityEngine;

// 오브젝트 켜기/끄기 스왑. (구 TidyBed, Curtain 대체)
// 토글 상호작용: IsOn 이면 onObjects 켜고 offObjects 끔 / 아니면 반대.
// 비토글 상호작용: 한 번 실행 시 onObjects 켜고 offObjects 끔 (되돌리기 없음, 침대 정리 등).
public class ChangeObjectEffect : InteractionEffect
{
    [SerializeField] private GameObject[] onObjects;
    [SerializeField] private GameObject[] offObjects;

    public override void Play(in InteractionContext ctx)
    {
        bool on = ctx.Interactable.IsToggle ? ctx.IsOn : true;
        Set(onObjects, on);
        Set(offObjects, !on);
    }

    private static void Set(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        foreach (var o in objs)
            if (o != null) o.SetActive(active);
    }
}
