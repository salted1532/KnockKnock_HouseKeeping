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
    [Tooltip("대사 텍스트 영역 (Dialogue). 버튼이 여러 줄이면 아래쪽을 더 비워준다. 비우면 스킵")]
    [SerializeField] private RectTransform dialogueArea;
    [Tooltip("버튼 행 좌우로 남길 여백")]
    [SerializeField] private float sidePadding = 32f;
    [Tooltip("패널 최소 폭 = 기본 폭. 버튼이 적어도(선택지 없을 때 등) 이보다 좁아지지 않음. 버튼 3개 정도 크기")]
    [SerializeField] private float minPanelWidth = 840f;
    [Tooltip("버튼(대화 종료 포함) 이 개수를 넘으면 다음 줄로 내린다. buttonParent 는 GridLayoutGroup 이어야 함")]
    [SerializeField] private int wrapAfter = 4;

    // null 이면 항상 선택 가능(접객). 새벽은 행동력 남았는지 반환.
    public Func<bool> CanAsk;
    // 질문 1건 답변이 끝난 뒤 (행동력 차감 등).
    public Action OnAsked;

    private readonly List<GameObject> spawned = new();

    private GridLayoutGroup grid;
    private float basePanelHeight;
    private float baseBottomInset;   // dialogueArea 아래쪽 여백 기본값 (버튼 1줄 분)
    private int lastRows = 1;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        grid = buttonParent != null ? buttonParent.GetComponent<GridLayoutGroup>() : null;
        if (panelRect != null) basePanelHeight = panelRect.rect.height;
        if (dialogueArea != null) baseBottomInset = dialogueArea.offsetMin.y;
    }

    // 버튼 총개수에 맞춰 그리드 열 수를 정한다. wrapAfter 이하 = 한 줄, 초과 = 여러 줄.
    private void ArrangeGrid(int total)
    {
        if (grid == null) return;
        int rows = total <= wrapAfter ? 1 : Mathf.CeilToInt(total / (float)wrapAfter);
        int cols = Mathf.Max(1, Mathf.CeilToInt(total / (float)Mathf.Max(1, rows)));
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        lastRows = rows;
    }

    // labels[i] 클릭 → onPick(i). showDone 이면 '대화 종료' 버튼 → onPick(-1).
    public void Show(IReadOnlyList<string> labels, Action<int> onPick, bool showDone)
    {
        if (root != null) root.SetActive(true);
        Clear();

        ArrangeGrid(labels.Count + (showDone ? 1 : 0));

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

    // 버튼 블록 크기에 맞춰 패널 폭을 조절. 버튼이 여러 줄이면 패널을 그만큼 위로 키우고
    // 대사 영역 아래 여백도 늘려 버튼과 겹치지 않게 한다.
    private void FitPanelToButtons()
    {
        if (panelRect == null || buttonParent is not RectTransform row) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(row);

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            Mathf.Max(minPanelWidth, row.rect.width + sidePadding * 2f));

        float rowStride = grid != null ? grid.cellSize.y + grid.spacing.y : 72f;
        float extra = Mathf.Max(0, lastRows - 1) * rowStride;

        if (basePanelHeight > 0f)
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, basePanelHeight + extra);

        if (dialogueArea != null)
            dialogueArea.offsetMin = new Vector2(dialogueArea.offsetMin.x, baseBottomInset + extra);
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

        lastRows = 1;
        if (panelRect != null && basePanelHeight > 0f)
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, basePanelHeight);
        if (dialogueArea != null)
            dialogueArea.offsetMin = new Vector2(dialogueArea.offsetMin.x, baseBottomInset);
    }

    private void Clear()
    {
        foreach (var g in spawned) if (g != null) Destroy(g);
        spawned.Clear();
    }
}
