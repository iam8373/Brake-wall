using System;
using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Save;
using BreakToEscape.Core.Services;
using BreakToEscape.Core.Story;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Explicit start-up order for the whole game (so no Start method can overwrite loaded values):
///  1. load economy.json and story.json;
///  2. read the save file;
///  3. detect the device tier (sizes the rubble pool);
///  4. generate the deterministic mine, then apply saved voxel edits and partial damage BEFORE the
///     first mesh build;
///  5. restore economy, tank, story, monetization and settings from the save, deriving every stat
///     from the economy tables;
///  6. place the crusher and story objects from the layout, restore or spawn the player;
///  7. build chunk meshes (near the player first), push stats to every system, start the tutorial.
/// Everything runs in Start with the earliest execution order, after every component's Awake.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    [Header("Systems")]
    public VoxelWorldManager world;
    public CompanyStoreEconomyManager economyManager;
    public RubbleSuctionManager suction;
    public Crusher07Controller crusher;
    public SaveManager saveManager;
    public DeviceTierScaler tierScaler;
    public MonetizationServicesManager monetization;
    public StoryDirector storyDirector;
    public TreasureAndMapManager treasureAndMap;
    public ExitVaultAndLogbookController exitVault;
    public UIScreenAndAccessibilityManager ui;
    public Dispatch dispatch;

    [Header("Player")]
    public MobileFirstPersonController player;
    public FirstPersonToolController tools;
    public PunchAction punch;
    public DynamiteAction dynamite;

    public EconomyData Data { get; private set; }
    public StoryData Story { get; private set; }
    public EconomyState Economy { get; private set; }
    public EscapeStateMachine Escape { get; private set; }
    public TutorialSequence Tutorial { get; private set; }
    public MonetizationRules MonetizationRules { get; private set; }
    public float RunSeconds { get; private set; }
    public bool Booted { get; private set; }

    private IAnalyticsService analytics;
    private CrashForwarder crashForwarder;
    private Action<int, Vector3> crushedHandler;

    void Awake()
    {
        Instance = this;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        analytics = new LogAnalyticsService();
#else
        analytics = new NullAnalyticsService();
#endif
        crashForwarder = new CrashForwarder(new NullCrashService());
    }

    void Start()
    {
        try
        {
            Boot();
        }
        catch (Exception e)
        {
            Debug.LogError("GameBootstrap: start-up failed: " + e);
            enabled = false;
        }
    }

    void OnDestroy()
    {
        // Static events outlive the scene; remove this scene's handler so a reload starts clean.
        if (crushedHandler != null) Crusher07Controller.CubeCrushed -= crushedHandler;
        crashForwarder?.Dispose();
    }

    void Update()
    {
        if (Booted && Escape != null && !Escape.Escaped) RunSeconds += Time.deltaTime;
    }

    private static string LoadText(string resourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (!asset) throw new InvalidOperationException("Missing Resources/" + resourcePath + ".json");
        return asset.text;
    }

    private void Boot()
    {
        if (!RequireReferences()) { enabled = false; return; }

        // 1. Data
        Data = EconomyData.FromJson(LoadText("Data/economy"));
        Story = StoryData.FromJson(LoadText("Data/story"));
        Economy = new EconomyState(Data);
        Escape = new EscapeStateMachine(Story.gateCode);
        Tutorial = new TutorialSequence();
        MonetizationRules = new MonetizationRules(Data.monetization);

        // 2. Save
        SaveData save = saveManager.Load();

        // 3. Device tier (sizes the rubble pool)
        int rubbleCap = tierScaler.Initialize(Data, save != null ? save.qualityOverride : -1);

        // 4. World, with saved edits applied before any mesh exists
        world.Initialize(Data, Economy);
        if (save != null)
        {
            List<int> broken = SaveData.DecodeRuns(save.brokenVoxelRuns, world.Grid.VoxelCount);
            var zeros = new List<byte>(broken.Count);
            for (int i = 0; i < broken.Count; i++) zeros.Add(0);
            world.Grid.ApplySavedEdits(broken, zeros, save.cubesBroken);
            world.Grid.ApplySavedDamage(save.damagedVoxelIndices, save.damagedVoxelAmounts);
        }

        // 5. State from the save (stats are derived from the tables by EconomyState)
        if (save != null) RestoreState(save);
        suction.Initialize(Data, Economy, rubbleCap);
        if (save != null) suction.Tank.RestoreRuns(save.tankMaterials, save.tankValues, save.tankCounts);
        economyManager.Initialize(Data, Economy, MonetizationRules);
        if (save != null) economyManager.RestoreDay(save.day, save.dayTimer);
        crusher.Initialize(Economy);
        monetization.Initialize(MonetizationRules, Economy);
        dispatch.Initialize(Story);
        exitVault.Initialize(Escape, Story, ui);

        // 6. Placement from the layout
        MineLayout layout = world.Layout;
        crusher.transform.position = world.VoxelUnitsToWorld(layout.crusherPoint);
        storyDirector.Initialize(Story, Economy, Escape, layout, save != null ? save.usedInteractables : null);
        treasureAndMap.Initialize(Data, Economy, layout, save != null ? save.discoveredRooms : null, save != null ? save.treasuresCollected : null);
        PlacePlayer(layout, save);

        // 7. Meshes, stats, UI, tutorial
        world.BuildChunks(player.transform.position);
        punch.Initialize(Data, Economy);
        dynamite.Initialize(Data, Economy);
        economyManager.ApplyAllStatsToSystems();
        if (save != null) ui.ApplySettings(save.leftHanded, save.vibration, save.reduceFlashing, save.lookSensitivity, save.effectsIntensity, save.qualityOverride);
        else ui.ApplySettings(false, true, false, player.lookSensitivity, 1f, -1);
        WireTutorialAndCaptions(save == null);

        saveManager.SetSnapshotProvider(BuildSnapshot);
        Booted = true;
        Analytics(AnalyticsEvents.SessionStart, "tier", tierScaler.CurrentTier.ToString(), "adsRemoved", MonetizationRules.AdsRemoved);
        Debug.Log("GameBootstrap: ready. Layout " + (layout.isProcedural ? "procedural seed " + layout.seed : "hand-built v1") +
                  ", " + layout.rooms.Count + " rooms, " + layout.interactables.Count + " interactables, " + world.Grid.EditCount + " saved edits.");
    }

    private bool RequireReferences()
    {
        bool ok = true;
        void Check(UnityEngine.Object o, string name)
        {
            if (o) return;
            Debug.LogError("GameBootstrap: '" + name + "' is not assigned. Run BreakToEscape > Build Complete Playable Scene again.", this);
            ok = false;
        }
        Check(world, nameof(world)); Check(economyManager, nameof(economyManager)); Check(suction, nameof(suction));
        Check(crusher, nameof(crusher)); Check(saveManager, nameof(saveManager)); Check(tierScaler, nameof(tierScaler));
        Check(monetization, nameof(monetization)); Check(storyDirector, nameof(storyDirector)); Check(treasureAndMap, nameof(treasureAndMap));
        Check(exitVault, nameof(exitVault)); Check(ui, nameof(ui)); Check(dispatch, nameof(dispatch));
        Check(player, nameof(player)); Check(tools, nameof(tools)); Check(punch, nameof(punch)); Check(dynamite, nameof(dynamite));
        return ok;
    }

    private void RestoreState(SaveData s)
    {
        for (int i = 0; i < UpgradeLines.Count; i++) Economy.SetLevel((UpgradeLine)i, s.upgradeLevels[i]);
        for (int i = 0; i < EconomyState.EquipmentKeys.Length; i++) Economy.RestoreEquipment(EconomyState.EquipmentKeys[i], s.equipmentRestored[i], s.partsFound[i]);
        Economy.RestoreItems(s.autoPunchUnlocked, s.treasureDetectorUnlocked, s.headlampLevel, s.dynamiteStock);
        Economy.RestoreCash(s.cash, s.lifetimeCashEarned);
        Escape.Restore(s.fuelCellFound, s.generatorRepaired, s.winchCableRecovered, s.logbooksFound, s.gateCodeEntry, s.liftGateOpened, s.escaped);
        Tutorial.Restore(s.tutorialStep);
        MonetizationRules.Restore(s.adsRemoved, s.doubleCashRemaining, s.doubleCashCooldownRemaining);
        RunSeconds = s.runSeconds;
    }

    private void PlacePlayer(MineLayout layout, SaveData save)
    {
        if (save != null && save.hasPlayerPose)
        {
            var feet = new Vector3(save.playerX, save.playerY, save.playerZ);
            Int3 v = world.WorldToVoxel(feet + Vector3.up * 0.25f);
            bool clear = world.Grid.InBounds(v.x, v.y, v.z) && !world.Grid.IsSolid(v.x, v.y, v.z) && !world.Grid.IsSolid(v.x, v.y + 1, v.z) && !world.Grid.IsSolid(v.x, v.y + 2, v.z);
            if (clear)
            {
                player.TeleportTo(feet, save.playerYaw, save.playerPitch);
                return;
            }
            Debug.LogWarning("GameBootstrap: saved player position is inside rock; using the spawn point.");
        }
        Vector3 spawn = world.VoxelUnitsToWorld(layout.spawnPoint);
        Float3 look = layout.spawnLookDirection;
        float yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
        player.TeleportTo(spawn, yaw, 0f);
    }

    private void WireTutorialAndCaptions(bool newRun)
    {
        world.CubesBroken += (broken, dir, blast) => Tutorial.OnCubeBroken();
        suction.CubeVacuumed += Tutorial.OnCubeVacuumed;
        suction.TankFull += () => dispatch.Say("tank_full");
        crushedHandler = (value, pos) => Tutorial.OnCashEarned(RunSeconds);
        Crusher07Controller.CubeCrushed += crushedHandler;
        economyManager.PurchaseAttempted += (item, result) =>
        {
            if (result == PurchaseResult.Success && Array.IndexOf(UpgradeLines.JsonKeys, item) >= 0) Tutorial.OnUpgradeBought(RunSeconds);
        };
        economyManager.DayStarted += day => dispatch.Say("day_start", day.ToString());
        Tutorial.StepShown += step =>
        {
            dispatch.Say(TutorialSequence.StepCaptionKey(step));
            if (step == TutorialSequence.Done)
                Debug.Log("Tutorial: first cash at " + Tutorial.FirstCashTime.ToString("0") + " s, first upgrade at " + Tutorial.FirstUpgradeTime.ToString("0") + " s (target: both within 120 s).");
        };
        if (newRun) dispatch.Say("wake");
        Tutorial.Begin();
    }

    // ------------------------------------------------------------------ save

    /// <summary>Snapshot of every system for the save file. Main thread only.</summary>
    public SaveData BuildSnapshot()
    {
        var s = new SaveData
        {
            savedAtUtcTicks = DateTime.UtcNow.Ticks,
            cash = Economy.Cash,
            lifetimeCashEarned = Economy.LifetimeCashEarned,
            day = economyManager.CurrentDay,
            dayTimer = economyManager.DayTimer,
            runSeconds = RunSeconds,
            cubesBroken = world.Grid.BrokenCount,
            autoPunchUnlocked = Economy.AutoPunchUnlocked,
            treasureDetectorUnlocked = Economy.TreasureDetectorUnlocked,
            headlampLevel = Economy.HeadlampLevel,
            dynamiteStock = Economy.DynamiteStock,
            hasPlayerPose = true,
            playerYaw = player.Yaw,
            playerPitch = player.Pitch,
            fuelCellFound = Escape.FuelCellFound,
            generatorRepaired = Escape.GeneratorRepaired,
            winchCableRecovered = Escape.CableRecovered,
            gateCodeEntry = new[] { Escape.Dial(0), Escape.Dial(1), Escape.Dial(2) },
            liftGateOpened = Escape.GateOpened,
            escaped = Escape.Escaped,
            tutorialStep = Tutorial.Step,
            adsRemoved = MonetizationRules.AdsRemoved,
            doubleCashRemaining = MonetizationRules.DoubleCashRemaining,
            doubleCashCooldownRemaining = MonetizationRules.DoubleCashCooldownRemaining,
            leftHanded = ui.LeftHanded,
            vibration = ui.VibrationEnabled,
            reduceFlashing = ui.ReduceFlashing,
            lookSensitivity = ui.LookSensitivity,
            qualityOverride = ui.QualityOverride,
            effectsIntensity = ui.EffectsIntensity,
        };
        for (int i = 0; i < UpgradeLines.Count; i++) s.upgradeLevels[i] = Economy.Level((UpgradeLine)i);
        for (int i = 0; i < EconomyState.EquipmentKeys.Length; i++)
        {
            s.equipmentRestored[i] = Economy.IsRestored(EconomyState.EquipmentKeys[i]);
            s.partsFound[i] = Economy.HasPart(EconomyState.EquipmentKeys[i]);
        }
        Vector3 feet = player.FeetPosition;
        s.playerX = feet.x;
        s.playerY = feet.y;
        s.playerZ = feet.z;
        suction.Tank.GetRuns(s.tankMaterials, s.tankValues, s.tankCounts);
        s.logbooksFound.AddRange(Escape.LogbooksFound);
        s.usedInteractables.AddRange(storyDirector.UsedInteractables);
        s.discoveredRooms.AddRange(treasureAndMap.DiscoveredRooms);
        s.treasuresCollected.AddRange(treasureAndMap.CollectedTreasureIds());

        var indices = new List<int>(world.Grid.EditCount);
        var ids = new List<byte>(world.Grid.EditCount);
        world.Grid.GetEdits(indices, ids);
        s.brokenVoxelRuns = SaveData.EncodeRuns(indices);
        world.Grid.GetPartialDamage(s.damagedVoxelIndices, s.damagedVoxelAmounts);
        return s;
    }

    public void SaveNow()
    {
        if (Booted && saveManager) saveManager.SaveNow();
    }

    /// <summary>Settings: delete the save and restart the scene as a fresh run.</summary>
    public void StartNewRun()
    {
        if (!saveManager) return;
        Booted = false;
        saveManager.SetSnapshotProvider(null);
        saveManager.DeleteSave();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ------------------------------------------------------------------ analytics

    /// <summary>Logs an analytics event with alternating key/value parameters.</summary>
    public void Analytics(string eventName, params object[] keyValues)
    {
        if (analytics == null) return;
        var parameters = new Dictionary<string, object>();
        for (int i = 0; i + 1 < keyValues.Length; i += 2) parameters[keyValues[i].ToString()] = keyValues[i + 1];
        analytics.LogEvent(eventName, parameters);
    }
}
