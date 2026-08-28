using UnityEngine;

// 게임 시작 시 지정한 오브젝트들을 활성화한다.
// 에디터에선 꺼둬서(뷰 가리지 않게) 두고 런타임에만 켜고 싶은 UI(FadeOverlay 등)에 쓴다.
// 이 컴포넌트는 항상 활성인 오브젝트(부모 Canvas 등)에 붙일 것.
public class ActivateOnAwake : MonoBehaviour
{
    [SerializeField] private GameObject[] targets;

    private void Awake()
    {
        if (targets == null) return;
        foreach (var t in targets)
            if (t != null) t.SetActive(true);
    }
}
