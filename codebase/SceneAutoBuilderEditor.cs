#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class SceneAutoBuilderEditor
{
    private const int LAYER_VOXEL_WALL = 6;
    private const int LAYER_RUBBLE = 7;

    [MenuItem("BreakToEscape/Build Complete Playable Scene (One-Click)")]
    public static void BuildCompleteScene()
    {
        EnsurePhysicsLayersExist();

        // 1. Create Materials
        Material voxelMat = CreateVertexColorMaterial("Mat_VoxelVertexColor");
        Material rubbleMat = CreateLitMaterial("Mat_RubbleInstanced", new Color(0.75f, 0.75f, 0.75f), true);
        Material gloveMat = CreateLitMaterial("Mat_WorkGlove", new Color(0.22f, 0.24f, 0.28f), false);
        Material gauntletMat = CreateLitMaterial("Mat_WorkGauntlet", new Color(0.85f, 0.55f, 0.12f), false);
        Material vacTier1Mat = CreateLitMaterial("Mat_VacuumTier1", new Color(0.92f, 0.76f, 0.12f), false);
        Material vacTier2Mat = CreateLitMaterial("Mat_VacuumTier2", new Color(0.90f, 0.42f, 0.12f), false);
        Material vacTier3Mat = CreateLitMaterial("Mat_VacuumTier3", new Color(0.38f, 0.26f, 0.65f), false);
        Material dynamiteMat = CreateLitMaterial("Mat_DynamiteRed", new Color(0.85f, 0.15f, 0.12f), false);
        Material ghostMat = CreateLitMaterial("Mat_DynamiteGhost", new Color(1f, 1f, 1f, 0.45f), false);
        Material metalMat = CreateLitMaterial("Mat_CrusherSteel", new Color(0.32f, 0.35f, 0.38f), false);
        Material goldMat = CreateLitMaterial("Mat_TreasureGold", new Color(1.0f, 0.82f, 0.15f), false);

        // 2. Hidden Runtime Prefab Templates Root
        GameObject templatesRoot = new GameObject("_RuntimeTemplates");

        RubbleCube rubbleTemplate = BuildRubbleTemplate(templatesRoot.transform, rubbleMat);
        StickyDynamite dynamiteTemplate = BuildDynamiteTemplate(templatesRoot.transform, dynamiteMat, goldMat);
        TextMeshPro cashPopupTemplate = BuildFloatingCashTemplate(templatesRoot.transform);

        // 3. Voxel World Root
        GameObject voxelWorldObj = new GameObject("VoxelWorld");
        VoxelWorldManager voxelWorld = voxelWorldObj.AddComponent<VoxelWorldManager>();
        voxelWorld.voxelVertexColorMaterial = voxelMat;
        voxelWorld.voxelWallLayer = LAYER_VOXEL_WALL;

        // 4. Crusher 07 Floor Hatch inside Starting Cell (X:7.0, Y:1.0, Z:2.2)
        Crusher07Controller crusher07 = BuildCrusher07(new Vector3(7.0f, 1.02f, 2.2f), metalMat, cashPopupTemplate);

        // 5. Buried Treasure & Exit Vault Door
        GameObject treasureObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        treasureObj.name = "BuriedTreasure_RoyalCrown";
        treasureObj.transform.position = new Vector3(7.0f, 1.75f, 5.25f);
        treasureObj.transform.localScale = new Vector3(0.32f, 0.15f, 0.32f);
        treasureObj.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
        Rigidbody treasureRb = treasureObj.AddComponent<Rigidbody>();
        treasureRb.isKinematic = true;

        ExitVaultAndLogbookController exitVault = BuildExitVaultDoor(new Vector3(7.0f, 2.0f, 33.2f), metalMat, goldMat);

        // 6. Player Rig, Camera & Procedural First-Person Viewmodel
        GameObject playerObj = new GameObject("Player_FirstPerson");
        playerObj.transform.position = new Vector3(7.0f, 1.65f, 1.6f);

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.6f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0f, 0f);

        MobileFirstPersonController playerController = playerObj.AddComponent<MobileFirstPersonController>();

        GameObject camPivot = new GameObject("CameraPivot");
        camPivot.transform.SetParent(playerObj.transform, false);
        camPivot.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        playerController.cameraPivot = camPivot.transform;

        Camera mainCam = BuildMainCamera(camPivot.transform, out VoxelWireframePreview wireframePreview);

        FirstPersonToolController toolController = BuildViewmodelRig(
            mainCam.transform, mainCam, dynamiteTemplate,
            gloveMat, gauntletMat, vacTier1Mat, vacTier2Mat, vacTier3Mat, dynamiteMat, ghostMat,
            out Transform vacuumNozzlePoint);

        voxelWorld.toolController = toolController;

        // 7. Central Game Systems Root
        GameObject systemsObj = new GameObject("_GameSystems");

        RubbleSuctionManager suctionManager = systemsObj.AddComponent<RubbleSuctionManager>();
        suctionManager.toolController = toolController;
        suctionManager.vacuumNozzlePoint = vacuumNozzlePoint;
        suctionManager.rubblePrefab = rubbleTemplate;
        suctionManager.rubbleLayerMask = 1 << LAYER_RUBBLE;

        CompanyStoreEconomyManager economyManager = systemsObj.AddComponent<CompanyStoreEconomyManager>();
        economyManager.toolController = toolController;
        economyManager.wireframePreview = wireframePreview;
        economyManager.crusher07 = crusher07;
        economyManager.suctionManager = suctionManager;

        AudioHapticsManager audioHaptics = BuildAudioHaptics(systemsObj, toolController, voxelWorld);

        TreasureAndMapManager treasureMap = systemsObj.AddComponent<TreasureAndMapManager>();
        treasureMap.playerTransform = playerObj.transform;
        treasureMap.playerCamera = mainCam;
        treasureMap.voxelWorld = voxelWorld;
        treasureMap.economyManager = economyManager;
        treasureMap.buriedTreasures.Add(new BuriedTreasureNode
        {
            treasureId = 1,
            displayName = "Royal Crown",
            cashValue = 2700,
            treasureObject = treasureObj.transform,
            treasureRigidbody = treasureRb
        });
        treasureMap.mineRooms.Add(new MineRoomZone
        {
            roomId = "CentralCell",
            roomTitle = "Central Cell",
            worldBounds = new Bounds(new Vector3(7f, 2f, 2f), new Vector3(4f, 3f, 3f))
        });
        treasureMap.mineRooms.Add(new MineRoomZone
        {
            roomId = "CrewRoom1",
            roomTitle = "Abandoned Crew Nook",
            worldBounds = new Bounds(new Vector3(7f, 2f, 8f), new Vector3(3f, 3f, 2f))
        });

        DeviceTierScaler tierScaler = systemsObj.AddComponent<DeviceTierScaler>();
        tierScaler.rubbleManager = suctionManager;
        tierScaler.mainCamera = mainCam;

        MonetizationServicesManager monetization = systemsObj.AddComponent<MonetizationServicesManager>();
        monetization.economyManager = economyManager;
        monetization.toolController = toolController;

        SaveManager saveManager = systemsObj.AddComponent<SaveManager>();
        saveManager.economyManager = economyManager;
        saveManager.toolController = toolController;
        saveManager.wireframePreview = wireframePreview;
        saveManager.crusher07 = crusher07;
        saveManager.suctionManager = suctionManager;
        saveManager.playerController = playerController;
        saveManager.treasureAndMapManager = treasureMap;

        // 8. Build Complete Landscape Touch UI Canvas
        BuildLandscapeHUDCanvas(
            playerController, toolController, wireframePreview,
            suctionManager, economyManager, audioHaptics,
            treasureMap, monetization, exitVault);

        Selection.activeGameObject = playerObj;
        Debug.Log("Break to Escape: Complete playable scene built and wired successfully! Press Play to test.");
    }

    // =========================================================================
    // HELPER BUILDERS
    // =========================================================================

    private static void EnsurePhysicsLayersExist()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        SetLayerNameIfEmpty(layersProp, LAYER_VOXEL_WALL, "VoxelWall");
        SetLayerNameIfEmpty(layersProp, LAYER_RUBBLE, "Rubble");
        tagManager.ApplyModifiedProperties();
    }

    private static void SetLayerNameIfEmpty(SerializedProperty layersProp, int index, string layerName)
    {
        SerializedProperty sp = layersProp.GetArrayElementAtIndex(index);
        if (sp != null && string.IsNullOrEmpty(sp.stringValue))
        {
            sp.stringValue = layerName;
        }
    }

    private static Material CreateVertexColorMaterial(string name)
    {
        Shader s = Shader.Find("Particles/Standard Unlit");
        if (!s) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!s) s = Shader.Find("Sprites/Default");
        return new Material(s) { name = name };
    }

    private static Material CreateLitMaterial(string name, Color col, bool instanced)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (!s) s = Shader.Find("Standard");
        Material m = new Material(s) { name = name, color = col, enableInstancing = instanced };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        return m;
    }

    private static RubbleCube BuildRubbleTemplate(Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "RubbleCube_Template";
        cube.transform.SetParent(parent, false);
        cube.layer = LAYER_RUBBLE;
        cube.transform.localScale = Vector3.one * 0.22f;
        cube.GetComponent<MeshRenderer>().sharedMaterial = mat;

        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.mass = 0.4f;

        RubbleCube rc = cube.AddComponent<RubbleCube>();
        cube.SetActive(false);
        return rc;
    }

    private static StickyDynamite BuildDynamiteTemplate(Transform parent, Material redMat, Material sparkMat)
    {
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "StickyDynamite_Template";
        stick.transform.SetParent(parent, false);
        stick.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
        stick.GetComponent<MeshRenderer>().sharedMaterial = redMat;

        Rigidbody rb = stick.AddComponent<Rigidbody>();
        rb.mass = 0.3f;

        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "FuseSpark";
        spark.transform.SetParent(stick.transform, false);
        spark.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        spark.transform.localScale = Vector3.one * 0.45f;
        Object.DestroyImmediate(spark.GetComponent<Collider>());
        spark.GetComponent<MeshRenderer>().sharedMaterial = sparkMat;

        StickyDynamite sd = stick.AddComponent<StickyDynamite>();
        sd.fuseSparkVisual = spark.transform;
        stick.SetActive(false);
        return sd;
    }

    private static TextMeshPro BuildFloatingCashTemplate(Transform parent)
    {
        GameObject obj = new GameObject("FloatingCash_Template");
        obj.transform.SetParent(parent, false);
        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = "+$15";
        tmp.fontSize = 3.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.88f, 0.22f);
        obj.SetActive(false);
        return tmp;
    }

    private static Crusher07Controller BuildCrusher07(Vector3 pos, Material metalMat, TextMeshPro popupTemplate)
    {
        GameObject root = new GameObject("Crusher_07_FloorHatch");
        root.transform.position = pos;

        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(0.85f, 0.45f, 0.85f);

        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "CrusherFrame";
        frame.transform.SetParent(root.transform, false);
        frame.transform.localScale = new Vector3(0.85f, 0.06f, 0.85f);
        frame.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

        GameObject leftRoller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftRoller.name = "LeftToothedRoller";
        leftRoller.transform.SetParent(frame.transform, false);
        leftRoller.transform.localPosition = new Vector3(-0.16f, -0.2f, 0f);
        leftRoller.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        leftRoller.transform.localScale = new Vector3(0.22f, 0.38f, 0.22f);
        leftRoller.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

        GameObject rightRoller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightRoller.name = "RightToothedRoller";
        rightRoller.transform.SetParent(frame.transform, false);
        rightRoller.transform.localPosition = new Vector3(0.16f, -0.2f, 0f);
        rightRoller.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rightRoller.transform.localScale = new Vector3(0.22f, 0.38f, 0.22f);
        rightRoller.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

        Crusher07Controller crusher = root.AddComponent<Crusher07Controller>();
        crusher.crusherFrame = frame.transform;
        crusher.leftRoller = leftRoller.transform;
        crusher.rightRoller = rightRoller.transform;
        crusher.rubbleLayerMask = 1 << LAYER_RUBBLE;
        crusher.floatingCashPrefab = popupTemplate;
        return crusher;
    }

    private static ExitVaultAndLogbookController BuildExitVaultDoor(Vector3 pos, Material metalMat, Material goldMat)
    {
        GameObject vaultRoot = new GameObject("ExitVault_Door");
        vaultRoot.transform.position = pos;

        GameObject hinge = new GameObject("VaultDoorHinge");
        hinge.transform.SetParent(vaultRoot.transform, false);
        hinge.transform.localPosition = new Vector3(-0.6f, 0f, 0f);

        GameObject doorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorSlab.name = "VaultDoorSlab";
        doorSlab.transform.SetParent(hinge.transform, false);
        doorSlab.transform.localPosition = new Vector3(0.6f, 0f, 0f);
        doorSlab.transform.localScale = new Vector3(1.2f, 1.8f, 0.15f);
        doorSlab.GetComponent<MeshRenderer>().sharedMaterial = metalMat;

        GameObject dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dial.name = "CombinationDial";
        dial.transform.SetParent(doorSlab.transform, false);
        dial.transform.localPosition = new Vector3(0.15f, 0.18f, -0.6f);
        dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        dial.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);
        dial.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "ValveWheel";
        wheel.transform.SetParent(doorSlab.transform, false);
        wheel.transform.localPosition = new Vector3(0.15f, -0.2f, -0.6f);
        wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wheel.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
        wheel.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

        GameObject lightObj = new GameObject("EscapeWhiteLight");
        lightObj.transform.SetParent(vaultRoot.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0.6f);
        Light escapeLight = lightObj.AddComponent<Light>();
        escapeLight.type = LightType.Point;
        escapeLight.range = 8f;
        escapeLight.intensity = 0f;
        lightObj.SetActive(false);

        ExitVaultAndLogbookController ctrl = vaultRoot.AddComponent<ExitVaultAndLogbookController>();
        ctrl.vaultDoorHinge = hinge.transform;
        ctrl.combinationDial = dial.transform;
        ctrl.valveWheel = wheel.transform;
        ctrl.escapeWhiteLight = escapeLight;
        return ctrl;
    }

    private static Camera BuildMainCamera(Transform parent, out VoxelWireframePreview wireframePreview)
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(parent, false);

        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 35f;
        camObj.AddComponent<AudioListener>();

        wireframePreview = camObj.AddComponent<VoxelWireframePreview>();
        wireframePreview.playerCamera = cam;
        wireframePreview.voxelWallMask = 1 << LAYER_VOXEL_WALL;
        return cam;
    }

    private static FirstPersonToolController BuildViewmodelRig(
        Transform camTransform, Camera cam, StickyDynamite dynamiteTemplate,
        Material gloveMat, Material gauntletMat,
        Material vac1Mat, Material vac2Mat, Material vac3Mat,
        Material dynMat, Material ghostMat,
        out Transform nozzlePoint)
    {
        GameObject holder = new GameObject("ViewmodelHolder");
        holder.transform.SetParent(camTransform, false);

        FirstPersonToolController ctrl = holder.AddComponent<FirstPersonToolController>();
        ctrl.playerCamera = cam;
        ctrl.voxelWallMask = 1 << LAYER_VOXEL_WALL;
        ctrl.dynamitePrefab = dynamiteTemplate;

        // Left & Right Gloved Arms (with Tier 0 Basic Glove & Tier 1 Work Gauntlet)
        ctrl.leftArm = BuildArmTransform(holder.transform, "LeftArm", new Vector3(-0.32f, -0.26f, 0.45f), gloveMat, gauntletMat, out GameObject lT0, out GameObject lT1);
        ctrl.rightArm = BuildArmTransform(holder.transform, "RightArm", new Vector3(0.32f, -0.26f, 0.45f), gloveMat, gauntletMat, out GameObject rT0, out GameObject rT1);
        ctrl.leftGloveTiers = new GameObject[] { lT0, lT1 };
        ctrl.rightGloveTiers = new GameObject[] { rT0, rT1 };

        // Vacuum Root (with 3 upgradeable vacuum tier meshes)
        GameObject vacRoot = new GameObject("VacuumRoot");
        vacRoot.transform.SetParent(holder.transform, false);
        vacRoot.transform.localPosition = new Vector3(0.35f, -0.28f, 0.52f);
        ctrl.vacuumRoot = vacRoot.transform;

        GameObject vTier0 = CreateMeshChild(vacRoot.transform, "VacTier0_DustBuster", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(78f, 0f, 0f), new Vector3(0.14f, 0.28f, 0.14f), vac1Mat);
        GameObject vTier1 = CreateMeshChild(vacRoot.transform, "VacTier1_BoxVac", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(12f, 0f, 0f), new Vector3(0.22f, 0.22f, 0.48f), vac2Mat);
        GameObject vTier2 = CreateMeshChild(vacRoot.transform, "VacTier2_SuctionCannon", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(78f, 0f, 0f), new Vector3(0.26f, 0.36f, 0.26f), vac3Mat);
        vTier1.SetActive(false);
        vTier2.SetActive(false);
        ctrl.vacuumTiers = new GameObject[] { vTier0, vTier1, vTier2 };

        GameObject nozzle = new GameObject("VacuumNozzlePoint");
        nozzle.transform.SetParent(vacRoot.transform, false);
        nozzle.transform.localPosition = new Vector3(0f, 0.05f, 0.35f);
        nozzlePoint = nozzle.transform;

        // Dynamite In Hand & Wall Ghost Preview
        GameObject dynHand = CreateMeshChild(holder.transform, "DynamiteInHand", PrimitiveType.Cylinder, new Vector3(0.32f, -0.14f, 0.52f), new Vector3(20f, 0f, 15f), new Vector3(0.06f, 0.14f, 0.06f), dynMat);
        ctrl.dynamiteInHand = dynHand.transform;

        GameObject ghost = CreateMeshChild(null, "DynamiteWallGhostPreview", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.08f, 0.16f, 0.08f), ghostMat);
        ghost.SetActive(false);
        ctrl.dynamiteGhostPreview = ghost;

        // Headlamp SpotLight
        GameObject lampObj = new GameObject("HeadlampSpotlight");
        lampObj.transform.SetParent(camTransform, false);
        Light spot = lampObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 16f;
        spot.spotAngle = 62f;
        spot.intensity = 2.2f;
        spot.color = new Color(1f, 0.92f, 0.76f);
        ctrl.headlampLight = spot;

        return ctrl;
    }

    private static Transform BuildArmTransform(Transform parent, string name, Vector3 localPos, Material gloveMat, Material gauntletMat, out GameObject tier0, out GameObject tier1)
    {
        GameObject armRoot = new GameObject(name);
        armRoot.transform.SetParent(parent, false);
        armRoot.transform.localPosition = localPos;

        tier0 = CreateMeshChild(armRoot.transform, "Tier0_BasicGlove", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0f, 0f), new Vector3(0.13f, 0.13f, 0.42f), gloveMat);
        tier1 = CreateMeshChild(armRoot.transform, "Tier1_WorkGauntlet", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0f, 0f), new Vector3(0.17f, 0.17f, 0.46f), gauntletMat);
        tier1.SetActive(false);
        return armRoot.transform;
    }

    private static GameObject CreateMeshChild(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
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

    private static AudioHapticsManager BuildAudioHaptics(GameObject systemsObj, FirstPersonToolController toolCtrl, VoxelWorldManager voxelWorld)
    {
        AudioHapticsManager mgr = systemsObj.AddComponent<AudioHapticsManager>();
        mgr.toolController = toolCtrl;
        mgr.voxelWorld = voxelWorld;
        mgr.sfxOneShotSource = systemsObj.AddComponent<AudioSource>();
        mgr.vacuumLoopSource = systemsObj.AddComponent<AudioSource>();
        mgr.vacuumLoopSource.loop = true;
        mgr.crusherLoopSource = systemsObj.AddComponent<AudioSource>();
        mgr.crusherLoopSource.loop = true;
        mgr.ambienceLoopSource = systemsObj.AddComponent<AudioSource>();
        mgr.ambienceLoopSource.loop = true;
        return mgr;
    }

    // =========================================================================
    // LANDSCAPE TOUCH UI CANVAS BUILDER (1920x1080)
    // =========================================================================

    private static void BuildLandscapeHUDCanvas(
        MobileFirstPersonController playerCtrl,
        FirstPersonToolController toolCtrl,
        VoxelWireframePreview wireframePreview,
        RubbleSuctionManager suctionMgr,
        CompanyStoreEconomyManager economyMgr,
        AudioHapticsManager audioHaptics,
        TreasureAndMapManager treasureMap,
        MonetizationServicesManager monetization,
        ExitVaultAndLogbookController exitVault)
    {
        if (!Object.FindFirstObjectByType<EventSystem>())
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("HUD_LandscapeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 1. Center Crosshair Dot
        CreateUIPanel(canvasObj.transform, "CrosshairDot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), new Color(1f, 1f, 1f, 0.85f));

        // 2. Top-Left HUD: Cash, $/sec, Day Counter, Tank Gauge
        RectTransform topBar = CreateUIPanel(canvasObj.transform, "TopLeft_StatsPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -85f), new Vector2(380f, 130f), new Color(0.08f, 0.09f, 0.12f, 0.78f));
        economyMgr.cashText = CreateUIText(topBar, "CashText", "0", 36f, new Vector2(-80f, 30f), new Vector2(200f, 50f), new Color(1f, 0.85f, 0.2f));
        economyMgr.incomePerSecText = CreateUIText(topBar, "IncomeSecText", "$0/s", 24f, new Vector2(95f, 30f), new Vector2(140f, 45f), Color.white);
        economyMgr.dayCounterText = CreateUIText(topBar, "DayCounterText", "DAY 1", 26f, new Vector2(-80f, -28f), new Vector2(160f, 45f), new Color(0.75f, 0.85f, 1f));
        economyMgr.tankCounterText = CreateUIText(topBar, "TankCounterText", "0 / 50", 26f, new Vector2(95f, -28f), new Vector2(160f, 45f), new Color(0.4f, 1f, 0.55f));

        // 3. Top-Center Tutorial Slide-In Ribbon
        RectTransform bannerRect = CreateUIPanel(canvasObj.transform, "TutorialBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(760f, 68f), new Color(0.92f, 0.85f, 0.68f, 0.95f));
        economyMgr.tutorialBannerRect = bannerRect;
        economyMgr.tutorialBannerText = CreateUIText(bannerRect, "BannerText", "THE WALLS ARE THICK.", 26f, Vector2.zero, new Vector2(720f, 55f), new Color(0.15f, 0.12f, 0.08f));

        // 4. Left-Thumb Virtual Joystick
        RectTransform joyBg = CreateUIPanel(canvasObj.transform, "LeftThumb_JoystickBG", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 220f), new Vector2(240f, 240f), new Color(1f, 1f, 1f, 0.18f));
        RectTransform joyHandle = CreateUIPanel(joyBg, "JoystickHandle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(95f, 95f), new Color(1f, 1f, 1f, 0.65f));
        MobileVirtualJoystick joystick = joyBg.gameObject.AddComponent<MobileVirtualJoystick>();
        joystick.joystickBackground = joyBg;
        joystick.joystickHandle = joyHandle;
        joystick.playerController = playerCtrl;

        // 5. Right-Thumb Action Buttons: Single PUNCH (Tap/Hold), VACUUM (Hold), Blow Toggle
        Button punchBtn = CreateUIButton(canvasObj.transform, "Btn_PunchTapOrHold", "PUNCH
(HOLD CHARGE)", new Vector2(1f, 0f), new Vector2(-210f, 230f), new Vector2(230f, 230f), new Color(0.92f, 0.38f, 0.15f, 0.9f));
        UIHoldButton punchHold = punchBtn.gameObject.AddComponent<UIHoldButton>();
        UnityEventTools.AddPersistentListener(punchHold.onPointerDown, toolCtrl.OnPunchButtonDown);
        UnityEventTools.AddPersistentListener(punchHold.onPointerDown, wireframePreview.OnPunchButtonDown);
        UnityEventTools.AddPersistentListener(punchHold.onPointerUp, toolCtrl.OnPunchButtonUp);
        UnityEventTools.AddPersistentListener(punchHold.onPointerUp, wireframePreview.OnPunchButtonUp);

        Button vacBtn = CreateUIButton(canvasObj.transform, "Btn_VacuumHold", "VACUUM
(HOLD)", new Vector2(1f, 0f), new Vector2(-460f, 180f), new Vector2(180f, 180f), new Color(0.2f, 0.72f, 0.35f, 0.9f));
        UIHoldButton vacHold = vacBtn.gameObject.AddComponent<UIHoldButton>();
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerDown, toolCtrl.SetVacuumButtonHeld, true);
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerDown, suctionMgr.SetVacuumActive, true);
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerDown, audioHaptics.SetVacuumAudioActive, true);
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerUp, toolCtrl.SetVacuumButtonHeld, false);
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerUp, suctionMgr.SetVacuumActive, false);
        UnityEventTools.AddBoolPersistentListener(vacHold.onPointerUp, audioHaptics.SetVacuumAudioActive, false);

        Button blowBtn = CreateUIButton(canvasObj.transform, "Btn_ToggleBlowMode", "SUCTION / BLOW", new Vector2(1f, 0f), new Vector2(-460f, 330f), new Vector2(180f, 75f), new Color(0.25f, 0.52f, 0.88f, 0.9f));
        UnityEventTools.AddPersistentListener(blowBtn.onClick, toolCtrl.ToggleVacuumBlowMode);

        // 6. 4-Slot Hotbar (Bottom Center: Fists, Vacuum, Dynamite, Headlamp)
        string[] slotTitles = { "1: FISTS", "2: VACUUM", "3: DYNAMITE", "4: HEADLAMP" };
        for (int i = 0; i < 4; i++)
        {
            float xOffset = -240f + (i * 160f);
            Button slotBtn = CreateUIButton(canvasObj.transform, $"HotbarSlot_{i}", slotTitles[i], new Vector2(0.5f, 0f), new Vector2(xOffset, 75f), new Vector2(148f, 88f), new Color(0.18f, 0.20f, 0.25f, 0.92f));
            UnityEventTools.AddIntPersistentListener(slotBtn.onClick, toolCtrl.SelectHotbarSlot, i);
        }

        // 7. Center Treasure Pickup Prompt Button
        Button pickupBtn = CreateUIButton(canvasObj.transform, "Btn_PickupTreasurePrompt", "TAP TO COLLECT TREASURE", new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(460f, 75f), new Color(0.95f, 0.76f, 0.12f, 0.95f));
        treasureMap.pickupPromptText = pickupBtn.GetComponentInChildren<TextMeshProUGUI>();
        UnityEventTools.AddPersistentListener(pickupBtn.onClick, treasureMap.TryCollectHoveredTreasure);
        pickupBtn.gameObject.SetActive(false);

        // 8. Company Store Upgrade Drawer (Top-Right Toggle + Store Popup)
        RectTransform storePanel = CreateUIPanel(canvasObj.transform, "CompanyStore_Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 620f), new Color(0.10f, 0.11f, 0.14f, 0.96f));
        CreateUIText(storePanel, "StoreHeader", "COMPANY STORE TERMINAL - UPGRADES", 32f, new Vector2(0f, 255f), new Vector2(750f, 55f), new Color(1f, 0.84f, 0.25f));

        Button bPower = CreateUIButton(storePanel, "Btn_Up_Power", "UPGRADE FIST POWER", new Vector2(0.5f, 0.5f), new Vector2(-230f, 130f), new Vector2(380f, 80f), new Color(0.25f, 0.32f, 0.42f));
        UnityEventTools.AddPersistentListener(bPower.onClick, economyMgr.BuyPunchPowerUpgrade);

        Button bPattern = CreateUIButton(storePanel, "Btn_Up_Pattern", "UPGRADE PUNCH AREA", new Vector2(0.5f, 0.5f), new Vector2(230f, 130f), new Vector2(380f, 80f), new Color(0.25f, 0.32f, 0.42f));
        UnityEventTools.AddPersistentListener(bPattern.onClick, economyMgr.BuyPunchPatternUpgrade);

        Button bVac = CreateUIButton(storePanel, "Btn_Up_Vacuum", "UPGRADE VACUUM TIER", new Vector2(0.5f, 0.5f), new Vector2(-230f, 25f), new Vector2(380f, 80f), new Color(0.25f, 0.32f, 0.42f));
        UnityEventTools.AddPersistentListener(bVac.onClick, economyMgr.BuyVacuumUpgrade);

        Button bCrusher = CreateUIButton(storePanel, "Btn_Up_Crusher", "UPGRADE CRUSHER 07", new Vector2(0.5f, 0.5f), new Vector2(230f, 25f), new Vector2(380f, 80f), new Color(0.25f, 0.32f, 0.42f));
        UnityEventTools.AddPersistentListener(bCrusher.onClick, economyMgr.BuyCrusherSpeedAndConveyor);

        Button bDyn = CreateUIButton(storePanel, "Btn_Up_Dynamite", "BUY +3 DYNAMITE ($400)", new Vector2(0.5f, 0.5f), new Vector2(-230f, -80f), new Vector2(380f, 80f), new Color(0.55f, 0.22f, 0.18f));
        UnityEventTools.AddPersistentListener(bDyn.onClick, economyMgr.BuyDynamitePack);

        Button bAuto = CreateUIButton(storePanel, "Btn_Up_AutoPunch", "UNLOCK AUTO-PUNCH ($1200)", new Vector2(0.5f, 0.5f), new Vector2(230f, -80f), new Vector2(380f, 80f), new Color(0.22f, 0.48f, 0.32f));
        UnityEventTools.AddPersistentListener(bAuto.onClick, economyMgr.BuyAutoPunchUpgrade);

        Button b2xAd = CreateUIButton(storePanel, "Btn_RewardAd_2X", "2X CASH (WATCH AD)", new Vector2(0.5f, 0.5f), new Vector2(-230f, -195f), new Vector2(380f, 75f), new Color(0.68f, 0.48f, 0.12f));
        UnityEventTools.AddPersistentListener(b2xAd.onClick, monetization.OnTapRewardedAdDoubleCash);

        Button bCloseStore = CreateUIButton(storePanel, "Btn_CloseStore", "CLOSE [X]", new Vector2(0.5f, 0.5f), new Vector2(230f, -195f), new Vector2(380f, 75f), new Color(0.45f, 0.18f, 0.18f));
        UnityEventTools.AddBoolPersistentListener(bCloseStore.onClick, storePanel.gameObject.SetActive, false);
        storePanel.gameObject.SetActive(false);

        Button openStoreBtn = CreateUIButton(canvasObj.transform, "Btn_OpenCompanyStore", "COMPANY STORE", new Vector2(1f, 1f), new Vector2(-150f, -65f), new Vector2(240f, 80f), new Color(0.88f, 0.62f, 0.12f, 0.95f));
        UnityEventTools.AddBoolPersistentListener(openStoreBtn.onClick, storePanel.gameObject.SetActive, true);
    }

    private static RectTransform CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = panelObj.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private static TextMeshProUGUI CreateUIText(Transform parent, string name, string content, float fontSize, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }

    private static Button CreateUIButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        RectTransform rt = CreateUIPanel(parent, name, anchor, anchor, anchoredPos, size, bgColor);
        Button btn = rt.gameObject.AddComponent<Button>();
        CreateUIText(rt, "Label", label, 22f, Vector2.zero, size - new Vector2(12f, 12f), Color.white);
        return btn;
    }
}
#endif
