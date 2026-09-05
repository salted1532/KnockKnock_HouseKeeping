using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

// 프롬프트 문구 목록. 여닫기/켜고끄기는 토글 상태(IsOn)에 따라 문구가 바뀐다.
// 새 문구가 필요하면 여기에 추가하거나 Custom 사용.
// 순서(정수값)로 직렬화됨 — 기존 값 사이에 삽입 금지, 새 값은 끝에만 추가.
// index 8 은 구 '접객' → ViewScreen 으로 rename (기존 프리팹은 그대로 매핑됨).
public enum InteractionPrompt
{
    Interact, OpenClose, Toggle, PickUp, Use, Inspect, CleanUp, Push, ViewScreen, Custom, Hang,
    Read, EndMorning, EndNoon, EndEvening, EndDay, CheckIn, Knock, Repair, Report,
}

// 플레이어의 레이/커서가 찾는 대상. 얇은 디스패처 — 실제 동작은 붙어 있는 InteractionEffect들이 담당.
// 한 GameObject에 Interactable 1 + Effect 여러 개 + Condition 0~N 를 조합한다.
public class Interactable : MonoBehaviour
{
    // 같은 GameObject 의 컴포넌트가 구현하면 프롬프트 문구를 상황에 따라 바꾼다.
    // null 반환 = promptType 기본값 사용.
    public interface IPromptOverride { string PromptOverride { get; } }

    [SerializeField] private InteractionPrompt promptType = InteractionPrompt.Interact;
    [Tooltip("promptType 이 Custom 일 때만 사용")]
    [SerializeField] private string customPrompt = "";
    [Tooltip("on/off 로 상태가 왕복하는 상호작용인가 (문/커튼/조명 등). 체크 시 IsOn 이 매번 뒤집힘")]
    [SerializeField] private bool isToggle;
    [SerializeField] private bool startOn;
    [Tooltip("자유 연출용 훅 (기존 Generic 대체)")]
    [FormerlySerializedAs("onInteract")]
    [SerializeField] private UnityEvent onInteracted;

    public string Prompt => promptOverride?.PromptOverride ?? DefaultPrompt;

    private string DefaultPrompt => promptType switch
    {
        InteractionPrompt.OpenClose => IsOn ? LocalizationManager.T("Close", "닫기") : LocalizationManager.T("Open", "열기"),
        InteractionPrompt.Toggle => IsOn ? LocalizationManager.T("Turn off", "끄기") : LocalizationManager.T("Turn on", "켜기"),
        InteractionPrompt.PickUp => LocalizationManager.T("Pick up", "줍기"),
        InteractionPrompt.Inspect => LocalizationManager.T("Inspect", "살펴보기"),
        InteractionPrompt.CleanUp => LocalizationManager.T("Clean up", "정리하기"),
        InteractionPrompt.ViewScreen => LocalizationManager.T("View", "보기"),
        InteractionPrompt.Custom => customPrompt,
        InteractionPrompt.Read => LocalizationManager.T("Read", "읽기"),
        InteractionPrompt.EndMorning or InteractionPrompt.EndNoon => LocalizationManager.T("End shift", "근무 종료"),
        InteractionPrompt.EndEvening => LocalizationManager.T("Close up", "마감"),
        InteractionPrompt.EndDay => LocalizationManager.T("Sleep", "잠자기"),
        InteractionPrompt.CheckIn => LocalizationManager.T("Check in", "체크인"),
        InteractionPrompt.Knock => LocalizationManager.T("Knock", "노크"),
        InteractionPrompt.Use => LocalizationManager.T("Use", "사용"),
        InteractionPrompt.Push => LocalizationManager.T("Push", "밀기"),
        InteractionPrompt.Hang => LocalizationManager.T("Hang", "걸기"),
        InteractionPrompt.Repair => LocalizationManager.T("Repair", "수리하기"),
        InteractionPrompt.Report => LocalizationManager.T("Report", "신고하기"),
        _ => LocalizationManager.T("Interact", "상호작용"),   // Interact
    };
    public bool IsToggle => isToggle;
    public bool IsOn { get; private set; }

