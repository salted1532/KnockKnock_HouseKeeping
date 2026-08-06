using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Splits every prop out of the raw imported models under Assets/AssetsFolder into
/// individual prefabs under Assets/My/Prefabs/&lt;Category&gt;/, and their materials
/// into Assets/My/Materials/&lt;Category&gt;/. Materials that were already extracted
/// as standalone .mat assets (the alpha-clip transparency work) are moved as-is so
/// every existing reference (model .meta externalObjects remaps) keeps working,
/// since AssetDatabase.MoveAsset preserves the asset GUID.
///
/// Run from the Unity menu: Tools/Organize Assets/Extract Props To Prefabs+Materials
/// Set DryRun = true first to see what would happen (Console log) without touching
/// any files, then flip it off to actually run.
/// </summary>
public static class AssetOrganizer
{
    // ponytail: flip to true to log the plan without writing anything, safest way to sanity check the category rules first
    private const bool DryRun = false;

    private const string PrefabRoot = "Assets/My/Prefabs";
    private const string MaterialRoot = "Assets/My/Materials";

    // Every extracted prop was coming out lying on its side — the source packs' root axis doesn't
    // match Unity's Y-up. Confirmed fix (2026-08-06): rotate -90 on X.
    private static readonly Quaternion FixedRotation = Quaternion.Euler(-90f, 0f, 0f);

    // ponytail: reset local position/rotation on extraction, keep scale — most of these packs lay
    // props out scattered across the source scene for preview purposes, not at a usable local origin.
    // Flip off per-pack in SourceModels below if a pack turns out to already be authored at origin.
    private const bool ResetPositionRotationDefault = true;

