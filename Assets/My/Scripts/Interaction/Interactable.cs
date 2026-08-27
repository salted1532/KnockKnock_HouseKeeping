using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

// 프롬프트 문구 목록. 여닫기/켜고끄기는 토글 상태(IsOn)에 따라 문구가 바뀐다.
// 새 문구가 필요하면 여기에 추가하거나 직접입력 사용.
public enum InteractionPrompt { 상호작용, 여닫기, 켜고끄기, 줍기, 사용, 조사, 정리하기, 밀기, 접객, 직접입력 }

// 플레이어의 레이/커서가 찾는 대상. 얇은 디스패처 — 실제 동작은 붙어 있는 InteractionEffect들이 담당.
// 한 GameObject에 Interactable 1 + Effect 여러 개 + Condition 0~N 를 조합한다.
public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractionPrompt promptType = InteractionPrompt.상호작용;
    [Tooltip("promptType 이 직접입력일 때만 사용")]
    [SerializeField] private string customPrompt = "";
    [Tooltip("on/off 로 상태가 왕복하는 상호작용인가 (문/커튼/조명 등). 체크 시 IsOn 이 매번 뒤집힘")]
    [SerializeField] private bool isToggle;
    [SerializeField] private bool startOn;
    [Tooltip("자유 연출용 훅 (기존 Generic 대체)")]
    [FormerlySerializedAs("onInteract")]
    [SerializeField] private UnityEvent onInteracted;

    public string Prompt => promptType switch
    {
        InteractionPrompt.여닫기 => IsOn ? "닫기" : "열기",
        InteractionPrompt.켜고끄기 => IsOn ? "끄기" : "켜기",
        InteractionPrompt.직접입력 => customPrompt,
        _ => promptType.ToString(),
    };
    public bool IsToggle => isToggle;
    public bool IsOn { get; private set; }

    private InteractionEffect[] effects;
    private InteractionCondition[] conditions;

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
        effects = GetComponents<InteractionEffect>();
        conditions = GetComponents<InteractionCondition>();
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
    }

    // 코드/마이그레이션용
    public void ForceState(bool on) => IsOn = on;

#if UNITY_EDITOR
    // 우클릭 메뉴로 관리되는 효과 목록 (이 안의 것만 자동 제거 대상)
    static readonly System.Type[] ManagedEffects =
    {
        typeof(SfxEffect), typeof(ChangeObjectEffect), typeof(HingeEffect),
        typeof(PushEffect), typeof(PickupEffect), typeof(SpawnObjectEffect), typeof(EnterUIModeEffect),
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
        switch (promptType)
        {
            case InteractionPrompt.여닫기:   wanted.Add(typeof(HingeEffect));        isToggle = true;  break;
            case InteractionPrompt.켜고끄기: wanted.Add(typeof(ChangeObjectEffect)); isToggle = true;  break;
            case InteractionPrompt.정리하기: wanted.Add(typeof(ChangeObjectEffect)); isToggle = false; break;
            case InteractionPrompt.줍기:     wanted.Add(typeof(PickupEffect)); wanted.Add(typeof(ItemImpactSound)); break;
            case InteractionPrompt.사용:     wanted.Add(typeof(SpawnObjectEffect));                    break;
            case InteractionPrompt.밀기:     wanted.Add(typeof(PushEffect));   wanted.Add(typeof(ItemImpactSound)); break;
            case InteractionPrompt.접객:     wanted.Add(typeof(EnterUIModeEffect));                    break;
            default: /* 상호작용 / 조사 / 직접입력 */                                                   break;
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

        if (promptType == InteractionPrompt.접객 && GetComponent<PhaseCondition>() == null)
            UnityEditor.Undo.AddComponent<PhaseCondition>(gameObject);

        EnsureOutline();
        ReorderComponents();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    // 상호작용 필수: Outline 컴포넌트. 평소엔 꺼져 있고(Interactor 가 켬), 모드는 OutlineVisible.
    private void EnsureOutline()
    {
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