    // 성공적으로 상호작용할 때마다 발생 (조건 불충족으로 막힌 경우엔 안 뜸). 코드 훅용 — Inspector 배선 불필요.
    public event System.Action Interacted;

    private InteractionEffect[] effects;
    private InteractionCondition[] conditions;
    private IPromptOverride promptOverride;

    public bool CanInteract
    {
        get
        {
            if (!enabled || !gameObject.activeInHierarchy) return false;
            if (conditions != null)
                foreach (var c in conditions)
                    if (c != null && c.enabled && !c.IsMet) return false;
            return true;
        }
    }

    private void Awake()
    {
        // SfxEffect 는 항상 먼저 실행 — 뒤따르는 효과(PickupEffect 등)가 오브젝트를 비활성화해도
        // 소리는 이미 재생을 시작한 뒤라 안 끊김. 인스펙터 컴포넌트 나열 순서와는 무관.
        effects = GetComponents<InteractionEffect>()
            .OrderBy(e => e is SfxEffect ? 0 : 1)
            .ToArray();
        conditions = GetComponents<InteractionCondition>();
        promptOverride = GetComponent<IPromptOverride>();
        IsOn = startOn;

        if (effects.Length == 0 && (onInteracted?.GetPersistentEventCount() ?? 0) == 0)
            Debug.LogWarning($"[Interactable] '{name}' 에 효과도 onInteracted도 없음", this);
    }

    public void Interact(Interactor source, Vector3 point)
    {
        if (!CanInteract) return;

        if (isToggle) IsOn = !IsOn;

        var ctx = new InteractionContext(this, source != null ? source.Owner : null, isToggle ? IsOn : true, point);
        foreach (var e in effects)
            if (e != null && e.enabled) e.Play(in ctx);

        onInteracted?.Invoke();
        Interacted?.Invoke();
    }

    // 코드/마이그레이션용
    public void ForceState(bool on) => IsOn = on;

    // 코드/연출용: 토글 상태를 강제 설정하고 효과 재생 (CanInteract·isToggle 무시).
    // NPC 가 문 여는 연출 등. 이미 그 상태면 아무것도 안 함.
    // silent: SfxEffect 는 건너뛴다 (RoomController 의 새벽 봉인 등, 소리 없이 상태만 바꿀 때).
    public void SetState(bool on, bool silent = false)
    {
        if (IsOn == on) return;
        IsOn = on;
        var ctx = new InteractionContext(this, null, on, transform.position);
        if (effects != null)
            foreach (var e in effects)
            {
                if (e == null || !e.enabled) continue;
                if (silent && e is SfxEffect) continue;
                e.Play(in ctx);
            }
    }

#if UNITY_EDITOR
    // 우클릭 메뉴로 관리되는 효과 목록 (이 안의 것만 자동 제거 대상)
    static readonly System.Type[] ManagedEffects =
    {
        typeof(SfxEffect), typeof(ChangeObjectEffect), typeof(HingeEffect),
        typeof(PushEffect), typeof(PickupEffect), typeof(SpawnObjectEffect), typeof(EnterUIModeEffect),
        typeof(HookEffect), typeof(ShowPanelEffect), typeof(PhaseSwitchEffect), typeof(KnockEffect),
    };

