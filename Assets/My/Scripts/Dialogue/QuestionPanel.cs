using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 접객/탐문의 버튼 목록 UI. 질문 허브(여러 번 선택)와 선택지(1회) 둘 다 이 하나로 그린다.
// DialogueRunner 가 Show() 로 버튼 목록을 넘기고 클릭 인덱스를 콜백으로 받는다.
// 새벽 탐문 행동력은 CanAsk / OnAsked 훅으로만 연결 — 행동력 시스템 자체는 별도.
public class QuestionPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform buttonParent;
    [Tooltip("자식에 TMP_Text 하나 가진 Button 프리팹")]
    [SerializeField] private Button buttonPrefab;
    [Tooltip("'대화 종료' 버튼 — 질문 허브에서만 표시")]
    [SerializeField] private Button doneButton;
    [Tooltip("버튼 목록 폭에 맞춰 크기가 조절될 패널 (Dialogue_Panel)")]
    [SerializeField] private RectTransform panelRect;
    [Tooltip("버튼 행 좌우로 남길 여백")]
    [SerializeField] private float sidePadding = 32f;
    [Tooltip("패널 최소 폭 = 기본 폭. 버튼이 적어도(선택지 없을 때 등) 이보다 좁아지지 않음. 버튼 3개 정도 크기")]
    [SerializeField] private float minPanelWidth = 840f;

    // null 이면 항상 선택 가능(접객). 새벽은 행동력 남았는지 반환.
    public Func<bool> CanAsk;
    // 질문 1건 답변이 끝난 뒤 (행동력 차감 등).
    public Action OnAsked;

    private readonly List<GameObject> spawned = new();

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    // labels[i] 클릭 → onPick(i). showDone 이면 '대화 종료' 버튼 → onPick(-1).
    public void Show(IReadOnlyList<string> labels, Action<int> onPick, bool showDone)
    {
        if (root != null) root.SetActive(true);
        Clear();

        // buttonPrefab 이 씬 오브젝트(예: 레이아웃 안의 템플릿 버튼)면 원본은 숨기고 복제본만 쓴다.
        if (buttonPrefab != null && buttonPrefab.gameObject.scene.IsValid())
            buttonPrefab.gameObject.SetActive(false);

        bool canAsk = CanAsk == null || CanAsk();
        for (int i = 0; i < labels.Count; i++)
        {
            var btn = Instantiate(buttonPrefab, buttonParent);
            btn.gameObject.SetActive(true);
            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = labels[i];
            btn.interactable = canAsk;
            int idx = i;
            btn.onClick.AddListener(() => onPick?.Invoke(idx));
            spawned.Add(btn.gameObject);
        }

        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(showDone);
            doneButton.onClick.RemoveAllListeners();
            if (showDone) doneButton.onClick.AddListener(() => onPick?.Invoke(-1));
            var doneTxt = doneButton.GetComponentInChildren<TMP_Text>(true);
            if (doneTxt != null) doneTxt.text = LocalizationManager.T("Done", "대화 종료");
            doneButton.transform.SetAsLastSibling();   // Exit 는 항상 맨 오른쪽
        }

        FitPanelToButtons();
    }

    // 버튼 행 폭에 맞춰 패널 폭을 조절. 높이는 대사 영역이 잡으므로 건드리지 않는다.
    private void FitPanelToButtons()
    {
        if (panelRect == null || buttonParent is not RectTransform row) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            Mathf.Max(minPanelWidth, row.rect.width + sidePadding * 2f));
    }

    // 답변 재생 중 잠금.
    public void SetInteractable(bool v)
    {
        bool canAsk = v && (CanAsk == null || CanAsk());
        foreach (var g in spawned)
        {
            var b = g != null ? g.GetComponent<Button>() : null;
            if (b != null) b.interactable = canAsk;
        }
        if (doneButton != null) doneButton.interactable = v;
    }

    public void Close()
    {
        Clear();
        if (root != null) root.SetActive(false);
    }

    private void Clear()
    {
        foreach (var g in spawned) if (g != null) Destroy(g);
        spawned.Clear();
    }
}
