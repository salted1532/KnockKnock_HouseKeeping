//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]

public class Outline : MonoBehaviour {
  private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

  public enum Mode {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly
  }

  public Mode OutlineMode {
    get { return outlineMode; }
    set {
      outlineMode = value;
      needsUpdate = true;
    }
  }

  public Color OutlineColor {
    get { return outlineColor; }
    set {
      outlineColor = value;
      needsUpdate = true;
    }
  }

  public float OutlineWidth {
    get { return outlineWidth; }
    set {
      outlineWidth = value;
      needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3 {
    public List<Vector3> data;
  }

  [SerializeField]
  private Mode outlineMode;

  [SerializeField]
  private Color outlineColor = Color.white;

  [SerializeField, Range(0f, 10f)]
  private float outlineWidth = 2f;

  [Header("Optional")]

  [SerializeField, Tooltip("이 Transform 들(및 그 자식)의 Renderer 는 외곽선에서 제외 (조명 스위치 안의 램프 등). LOCAL PATCH — doc/0083")]
  private Transform[] excludeRoots;

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  private bool precomputeOutline;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  private Renderer[] renderers;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  // LOCAL PATCH (doc/0144): renderer.materials 세터가 머티리얼을 복제하므로, 렌더러에 실제로 붙은
  // 복제본을 따로 잡아 여기에 담고 UpdateMaterialProperties 가 그 인스턴스들에도 색/두께/ZTest 를 쓴다.
  // (안 그러면 복제본이 OutlineFill.mat 애셋 기본값 — 흰색·두께2 — 으로 고정돼 외곽선 색이 안 먹음)
  private readonly List<Material> liveMaskMaterials = new List<Material>();
  private readonly List<Material> liveFillMaterials = new List<Material>();

  private bool needsUpdate;

  void Awake() {

    // Cache renderers (include inactive: 토글로 스왑되는 자식 메쉬도 캐시)
    renderers = GetComponentsInChildren<Renderer>(true);

    // LOCAL PATCH (doc/0083): excludeRoots 자식 렌더러 제외
    if (excludeRoots != null && excludeRoots.Length > 0) {
      renderers = System.Array.FindAll(renderers, r => !IsExcluded(r.transform));
    }

    // Instantiate outline materials
    outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
    outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

    outlineMaskMaterial.name = "OutlineMask (Instance)";
    outlineFillMaterial.name = "OutlineFill (Instance)";

    // Retrieve or generate smooth normals
    LoadSmoothNormals();

    // Apply material properties immediately
    needsUpdate = true;
  }

  // LOCAL PATCH (doc/0083)
  bool IsExcluded(Transform t) {
    foreach (var root in excludeRoots) {
      if (root != null && t.IsChildOf(root)) {
        return true; // IsChildOf 는 자기 자신도 true
      }
    }
    return false;
  }

  void OnEnable() {
    liveMaskMaterials.Clear();
    liveFillMaterials.Clear();

    foreach (var renderer in renderers) {

      // Append outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Add(outlineMaskMaterial);
      materials.Add(outlineFillMaterial);

      renderer.materials = materials.ToArray();

      // LOCAL PATCH (doc/0144): 세터가 복제했을 수 있으니 렌더러에 실제 붙은 인스턴스를 셰이더로 골라 캐시
      // (sharedMaterials 게터는 복제하지 않음)
      foreach (var m in renderer.sharedMaterials) {
        if (m == null) continue;
        if (m.shader == outlineFillMaterial.shader) { if (!liveFillMaterials.Contains(m)) liveFillMaterials.Add(m); }
        else if (m.shader == outlineMaskMaterial.shader) { if (!liveMaskMaterials.Contains(m)) liveMaskMaterials.Add(m); }
      }
    }

    // 재활성화 때마다 새 복제본에 프로퍼티를 다시 적용해야 함
    needsUpdate = true;
  }

  void OnValidate() {

    // Update material properties
    needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0) {
      Bake();
    }
  }

  void Update() {
    if (needsUpdate) {
      needsUpdate = false;

      UpdateMaterialProperties();
    }
  }

  void OnDisable() {
    foreach (var renderer in renderers) {

      // Remove outline shaders
      var materials = renderer.sharedMaterials.ToList();

      // LOCAL PATCH (doc/0144): 참조가 아니라 셰이더로 제거 — 복제본이라 Remove(field) 가 안 먹음
      materials.RemoveAll(m => m != null &&
        (m.shader == outlineFillMaterial.shader || m.shader == outlineMaskMaterial.shader));

      renderer.materials = materials.ToArray();
    }

    liveMaskMaterials.Clear();
    liveFillMaterials.Clear();
  }

  void OnDestroy() {

    // Destroy material instances
    Destroy(outlineMaskMaterial);
    Destroy(outlineFillMaterial);
  }

  void Bake() {

    // Generate smooth normals for each mesh
    var bakedMeshes = new HashSet<Mesh>();

    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true)) {

      // Skip duplicates
      if (!bakedMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Serialize smooth normals
      var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {

    // Retrieve or generate smooth normals
    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true)) {

      // Skip if smooth normals have already been adopted
      if (!registeredMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Retrieve or generate smooth normals
      var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
      var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

      // Store smooth normals in UV3
      meshFilter.sharedMesh.SetUVs(3, smoothNormals);

      // Combine submeshes
      var renderer = meshFilter.GetComponent<Renderer>();

      if (renderer != null) {
        CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
      }
    }

    // Clear UV3 on skinned mesh renderers
    foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>(true)) {

      // Skip if UV3 has already been reset
      if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) {
        continue;
      }

      // Clear UV3
      skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];

