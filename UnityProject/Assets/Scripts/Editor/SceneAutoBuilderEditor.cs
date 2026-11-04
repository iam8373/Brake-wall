#if UNITY_EDITOR
using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// One-click builder: BreakToEscape > Build Complete Playable Scene.
/// Creates material and prefab assets under Assets/Generated, builds the scene (systems, player
/// rig, crusher, every UI screen from docs/wireframes/WIREFRAMES.md), wires every button, places
/// the player and crusher from the generated mine layout, and saves Assets/Scenes/Main.unity.
/// At runtime GameBootstrap regenerates the same deterministic mine and re-applies the layout, so
/// nothing depends on hard-coded coordinates.
/// </summary>
public static class SceneAutoBuilderEditor
{
    private const int LayerVoxelWall = 6;
    private const int LayerRubble = 7;
    private const string GeneratedRoot = "Assets/Generated";
    private const string ScenePath = "Assets/Scenes/Main.unity";

    // Reference resolution = the 844 × 390 wireframe artboard at 2×. Wireframe pixel values are
    // multiplied by 2 below.
    private static readonly Vector2 ReferenceResolution = new Vector2(1688f, 780f);

    private static readonly Color HazardYellow = new Color(0.96f, 0.78f, 0.1f, 1f);
    private static readonly Color PanelDark = new Color(0.08f, 0.08f, 0.09f, 0.94f);
    private static readonly Color PanelMid = new Color(0.16f, 0.16f, 0.17f, 0.96f);
    private static readonly Color TextLight = new Color(0.96f, 0.95f, 0.9f, 1f);
    private static readonly Color TextDark = new Color(0.08f, 0.08f, 0.08f, 1f);