    // 컴포넌트 우클릭 메뉴: promptType 에 맞게 효과 구성을 맞춘다.
    //  - 필요한 효과 추가, 필요 없는 managed 효과 제거 (Undo 가능)
    //  - SfxEffect 는 항상 포함 → AudioSource 자동 부착([RequireComponent])
    //  - 콜라이더 없으면 BoxCollider 추가, Interaction 레이어 아니면 맞춤
    //  - 각 효과의 오브젝트/클립 필드는 수동으로 채워야 함 (Unity가 알 수 없는 값)
    [ContextMenu("Prompt Type에 맞게 효과 재설정")]
    private void SyncEffectsToPrompt()
    {
        UnityEditor.Undo.RecordObjects(new Object[] { this, gameObject }, "Sync Interaction Effects");

        var wanted = new System.Collections.Generic.List<System.Type> { typeof(SfxEffect) };
        DayPhase? fromPhase = null;   // 값이 있으면 PhaseSwitchEffect(from/to) + PhaseCondition 을 자동 설정
        switch (promptType)
        {
            case InteractionPrompt.OpenClose:  wanted.Add(typeof(HingeEffect));        isToggle = true;  break;
            case InteractionPrompt.Toggle:     wanted.Add(typeof(ChangeObjectEffect)); isToggle = true;  break;
            case InteractionPrompt.CleanUp:    wanted.Add(typeof(ChangeObjectEffect)); isToggle = false; break;
            case InteractionPrompt.Repair:     wanted.Add(typeof(ChangeObjectEffect)); isToggle = false; break;
            case InteractionPrompt.Report:     wanted.Add(typeof(ChangeObjectEffect)); isToggle = false; break;
            case InteractionPrompt.PickUp:     wanted.Add(typeof(PickupEffect)); wanted.Add(typeof(ItemImpactSound)); break;
            case InteractionPrompt.Use:        wanted.Add(typeof(SpawnObjectEffect));                    break;
            case InteractionPrompt.Push:       wanted.Add(typeof(PushEffect));   wanted.Add(typeof(ItemImpactSound)); break;
            case InteractionPrompt.ViewScreen: wanted.Add(typeof(EnterUIModeEffect));                    break;
            case InteractionPrompt.Knock:      wanted.Add(typeof(KnockEffect));                          break;
            case InteractionPrompt.Read:       wanted.Add(typeof(ShowPanelEffect));                      break;
            case InteractionPrompt.Hang:       wanted.Add(typeof(HookEffect));                           break;
            case InteractionPrompt.EndMorning: wanted.Add(typeof(PhaseSwitchEffect)); fromPhase = DayPhase.Morning; break;
            case InteractionPrompt.EndNoon:    wanted.Add(typeof(PhaseSwitchEffect)); fromPhase = DayPhase.Noon;    break;
            case InteractionPrompt.EndEvening: wanted.Add(typeof(PhaseSwitchEffect)); fromPhase = DayPhase.Evening; break;
            case InteractionPrompt.EndDay:     wanted.Add(typeof(PhaseSwitchEffect)); fromPhase = DayPhase.Dawn;    break;
            default: /* Interact / Inspect / Custom */                                                   break;
        }

        foreach (var t in ManagedEffects)
        {
            if (wanted.Contains(t)) continue;
            var comp = GetComponent(t);
            if (comp != null)
            {
                Debug.Log($"[Interactable] '{name}' 효과 제거: {t.Name}", this);
                UnityEditor.Undo.DestroyObjectImmediate(comp);
            }
        }

        EnsureColliderAndLayer(); // ItemImpactSound 등의 RequireComponent(Collider) 보다 먼저

        foreach (var t in wanted)
            if (GetComponent(t) == null)
            {
                Debug.Log($"[Interactable] '{name}' 효과 추가: {t.Name}", this);
                UnityEditor.Undo.AddComponent(gameObject, t);
            }

        if (fromPhase.HasValue)
        {
            DayPhase from = fromPhase.Value;
            DayPhase to = (DayPhase)(((int)from + 1) % 4);   // 하루는 한 방향 순환

            // PhaseSwitchEffect.from / .to
            var sw = GetComponent<PhaseSwitchEffect>();
            if (sw != null)
            {
                UnityEditor.Undo.RecordObject(sw, "Sync Interaction Effects");
                var so = new UnityEditor.SerializedObject(sw);
                so.FindProperty("from").enumValueIndex = (int)from;
                so.FindProperty("to").enumValueIndex = (int)to;
                so.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(sw);
            }

            // PhaseCondition.allowedPhases = [from] — 프롬프트/아웃라인이 from 단계에서만 뜨게
            var pc = GetComponent<PhaseCondition>();
            if (pc == null) pc = UnityEditor.Undo.AddComponent<PhaseCondition>(gameObject);
            if (pc != null)
            {
                UnityEditor.Undo.RecordObject(pc, "Sync Interaction Effects");
                var so = new UnityEditor.SerializedObject(pc);
                var arr = so.FindProperty("allowedPhases");
                arr.arraySize = 1;
                arr.GetArrayElementAtIndex(0).enumValueIndex = (int)from;
                so.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(pc);
            }

            Debug.Log($"[Interactable] '{name}' 단계 전환 스위치: {from} → {to}", this);
        }

        EnsureOutline();
        if (promptType == InteractionPrompt.PickUp)
            EnsureRigidbody();
        ReorderComponents();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    // 상호작용 필수: Outline 컴포넌트. 평소엔 꺼져 있고(Interactor 가 켬), 모드는 OutlineVisible.
    private void EnsureOutline()
    {
        if (GetComponent<SpriteOutline>() != null) return;   // 스프라이트 손님은 QuickOutline 대신 SpriteOutline 사용

        var outline = GetComponent<Outline>();
        if (outline == null)
            outline = UnityEditor.Undo.AddComponent<Outline>(gameObject);
        if (outline == null) return;

        UnityEditor.Undo.RecordObject(outline, "Sync Interaction Effects");
        var so = new UnityEditor.SerializedObject(outline);
        so.FindProperty("outlineMode").enumValueIndex = 1; // Outline.Mode.OutlineVisible
        so.ApplyModifiedProperties();
        outline.enabled = false;
        UnityEditor.EditorUtility.SetDirty(outline);
    }

    // 줍기 아이템은 바닥에 물리적으로 놓이므로 Rigidbody 필요. 이미 있으면 손대지 않음.
    private void EnsureRigidbody()
    {
        if (GetComponent<Rigidbody>() != null) return;
        UnityEditor.Undo.AddComponent<Rigidbody>(gameObject);
        Debug.Log($"[Interactable] '{name}' Rigidbody 추가 (줍기)", this);
    }

    // 인스펙터 컴포넌트 순서:
    //   Transform → MeshFilter → Renderer → Collider → Rigidbody → Interactable
    //   → Condition → 일반 Effect → SfxEffect → Outline·기타 .cs → AudioSource → 그 외
    private void ReorderComponents()
    {
        var comps = GetComponents<Component>();
        var order = new System.Collections.Generic.List<Component>(comps);
        order.Sort((a, b) =>
        {
            int r = Rank(a).CompareTo(Rank(b));
            if (r != 0) return r;
            return System.Array.IndexOf(comps, a).CompareTo(System.Array.IndexOf(comps, b)); // 동순위는 기존 순서 유지
        });

        for (int i = 0; i < order.Count; i++)
        {
            var c = order[i];
            int guard = 0;
            while (guard++ < 32)
            {
                int at = System.Array.IndexOf(GetComponents<Component>(), c);
                if (at <= i) break;
                if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(c)) break;
            }
        }
    }

