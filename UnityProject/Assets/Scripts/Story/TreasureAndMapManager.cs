using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Named showcase treasures, the treasure detector radar, room discovery and the hand-drawn map.
/// Treasure design (one system): ordinary treasure is a special voxel type that drops high-value
/// rubble (handled by the world); this manager only adds a few named buried objects from the
/// layout. Dynamite destroys treasure voxels in its blast; named treasures survive and pop free.
/// The map is built at runtime from the layout's real room bounds, so it also works for
/// procedural mines. Undiscovered rooms show a "?" until entered.
/// </summary>
public class TreasureAndMapManager : MonoBehaviour
{
    public static TreasureAndMapManager Instance { get; private set; }

    [Header("References")]
    public Transform playerTransform;
    public InteractionController interaction;
    public Material placeholderMaterial;

    [Header("Detector")]
    public GameObject detectorSignalRoot;
    public TextMeshProUGUI detectorDistanceText;

    [Header("Map")]
    [Tooltip("Paper map panel; rooms are drawn inside this rectangle.")]
    public RectTransform mapArea;
    public RectTransform mapPlayerPin;
    public Color roomColor = new Color(0.25f, 0.2f, 0.14f, 0.9f);
    public Color undiscoveredColor = new Color(0.25f, 0.2f, 0.14f, 0.25f);
    public Color currentRoomColor = new Color(0.95f, 0.78f, 0.1f, 1f);

    public event System.Action<RoomDefinition> RoomDiscovered;

    private readonly List<TreasurePickup> treasures = new List<TreasurePickup>();
    private readonly HashSet<string> discovered = new HashSet<string>();
    private readonly Dictionary<string, Image> roomImages = new Dictionary<string, Image>();
    private readonly Dictionary<string, TextMeshProUGUI> roomLabels = new Dictionary<string, TextMeshProUGUI>();
    private MineLayout layout;
    private EconomyState economy;
    private EconomyData data;
    private float discoveryTimer;
    private float radarTimer;
    private string currentRoom;