    [MenuItem("BreakToEscape/Build Complete Playable Scene")]
    public static void BuildCompleteScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var economyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Data/economy.json");
        if (!economyAsset)
        {
            EditorUtility.DisplayDialog("Break to Escape", "Assets/Resources/Data/economy.json is missing. Copy the whole UnityProject/Assets folder first.", "OK");
            return;
        }
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("Break to Escape", "TextMeshPro essentials are not imported. Use Window > TextMeshPro > Import TMP Essential Resources, then run the builder again.", "OK");
            return;
        }
        EconomyData data = EconomyData.FromJson(economyAsset.text);

        EnsureLayers();
        Physics.IgnoreLayerCollision(LayerRubble, LayerRubble, false);
        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedRoot, "Materials");
        EnsureFolder(GeneratedRoot, "Prefabs");
        EnsureFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ------------------------------------------------------------ materials (assets)
        Material voxelMat = VoxelMaterial();
        Material lineMat = ShaderMaterial("Mat_PreviewLines", "BreakToEscape/UnlitVertexColor", "Universal Render Pipeline/Particles/Unlit", Color.white);
        Material particleMat = ShaderMaterial("Mat_Particles", "Universal Render Pipeline/Particles/Unlit", "BreakToEscape/UnlitVertexColor", Color.white);
        Material placeholderMat = LitMaterial("Mat_Placeholder", Color.white, false);
        Material rubbleMat = LitMaterial("Mat_Rubble", Color.white, true);
        Material gloveMat = LitMaterial("Mat_WorkGlove", new Color(0.22f, 0.24f, 0.28f), false);
        Material gauntletMat = LitMaterial("Mat_WorkGauntlet", new Color(0.85f, 0.55f, 0.12f), false);
        Material vac1 = LitMaterial("Mat_Vacuum_DustBuster", new Color(0.92f, 0.76f, 0.12f), false);
        Material vac2 = LitMaterial("Mat_Vacuum_OreHoover", new Color(0.9f, 0.42f, 0.12f), false);
        Material vac3 = LitMaterial("Mat_Vacuum_SuctionCannon", new Color(0.38f, 0.26f, 0.65f), false);
        Material dynamiteMat = LitMaterial("Mat_Dynamite", new Color(0.85f, 0.15f, 0.12f), false);
        Material sparkMat = LitMaterial("Mat_FuseSpark", new Color(1f, 0.85f, 0.3f), false);
        Material ghostMat = ShaderMaterial("Mat_DynamiteGhost", "BreakToEscape/UnlitVertexColor", "Universal Render Pipeline/Unlit", new Color(1f, 1f, 1f, 0.45f));
        Material steelMat = LitMaterial("Mat_CrusherSteel", new Color(0.32f, 0.35f, 0.38f), false);

        // ------------------------------------------------------------ prefabs (assets)
        RubbleCube rubblePrefab = BuildRubblePrefab(rubbleMat);
        StickyDynamite dynamitePrefab = BuildDynamitePrefab(dynamiteMat, sparkMat);
        TextMeshPro popupPrefab = BuildPopupPrefab();

        // ------------------------------------------------------------ layout preview (same generator as runtime)
        var grid = new VoxelGrid(data.worldSizeVoxels.x, data.worldSizeVoxels.y, data.worldSizeVoxels.z, new VoxelMaterialTable(data));
        MineLayout layout = HandBuiltMineGenerator.Generate(grid, data.treasureOneInVoxels, data.nookTreasureClusterSize);
        Vector3 ToWorld(BreakToEscape.Core.Float3 p) => new Vector3(p.x, p.y, p.z) * data.voxelSize;

        // ------------------------------------------------------------ lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.2f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.04f);
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance = 30f;
        var sunObj = new GameObject("Main Light (shaft light)");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.35f;
        sun.color = new Color(0.75f, 0.8f, 1f);
        sun.shadows = LightShadows.Soft;
        sunObj.transform.rotation = Quaternion.Euler(60f, -30f, 0f);

        // ------------------------------------------------------------ world
        var worldObj = new GameObject("VoxelWorld");
        VoxelWorldManager world = worldObj.AddComponent<VoxelWorldManager>();
        world.voxelVertexColorMaterial = voxelMat;
        world.voxelWallLayer = LayerVoxelWall;

        // ------------------------------------------------------------ crusher
        Crusher07Controller crusher = BuildCrusher(ToWorld(layout.crusherPoint), steelMat, popupPrefab);

        // ------------------------------------------------------------ player rig
        var playerObj = new GameObject("Player");
        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.6f;
        cc.radius = 0.3f;
        cc.stepOffset = 0.3f;
        cc.center = Vector3.zero;
        playerObj.transform.position = ToWorld(layout.spawnPoint) + Vector3.up * 0.85f;
        MobileFirstPersonController controller = playerObj.AddComponent<MobileFirstPersonController>();
        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(playerObj.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        controller.cameraPivot = pivot.transform;

        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(pivot.transform, false);
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 72f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 32f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        camObj.AddComponent<AudioListener>();
        VoxelAim aim = camObj.AddComponent<VoxelAim>();
        aim.playerCamera = cam;
        CameraEffects camFx = camObj.AddComponent<CameraEffects>();
        camFx.targetCamera = cam;
        camFx.player = controller;

        var viewmodel = new GameObject("Viewmodel");
        viewmodel.transform.SetParent(camObj.transform, false);
        ToolViewmodelAnimator animator = viewmodel.AddComponent<ToolViewmodelAnimator>();
        animator.leftArm = BuildArm(viewmodel.transform, "LeftArm", new Vector3(-0.32f, -0.26f, 0.45f), gloveMat, gauntletMat, out GameObject l0, out GameObject l1);
        animator.rightArm = BuildArm(viewmodel.transform, "RightArm", new Vector3(0.32f, -0.26f, 0.45f), gloveMat, gauntletMat, out GameObject r0, out GameObject r1);
        animator.leftGloveTiers = new[] { l0, l1 };
        animator.rightGloveTiers = new[] { r0, r1 };
        var vacRoot = new GameObject("VacuumRoot");
        vacRoot.transform.SetParent(viewmodel.transform, false);
        vacRoot.transform.localPosition = new Vector3(0.35f, -0.28f, 0.52f);
        animator.vacuumRoot = vacRoot.transform;
        GameObject vt0 = Child(vacRoot.transform, "DustBuster", PrimitiveType.Cylinder, Vector3.zero, new Vector3(78f, 0f, 0f), new Vector3(0.14f, 0.28f, 0.14f), vac1);
        GameObject vt1 = Child(vacRoot.transform, "OreHoover", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0f, 0f), new Vector3(0.22f, 0.22f, 0.48f), vac2);
        GameObject vt2 = Child(vacRoot.transform, "SuctionCannon", PrimitiveType.Capsule, Vector3.zero, new Vector3(78f, 0f, 0f), new Vector3(0.26f, 0.36f, 0.26f), vac3);
        vt1.SetActive(false);
        vt2.SetActive(false);
        animator.vacuumTiers = new[] { vt0, vt1, vt2 };
        var nozzle = new GameObject("VacuumNozzle");
        nozzle.transform.SetParent(vacRoot.transform, false);
        nozzle.transform.localPosition = new Vector3(0f, 0.05f, 0.35f);
        animator.dynamiteInHand = Child(viewmodel.transform, "DynamiteInHand", PrimitiveType.Cylinder, new Vector3(0.32f, -0.14f, 0.52f), new Vector3(20f, 0f, 15f), new Vector3(0.06f, 0.14f, 0.06f), dynamiteMat).transform;

        PunchAction punch = viewmodel.AddComponent<PunchAction>();
        punch.aim = aim;
        punch.animator = animator;
        DynamiteAction dynamite = viewmodel.AddComponent<DynamiteAction>();
        dynamite.aim = aim;
        dynamite.animator = animator;
        dynamite.dynamitePrefab = dynamitePrefab;
        GameObject ghost = Child(null, "DynamiteGhost", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.08f, 0.16f, 0.08f), ghostMat);
        ghost.SetActive(false);
        dynamite.ghostPreview = ghost;

        var lampObj = new GameObject("Headlamp");
        lampObj.transform.SetParent(camObj.transform, false);
        lampObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        Light lamp = lampObj.AddComponent<Light>();
        lamp.type = LightType.Spot;
        lamp.color = new Color(1f, 0.92f, 0.76f);
        lamp.shadows = LightShadows.None;
        HeadlampController headlamp = viewmodel.AddComponent<HeadlampController>();
        headlamp.lamp = lamp;

        FirstPersonToolController tools = viewmodel.AddComponent<FirstPersonToolController>();
        tools.animator = animator;
        tools.punch = punch;
        tools.dynamite = dynamite;
        tools.headlamp = headlamp;
        tools.aim = aim;
        tools.playerCamera = cam;

        VoxelWireframePreview preview = camObj.AddComponent<VoxelWireframePreview>();
        preview.aim = aim;
        preview.punch = punch;
        preview.lineMaterial = lineMat;

        // ------------------------------------------------------------ systems
        var systems = new GameObject("_Systems");
        GameBootstrap boot = systems.AddComponent<GameBootstrap>();
        SaveManager save = systems.AddComponent<SaveManager>();
        DeviceTierScaler tiers = systems.AddComponent<DeviceTierScaler>();
        CompanyStoreEconomyManager economy = systems.AddComponent<CompanyStoreEconomyManager>();
        RubbleSuctionManager suction = systems.AddComponent<RubbleSuctionManager>();
        MonetizationServicesManager monetization = systems.AddComponent<MonetizationServicesManager>();
        StoryDirector story = systems.AddComponent<StoryDirector>();
        TreasureAndMapManager treasure = systems.AddComponent<TreasureAndMapManager>();
        ExitVaultAndLogbookController exitVault = systems.AddComponent<ExitVaultAndLogbookController>();
        AudioHapticsManager audio = systems.AddComponent<AudioHapticsManager>();
        EffectsManager effects = systems.AddComponent<EffectsManager>();
        InteractionController interaction = systems.AddComponent<InteractionController>();

        suction.toolController = tools;
        suction.vacuumNozzlePoint = nozzle.transform;
        suction.rubblePrefab = rubblePrefab;
        suction.rubbleLayerMask = 1 << LayerRubble;
        tiers.rubbleManager = suction;
        tiers.mainCamera = cam;
        tiers.mainLight = sun;
        economy.toolController = tools;
        economy.punch = punch;
        economy.headlamp = headlamp;
        economy.crusher07 = crusher;
        economy.suctionManager = suction;
        crusher.rubbleLayerMask = 1 << LayerRubble;
        story.interaction = interaction;
        story.exitVault = exitVault;
        story.player = playerObj.transform;
        story.placeholderMaterial = placeholderMat;
        treasure.playerTransform = playerObj.transform;
        treasure.interaction = interaction;
        treasure.placeholderMaterial = LitMaterial("Mat_TreasureGold", new Color(1f, 0.82f, 0.15f), false);
        interaction.playerCamera = cam;
        audio.toolController = tools;
        audio.punch = punch;
        audio.suction = suction;
        audio.sfxOneShotSource = systems.AddComponent<AudioSource>();
        audio.vacuumLoopSource = systems.AddComponent<AudioSource>();
        audio.crusherLoopSource = systems.AddComponent<AudioSource>();
        audio.ambienceLoopSource = systems.AddComponent<AudioSource>();
        audio.musicSource = systems.AddComponent<AudioSource>();
        foreach (AudioSource src in systems.GetComponents<AudioSource>()) { src.playOnAwake = false; src.spatialBlend = 0f; }
        effects.punch = punch;
        effects.player = playerObj.transform;
        effects.particleMaterial = particleMat;
        var flashObj = new GameObject("BlastFlash");
        flashObj.transform.SetParent(systems.transform, false);
        Light flash = flashObj.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.range = 10f;
        flash.intensity = 6f;
        flash.color = new Color(1f, 0.75f, 0.4f);
        effects.flashLight = flash;
        var escapeLightObj = new GameObject("EscapeDaylight");
        escapeLightObj.transform.SetParent(systems.transform, false);
        Light daylight = escapeLightObj.AddComponent<Light>();
        daylight.type = LightType.Point;
        daylight.range = 14f;
        daylight.color = new Color(1f, 0.97f, 0.9f);
        exitVault.escapeLight = daylight;

        // ------------------------------------------------------------ UI
        UIBuild ui = BuildUI(systems, controller, tools, punch, economy, suction, crusher, monetization, tiers, interaction, treasure, exitVault, headlamp, audio, effects, boot);

        boot.world = world;
        boot.economyManager = economy;
        boot.suction = suction;
        boot.crusher = crusher;
        boot.saveManager = save;
        boot.tierScaler = tiers;
        boot.monetization = monetization;
        boot.storyDirector = story;
        boot.treasureAndMap = treasure;
        boot.exitVault = exitVault;
        boot.ui = ui.manager;
        boot.dispatch = ui.dispatch;
        boot.player = controller;
        boot.tools = tools;
        boot.punch = punch;
        boot.dynamite = dynamite;
        story.ui = ui.manager;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = playerObj;
        Debug.Log(saved
            ? "Break to Escape: scene built and saved to " + ScenePath + ". Press Play. (Spawn " + playerObj.transform.position + ", crusher " + crusher.transform.position + ")"
            : "Break to Escape: scene built but could not be saved to " + ScenePath + ".");
    }

    // ================================================================ assets

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    private static T SaveAsset<T>(T asset, string path) where T : Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static Material VoxelMaterial()
    {
        Shader shader = Shader.Find("BreakToEscape/VoxelLit");
        if (!shader || !shader.isSupported)
        {
            Debug.LogError("Break to Escape: shader BreakToEscape/VoxelLit is missing or failed to compile; using URP Particles/Unlit (vertex colours, no lighting). Check Assets/Shaders/VoxelLit.shader in the Console.");
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }
        var mat = new Material(shader) { name = "Mat_VoxelWalls", enableInstancing = false };
        return SaveAsset(mat, GeneratedRoot + "/Materials/Mat_VoxelWalls.mat");
    }

    private static Material ShaderMaterial(string name, string shaderName, string fallbackName, Color color)
    {
        Shader shader = Shader.Find(shaderName);
        if (!shader) shader = Shader.Find(fallbackName);
        if (!shader) { Debug.LogError("Break to Escape: neither " + shaderName + " nor " + fallbackName + " exists."); shader = Shader.Find("Sprites/Default"); }
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return SaveAsset(mat, GeneratedRoot + "/Materials/" + name + ".mat");
    }

    private static Material LitMaterial(string name, Color color, bool instanced)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader)
        {
            Debug.LogError("Break to Escape: URP is not installed or not active (Universal Render Pipeline/Lit not found). Create the project from the Universal 3D template.");
            shader = Shader.Find("Standard");
        }
        var mat = new Material(shader) { name = name, enableInstancing = instanced };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        return SaveAsset(mat, GeneratedRoot + "/Materials/" + name + ".mat");
    }

    private static RubbleCube BuildRubblePrefab(Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "RubbleCube";
        cube.layer = LayerRubble;
        cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.mass = 0.4f;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        cube.AddComponent<RubbleCube>();
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(cube, GeneratedRoot + "/Prefabs/RubbleCube.prefab");
        Object.DestroyImmediate(cube);
        return saved.GetComponent<RubbleCube>();
    }

    private static StickyDynamite BuildDynamitePrefab(Material red, Material spark)
    {
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "StickyDynamite";
        Object.DestroyImmediate(stick.GetComponent<Collider>());
        stick.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
        stick.GetComponent<MeshRenderer>().sharedMaterial = red;
        GameObject sparkObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sparkObj.name = "FuseSpark";
        Object.DestroyImmediate(sparkObj.GetComponent<Collider>());
        sparkObj.transform.SetParent(stick.transform, false);
        sparkObj.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        sparkObj.transform.localScale = Vector3.one * 0.45f;
        sparkObj.GetComponent<MeshRenderer>().sharedMaterial = spark;
        StickyDynamite sd = stick.AddComponent<StickyDynamite>();
        sd.fuseSparkVisual = sparkObj.transform;
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(stick, GeneratedRoot + "/Prefabs/StickyDynamite.prefab");
        Object.DestroyImmediate(stick);
        return saved.GetComponent<StickyDynamite>();
    }

    private static TextMeshPro BuildPopupPrefab()
    {
        var obj = new GameObject("CashPopup");
        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = "+$0";
        tmp.fontSize = 3.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = HazardYellow;
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(obj, GeneratedRoot + "/Prefabs/CashPopup.prefab");
        Object.DestroyImmediate(obj);
        return saved.GetComponent<TextMeshPro>();
    }

    private static void EnsureLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        SetLayer(layers, LayerVoxelWall, "VoxelWall");
        SetLayer(layers, LayerRubble, "Rubble");
        tagManager.ApplyModifiedProperties();
    }

    private static void SetLayer(SerializedProperty layers, int index, string name)
    {
        SerializedProperty sp = layers.GetArrayElementAtIndex(index);
        if (sp == null) return;
        if (string.IsNullOrEmpty(sp.stringValue)) sp.stringValue = name;
        else if (sp.stringValue != name) Debug.LogWarning("Break to Escape: layer " + index + " is already named '" + sp.stringValue + "'; the game uses it as " + name + ".");
    }

    private static void AddSceneToBuild(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in scenes) if (s.path == path) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ================================================================ scene objects

    private static Crusher07Controller BuildCrusher(Vector3 floorPoint, Material steel, TextMeshPro popupPrefab)
    {
        var root = new GameObject("Crusher07");
        root.transform.position = floorPoint;
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.25f, 0f);
        trigger.size = new Vector3(0.9f, 0.5f, 0.9f);
        GameObject frame = Child(root.transform, "Frame", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(1.0f, 0.06f, 1.0f), steel);
        // Rollers lie along x (cylinder axis = local y, rotated 90 degrees about z); they spin about that axis.
        GameObject left = Child(root.transform, "LeftRoller", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, -0.17f), new Vector3(0f, 0f, 90f), new Vector3(0.2f, 0.42f, 0.2f), steel);
        GameObject right = Child(root.transform, "RightRoller", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0.17f), new Vector3(0f, 0f, 90f), new Vector3(0.2f, 0.42f, 0.2f), steel);
        Child(root.transform, "HazardRim", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.55f), Vector3.zero, new Vector3(1.1f, 0.16f, 0.08f), LitMaterial("Mat_HazardYellow", HazardYellow, false));
        Crusher07Controller crusher = root.AddComponent<Crusher07Controller>();
        crusher.crusherFrame = frame.transform;
        crusher.leftRoller = left.transform;
        crusher.rightRoller = right.transform;
        crusher.floatingCashPrefab = popupPrefab;
        return crusher;
    }

    private static Transform BuildArm(Transform parent, string name, Vector3 localPos, Material glove, Material gauntlet, out GameObject tier0, out GameObject tier1)
    {
        var arm = new GameObject(name);
        arm.transform.SetParent(parent, false);
        arm.transform.localPosition = localPos;
        tier0 = Child(arm.transform, "BasicGlove", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0f, 0f), new Vector3(0.13f, 0.13f, 0.42f), glove);
        tier1 = Child(arm.transform, "WorkGauntlet", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0f, 0f), new Vector3(0.17f, 0.17f, 0.46f), gauntlet);
        tier1.SetActive(false);
        return arm.transform;
    }

    private static GameObject Child(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        if (parent) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localRotation = Quaternion.Euler(euler);
        obj.transform.localScale = scale;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return obj;
    }

    // ================================================================ UI

    private struct UIBuild
    {
        public UIScreenAndAccessibilityManager manager;
        public Dispatch dispatch;
    }

    private static UIBuild BuildUI(GameObject systems, MobileFirstPersonController controller, FirstPersonToolController tools, PunchAction punch,
        CompanyStoreEconomyManager economy, RubbleSuctionManager suction, Crusher07Controller crusher, MonetizationServicesManager monetization,
        DeviceTierScaler tiers, InteractionController interaction, TreasureAndMapManager treasure, ExitVaultAndLogbookController exitVault,
        HeadlampController headlamp, AudioHapticsManager audio, EffectsManager effects, GameBootstrap boot)
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        var canvasObj = new GameObject("UI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 1f;
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform safe = Rect(canvasObj.transform, "SafeArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        safe.gameObject.AddComponent<SafeAreaFitter>();

        UIScreenAndAccessibilityManager manager = canvasObj.AddComponent<UIScreenAndAccessibilityManager>();
        Dispatch dispatch = canvasObj.AddComponent<Dispatch>();
        manager.playerController = controller;
        manager.audioHaptics = audio;
        manager.monetization = monetization;
        manager.tierScaler = tiers;
        manager.punch = punch;
        manager.headlamp = headlamp;
        manager.effects = effects;

        // ---------------- Screen 1: play HUD
        RectTransform hud = Rect(safe, "PlayHUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        manager.playScreenHUD = hud.gameObject;
        HudPresenter presenter = hud.gameObject.AddComponent<HudPresenter>();
        presenter.economyManager = economy;
        presenter.suction = suction;
        presenter.tools = tools;
        presenter.monetization = monetization;

        Image crosshair = Panel(hud, "Crosshair", Center, Center, Vector2.zero, new Vector2(16f, 16f), new Color(1f, 1f, 1f, 0.85f));
        crosshair.raycastTarget = false;

        RectTransform cashBox = Panel(hud, "CashBox", TopLeft, TopLeft, new Vector2(0f, 0f), new Vector2(340f, 92f), PanelDark).rectTransform;
        cashBox.pivot = TopLeft;
        presenter.cashText = Text(cashBox, "Cash", "$ 0", 38f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(-24f, 48f), HazardYellow, TextAlignmentOptions.Left);
        presenter.incomeText = Text(cashBox, "Income", "+$0.0 / s", 24f, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(-24f, 34f), TextLight, TextAlignmentOptions.Left);
        presenter.dayText = Text(hud, "Day", "DAY 1", 24f, TopLeft, TopLeft, new Vector2(12f, -104f), new Vector2(200f, 36f), new Color(0.75f, 0.85f, 1f), TextAlignmentOptions.Left);
        presenter.dayText.rectTransform.pivot = TopLeft;

        RectTransform tank = Panel(hud, "TankGauge", TopCenter, TopCenter, Vector2.zero, new Vector2(280f, 80f), PanelDark).rectTransform;
        tank.pivot = TopCenter;
        presenter.tankText = Text(tank, "TankText", "TANK 0 / 60", 30f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.45f, 1f, 0.55f), TextAlignmentOptions.Center);

        RectTransform detector = Panel(hud, "DetectorSignal", TopCenter, TopCenter, new Vector2(0f, -92f), new Vector2(220f, 52f), new Color(0.1f, 0.5f, 0.3f, 0.85f)).rectTransform;
        detector.pivot = TopCenter;
        treasure.detectorSignalRoot = detector.gameObject;
        treasure.detectorDistanceText = Text(detector, "Distance", "TREASURE 0.0 m", 24f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextLight, TextAlignmentOptions.Center);
        detector.gameObject.SetActive(false);

        presenter.doubleCashText = Text(hud, "DoubleCashTimer", "2X CASH 5:00", 26f, TopLeft, TopLeft, new Vector2(12f, -146f), new Vector2(260f, 36f), HazardYellow, TextAlignmentOptions.Left);
        presenter.doubleCashText.rectTransform.pivot = TopLeft;
        presenter.doubleCashText.gameObject.SetActive(false);

        // Top right: PAUSE (14 px from the right edge) and MAP (78 px), 56 × 56 in the wireframe.
        Button pauseBtn = ButtonBox(hud, "Btn_Pause", "II", TopRight, new Vector2(0f, 0f), new Vector2(112f, 112f), PanelDark, TextLight, 44f);
        pauseBtn.GetComponent<RectTransform>().pivot = TopRight;
        UnityEventTools.AddPersistentListener(pauseBtn.onClick, manager.Pause);
        Button mapBtn = ButtonBox(hud, "Btn_Map", "MAP", TopRight, new Vector2(-128f, 0f), new Vector2(112f, 112f), PanelDark, TextLight, 30f);
        mapBtn.GetComponent<RectTransform>().pivot = TopRight;
        UnityEventTools.AddPersistentListener(mapBtn.onClick, manager.ToggleMapScreen);
        Button storeBtn = ButtonBox(hud, "Btn_Store", "STORE", TopRight, new Vector2(-256f, 0f), new Vector2(160f, 112f), HazardYellow, TextDark, 30f);
        storeBtn.GetComponent<RectTransform>().pivot = TopRight;
        UnityEventTools.AddPersistentListener(storeBtn.onClick, manager.OpenCompanyStore);
        Button boostBtn = ButtonBox(hud, "Btn_DoubleCashOffer", "2X CASH", TopRight, new Vector2(-432f, 0f), new Vector2(160f, 112f), new Color(0.55f, 0.4f, 0.08f, 0.95f), TextLight, 26f);
        boostBtn.GetComponent<RectTransform>().pivot = TopRight;
        UnityEventTools.AddPersistentListener(boostBtn.onClick, manager.OpenDoubleCashOffer);

        // Bottom left: MOVE stick, 132 × 132 at (24, 234).
        Image joyBg = Panel(hud, "MoveStick", BottomLeft, BottomLeft, new Vector2(36f, 36f), new Vector2(264f, 264f), new Color(1f, 1f, 1f, 0.16f));
        joyBg.rectTransform.pivot = BottomLeft;
        Image joyHandle = Panel(joyBg.rectTransform, "Handle", Center, Center, Vector2.zero, new Vector2(110f, 110f), new Color(1f, 1f, 1f, 0.6f));
        joyHandle.raycastTarget = false;
        MobileVirtualJoystick joystick = joyBg.gameObject.AddComponent<MobileVirtualJoystick>();
        joystick.joystickBackground = joyBg.rectTransform;
        joystick.joystickHandle = joyHandle.rectTransform;
        joystick.playerController = controller;

        // Bottom right: PUNCH 122 × 122, 28 px from the right; VACUUM 84 × 84, 172 px from the right.
        Button punchBtn = ButtonBox(hud, "Btn_Punch", "PUNCH\n<size=60%>hold = charge</size>", BottomRight, new Vector2(-56f, 48f), new Vector2(244f, 244f), new Color(0.92f, 0.38f, 0.15f, 0.92f), TextLight, 40f);
        punchBtn.GetComponent<RectTransform>().pivot = BottomRight;
        UIHoldButton punchHold = punchBtn.gameObject.AddComponent<UIHoldButton>();
        UnityEventTools.AddPersistentListener(punchHold.onPointerDown, tools.OnPunchButtonDown);
        UnityEventTools.AddPersistentListener(punchHold.onPointerUp, tools.OnPunchButtonUp);
        Button vacBtn = ButtonBox(hud, "Btn_Vacuum", "VACUUM\n<size=60%>hold</size>", BottomRight, new Vector2(-344f, 32f), new Vector2(168f, 168f), new Color(0.2f, 0.72f, 0.35f, 0.92f), TextLight, 30f);
        vacBtn.GetComponent<RectTransform>().pivot = BottomRight;
        UIHoldButton vacHold = vacBtn.gameObject.AddComponent<UIHoldButton>();
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerDown, tools.SetVacuumButtonHeld, true);
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerUp, tools.SetVacuumButtonHeld, false);
        Button blowBtn = ButtonBox(hud, "Btn_SuckBlow", "SUCK / BLOW", BottomRight, new Vector2(-344f, 216f), new Vector2(168f, 96f), new Color(0.25f, 0.52f, 0.88f, 0.92f), TextLight, 24f);
        blowBtn.GetComponent<RectTransform>().pivot = BottomRight;
        UnityEventTools.AddPersistentListener(blowBtn.onClick, tools.ToggleVacuumBlowMode);

        // Bottom centre hotbar: four 56 × 56 slots, 8 px gaps.
        string[] slots = { "FIST", "VAC", "DYN x3", "LAMP" };
        float[] slotX = { -192f, -64f, 64f, 192f };
        for (int i = 0; i < 4; i++)
        {
            Button slot = ButtonBox(hud, "Hotbar_" + slots[i], slots[i], BottomCenter, new Vector2(slotX[i], 24f), new Vector2(112f, 112f), new Color(0.12f, 0.12f, 0.12f, 0.92f), TextLight, 24f);
            slot.GetComponent<RectTransform>().pivot = BottomCenter;
            UnityEventTools.AddIntPersistentListener(slot.onClick, tools.SelectHotbarSlot, i);
            presenter.hotbarSlotImages[i] = slot.GetComponent<Image>();
            if (i == 2) presenter.dynamiteSlotText = slot.GetComponentInChildren<TextMeshProUGUI>();
        }

        Button useBtn = ButtonBox(hud, "Btn_Use", "USE", BottomCenter, new Vector2(0f, 156f), new Vector2(560f, 96f), HazardYellow, TextDark, 30f);
        useBtn.GetComponent<RectTransform>().pivot = BottomCenter;
        UnityEventTools.AddPersistentListener(useBtn.onClick, interaction.UseCurrent);
        interaction.useButton = useBtn;
        interaction.useButtonLabel = useBtn.GetComponentInChildren<TextMeshProUGUI>();

        RectTransform overlay = Rect(hud, "DebugRates", TopLeft, TopLeft, new Vector2(12f, -190f), new Vector2(520f, 190f), TopLeft);
        DebugRateOverlay rates = overlay.gameObject.AddComponent<DebugRateOverlay>();
        rates.text = Text(overlay, "Text", "", 20f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.7f, 1f, 0.7f), TextAlignmentOptions.TopLeft);
        rates.economyManager = economy;
        rates.suction = suction;
        rates.crusher = crusher;
        rates.tierScaler = tiers;
        manager.debugOverlay = rates;
        overlay.gameObject.SetActive(false);

        manager.mirroredControls = new List<RectTransform>
        {
            joyBg.rectTransform, punchBtn.GetComponent<RectTransform>(), vacBtn.GetComponent<RectTransform>(), blowBtn.GetComponent<RectTransform>(),
            cashBox, presenter.dayText.rectTransform, presenter.doubleCashText.rectTransform, pauseBtn.GetComponent<RectTransform>(),
            mapBtn.GetComponent<RectTransform>(), storeBtn.GetComponent<RectTransform>(), boostBtn.GetComponent<RectTransform>(), overlay
        };

        // Dispatch banner (top centre), above every screen so captions stay readable in the store.
        RectTransform banner = Panel(canvasObj.transform, "DispatchBanner", TopCenter, TopCenter, new Vector2(0f, 120f), new Vector2(1100f, 80f), new Color(0.96f, 0.78f, 0.1f, 0.96f)).rectTransform;
        banner.pivot = TopCenter;
        banner.GetComponent<Image>().raycastTarget = false;
        dispatch.bannerRect = banner;
        dispatch.bannerText = Text(banner, "Text", "", 28f, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f), TextDark, TextAlignmentOptions.Center);

        // Dimmer shared by every screen and overlay.
        Image dim = Panel(safe, "Dimmer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f));
        dim.rectTransform.offsetMin = new Vector2(-400f, -400f);
        dim.rectTransform.offsetMax = new Vector2(400f, 400f);
        manager.dimmer = dim.gameObject;

        BuildStore(safe, manager, economy, monetization);
        BuildMap(safe, manager, treasure);
        BuildPause(safe, manager);
        BuildAdOffer(safe, manager);
        BuildSettings(safe, manager, boot);
        BuildTextPopup(safe, exitVault);
        BuildCodeCard(safe, exitVault);
        BuildEnding(safe, exitVault);
        return new UIBuild { manager = manager, dispatch = dispatch };
    }

    private static void BuildStore(RectTransform parent, UIScreenAndAccessibilityManager manager, CompanyStoreEconomyManager economy, MonetizationServicesManager monetization)
    {
        RectTransform screen = FullPanel(parent, "Screen_CompanyStore", PanelDark);
        manager.companyStoreScreen = screen.gameObject;
        StoreUIController store = screen.gameObject.AddComponent<StoreUIController>();
        store.economyManager = economy;
        store.monetization = monetization;

        Text(screen, "Title", "COMPANY STORE", 44f, TopLeft, TopLeft, new Vector2(28f, -24f), new Vector2(600f, 88f), HazardYellow, TextAlignmentOptions.Left).rectTransform.pivot = TopLeft;
        RectTransform cash = Panel(screen, "Cash", TopLeft, TopLeft, new Vector2(660f, -24f), new Vector2(340f, 88f), PanelMid).rectTransform;
        cash.pivot = TopLeft;
        store.cashText = Text(cash, "Text", "$ 0", 36f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, HazardYellow, TextAlignmentOptions.Center);
        Button close = ButtonBox(screen, "Btn_Close", "CLOSE", TopRight, new Vector2(-28f, -24f), new Vector2(176f, 88f), new Color(0.45f, 0.18f, 0.18f), TextLight, 30f);
        close.GetComponent<RectTransform>().pivot = TopRight;
        UnityEventTools.AddPersistentListener(close.onClick, manager.CloseCompanyStore);

        RectTransform upgradesPage = Rect(screen, "Page_Upgrades", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        RectTransform suppliesPage = Rect(screen, "Page_Supplies", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Button tabUp = ButtonBox(screen, "Tab_Upgrades", "UPGRADES", TopLeft, new Vector2(1024f, -24f), new Vector2(200f, 88f), PanelMid, TextLight, 24f);
        tabUp.GetComponent<RectTransform>().pivot = TopLeft;
        UnityEventTools.AddBoolPersistentListener(tabUp.onClick, upgradesPage.gameObject.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(tabUp.onClick, suppliesPage.gameObject.SetActive, false);
        Button tabSup = ButtonBox(screen, "Tab_Supplies", "SUPPLIES", TopLeft, new Vector2(1236f, -24f), new Vector2(200f, 88f), PanelMid, TextLight, 24f);
        tabSup.GetComponent<RectTransform>().pivot = TopLeft;
        UnityEventTools.AddBoolPersistentListener(tabSup.onClick, upgradesPage.gameObject.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(tabSup.onClick, suppliesPage.gameObject.SetActive, true);

        // Card grid: from (14, 70) on the artboard, 4 columns, 10 px gap, rows 146 px high (× 2).
        const float cardW = 393f, cardH = 292f, gap = 20f, left = 28f, top = -140f;
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4, row = i / 4;
            var pos = new Vector2(left + col * (cardW + gap), top - row * (cardH + gap));
            RectTransform card = Panel(upgradesPage, "Card_" + i, TopLeft, TopLeft, pos, new Vector2(cardW, cardH), i == 7 ? new Color(0.35f, 0.28f, 0.06f, 0.98f) : PanelMid).rectTransform;
            card.pivot = TopLeft;
            if (i < 7)
            {
                var view = new UpgradeCardView
                {
                    line = (UpgradeLine)i,
                    title = Text(card, "Title", "", 28f, TopLeft, new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-24f, 48f), HazardYellow, TextAlignmentOptions.Center),
                    levelText = Text(card, "Level", "", 26f, TopLeft, new Vector2(1f, 1f), new Vector2(0f, -88f), new Vector2(-24f, 40f), TextLight, TextAlignmentOptions.Center),
                    effectText = Text(card, "Effect", "", 22f, TopLeft, new Vector2(1f, 1f), new Vector2(0f, -136f), new Vector2(-24f, 40f), new Color(0.8f, 0.85f, 0.9f), TextAlignmentOptions.Center),
                };
                Button buy = ButtonBox(card, "Btn_Buy", "BUY", BottomCenter, new Vector2(0f, 24f), new Vector2(240f, 88f), HazardYellow, TextDark, 26f);
                buy.GetComponent<RectTransform>().pivot = BottomCenter;
                UnityEventTools.AddIntPersistentListener(buy.onClick, economy.BuyUpgrade, i);
                view.buyButton = buy;
                view.buyLabel = buy.GetComponentInChildren<TextMeshProUGUI>();
                store.cards.Add(view);
            }
            else
            {
                Text(card, "Title", "DOUBLE CASH\n5 MIN", 30f, TopLeft, new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(-24f, 100f), HazardYellow, TextAlignmentOptions.Center);
                Button watch = ButtonBox(card, "Btn_WatchAd", "WATCH AD", BottomCenter, new Vector2(0f, 24f), new Vector2(240f, 88f), HazardYellow, TextDark, 26f);
                watch.GetComponent<RectTransform>().pivot = BottomCenter;
                UnityEventTools.AddPersistentListener(watch.onClick, manager.OpenDoubleCashOffer);
                store.items.Add(new StoreItemView { key = "doubleCashAd", label = card.Find("Title").GetComponent<TextMeshProUGUI>(), button = watch });
            }
        }

        // Supplies and repairs page: two columns of rows.
        var rows = new List<(string key, UnityAction action)>
        {
            ("dynamitePack", economy.BuyDynamitePack),
            ("headlamp", economy.BuyHeadlampLevel),
            ("treasureDetector", economy.BuyTreasureDetector),
            ("autoPunch", economy.BuyAutoPunchUpgrade),
            ("freeDynamiteAd", manager.OpenFreeDynamiteOffer),
            (EconomyState.PneumaticHammer, economy.RepairPneumaticHammer),
            (EconomyState.Conveyor, economy.RepairConveyor),
            (EconomyState.RockDrill, economy.RepairRockDrill),
            ("removeAds", monetization.PurchaseRemoveAds),
            ("restorePurchases", monetization.RestorePurchases),
        };
        for (int i = 0; i < rows.Count; i++)
        {
            int col = i / 5, row = i % 5;
            Button b = ButtonBox(suppliesPage, "Row_" + rows[i].key, rows[i].key == "restorePurchases" ? "RESTORE PURCHASES" : rows[i].key, TopLeft,
                new Vector2(28f + col * 824f, -140f - row * 120f), new Vector2(800f, 104f), PanelMid, TextLight, 26f);
            b.GetComponent<RectTransform>().pivot = TopLeft;
            UnityEventTools.AddPersistentListener(b.onClick, rows[i].action);
            if (rows[i].key != "restorePurchases")
                store.items.Add(new StoreItemView { key = rows[i].key, label = b.GetComponentInChildren<TextMeshProUGUI>(), button = b });
        }
        suppliesPage.gameObject.SetActive(false);
        screen.gameObject.SetActive(false);
    }

    private static void BuildMap(RectTransform parent, UIScreenAndAccessibilityManager manager, TreasureAndMapManager treasure)
    {
        RectTransform screen = Rect(parent, "Screen_Map", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Button closeAll = screen.gameObject.AddComponent<Button>();
        Image catcher = screen.gameObject.AddComponent<Image>();
        catcher.color = new Color(0f, 0f, 0f, 0.01f);
        closeAll.targetGraphic = catcher;
        UnityEventTools.AddPersistentListener(closeAll.onClick, manager.ToggleMapScreen);
        manager.mapScreen = screen.gameObject;
        // Paper map panel, 520 × 340 on the artboard.
        RectTransform paper = Panel(screen, "Paper", Center, Center, Vector2.zero, new Vector2(1040f, 680f), new Color(0.86f, 0.8f, 0.64f, 1f)).rectTransform;
        paper.GetComponent<Image>().raycastTarget = false;
        Text(paper, "Title", "MAP", 40f, TopCenter, TopCenter, new Vector2(0f, -36f), new Vector2(400f, 60f), new Color(0.25f, 0.2f, 0.14f), TextAlignmentOptions.Center);
        RectTransform area = Rect(paper, "MapArea", new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        treasure.mapArea = area;
        Image pin = Panel(area, "PlayerPin", Center, Center, Vector2.zero, new Vector2(22f, 30f), new Color(0.85f, 0.1f, 0.1f, 1f));
        pin.raycastTarget = false;
        treasure.mapPlayerPin = pin.rectTransform;
        Text(paper, "Footer", "Tap anywhere or MAP to put it away", 22f, BottomCenter, BottomCenter, new Vector2(0f, 28f), new Vector2(800f, 40f), new Color(0.25f, 0.2f, 0.14f), TextAlignmentOptions.Center);
        screen.gameObject.SetActive(false);
    }

    private static void BuildPause(RectTransform parent, UIScreenAndAccessibilityManager manager)
    {
        RectTransform panel = Panel(parent, "Screen_Pause", Center, Center, Vector2.zero, new Vector2(648f, 560f), PanelDark).rectTransform;
        manager.pauseScreen = panel.gameObject;
        Text(panel, "Title", "PAUSED", 48f, TopCenter, TopCenter, new Vector2(0f, -48f), new Vector2(500f, 70f), HazardYellow, TextAlignmentOptions.Center);
        UnityEventTools.AddPersistentListener(ButtonBox(panel, "Btn_Resume", "RESUME", Center, new Vector2(0f, 90f), new Vector2(480f, 104f), HazardYellow, TextDark, 32f).onClick, manager.Resume);
        UnityEventTools.AddPersistentListener(ButtonBox(panel, "Btn_Settings", "SETTINGS", Center, new Vector2(0f, -30f), new Vector2(480f, 104f), PanelMid, TextLight, 32f).onClick, manager.OpenSettings);
        UnityEventTools.AddPersistentListener(ButtonBox(panel, "Btn_Quit", "QUIT TO MENU", Center, new Vector2(0f, -150f), new Vector2(480f, 104f), PanelMid, TextLight, 32f).onClick, manager.QuitToMenu);
        Text(panel, "Note", "Left-handed mode and vibration are in Settings", 20f, BottomCenter, BottomCenter, new Vector2(0f, 24f), new Vector2(600f, 36f), new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        panel.gameObject.SetActive(false);
    }

    private static void BuildAdOffer(RectTransform parent, UIScreenAndAccessibilityManager manager)
    {
        // Card 380 × 230; two equal buttons 150 × 56 (No thanks is never smaller than Watch ad).
        RectTransform card = Panel(parent, "Screen_AdOffer", Center, Center, Vector2.zero, new Vector2(760f, 460f), PanelDark).rectTransform;
        manager.adOfferPopupScreen = card.gameObject;
        manager.adOfferTitle = Text(card, "Title", "Double cash for 5 minutes?", 36f, TopCenter, TopCenter, new Vector2(0f, -56f), new Vector2(700f, 60f), HazardYellow, TextAlignmentOptions.Center);
        manager.adOfferSubtitle = Text(card, "Subtitle", "Watch a short ad. Totally optional.", 26f, TopCenter, TopCenter, new Vector2(0f, -124f), new Vector2(700f, 50f), TextLight, TextAlignmentOptions.Center);
        UnityEventTools.AddPersistentListener(ButtonBox(card, "Btn_WatchAd", "WATCH AD", Center, new Vector2(-170f, -60f), new Vector2(300f, 112f), HazardYellow, TextDark, 30f).onClick, manager.ConfirmWatchAdFromPopup);
        UnityEventTools.AddPersistentListener(ButtonBox(card, "Btn_NoThanks", "NO THANKS", Center, new Vector2(170f, -60f), new Vector2(300f, 112f), PanelMid, TextLight, 30f).onClick, manager.DeclineAdFromPopup);
        Text(card, "Caption", "Both buttons are the same size on purpose.", 18f, BottomCenter, BottomCenter, new Vector2(0f, 20f), new Vector2(700f, 30f), new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center);
        card.gameObject.SetActive(false);
    }

    private static void BuildSettings(RectTransform parent, UIScreenAndAccessibilityManager manager, GameBootstrap boot)
    {
        RectTransform panel = Panel(parent, "Screen_Settings", Center, Center, Vector2.zero, new Vector2(1200f, 700f), PanelDark).rectTransform;
        manager.settingsScreen = panel.gameObject;
        Text(panel, "Title", "SETTINGS", 44f, TopCenter, TopCenter, new Vector2(0f, -40f), new Vector2(600f, 64f), HazardYellow, TextAlignmentOptions.Center);

        manager.leftHandedToggle = ToggleRow(panel, "LEFT-HANDED MODE", new Vector2(-300f, 200f), false);
        UnityEventTools.AddPersistentListener(manager.leftHandedToggle.onValueChanged, manager.SetLeftHandedMode);
        manager.vibrationToggle = ToggleRow(panel, "VIBRATION", new Vector2(-300f, 100f), true);
        UnityEventTools.AddPersistentListener(manager.vibrationToggle.onValueChanged, manager.SetVibration);
        manager.reduceFlashingToggle = ToggleRow(panel, "REDUCE FLASHING", new Vector2(-300f, 0f), false);
        UnityEventTools.AddPersistentListener(manager.reduceFlashingToggle.onValueChanged, manager.SetReduceFlashing);
        manager.autoPunchToggle = ToggleRow(panel, "AUTO-PUNCH (WHEN OWNED)", new Vector2(-300f, -100f), false);
        UnityEventTools.AddPersistentListener(manager.autoPunchToggle.onValueChanged, manager.SetAutoPunch);
        manager.debugOverlayToggle = ToggleRow(panel, "SHOW RATE OVERLAY", new Vector2(-300f, -200f), false);
        UnityEventTools.AddPersistentListener(manager.debugOverlayToggle.onValueChanged, manager.SetDebugOverlay);

        manager.lookSensitivitySlider = SliderRow(panel, "LOOK SENSITIVITY", new Vector2(300f, 200f), 0.05f, 0.45f, 0.16f);
        UnityEventTools.AddPersistentListener(manager.lookSensitivitySlider.onValueChanged, manager.SetLookSensitivity);
        manager.effectsIntensitySlider = SliderRow(panel, "SHAKE AND EFFECTS", new Vector2(300f, 80f), 0f, 1f, 1f);
        UnityEventTools.AddPersistentListener(manager.effectsIntensitySlider.onValueChanged, manager.SetEffectsIntensity);
        Button quality = ButtonBox(panel, "Btn_Quality", "GRAPHICS: AUTO", Center, new Vector2(300f, -40f), new Vector2(480f, 96f), PanelMid, TextLight, 26f);
        UnityEventTools.AddPersistentListener(quality.onClick, manager.CycleQuality);
        manager.qualityLabel = quality.GetComponentInChildren<TextMeshProUGUI>();
        Button newRun = ButtonBox(panel, "Btn_NewRun", "START A NEW RUN (DELETES SAVE)", Center, new Vector2(300f, -160f), new Vector2(480f, 96f), new Color(0.45f, 0.18f, 0.18f), TextLight, 22f);
        UnityEventTools.AddPersistentListener(newRun.onClick, boot.StartNewRun);
        Button close = ButtonBox(panel, "Btn_Close", "BACK", BottomCenter, new Vector2(0f, 28f), new Vector2(320f, 96f), HazardYellow, TextDark, 30f);
        close.GetComponent<RectTransform>().pivot = BottomCenter;
        UnityEventTools.AddPersistentListener(close.onClick, manager.CloseSettings);
        panel.gameObject.SetActive(false);
    }

    private static void BuildTextPopup(RectTransform parent, ExitVaultAndLogbookController exitVault)
    {
        RectTransform panel = Panel(parent, "Overlay_Text", Center, Center, Vector2.zero, new Vector2(1100f, 660f), new Color(0.9f, 0.86f, 0.74f, 1f)).rectTransform;
        exitVault.textPopupPanel = panel.gameObject;
        exitVault.textPopupTitle = Text(panel, "Title", "", 36f, TopCenter, TopCenter, new Vector2(0f, -44f), new Vector2(1000f, 60f), new Color(0.3f, 0.12f, 0.08f), TextAlignmentOptions.Center);
        exitVault.textPopupBody = Text(panel, "Body", "", 26f, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 10f), new Vector2(-100f, -240f), TextDark, TextAlignmentOptions.TopLeft);
        Button close = ButtonBox(panel, "Btn_Close", "CLOSE", BottomCenter, new Vector2(0f, 24f), new Vector2(320f, 96f), PanelDark, TextLight, 30f);
        close.GetComponent<RectTransform>().pivot = BottomCenter;
        UnityEventTools.AddPersistentListener(close.onClick, exitVault.CloseText);
        panel.gameObject.SetActive(false);
    }

    private static void BuildCodeCard(RectTransform parent, ExitVaultAndLogbookController exitVault)
    {
        RectTransform panel = Panel(parent, "Overlay_GateCode", Center, Center, Vector2.zero, new Vector2(1000f, 640f), PanelDark).rectTransform;
        exitVault.codeCardPanel = panel.gameObject;
        Text(panel, "Title", "MAIN SHAFT LIFT GATE", 40f, TopCenter, TopCenter, new Vector2(0f, -40f), new Vector2(900f, 60f), HazardYellow, TextAlignmentOptions.Center);
        exitVault.codeStatusText = Text(panel, "Status", "", 24f, TopCenter, TopCenter, new Vector2(0f, -104f), new Vector2(900f, 40f), TextLight, TextAlignmentOptions.Center);
        UnityAction[] ups = { exitVault.DialAUp, exitVault.DialBUp, exitVault.DialCUp };
        UnityAction[] downs = { exitVault.DialADown, exitVault.DialBDown, exitVault.DialCDown };
        var displays = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            float x = -260f + i * 260f;
            UnityEventTools.AddPersistentListener(ButtonBox(panel, "Up_" + i, "\u25B2", Center, new Vector2(x, 120f), new Vector2(160f, 96f), PanelMid, TextLight, 36f).onClick, ups[i]);
            RectTransform box = Panel(panel, "Dial_" + i, Center, Center, new Vector2(x, 10f), new Vector2(180f, 110f), Color.black).rectTransform;
            displays[i] = Text(box, "Digits", "00", 64f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, HazardYellow, TextAlignmentOptions.Center);
            UnityEventTools.AddPersistentListener(ButtonBox(panel, "Down_" + i, "\u25BC", Center, new Vector2(x, -100f), new Vector2(160f, 96f), PanelMid, TextLight, 36f).onClick, downs[i]);
        }
        exitVault.codeDisplayA = displays[0];
        exitVault.codeDisplayB = displays[1];
        exitVault.codeDisplayC = displays[2];
        UnityEventTools.AddPersistentListener(ButtonBox(panel, "Btn_TryGate", "TRY THE GATE", BottomCenter, new Vector2(-180f, 28f), new Vector2(320f, 96f), HazardYellow, TextDark, 28f).onClick, exitVault.TryUnlockExitVault);
        UnityEventTools.AddPersistentListener(ButtonBox(panel, "Btn_Close", "CLOSE", BottomCenter, new Vector2(180f, 28f), new Vector2(320f, 96f), PanelMid, TextLight, 28f).onClick, exitVault.CloseVaultCodeCard);
        foreach (Transform t in panel) if (t.name.StartsWith("Btn_")) t.GetComponent<RectTransform>().pivot = BottomCenter;
        panel.gameObject.SetActive(false);
    }

    private static void BuildEnding(RectTransform parent, ExitVaultAndLogbookController exitVault)
    {
        RectTransform panel = FullPanel(parent, "Overlay_Ending", new Color(0.96f, 0.94f, 0.86f, 1f));
        exitVault.endingPanel = panel.gameObject;
        exitVault.endingTitleText = Text(panel, "Title", "", 52f, TopCenter, TopCenter, new Vector2(0f, -60f), new Vector2(1400f, 80f), TextDark, TextAlignmentOptions.Center);
        exitVault.endingBodyText = Text(panel, "Body", "", 28f, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-300f, -300f), TextDark, TextAlignmentOptions.Top);
        Button close = ButtonBox(panel, "Btn_Continue", "CONTINUE", BottomCenter, new Vector2(0f, 40f), new Vector2(360f, 100f), PanelDark, TextLight, 30f);
        close.GetComponent<RectTransform>().pivot = BottomCenter;
        UnityEventTools.AddPersistentListener(close.onClick, exitVault.CloseEnding);
        panel.gameObject.SetActive(false);
    }

    // ================================================================ UI helpers

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 TopRight = new Vector2(1f, 1f);
    private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

    private static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform FullPanel(Transform parent, string name, Color color)
    {
        RectTransform rt = Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Center);
        rt.gameObject.AddComponent<Image>().color = color;
        return rt;
    }

    private static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = Rect(parent, name, anchorMin, anchorMax, pos, size, Center);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string content, float size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 sizeDelta, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = Rect(parent, name, anchorMin, anchorMax, pos, sizeDelta, Center);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = Mathf.Max(14f, size * 0.6f);
        t.fontSizeMax = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Button with a centred label. Every button is at least 96 canvas units (48 dp at 2×) in both directions.</summary>
    private static Button ButtonBox(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color background, Color textColor, float fontSize)
    {
        size = new Vector2(Mathf.Max(96f, size.x), Mathf.Max(96f, size.y));
        Image img = Panel(parent, name, anchor, anchor, pos, size, background);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Text(img.rectTransform, "Label", label, fontSize, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, -12f), textColor, TextAlignmentOptions.Center);
        return b;
    }

    private static Toggle ToggleRow(Transform parent, string label, Vector2 pos, bool on)
    {
        RectTransform row = Rect(parent, "Toggle_" + label, Center, Center, pos, new Vector2(520f, 96f), Center);
        Image bg = Panel(row, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(96f, 96f), PanelMid);
        Image check = Panel(bg.rectTransform, "Check", Center, Center, Vector2.zero, new Vector2(60f, 60f), HazardYellow);
        Text(row, "Label", label, 26f, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 0f), new Vector2(-120f, 0f), TextLight, TextAlignmentOptions.Left);
        Toggle t = row.gameObject.AddComponent<Toggle>();
        t.targetGraphic = bg;
        t.graphic = check;
        t.isOn = on;
        return t;
    }

    private static Slider SliderRow(Transform parent, string label, Vector2 pos, float min, float max, float value)
    {
        RectTransform row = Rect(parent, "Slider_" + label, Center, Center, pos, new Vector2(480f, 110f), Center);
        Text(row, "Label", label, 24f, TopLeft, new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(0f, 36f), TextLight, TextAlignmentOptions.Left);
        RectTransform track = Panel(row, "Track", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 30f), new Vector2(-20f, 20f), PanelMid).rectTransform;
        RectTransform fillArea = Rect(track, "FillArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Center);
        Image fill = Panel(fillArea, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, HazardYellow);
        RectTransform handleArea = Rect(track, "HandleArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Center);
        Image handle = Panel(handleArea, "Handle", Center, Center, Vector2.zero, new Vector2(56f, 72f), TextLight);
        Slider s = row.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.minValue = min;
        s.maxValue = max;
        s.value = value;
        return s;
    }
}
#endif
