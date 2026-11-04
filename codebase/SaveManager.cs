using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public int seed = 10492;
    public int currentCash = 0;
    public int currentDay = 1;

    // Upgrade levels
    public int punchPowerLevel = 1;
    public int punchPatternLevel = 1;
    public int vacuumTierLevel = 1;
    public int crusherSpeedLevel = 1;
    public bool autoPunchUnlocked = false;
    public bool treasureDetectorUnlocked = false;
    public bool adsRemoved = false;

    // Inventory & tank
    public int dynamiteStock = 3;
    public int currentTankCount = 0;

    // Player Transform
    public float playerX = 7f, playerY = 1.5f, playerZ = 2f;
    public float playerYaw = 0f;

    // Map & narrative progression
    public List<string> discoveredRoomIds = new List<string>();
    public List<int> collectedTreasureIds = new List<int>();

    // Sparse delta-compressed voxel edits (only stores voxels that differ from initial generation)
    public List<int> editedVoxelFlatIndices = new List<int>();
    public List<byte> editedVoxelNewIds = new List<byte>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("System References")]
    public CompanyStoreEconomyManager economyManager;
    public FirstPersonToolController toolController;
    public VoxelWireframePreview wireframePreview;
    public Crusher07Controller crusher07;
    public RubbleSuctionManager suctionManager;
    public MobileFirstPersonController playerController;
    public TreasureAndMapManager treasureAndMapManager;

    [Header("Auto-Save Settings")]
    public float autoSaveIntervalSeconds = 45f;

    // Hook your Google Play Games SavedGame client callbacks here
    public event Action<byte[]> OnPushCloudSaveBytes;

    private string saveFilePath;
    private float autoSaveTimer = 0f;

    // Tracks runtime voxel edits as (flatIndex -> newVoxelId) for lightweight delta saving
    private readonly Dictionary<int, byte> voxelEditDeltas = new Dictionary<int, byte>(4096);

    void Awake()
    {
        Instance = this;
        saveFilePath = Path.Combine(Application.persistentDataPath, "break_to_escape_save.dat");
    }

    void Start()
    {
        LoadGameLocal();
    }

    void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveIntervalSeconds)
        {
            autoSaveTimer = 0f;
            SaveGameLocal();
        }
    }

    // Crucial on Android: save immediately when the user switches apps or locks the phone
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameLocal();
    }

    void OnApplicationQuit()
    {
        SaveGameLocal();
    }

    // Call this from VoxelWorldManager whenever a voxel is destroyed or modified
    public void RecordVoxelEdit(int x, int y, int z, int totalY, int totalZ, byte newId)
    {
        int flatIndex = (x * totalY * totalZ) + (y * totalZ) + z;
        voxelEditDeltas[flatIndex] = newId;
    }

    public void SaveGameLocal()
    {
        GameSaveData data = BuildSaveDataSnapshot();
        byte[] compressedBytes = CompressSaveData(data);

        try
        {
            File.WriteAllBytes(saveFilePath, compressedBytes);
            OnPushCloudSaveBytes?.Invoke(compressedBytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveManager: Failed to write local save: {e.Message}");
        }
    }

    public void LoadGameLocal()
    {
        if (!File.Exists(saveFilePath)) return;

        try
        {
            byte[] compressedBytes = File.ReadAllBytes(saveFilePath);
            GameSaveData data = DecompressSaveData(compressedBytes);
            if (data != null) ApplySaveDataSnapshot(data);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveManager: Failed to load local save: {e.Message}");
        }
    }

    // Call this when Google Play Games Cloud Save returns a byte array
    public void ApplyCloudSaveBytes(byte[] cloudBytes)
    {
        if (cloudBytes == null || cloudBytes.Length == 0) return;
        GameSaveData cloudData = DecompressSaveData(cloudBytes);
        if (cloudData != null)
        {
            ApplySaveDataSnapshot(cloudData);
            File.WriteAllBytes(saveFilePath, cloudBytes);
        }
    }

    private GameSaveData BuildSaveDataSnapshot()
    {
        GameSaveData data = new GameSaveData();

        if (economyManager)
        {
            data.currentCash = economyManager.currentCash;
            data.currentDay = economyManager.currentDay;
            data.punchPowerLevel = economyManager.punchPowerLevel;
            data.punchPatternLevel = economyManager.punchPatternLevel;
            data.vacuumTierLevel = economyManager.vacuumTierLevel;
            data.crusherSpeedLevel = economyManager.crusherSpeedLevel;
            data.autoPunchUnlocked = economyManager.autoPunchUnlocked;
        }

        if (toolController)
        {
            data.dynamiteStock = toolController.dynamiteStock;
            data.currentTankCount = toolController.currentTankCount;
        }

        if (playerController)
        {
            Vector3 pos = playerController.transform.position;
            data.playerX = pos.x;
            data.playerY = pos.y;
            data.playerZ = pos.z;
            data.playerYaw = playerController.transform.eulerAngles.y;
        }

        if (treasureAndMapManager)
        {
            data.treasureDetectorUnlocked = treasureAndMapManager.detectorUnlocked;
            data.discoveredRoomIds = new List<string>(treasureAndMapManager.GetDiscoveredRooms());
            data.collectedTreasureIds = new List<int>(treasureAndMapManager.GetCollectedTreasures());
        }

        foreach (var kvp in voxelEditDeltas)
        {
            data.editedVoxelFlatIndices.Add(kvp.Key);
            data.editedVoxelNewIds.Add(kvp.Value);
        }

        return data;
    }

    private void ApplySaveDataSnapshot(GameSaveData data)
    {
        if (economyManager)
        {
            economyManager.currentCash = data.currentCash;
            economyManager.currentDay = data.currentDay;
            economyManager.punchPowerLevel = data.punchPowerLevel;
            economyManager.punchPatternLevel = data.punchPatternLevel;
            economyManager.vacuumTierLevel = data.vacuumTierLevel;
            economyManager.crusherSpeedLevel = data.crusherSpeedLevel;
            economyManager.autoPunchUnlocked = data.autoPunchUnlocked;
            economyManager.AddCash(0); // Refresh HUD
        }

        if (toolController)
        {
            toolController.dynamiteStock = data.dynamiteStock;
            toolController.currentTankCount = data.currentTankCount;
            toolController.basePunchPower = 1f + (data.punchPowerLevel - 1) * 1.2f;
            toolController.SetGloveUpgradeTier(data.punchPowerLevel >= 3 ? 1 : 0);

            int vacIndex = Mathf.Clamp(data.vacuumTierLevel - 1, 0, 2);
            toolController.SetVacuumUpgradeTier(vacIndex);
            toolController.maxTankCapacity = (data.vacuumTierLevel == 1) ? 50 : (data.vacuumTierLevel == 2 ? 150 : 350);
        }

        if (wireframePreview)
        {
            wireframePreview.basePatternRadius = Mathf.Clamp(data.punchPatternLevel, 1, 3);
        }

        if (crusher07)
        {
            crusher07.crushesPerSecond = 6f + (data.crusherSpeedLevel - 1) * 4f;
            crusher07.autoConveyorUnlocked = data.crusherSpeedLevel >= 3;
        }

        if (playerController)
        {
            CharacterController cc = playerController.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            playerController.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            playerController.transform.rotation = Quaternion.Euler(0f, data.playerYaw, 0f);
            if (cc) cc.enabled = true;
        }

        if (treasureAndMapManager)
        {
            treasureAndMapManager.RestoreState(data.treasureDetectorUnlocked, data.discoveredRoomIds, data.collectedTreasureIds);
        }

        voxelEditDeltas.Clear();
        int count = Mathf.Min(data.editedVoxelFlatIndices.Count, data.editedVoxelNewIds.Count);
        for (int i = 0; i < count; i++)
        {
            voxelEditDeltas[data.editedVoxelFlatIndices[i]] = data.editedVoxelNewIds[i];
        }
    }

    private byte[] CompressSaveData(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        byte[] rawBytes = Encoding.UTF8.GetBytes(json);

        using (MemoryStream output = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                gzip.Write(rawBytes, 0, rawBytes.Length);
            }
            return output.ToArray();
        }
    }

    private GameSaveData DecompressSaveData(byte[] compressedBytes)
    {
        using (MemoryStream input = new MemoryStream(compressedBytes))
        using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
        using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))
        {
            string json = reader.ReadToEnd();
            return JsonUtility.FromJson<GameSaveData>(json);
        }
    }
}