    private static int Rank(Component c)
    {
        if (c is Transform) return 0;
        if (c is MeshFilter) return 1;
        if (c is Renderer) return 2;            // MeshRenderer / SkinnedMeshRenderer
        if (c is Collider) return 3;
        if (c is Rigidbody) return 4;
        if (c is Interactable) return 5;
        if (c is InteractionCondition) return 6;
        if (c is SfxEffect) return 8;           // 다른 Effect 다음
        if (c is InteractionEffect) return 7;
        if (c is MonoBehaviour) return 9;       // Outline · 기타 커스텀 .cs
        if (c is AudioSource) return 10;
        return 11;                              // 그 외
    }

    private void EnsureColliderAndLayer()
    {
        int layer = LayerMask.NameToLayer("Interaction");

        if (GetComponent<Collider>() != null)
        {
            if (layer >= 0 && gameObject.layer != layer)
            {
                gameObject.layer = layer;
                Debug.Log($"[Interactable] '{name}' 레이어 → Interaction", this);
            }
        }
        else if (GetComponentInChildren<Collider>() == null)
        {
            var box = UnityEditor.Undo.AddComponent<BoxCollider>(gameObject);
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                box.center = mf.sharedMesh.bounds.center;
                box.size = mf.sharedMesh.bounds.size;
            }
            if (layer >= 0) gameObject.layer = layer;
            Debug.Log($"[Interactable] '{name}' BoxCollider 추가 + Interaction 레이어 — 크기 확인 필요", this);
        }
        else
        {
            Debug.LogWarning($"[Interactable] '{name}' 콜라이더가 자식에 있음 — 그 자식을 Interaction 레이어(11)로 두어야 레이가 잡음", this);
        }
    }
#endif
}
