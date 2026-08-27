using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1회용: 구 Interactable(enum + switch) 프리팹/씬을 새 컴포넌트 조합으로 변환.
// Tools > Interaction > Migrate Legacy Interactables 실행 후 결과 확인.
// 마이그레이션 검증 끝나면 이 파일, Door.cs, ItemDispenser.cs, Interactable 의 LEGACY 필드 삭제(doc/0078).
public static class InteractionMigrator
{
    static readonly string[] PrefabPaths =
    {
        "Assets/My/InGame/Prefabs/Item/Can_Coke.prefab",
        "Assets/My/InGame/Prefabs/FlashLight/FlashLight_low-Poly.prefab",
        "Assets/My/InGame/Prefabs/MotelRoom/Motel_Room.prefab",
    };

    [MenuItem("Tools/Interaction/Migrate Legacy Interactables")]
    public static void Migrate()
    {
        int total = 0;

        foreach (string path in PrefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogWarning($"프리팹 못 엶: {path}"); continue; }

            int n = 0;
            foreach (var it in root.GetComponentsInChildren<Interactable>(true))
                if (MigrateOne(it)) n++;

            if (n > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[Migrate] {path}: {n}개 변환");
                total += n;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene sc = SceneManager.GetSceneAt(i);
            if (!sc.isLoaded) continue;
            int n = 0;
            foreach (GameObject go in sc.GetRootGameObjects())
                foreach (var it in go.GetComponentsInChildren<Interactable>(true))
                    if (MigrateOne(it)) n++;
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(sc);
                Debug.Log($"[Migrate] scene {sc.name}: {n}개 변환 (씬 저장 필요)");
                total += n;
            }
        }

