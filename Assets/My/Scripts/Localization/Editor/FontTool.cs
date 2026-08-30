using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Localization > Apply Galmuri Font
// 1) TMP Settings 기본 폰트 → Galmuri11 SDF, 폴백에 기존 LiberationSans SDF 추가(전역).
//    → Galmuri 에 없는 글리프(·, 악센트 등)는 자동으로 LiberationSans 에서 렌더.
// 2) 열린 씬 + Assets/My 아래 모든 프리팹의 TMP_Text.font 를 Galmuri 로 교체 (머티리얼 자동 동기화).
// 씬/프리팹이 늘어나면 다시 실행하면 됨.
public static class FontTool
{
    private const string GalmuriPath = "Assets/My/font/Galmuri11 SDF.asset";
    private const string FallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string PrefabSearchFolder = "Assets/My";

    [MenuItem("Tools/Localization/Apply Galmuri Font")]
    public static void Apply()
    {
        var galmuri = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GalmuriPath);
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        if (galmuri == null) { Debug.LogError($"[FontTool] Galmuri 폰트 없음: {GalmuriPath}"); return; }

        ApplyToSettings(galmuri, fallback);

        int count = 0;
        count += ApplyToOpenScenes(galmuri);
        count += ApplyToPrefabs(galmuri);

        AssetDatabase.SaveAssets();
        Debug.Log($"[FontTool] 완료 — TMP_Text {count}개 폰트 교체 + TMP Settings 기본/폴백 설정");
    }

    private static void ApplyToSettings(TMP_FontAsset galmuri, TMP_FontAsset fallback)
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
        if (settings == null) { Debug.LogWarning($"[FontTool] TMP Settings 없음: {SettingsPath} — 기본 폰트 스킵"); return; }

        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = galmuri;

        if (fallback != null)
        {
            var list = so.FindProperty("m_fallbackFontAssets");
            bool has = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == fallback) has = true;
            if (!has)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = fallback;
            }
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
    }

    private static int ApplyToOpenScenes(TMP_FontAsset galmuri)
    {
        int n = 0;
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (Retarget(t, galmuri)) { EditorUtility.SetDirty(t); n++; }
                }
            EditorSceneManager.MarkSceneDirty(scene);
        }
        return n;
    }

    private static int ApplyToPrefabs(TMP_FontAsset galmuri)
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                if (Retarget(t, galmuri)) { changed = true; n++; }
            if (changed) PrefabUtility.SaveAsPrefabAsset(go, path);
            PrefabUtility.UnloadPrefabContents(go);
        }
        return n;
    }

    // 폰트 + 머티리얼을 galmuri 로 맞춘다. 폰트 GUID 가 그대로여도 머티리얼(서브에셋)이
    // 재생성되면 fileID 가 바뀌므로 항상 재지정한다. 바뀐 게 있으면 true.
    private static bool Retarget(TMP_Text t, TMP_FontAsset galmuri)
    {
        bool changed = false;
        if (t.font != galmuri) { t.font = galmuri; changed = true; }
        if (t.fontSharedMaterial != galmuri.material)
        {
            t.fontSharedMaterial = galmuri.material;
            changed = true;
        }
        return changed;
    }
}
