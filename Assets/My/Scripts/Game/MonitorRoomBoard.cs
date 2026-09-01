using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// CRT 모니터 화면(uGUI, doc/0119)의 방배정 보드 = 요청의 "UI Controller".
// 버튼은 배정 로직이 없다 — onClick → ReceptionManager.AssignRoom(n). 상태 표시는 Refresh().
// 접객 자리 화면고정에서도, 모니터 화면고정에서도 동작 (doc/0119 클릭 게이트 = UIInteractionMode.Active). doc/0118.
public class MonitorRoomBoard : MonoBehaviour
{
    [Serializable]
    public struct RoomButton
    {
        public int roomNumber;       // 101~110
        public Button button;
        public Image tint;           // 상태 색을 칠할 Image (비우면 button.image)
        public TMP_Text label;       // 방번호 라벨 (선택)
        [Tooltip("이 방이 선택됐을 때만 켜지는 오브젝트 — 하이라이트 테두리·체크·활성 이미지 등 (선택)")]
        public GameObject selectedMark;
    }

    [SerializeField] private RoomButton[] rooms;
    [Tooltip("현재 배정 대상 손님 표시 (선택)")]
    [SerializeField] private TMP_Text header;
    [SerializeField] private Color vacantColor = new(0.20f, 0.25f, 0.35f);
    [SerializeField] private Color selectedColor = new(0.20f, 0.55f, 0.30f);
    [SerializeField] private Color occupiedColor = new(0.35f, 0.15f, 0.15f);

    private void Awake()
    {
        if (rooms == null) return;
        foreach (var r in rooms)
        {
            if (r.button == null) continue;
            int n = r.roomNumber;
            r.button.onClick.AddListener(() => Pick(n));
            if (r.label != null) r.label.text = n.ToString();
        }
    }

    private void Pick(int room)
    {
        var rm = ReceptionManager.Instance;
        var guest = rm != null ? rm.CurrentGuest : null;
        bool taken = GuestManager.Instance != null && GuestManager.Instance.RoomTaken(room);

        if (guest == null)
            Debug.Log($"[MonitorRoomBoard] {room}호 버튼 클릭 — 배정 대상 손님 없음 (무시)", this);
        else if (taken)
            Debug.Log($"[MonitorRoomBoard] {room}호 버튼 클릭 — 이미 사용중 (무시)", this);
        else if (rm.PendingRoom == room)
            Debug.Log($"[MonitorRoomBoard] {room}호 버튼 클릭 → '{guest.DisplayName}' (id {guest.id}) 배정 해제 (토글)", this);
        else
            Debug.Log($"[MonitorRoomBoard] {room}호 버튼 클릭 → '{guest.DisplayName}' (id {guest.id}) 배정", this);

        rm?.AssignRoom(room);
    }

    private void OnEnable() => Refresh();
    private void Update() => Refresh();

    private void Refresh()
    {
        var rm = ReceptionManager.Instance;
        var gm = GuestManager.Instance;

        if (header != null)
        {
            var g = rm != null ? rm.CurrentGuest : null;
            header.text = g != null
                ? LocalizationManager.T("Assign a room for " + g.DisplayName, g.DisplayName + " — 방 배정")
                : LocalizationManager.T("No guest waiting", "대기 중인 손님 없음");
        }

        if (rooms == null) return;
        bool canAssign = rm != null && rm.CurrentGuest != null;
        foreach (var r in rooms)
        {
            if (r.button == null) continue;
            bool occ = gm != null && gm.RoomTaken(r.roomNumber);
            bool picked = rm != null && rm.PendingRoom == r.roomNumber;

            var tint = r.tint != null ? r.tint : r.button.image;
            if (tint != null)
                tint.color = occ ? occupiedColor : picked ? selectedColor : vacantColor;

            // 배정된 방은 번호에 취소선 (TMP <s> 태그). 빈 방은 그냥 번호.
            if (r.label != null)
                r.label.text = occ ? $"<s>{r.roomNumber}</s>" : r.roomNumber.ToString();

            if (r.selectedMark != null) r.selectedMark.SetActive(picked);

            r.button.interactable = canAssign && !occ;
        }
    }
}