      // Combine submeshes
      CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
    }
  }

  List<Vector3> SmoothNormals(Mesh mesh) {

    // Group vertices by location
    var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

    // Copy normals to a new list
    var smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (var group in groups) {

      // Skip single vertices
      if (group.Count() == 1) {
        continue;
      }

      // Calculate the average normal
      var smoothNormal = Vector3.zero;

      foreach (var pair in group) {
        smoothNormal += smoothNormals[pair.Value];
      }

      smoothNormal.Normalize();

      // Assign smooth normal to each vertex
      foreach (var pair in group) {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals;
  }

  void CombineSubmeshes(Mesh mesh, Material[] materials) {

    // Skip meshes with a single submesh
    if (mesh.subMeshCount == 1) {
      return;
    }

    // Skip if submesh count exceeds material count
    if (mesh.subMeshCount > materials.Length) {
      return;
    }

    // Append combined submesh
    mesh.subMeshCount++;
    mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
  }

  void UpdateMaterialProperties() {

    // 템플릿 인스턴스 + 렌더러에 실제 붙은 복제본 모두에 적용 (LOCAL PATCH doc/0144)
    ApplyProperties(outlineMaskMaterial, outlineFillMaterial);

    int n = Mathf.Max(liveMaskMaterials.Count, liveFillMaterials.Count);
    for (int i = 0; i < n; i++) {
      var mask = i < liveMaskMaterials.Count ? liveMaskMaterials[i] : null;
      var fill = i < liveFillMaterials.Count ? liveFillMaterials[i] : null;
      ApplyProperties(mask, fill);
    }
  }

  void ApplyProperties(Material mask, Material fill) {

    if (fill != null) {
      fill.SetColor("_OutlineColor", outlineColor);
    }

    // Apply properties according to mode
    var maskZTest = UnityEngine.Rendering.CompareFunction.Always;
    var fillZTest = UnityEngine.Rendering.CompareFunction.Always;
    float width = outlineWidth;

    switch (outlineMode) {
      case Mode.OutlineAll:
        maskZTest = UnityEngine.Rendering.CompareFunction.Always;
        fillZTest = UnityEngine.Rendering.CompareFunction.Always;
        break;

      case Mode.OutlineVisible:
        maskZTest = UnityEngine.Rendering.CompareFunction.Always;
        fillZTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        break;

      case Mode.OutlineHidden:
        maskZTest = UnityEngine.Rendering.CompareFunction.Always;
        fillZTest = UnityEngine.Rendering.CompareFunction.Greater;
        break;

      case Mode.OutlineAndSilhouette:
        maskZTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        fillZTest = UnityEngine.Rendering.CompareFunction.Always;
        break;

      case Mode.SilhouetteOnly:
        maskZTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        fillZTest = UnityEngine.Rendering.CompareFunction.Greater;
        width = 0f;
        break;
    }

    if (mask != null) mask.SetFloat("_ZTest", (float)maskZTest);
    if (fill != null) {
      fill.SetFloat("_ZTest", (float)fillZTest);
      fill.SetFloat("_OutlineWidth", width);
    }
  }
}