    private static readonly (string path, string label, bool resetTransform)[] SourceModels =
    {
        ("Assets/AssetsFolder/6twelve/Models/6twelve.fbx", "6twelve", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/All/Models/All.fbx", "All", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Buildings/Models/Buildings.fbx", "Buildings", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/BurgerPiz/Models/BurgerPiz.fbx", "BurgerPiz", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Bus_stop/Models/Props/Props.fbx", "BusStop_Props", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Bus_stop/Models/Stops/Stop.fbx", "BusStop_Stop", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Bus_stop/Models/Stops/Stop_01.fbx", "BusStop_Stop01", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/DINER/Models/DINER.fbx", "DINER", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/DINER/Models/Objects.fbx", "DINER_Objects", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/House/Models/House.fbx", "House", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/House/Models/House_Colliders.fbx", "House_Colliders", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Laundry/Models/Laundry.fbx", "Laundry", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Laundry/Models/Laundry_Props.fbx", "Laundry_Props", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Models pack psx/Models/models.fbx", "PSX", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Objects_Interior(Village)_Demo/Models/Objects_Interior(Village)_Demo.fbx", "Interior", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Pizzeria/Pizzeria/Models/Pizzeria_Props.fbx", "Pizzeria_Props", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Pizzeria/Pizzeria/Models/Pizzeria_Scene.fbx", "Pizzeria_Scene", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Tacos/Models/Tacos.fbx", "Tacos", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Tacos/Models/Tacos_Props.fbx", "Tacos_Props", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Trailer_Park/Characters/Character_Female.fbx", "TP_CharFemale", false),
        ("Assets/AssetsFolder/Trailer_Park/Characters/Character_Female_01.fbx", "TP_CharFemale01", false),
        ("Assets/AssetsFolder/Trailer_Park/Characters/Character_Male.fbx", "TP_CharMale", false),
        ("Assets/AssetsFolder/Trailer_Park/Characters/Character_Male_01.fbx", "TP_CharMale01", false),
        ("Assets/AssetsFolder/Trailer_Park/Models/Trailer_Park.fbx", "TrailerPark", ResetPositionRotationDefault),
        ("Assets/AssetsFolder/Trailer_Park/Models/Trailer_Park_Props.fbx", "TrailerPark_Props", ResetPositionRotationDefault),
    };

    // First matching rule wins. Keywords are matched case-insensitively as substrings
    // of the prop's base name (trailing _01 / .001 style suffixes already stripped).
    private static readonly (string category, string[] keywords)[] Rules =
    {
        ("Lighting/Indoor_Lamps", new[] { "desk_lamp", "ceiling_fan", "ventilator", "focus", "lamp" }),
        ("Lighting/Outdoor_Lighting", new[] { "lamppost", "streetlight", "spotlight", "post_light", "electric_pole" }),

        ("Furniture/Beds", new[] { "bed", "litera", "colchon", "mattress" }),
        ("Furniture/Chairs_Seating", new[] { "armchair", "chair", "seat", "silla" }),
        ("Furniture/Sofas_Couches", new[] { "couch", "sofa" }),
        ("Furniture/Tables", new[] { "table", "mesa", "desk" }),
        ("Furniture/Storage", new[] { "wardrobe", "cabinet", "cajonera", "drawer", "cupboard", "shelving", "shelf", "bookcase", "librero", "toolbox", "closet" }),
        ("Furniture/Soft_Furnishing", new[] { "curtain", "blind", "rug", "pillow", "almohada", "cushion", "fabricplain" }),

        ("Bathroom/Fixtures", new[] { "toilet", "shower", "bathtub", "tub", "urinary", "washbasin", "lavamanos", "hand_dryer" }),
        ("Bathroom/Toiletries", new[] { "shampoo", "champoo", "toothbrush", "toothpaste", "towel", "toilet_paper", "bar_soap", "hand_soap", "soap_holder", "soap_dispenser" }),

        ("Kitchen/Sinks", new[] { "sink" }),
        ("Kitchen/Appliances", new[] { "refrigerator", "refrijerator", "fridge", "oven", "stove", "microwave", "microondas", "toaster", "blender", "licuadora", "coffee_m", "coffee_maker", "dough_", "fryer", "frying", "grill", "kitchen_hood", "cooker_hood", "washing_machine", "dishwasher" }),
        ("Kitchen/Cookware", new[] { "cookware", "casserole", "cooking_pot", "skillet", "sarten", "pot_lid", "cutting_board", "ladle", "stewpot", "olla" }),
        ("Kitchen/Tableware_Cutlery", new[] { "cutlery", "fork", "knife", "spoon", "plate", "dish", "bowl", "cup", "glass", "napkin", "tray", "salt_shaker", "cutlery_holder", "tumbler", "vaso", "baso" }),

        ("Laundry_Cleaning/Appliances", new[] { "laundry" }),
        ("Laundry_Cleaning/Supplies", new[] { "detergent", "softener", "soap_powder", "soap_box", "soap_wrapper" }),
        ("Laundry_Cleaning/Cleaning_Tools", new[] { "broom", "mop", "trash_can", "trash", "garbage_bag", "dustpan", "bucket", "sponge", "cleaning" }),

        ("Food/Fruits_Vegetables", new[] { "apple", "banana", "avocado", "broccoli", "cabbage", "carrot", "cauliflower", "celery", "cucumber", "eggplant", "garlic", "grape", "kiwi", "limon", "lemon", "mango", "onion", "orange", "papaya", "pear", "pepper", "pineapple", "potato", "pumpkin", "spinach", "strawberry", "turnip", "watermelon", "zucchini", "beetroot", "artichoke", "asparagus", "brussel_sprouts", "cantaloupe", "coconut", "tomato", "yam" }),
        ("Food/Bakery_Snacks", new[] { "bread", "donut", "donus", "cookies", "waffle", "popcorn", "cake", "candys" }),
        ("Food/Prepared_Food", new[] { "pizza", "burger", "hot_dog", "fried_chicken", "fried_foods", "taco", "chicken", "meat", "egg", "cereal", "flan" }),
        ("Food/Drinks_Condiments", new[] { "mayonnaise", "mustard", "ketchup", "milk", "water", "soda", "soft_drink", "coffe", "cooking_oil", "olive_oil", "sauce", "spices", "sal", "salt", "rice", "refreshment" }),

        ("Plants_Nature/Trees", new[] { "tree", "arbol" }),
        ("Plants_Nature/Flowers", new[] { "flower" }),
        ("Plants_Nature/Bushes_Plants", new[] { "natureplants", "bush", "plant", "branch" }),
        ("Plants_Nature/Ground_Terrain", new[] { "grass", "terrain", "rocks", "dry_leaves", "wheat", "hedges", "ground", "soil", "garden", "land" }),

        ("Buildings_Architecture/Doors_Windows", new[] { "garage_door", "door", "window", "ventana" }),
        ("Buildings_Architecture/Roofing", new[] { "roofing", "rooftiles", "roof" }),
        ("Buildings_Architecture/Fences_Railings", new[] { "fence", "railing", "guard_rail" }),
        ("Buildings_Architecture/Walls_Floors_Ceilings", new[] { "wall", "floor", "ceiling", "techo", "tilesplain", "tiles", "brick", "concrete", "asphalt", "plaster" }),
        ("Buildings_Architecture/Buildings", new[] { "building", "house", "casa", "home" }),

        ("Electronics/Phones", new[] { "phone" }),
        ("Electronics/Registers_Machines", new[] { "cash_register", "payment_terminal", "vending_machine", "soda_machine", "soda_fountain", "money" }),
        ("Electronics/Appliances", new[] { "tv", "radio", "monitor", "keyboard", "video_player", "fuse_box", "alarm_clock", "clock", "light_switch" }),

        ("Vehicles/Parts", new[] { "tire", "rueda", "petrol_can", "gas_tank" }),
        ("Vehicles/Cars", new[] { "auto", "bus_stop", "bus", "carro" }),

        ("Decor/Wall_Art", new[] { "painting", "photo_frame", "picture_frame", "mirror" }),
        ("Decor/Accessories", new[] { "vase", "book", "magazine", "palette", "figurine" }),

        ("Outdoor_Street/Street_Furniture", new[] { "traffic_light", "electric_pole", "crosswalk" }),
        ("Outdoor_Street/Road", new[] { "road", "sidewalk", "parking_lot" }),

        ("Signage", new[] { "sign", "menu", "lottery", "oxxo" }),

        ("Characters", new[] { "character" }),
    };

    // Not real props: bare Blender primitives, rig/armature helper nodes, single-letter or
    // letter+digit code names left over from the source files (e.g. "Ar10", "B1", "V2").
    private static readonly HashSet<string> SkipExact = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "cube", "cylinder", "sphere", "torus", "circle", "plane", "nurbscurve", "board",
        "checker", "resources", "base_c", "text", "entry", "ar", "ma", "taa", "med", "r_v", "pu_m",
    };

