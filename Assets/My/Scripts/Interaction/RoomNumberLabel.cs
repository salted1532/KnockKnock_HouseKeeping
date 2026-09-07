using TMPro;
using UnityEngine;

// Room_Number 큐브 위 월드 캔버스의 방 번호 라벨. 부모 RoomController.RoomNumber 를 그대로 표시.
// 프리팹 인스턴스마다 방 번호가 다르므로 OnEnable/OnValidate 로 런타임·에디터 모두에서 자동 반영.
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class RoomNumberLabel : MonoBehaviour
{
    private void OnEnable() => Refresh();
#if UNITY_EDITOR
    private void OnValidate() => UnityEditor.EditorApplication.delayCall += Refresh;
#endif

    private void Refresh()
    {
        if (this == null) return;
        var rc = GetComponentInParent<RoomController>(true);
        var label = GetComponent<TMP_Text>();
        if (rc != null && label != null && label.text != rc.RoomNumber.ToString())
            label.text = rc.RoomNumber.ToString();
    }
}
