using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Story;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Spawns the story objects from the mine layout (simple primitive placeholders until real models
/// exist) and runs what each one does: crew locker → Work Gauntlets and Quill's logbook; logbooks
/// and notes → text popups; parts → equipment repair parts; fuel cell → generator → power;
/// winch cable; dynamite crates; lift gate → code card. All text comes from story.json.
/// Also announces each rock band the first time the player enters it.
/// </summary>
public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance { get; private set; }

    [Header("References")]
    public InteractionController interaction;
    public ExitVaultAndLogbookController exitVault;
    public UIScreenAndAccessibilityManager ui;
    public Transform player;
    [Tooltip("Lit material used for every placeholder object; colours are set per object.")]
    public Material placeholderMaterial;

    public IReadOnlyCollection<string> UsedInteractables => used;

    private readonly HashSet<string> used = new HashSet<string>();
    private readonly List<MineInteractable> spawned = new List<MineInteractable>();
    private readonly HashSet<int> bandsAnnounced = new HashSet<int> { 1 };
    private StoryData story;
    private EconomyState economy;
    private EscapeStateMachine escape;
    private Light generatorLight;
    private float bandTimer;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(StoryData storyData, EconomyState economyState, EscapeStateMachine escapeState, MineLayout layout, IEnumerable<string> alreadyUsed)
    {
        story = storyData;
        economy = economyState;
        escape = escapeState;
        if (alreadyUsed != null) foreach (string id in alreadyUsed) used.Add(id);
        if (!interaction) Debug.LogError("StoryDirector: interaction is not assigned.", this);
        if (!placeholderMaterial) Debug.LogError("StoryDirector: placeholderMaterial is not assigned; story objects will be invisible.", this);

        VoxelWorldManager world = VoxelWorldManager.Instance;
        foreach (InteractableSpot spot in layout.interactables)
        {
            GameObject go = PlaceholderFactory.Build(spot.type, placeholderMaterial, transform);
            go.name = "Interactable_" + spot.id;
            go.transform.position = world.VoxelUnitsToWorld(spot.position);
            if (spot.type == InteractableType.LiftGate)
            {
                go.transform.rotation = Quaternion.LookRotation(VoxelWorldManager.ToVector3(layout.liftGateFacing));
                if (exitVault) exitVault.AttachGate(go.transform);
            }
            if (spot.type == InteractableType.Generator) generatorLight = go.GetComponentInChildren<Light>(true);
            MineInteractable item = go.AddComponent<MineInteractable>();
            item.Setup(spot, this, story.Prompt(spot.type.ToString(), "USE"));
            spawned.Add(item);
            if (used.Contains(spot.id) && IsPickup(spot.type)) item.Consume(true);
            if (interaction) interaction.Register(item);
        }
        if (generatorLight) generatorLight.enabled = escape.GeneratorRepaired;
    }

    private static bool IsPickup(InteractableType type) =>
        type == InteractableType.RepairPart || type == InteractableType.FuelCell || type == InteractableType.WinchCable || type == InteractableType.DynamiteCrate;

    void Update()
    {
        if (!player || story == null) return;
        bandTimer -= Time.deltaTime;
        if (bandTimer > 0f) return;
        bandTimer = 0.5f;
        VoxelWorldManager world = VoxelWorldManager.Instance;
        if (!world || !world.IsReady) return;
        int band = world.BandAtWorld(player.position);
        if (bandsAnnounced.Add(band))
        {
            if (Dispatch.Instance) Dispatch.Instance.Say("band_" + band);
            if (GameBootstrap.Instance) GameBootstrap.Instance.Analytics(BreakToEscape.Core.Services.AnalyticsEvents.BandReached, "band", band, "runMinutes", GameBootstrap.Instance.RunSeconds / 60f);
        }
    }

    public void Handle(MineInteractable item)
    {
        InteractableSpot spot = item.Spot;
        Dispatch d = Dispatch.Instance;
        bool firstTime = used.Add(spot.id);
        switch (spot.type)
        {
            case InteractableType.StoreTerminal:
                if (ui) ui.OpenCompanyStore();
                break;

            case InteractableType.CrewLocker:
            {
                string[] parts = (spot.payload ?? "").Split('|');
                if (parts.Length > 0 && economy.RestoreFromLocker(parts[0]))
                {
                    if (d) d.Say("locker_opened");
                    if (CompanyStoreEconomyManager.Instance) CompanyStoreEconomyManager.Instance.ApplyAllStatsToSystems();
                }
                if (parts.Length > 1) ShowLogbook(parts[1]);
                break;
            }

            case InteractableType.Logbook:
                if (firstTime && d) d.Say("logbook_found");
                ShowLogbook(spot.payload);
                break;

            case InteractableType.HardHat:
                if (exitVault && story.hardHats.TryGetValue(spot.payload, out string hat)) exitVault.ShowText(spot.payload.ToUpperInvariant() + "'S HARD HAT", hat);
                break;

            case InteractableType.WallNote:
                if (exitVault && story.notes.TryGetValue(spot.payload, out WallNote note)) exitVault.ShowText(note.title, note.body);
                break;

            case InteractableType.RepairPart:
                economy.FindPart(spot.payload);
                item.Consume(true);
                if (d) d.Say("part_found");
                break;

            case InteractableType.FuelCell:
                escape.FindFuelCell();
                item.Consume(true);
                if (d) d.Say("fuel_cell_found");
                break;

            case InteractableType.Generator:
            {
                GeneratorResult result = escape.TryRepairGenerator();
                if (result == GeneratorResult.NeedFuelCell) { used.Remove(spot.id); if (d) d.Say("generator_need_fuel"); }
                else if (result == GeneratorResult.Repaired && d) d.Say("generator_repaired");
                if (generatorLight) generatorLight.enabled = escape.GeneratorRepaired;
                break;
            }

            case InteractableType.WinchCable:
                escape.RecoverCable();
                item.Consume(true);
                if (d) d.Say("cable_recovered");
                break;

            case InteractableType.DynamiteCrate:
                economy.AddDynamite(int.TryParse(spot.payload, out int sticks) ? sticks : 2);
                item.Consume(true);
                if (d) d.Say("dynamite_found");
                break;

            case InteractableType.LiftGate:
                if (exitVault) exitVault.OpenVaultCodeCard();
                break;
        }
    }

    private void ShowLogbook(string id)
    {
        if (string.IsNullOrEmpty(id) || !story.logbooks.TryGetValue(id, out LogbookEntry entry)) return;
        escape.FindLogbook(id);
        if (exitVault) exitVault.ShowText(entry.title, entry.body);
    }
}