    private static readonly Regex ShortCodeName = new Regex(@"^[A-Za-z]{1,2}[0-9]{1,3}$", RegexOptions.Compiled);
    private static readonly Regex TrailingIndex = new Regex(@"(_[0-9]{1,3}|\.[0-9]{3})$", RegexOptions.Compiled);

    [MenuItem("Tools/Organize Assets/Extract Props To Prefabs+Materials")]
    public static void Run()
    {
        if (!DryRun)
        {
            EnsureFolder(PrefabRoot);
            EnsureFolder(MaterialRoot);
        }

        var materialCache = new Dictionary<string, string>(); // source material asset path -> new/moved destination path
        // Tracks paths this run has already handed out, so re-running the tool overwrites its own
        // previous output cleanly instead of piling up "_label", "_label_2", ... duplicates each time.
        var allocatedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int prefabCount = 0, materialMoved = 0, materialCreated = 0, skipped = 0;

        foreach (var (path, label, resetTransform) in SourceModels)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                Debug.LogWarning($"[AssetOrganizer] Model not found, skipping: {path}");
                continue;
            }

            // Read the top-level prop list straight off the model asset (not a scene instance of it) —
            // we only need it to enumerate names here, each prop gets its own disconnected clone below.
            var children = new List<Transform>();
            foreach (Transform child in root.transform) children.Add(child);

            foreach (var child in children)
            {
                string rawName = child.name;
                string baseName = TrailingIndex.Replace(rawName, "");

                if (SkipExact.Contains(baseName) || ShortCodeName.IsMatch(baseName))
                {
                    skipped++;
                    continue;
                }

                string category = Classify(baseName);

                // A GameObject that's still nested inside a live Prefab instance can't be handed to
                // SaveAsPrefabAsset directly ("Can't save part of a Prefab instance as a Prefab").
                // Object.Instantiate gives us a fully independent clone instead — no prefab connection.
                GameObject clone = (GameObject)UnityEngine.Object.Instantiate(child.gameObject);
                clone.name = rawName;
                try
                {
                    // Source packs export Z-up/lying-down; every extracted prop gets this fixed rotation
                    // regardless of resetTransform (that flag only controls whether position is re-centered).
                    clone.transform.localRotation = FixedRotation;
                    if (resetTransform)
                    {
                        clone.transform.localPosition = Vector3.zero;
                    }

                    RemapMaterials(clone.transform, category, label, materialCache, allocatedPaths, ref materialMoved, ref materialCreated);

                    string prefabFolder = $"{PrefabRoot}/{category}";
                    string prefabPath = UniquePath(prefabFolder, rawName, label, "prefab", allocatedPaths);

                    if (DryRun)
                    {
                        Debug.Log($"[AssetOrganizer][dry-run] {path} :: {rawName} -> {prefabPath}");
                    }
                    else
                    {
                        EnsureFolder(prefabFolder);
                        PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
                    }
                    prefabCount++;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }
        }

