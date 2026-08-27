using UnityEngine;

// 입력 드라이버 베이스 (GazeInteractor: 화면중앙+E, CursorInteractor: 마우스+클릭).
public abstract class Interactor : MonoBehaviour
{
    // 상호작용 주체. 기본은 Player 태그 오브젝트, 없으면 자신.
    public GameObject Owner
    {
        get
        {
            if (cachedOwner == null)
                cachedOwner = GameObject.FindGameObjectWithTag("Player") ?? gameObject;
            return cachedOwner;
        }
    }
    private GameObject cachedOwner;

    protected void TryInteract(Interactable target, Vector3 point)
    {
        if (target != null && target.CanInteract)
            target.Interact(this, point);
    }
}
