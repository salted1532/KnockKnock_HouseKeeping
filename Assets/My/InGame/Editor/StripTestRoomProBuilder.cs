using System.Reflection;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEngine.ProBuilder;

static class StripTestRoomProBuilder
{
    const string k_PrefabPath = "Assets/My/InGame/Prefabs/Test_Room.prefab";

    [MenuItem("Tools/KnockKnock/Strip ProBuilder in Test_Room.prefab")]
    static void Run()
    {
        // StripProBuilderScripts is internal to ProBuilder's editor assembly, so reflect into it.
        var stripType = typeof(ProBuilderEditor).Assembly.GetType("UnityEditor.ProBuilder.Actions.StripProBuilderScripts");
        var doStrip = stripType.GetMethod("DoStrip", BindingFlags.Public | BindingFlags.Static);

        var root = PrefabUtility.LoadPrefabContents(k_PrefabPath);

        var meshes = root.GetComponentsInChildren<ProBuilderMesh>(true);
        Debug.Log($"Test_Room.prefab: stripping {meshes.Length} ProBuilderMesh component(s).");

        foreach (var pb in meshes)
            doStrip.Invoke(null, new object[] { pb, false });

        PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();

        Debug.Log("Done. Test_Room.prefab meshes are now static (ProBuilder editing no longer available).");
    }
}