        if (!DryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[AssetOrganizer] Done (DryRun={DryRun}). Prefabs: {prefabCount}, materials moved: {materialMoved}, materials created: {materialCreated}, props skipped as non-prop: {skipped}");
    }

    private static void RemapMaterials(Transform propRoot, string category, string packLabel, Dictionary<string, string> cache, HashSet<string> allocatedPaths, ref int moved, ref int created)
    {
        var renderers = propRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var mats = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;

                string sourcePath = AssetDatabase.GetAssetPath(mat);
                bool isEmbedded = string.IsNullOrEmpty(sourcePath) || sourcePath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
                string cacheKey = isEmbedded ? $"{packLabel}|{mat.name}" : sourcePath;

                // Already organized from a previous run (re-running the tool) — leave it where it is.
                if (!isEmbedded && sourcePath.StartsWith(MaterialRoot, StringComparison.OrdinalIgnoreCase))
                {
                    cache[cacheKey] = sourcePath;
                    continue;
                }

                if (cache.TryGetValue(cacheKey, out string existingDest))
                {
                    if (isEmbedded)
                    {
                        var existingAsset = AssetDatabase.LoadAssetAtPath<Material>(existingDest);
                        if (existingAsset != null && mats[i] != existingAsset)
                        {
                            mats[i] = existingAsset;
                            changed = true;
                        }
                    }
                    continue; // already handled (moved or created) this material once
                }

                string matFolder = $"{MaterialRoot}/{category}";
                string destPath;

                if (isEmbedded)
                {
                    destPath = UniquePath(matFolder, mat.name, packLabel, "mat", allocatedPaths);
                    if (!DryRun)
                    {
                        EnsureFolder(matFolder);
                        Material copy = new Material(mat);
                        AssetDatabase.CreateAsset(copy, destPath);
                        var createdAsset = AssetDatabase.LoadAssetAtPath<Material>(destPath);
                        mats[i] = createdAsset;
                        changed = true;
                    }
                    created++;
                }
                else
                {
                    string matName = Path.GetFileNameWithoutExtension(sourcePath);
                    destPath = UniquePath(matFolder, matName, packLabel, "mat", allocatedPaths);
                    if (!DryRun)
                    {
                        EnsureFolder(matFolder);
                        string err = AssetDatabase.MoveAsset(sourcePath, destPath);
                        if (!string.IsNullOrEmpty(err))
                        {
                            Debug.LogWarning($"[AssetOrganizer] Move failed for {sourcePath} -> {destPath}: {err}");
                            destPath = sourcePath; // keep original reference, don't lose the link
                        }
                    }
                    moved++;
                }

                cache[cacheKey] = destPath;
            }

            if (changed && !DryRun)
            {
                renderer.sharedMaterials = mats;
            }
        }
    }

    private static string Classify(string baseName)
    {
        string lower = baseName.ToLowerInvariant();
        foreach (var (category, keywords) in Rules)
        {
            foreach (var kw in keywords)
            {
                if (lower.Contains(kw)) return category;
            }
        }
        return "Uncategorized";
    }

    // Uniqueness is decided against paths this same run has already handed out (allocatedPaths),
    // not against what's currently on disk — that way re-running the tool overwrites its own
    // previous output at the same paths instead of accumulating "_label_2", "_label_3", ... clones.
    private static string UniquePath(string folder, string name, string packLabel, string extension, HashSet<string> allocatedPaths)
    {
        string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        string plain = $"{folder}/{safeName}.{extension}";
        if (allocatedPaths.Add(plain)) return plain;

        string withLabel = $"{folder}/{safeName}_{packLabel}.{extension}";
        if (allocatedPaths.Add(withLabel)) return withLabel;

        int i = 2;
        string numbered;
        do
        {
            numbered = $"{folder}/{safeName}_{packLabel}_{i}.{extension}";
            i++;
        } while (!allocatedPaths.Add(numbered));
        return numbered;
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
        string parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
        string leaf = Path.GetFileName(assetFolderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