    public IEnumerable<string> DiscoveredRooms => discovered;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(EconomyData economyData, EconomyState economyState, MineLayout mineLayout, IEnumerable<string> discoveredIds, IEnumerable<int> collectedIds)
    {
        data = economyData;
        economy = economyState;
        layout = mineLayout;
        var collected = new HashSet<int>(collectedIds ?? new int[0]);
        foreach (RoomDefinition r in layout.rooms) if (r.startsDiscovered) discovered.Add(r.id);
        if (discoveredIds != null) foreach (string id in discoveredIds) discovered.Add(id);

        VoxelWorldManager world = VoxelWorldManager.Instance;
        foreach (TreasureSpot spot in layout.treasures)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Treasure_" + spot.id;
            go.transform.SetParent(transform, false);
            go.transform.position = world.VoxelCenterToWorld(spot.voxel);
            go.transform.localScale = new Vector3(0.3f, 0.12f, 0.3f);
            Collider c = go.GetComponent<Collider>();
            if (c) Destroy(c);
            if (placeholderMaterial) go.GetComponent<MeshRenderer>().sharedMaterial = placeholderMaterial;
            TreasurePickup pickup = go.AddComponent<TreasurePickup>();
            pickup.Setup(spot, this);
            if (collected.Contains(spot.id)) pickup.MarkCollected();
            else pickup.SetUncovered(!world.Grid.IsSolid(spot.voxel.x, spot.voxel.y, spot.voxel.z));
            treasures.Add(pickup);
            if (interaction) interaction.Register(pickup);
        }
        world.CubesBroken += OnCubesBroken;
        BuildMap();
    }

    void OnDestroy()
    {
        if (VoxelWorldManager.Instance) VoxelWorldManager.Instance.CubesBroken -= OnCubesBroken;
    }

    private void OnCubesBroken(List<BrokenVoxel> broken, Vector3 direction, bool isBlast)
    {
        VoxelWorldManager world = VoxelWorldManager.Instance;
        for (int t = 0; t < treasures.Count; t++)
        {
            TreasurePickup pickup = treasures[t];
            if (pickup.Uncovered || pickup.Collected) continue;
            Int3 v = pickup.Spot.voxel;
            if (world.Grid.IsSolid(v.x, v.y, v.z)) continue;
            pickup.SetUncovered(true);
            if (Dispatch.Instance) Dispatch.Instance.Say("treasure");
        }
    }

    public void Collect(TreasurePickup pickup)
    {
        if (!pickup.CanInteract) return;
        pickup.MarkCollected();
        if (CompanyStoreEconomyManager.Instance) CompanyStoreEconomyManager.Instance.Earn(pickup.Spot.cashValue);
        if (Crusher07Controller.Instance) Crusher07Controller.Instance.SpawnPopup(pickup.Spot.cashValue, pickup.transform.position);
    }

    public List<int> CollectedTreasureIds()
    {
        var ids = new List<int>();
        foreach (TreasurePickup p in treasures) if (p.Collected) ids.Add(p.Spot.id);
        return ids;
    }

    void Update()
    {
        if (layout == null || !playerTransform) return;
        discoveryTimer -= Time.deltaTime;
        if (discoveryTimer <= 0f)
        {
            discoveryTimer = 0.25f;
            CheckRooms();
        }
        UpdateRadar();
        UpdatePin();
    }

    private void CheckRooms()
    {
        VoxelWorldManager world = VoxelWorldManager.Instance;
        Int3 v = world.WorldToVoxel(playerTransform.position);
        string inside = null;
        foreach (RoomDefinition room in layout.rooms)
        {
            VoxelBox b = room.bounds;
            var feet = new Int3(v.x, Mathf.Clamp(v.y, b.min.y, b.max.y - 1), v.z);
            if (!b.Contains(feet)) continue;
            inside = room.id;
            if (discovered.Add(room.id))
            {
                if (Dispatch.Instance) Dispatch.Instance.SayText("DISCOVERED: " + room.title.ToUpperInvariant());
                RoomDiscovered?.Invoke(room);
            }
        }
        if (inside != currentRoom)
        {
            currentRoom = inside;
            RefreshMapColours();
        }
    }

    private void UpdateRadar()
    {
        bool on = economy != null && economy.TreasureDetectorUnlocked;
        float closest = float.MaxValue;
        if (on)
        {
            Vector3 p = playerTransform.position;
            for (int i = 0; i < treasures.Count; i++)
            {
                if (treasures[i].Collected) continue;
                float d = Vector3.Distance(p, treasures[i].transform.position);
                if (d < closest) closest = d;
            }
        }
        bool inRange = on && closest <= data.treasureDetectorRangeMeters;
        if (detectorSignalRoot && detectorSignalRoot.activeSelf != inRange) detectorSignalRoot.SetActive(inRange);
        if (!inRange) return;
        if (detectorDistanceText) detectorDistanceText.text = closest.ToString("F1") + " m";
        float proximity = 1f - Mathf.Clamp01(closest / data.treasureDetectorRangeMeters);
        radarTimer -= Time.deltaTime;
        if (radarTimer <= 0f)
        {
            radarTimer = Mathf.Lerp(1.2f, 0.22f, proximity);
            if (AudioHapticsManager.Instance) AudioHapticsManager.Instance.PlayRadarPing(proximity);
        }
    }

    // ------------------------------------------------------------------ map

    private void BuildMap()
    {
        if (!mapArea) return;
        foreach (RoomDefinition room in layout.rooms)
        {
            var go = new GameObject("MapRoom_" + room.id);
            go.transform.SetParent(mapArea, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            SetRect(rt, room.bounds);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            roomImages[room.id] = img;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rt, false);
            RectTransform lrt = labelObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 22f;
            label.color = new Color(0.97f, 0.93f, 0.82f);
            label.raycastTarget = false;
            roomLabels[room.id] = label;
        }
        if (mapPlayerPin) mapPlayerPin.SetAsLastSibling();
        RefreshMapColours();
    }

    private void SetRect(RectTransform rt, VoxelBox b)
    {
        float sx = layout.worldSize.x, sz = layout.worldSize.z;
        rt.anchorMin = new Vector2(b.min.x / sx, b.min.z / sz);
        rt.anchorMax = new Vector2(b.max.x / sx, b.max.z / sz);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void RefreshMapColours()
    {
        foreach (RoomDefinition room in layout.rooms)
        {
            if (!roomImages.TryGetValue(room.id, out Image img)) continue;
            bool known = discovered.Contains(room.id);
            img.color = room.id == currentRoom ? currentRoomColor : (known ? roomColor : undiscoveredColor);
            TextMeshProUGUI label = roomLabels[room.id];
            label.text = known ? room.title + (room.id == currentRoom ? "\nyou are here" : "\n" + room.mapLabel) : "?";
        }
    }

    private void UpdatePin()
    {
        if (!mapPlayerPin || !mapArea || !mapArea.gameObject.activeInHierarchy) return;
        Int3 v = VoxelWorldManager.Instance.WorldToVoxel(playerTransform.position);
        float nx = v.x / (float)layout.worldSize.x, nz = v.z / (float)layout.worldSize.z;
        mapPlayerPin.anchorMin = mapPlayerPin.anchorMax = new Vector2(nx, nz);
        mapPlayerPin.anchoredPosition = Vector2.zero;
        mapPlayerPin.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
    }
}
