using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class BuriedTreasureNode
{
    public int treasureId;
    public string displayName = "Gold Ring";
    public int cashValue = 2700;
    public Transform treasureObject;
    public Rigidbody treasureRigidbody;
    public bool isUncovered = false;
    public bool isCollected = false;
}

[Serializable]
public class MineRoomZone
{
    public string roomId;        // e.g. "CentralCell", "DeadEnd", "Burrow", "Nook", "ExitVault"
    public string roomTitle;
    public Bounds worldBounds;
    public GameObject mapRoomFogOverlay; // Hidden on the 2D hand-drawn map once discovered
    public bool isDiscovered = false;
}

public class TreasureAndMapManager : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Camera playerCamera;
    public VoxelWorldManager voxelWorld;
    public CompanyStoreEconomyManager economyManager;

    [Header("Buried Treasure & Detector Upgrade")]
    public bool detectorUnlocked = false;
    public float detectorMaxRange = 6.5f;
    public Image detectorSignalIcon;
    public TextMeshProUGUI detectorDistanceText;
    public TextMeshProUGUI pickupPromptText;
    public List<BuriedTreasureNode> buriedTreasures = new List<BuriedTreasureNode>();

    [Header("2D Hand-Drawn Map Overlay")]
    public GameObject mapOverlayPanel;
    public RectTransform mapPlayerPin;
    public Vector2 worldMinXZ = new Vector2(0f, 0f);
    public Vector2 worldMaxXZ = new Vector2(16f, 24f);
    public Vector2 mapCanvasMin = new Vector2(-300f, -200f);
    public Vector2 mapCanvasMax = new Vector2(300f, 200f);
    public List<MineRoomZone> mineRooms = new List<MineRoomZone>();

    private float radarPingTimer = 0f;
    private BuriedTreasureNode hoveredTreasureForPickup = null;

    void Start()
    {
        if (!playerCamera) playerCamera = Camera.main;

        // Lock buried treasures inside the wall until neighboring voxels are dug away
        foreach (var t in buriedTreasures)
        {
            if (t.treasureRigidbody) t.treasureRigidbody.isKinematic = true;
        }
    }

    void Update()
    {
        CheckRoomDiscoveryAndMapPin();
        CheckBuriedTreasureUncovering();
        UpdateTreasureDetectorRadar();
        CheckTreasurePickupLook();
    }

    // =========================================================================
    // 1. BURIED TREASURE UNCOVERING & PICKUP / CRUSHER PAYOUT
    // =========================================================================

    private void CheckBuriedTreasureUncovering()
    {
        if (!voxelWorld) return;

        for (int i = 0; i < buriedTreasures.Count; i++)
        {
            BuriedTreasureNode node = buriedTreasures[i];
            if (node.isCollected || node.isUncovered || !node.treasureObject) continue;

            Vector3 localPos = node.treasureObject.position - voxelWorld.transform.position;
            int vx = Mathf.FloorToInt(localPos.x / voxelWorld.voxelSize);
            int vy = Mathf.FloorToInt(localPos.y / voxelWorld.voxelSize);
            int vz = Mathf.FloorToInt(localPos.z / voxelWorld.voxelSize);

            // If the voxel containing the treasure is carved to Air (0), pop the treasure free!
            if (voxelWorld.GetVoxelId(vx, vy, vz) == 0)
            {
                node.isUncovered = true;
                if (node.treasureRigidbody)
                {
                    node.treasureRigidbody.isKinematic = false;
                    node.treasureRigidbody.WakeUp();
                }
                if (economyManager)
                {
                    economyManager.ShowTutorialCaption("TREASURE IN THE WALLS! EVERYTHING IN THE WALL IS YOURS.");
                }
            }
        }
    }

    private void CheckTreasurePickupLook()
    {
        hoveredTreasureForPickup = null;
        if (!playerCamera) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 2.4f))
        {
            for (int i = 0; i < buriedTreasures.Count; i++)
            {
                BuriedTreasureNode node = buriedTreasures[i];
                if (!node.isCollected && node.treasureObject && hit.transform == node.treasureObject)
                {
                    hoveredTreasureForPickup = node;
                    break;
                }
            }
        }

        if (pickupPromptText)
        {
            bool show = hoveredTreasureForPickup != null;
            if (pickupPromptText.gameObject.activeSelf != show)
                pickupPromptText.gameObject.SetActive(show);

            if (show)
                pickupPromptText.text = $"TAP TO COLLECT {hoveredTreasureForPickup.displayName.ToUpper()} (+${hoveredTreasureForPickup.cashValue:N0})";
        }
    }

    // Bind to your centre "PICK UP" interaction button
    public void TryCollectHoveredTreasure()
    {
        if (hoveredTreasureForPickup == null || hoveredTreasureForPickup.isCollected) return;

        hoveredTreasureForPickup.isCollected = true;
        if (hoveredTreasureForPickup.treasureObject)
            hoveredTreasureForPickup.treasureObject.gameObject.SetActive(false);

        if (economyManager)
            economyManager.AddCash(hoveredTreasureForPickup.cashValue);

        hoveredTreasureForPickup = null;
        if (pickupPromptText) pickupPromptText.gameObject.SetActive(false);
    }

    // =========================================================================
    // 2. TREASURE DETECTOR UPGRADE & PROXIMITY RADAR
    // =========================================================================

    public void BuyTreasureDetectorUpgrade()
    {
        int cost = 650;
        if (!economyManager || economyManager.currentCash < cost || detectorUnlocked) return;

        economyManager.currentCash -= cost;
        economyManager.AddCash(0);
        detectorUnlocked = true;
    }

    private void UpdateTreasureDetectorRadar()
    {
        if (!detectorUnlocked || !playerTransform)
        {
            if (detectorSignalIcon && detectorSignalIcon.gameObject.activeSelf)
                detectorSignalIcon.gameObject.SetActive(false);
            return;
        }

        float closestDist = float.MaxValue;
        for (int i = 0; i < buriedTreasures.Count; i++)
        {
            BuriedTreasureNode node = buriedTreasures[i];
            if (node.isCollected || !node.treasureObject) continue;

            float d = Vector3.Distance(playerTransform.position, node.treasureObject.position);
            if (d < closestDist) closestDist = d;
        }

        if (closestDist <= detectorMaxRange)
        {
            if (detectorSignalIcon) detectorSignalIcon.gameObject.SetActive(true);
            float proximityRatio = 1f - Mathf.Clamp01(closestDist / detectorMaxRange);

            if (detectorDistanceText)
                detectorDistanceText.text = $"{closestDist:F1}m";

            radarPingTimer -= Time.deltaTime;
            if (radarPingTimer <= 0f)
            {
                radarPingTimer = Mathf.Lerp(1.2f, 0.22f, proximityRatio);
                if (AudioHapticsManager.Instance)
                    AudioHapticsManager.Instance.PlayRadarPing(proximityRatio);
            }
        }
        else
        {
            if (detectorSignalIcon) detectorSignalIcon.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // 3. 2D HAND-DRAWN MAP & ROOM FOG-OF-WAR
    // =========================================================================

    public void ToggleMapOverlay()
    {
        if (!mapOverlayPanel) return;
        mapOverlayPanel.SetActive(!mapOverlayPanel.activeSelf);
    }

    private void CheckRoomDiscoveryAndMapPin()
    {
        if (!playerTransform) return;
        Vector3 pos = playerTransform.position;

        for (int i = 0; i < mineRooms.Count; i++)
        {
            MineRoomZone room = mineRooms[i];
            if (!room.isDiscovered && room.worldBounds.Contains(pos))
            {
                room.isDiscovered = true;
                if (room.mapRoomFogOverlay) room.mapRoomFogOverlay.SetActive(false);
                if (economyManager && !string.IsNullOrEmpty(room.roomTitle))
                {
                    economyManager.ShowTutorialCaption($"DISCOVERED: {room.roomTitle.ToUpper()}");
                }
            }
        }

        if (mapOverlayPanel && mapOverlayPanel.activeSelf && mapPlayerPin)
        {
            float normX = Mathf.InverseLerp(worldMinXZ.x, worldMaxXZ.x, pos.x);
            float normZ = Mathf.InverseLerp(worldMinXZ.y, worldMaxXZ.y, pos.z);
            float uiX = Mathf.Lerp(mapCanvasMin.x, mapCanvasMax.x, normX);
            float uiY = Mathf.Lerp(mapCanvasMin.y, mapCanvasMax.y, normZ);
            mapPlayerPin.anchoredPosition = new Vector2(uiX, uiY);
            mapPlayerPin.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
        }
    }

    // Save / Load helpers
    public List<string> GetDiscoveredRooms()
    {
        List<string> list = new List<string>();
        foreach (var r in mineRooms)
            if (r.isDiscovered) list.Add(r.roomId);
        return list;
    }

    public List<int> GetCollectedTreasures()
    {
        List<int> list = new List<int>();
        foreach (var t in buriedTreasures)
            if (t.isCollected) list.Add(t.treasureId);
        return list;
    }

    public void RestoreState(bool hasDetector, List<string> discoveredIds, List<int> collectedIds)
    {
        detectorUnlocked = hasDetector;

        if (discoveredIds != null)
        {
            foreach (var r in mineRooms)
            {
                if (discoveredIds.Contains(r.roomId))
                {
                    r.isDiscovered = true;
                    if (r.mapRoomFogOverlay) r.mapRoomFogOverlay.SetActive(false);
                }
            }
        }

        if (collectedIds != null)
        {
            foreach (var t in buriedTreasures)
            {
                if (collectedIds.Contains(t.treasureId))
                {
                    t.isCollected = true;
                    if (t.treasureObject) t.treasureObject.gameObject.SetActive(false);
                }
            }
        }
    }
}