        Debug.Log($"[Migrate] 완료 — 총 {total}개.");
    }

    static bool MigrateOne(Interactable it)
    {
        if (it.type == Interactable.LegacyType.Migrated) return false;
        var legacy = it.type;
        GameObject go = it.gameObject;

        switch (legacy)
        {
            case Interactable.LegacyType.Pickup:
            case Interactable.LegacyType.Flashlight:
            {
                var pe = Add<PickupEffect>(go);
                var so = new SerializedObject(pe);
                so.FindProperty("icon").objectReferenceValue = it.itemIcon;
                so.FindProperty("equipTargetOverride").objectReferenceValue = it.equipTarget;
                so.FindProperty("useClip").objectReferenceValue = it.useClip;
                so.FindProperty("consumeOnUse").boolValue = it.consumeOnUse;
                so.FindProperty("itemId").enumValueIndex =
                    legacy == Interactable.LegacyType.Flashlight ? (int)ItemId.Flashlight : (int)ItemId.None;
                so.ApplyModifiedProperties();
                if (legacy == Interactable.LegacyType.Pickup && it.equipTarget == null)
                    Debug.LogWarning($"[Migrate] '{it.name}' PickupEffect.itemId 수동 지정 필요", it);
                SetHeader(it, InteractionPrompt.줍기, false, false);
                Add<SfxEffect>(go); // 클립은 수동 지정
                break;
            }
            case Interactable.LegacyType.TidyBed:
            {
                var ce = Add<ChangeObjectEffect>(go);
                var so = new SerializedObject(ce);
                SetArray(so.FindProperty("onObjects"), it.tidyVisual);
                SetArray(so.FindProperty("offObjects"), it.messyVisual);
                so.ApplyModifiedProperties();
                SetHeader(it, InteractionPrompt.정리하기, false, false);
                Add<SfxEffect>(go);
                break;
            }
            case Interactable.LegacyType.Curtain:
            {
                var ce = Add<ChangeObjectEffect>(go);
                var so = new SerializedObject(ce);
                SetArray(so.FindProperty("onObjects"), it.curtainOpen);
                SetArray(so.FindProperty("offObjects"), it.curtainClosed);
                so.ApplyModifiedProperties();
                bool startOpen = it.curtainOpen != null && it.curtainOpen.activeSelf;
                SetHeader(it, InteractionPrompt.켜고끄기, true, startOpen);
                var sfx = Add<SfxEffect>(go);
                var sso = new SerializedObject(sfx);
                sso.FindProperty("onClip").objectReferenceValue = it.curtainOpenClip;
                sso.FindProperty("offClip").objectReferenceValue = it.curtainCloseClip;
                sso.ApplyModifiedProperties();
                break;
            }
            case Interactable.LegacyType.Push:
            {
                var pe = Add<PushEffect>(go);
                var so = new SerializedObject(pe);
                so.FindProperty("pushForce").floatValue = it.pushForce;
                so.FindProperty("torqueForce").floatValue = it.rotationForce;
                so.ApplyModifiedProperties();
                SetHeader(it, InteractionPrompt.밀기, false, false);
                Add<SfxEffect>(go);
                break;
            }
            case Interactable.LegacyType.Door:
            {
                MigrateDoor(it);
                return true;
            }
            case Interactable.LegacyType.Generic:
            default:
            {
                SetHeader(it, InteractionPrompt.상호작용, false, false); // onInteract → onInteracted 는 FormerlySerializedAs 로 자동 이관
                break;
            }
        }

        EnsureOutline(go);
        it.type = Interactable.LegacyType.Migrated;
        ClearLegacyRefs(it);
        EditorUtility.SetDirty(it);
        return true;
    }

    static void MigrateDoor(Interactable it)
    {
        Door door = it.door;
        if (door == null)
        {
            Debug.LogWarning($"[Migrate] '{it.name}' Door 참조 없음 — 수동 처리 필요", it);
            it.type = Interactable.LegacyType.Migrated;
            EditorUtility.SetDirty(it);
            return;
        }

        GameObject hinge = door.gameObject;

        var doorSO = new SerializedObject(door);
        float openAngle = doorSO.FindProperty("openAngle").floatValue;
        float openTime = doorSO.FindProperty("openTime").floatValue;
        AnimationCurve ease = doorSO.FindProperty("ease").animationCurveValue;
        bool startOpen = doorSO.FindProperty("startOpen").boolValue;
        Object openClip = doorSO.FindProperty("openClip").objectReferenceValue;
        Object closeClip = doorSO.FindProperty("closeClip").objectReferenceValue;

        var hi = Add<Interactable>(hinge);
        SetHeader(hi, InteractionPrompt.여닫기, true, startOpen);

        var he = Add<HingeEffect>(hinge);
        var heSO = new SerializedObject(he);
        heSO.FindProperty("openAngle").floatValue = openAngle;
        heSO.FindProperty("openTime").floatValue = openTime;
        heSO.FindProperty("ease").animationCurveValue = ease;
        heSO.ApplyModifiedProperties();

        var sfx = Add<SfxEffect>(hinge);
        var sfxSO = new SerializedObject(sfx);
        sfxSO.FindProperty("onClip").objectReferenceValue = openClip;
        sfxSO.FindProperty("offClip").objectReferenceValue = closeClip;
        sfxSO.ApplyModifiedProperties();

        EnsureOutline(hinge);
        Object.DestroyImmediate(door, true);
        if (it != hi) Object.DestroyImmediate(it, true); // 루트의 구 Interactable 제거
        EditorUtility.SetDirty(hinge);
    }

    // ── helpers ──────────────────────────────────────────
    static T Add<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // 상호작용 필수 Outline: 평소 꺼짐, 모드 OutlineVisible
    static void EnsureOutline(GameObject go)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        if (outline == null) return;
        var so = new SerializedObject(outline);
        so.FindProperty("outlineMode").enumValueIndex = 1; // OutlineVisible
        so.ApplyModifiedProperties();
        outline.enabled = false;
        EditorUtility.SetDirty(outline);
    }

    static void SetHeader(Interactable it, InteractionPrompt prompt, bool isToggle, bool startOn)
    {
        var so = new SerializedObject(it);
        so.FindProperty("promptType").enumValueIndex = (int)prompt;
        so.FindProperty("isToggle").boolValue = isToggle;
        so.FindProperty("startOn").boolValue = startOn;
        so.ApplyModifiedProperties();
    }

    static void SetArray(SerializedProperty arr, GameObject one)
    {
        arr.ClearArray();
        if (one != null)
        {
            arr.InsertArrayElementAtIndex(0);
            arr.GetArrayElementAtIndex(0).objectReferenceValue = one;
        }
    }

    static void ClearLegacyRefs(Interactable it)
    {
        it.itemIcon = null; it.equipTarget = null; it.useClip = null;
        it.messyVisual = null; it.tidyVisual = null; it.door = null;
        it.curtainOpen = null; it.curtainClosed = null;
        it.curtainOpenClip = null; it.curtainCloseClip = null;
        it.itemName = null;
    }
}
