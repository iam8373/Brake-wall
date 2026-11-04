using System;
using System.Collections.Generic;
using BreakToEscape.Core.Json;

namespace BreakToEscape.Core.Economy
{
    /// <summary>The seven numeric upgrade lines. Order matches the economy spreadsheet.</summary>
    public enum UpgradeLine
    {
        PunchPower = 0,
        PunchSpeed = 1,
        PunchPattern = 2,
        VacuumSuction = 3,
        TankCapacity = 4,
        CrusherSpeed = 5,
        OreSense = 6
    }

    public static class UpgradeLines
    {
        public const int Count = 7;
        public const int MaxLevel = 10;

        public static readonly string[] JsonKeys =
        {
            "punchPower", "punchSpeed", "punchPattern", "vacuumSuction", "tankCapacity", "crusherSpeed", "oreSense"
        };

        public static string JsonKey(UpgradeLine line) => JsonKeys[(int)line];
    }

    public sealed class UpgradeDefinition
    {
        public UpgradeLine line;
        public string displayName;
        public string description;
        public string unit;
        public double baseCost;
        public double costGrowth;
        /// <summary>Effect at levels 1..10 (index 0 = level 1).</summary>
        public double[] effects;
        /// <summary>Spreadsheet cumulative cost to reach levels 1..10 (index 0 = level 1 = 0).</summary>
        public double[] cumulativeCost;

        /// <summary>Effect value at a level (clamped to 1..10).</summary>
        public double EffectAt(int level) => effects[Clamp(level) - 1];

        /// <summary>
        /// Exact (unrounded) spreadsheet cost to go from <paramref name="currentLevel"/> to the next level:
        /// baseCost × growth^(currentLevel − 1). Returns 0 at max level.
        /// </summary>
        public double ExactCostToNext(int currentLevel)
        {
            if (currentLevel >= UpgradeLines.MaxLevel) return 0;
            return baseCost * Math.Pow(costGrowth, Clamp(currentLevel) - 1);
        }

        /// <summary>Cost charged in game: the exact cost rounded to whole dollars (away from zero).</summary>
        public long PriceToNext(int currentLevel) => (long)Math.Round(ExactCostToNext(currentLevel), MidpointRounding.AwayFromZero);

        private static int Clamp(int level) => level < 1 ? 1 : (level > UpgradeLines.MaxLevel ? UpgradeLines.MaxLevel : level);
    }

    public sealed class BandDefinition
    {
        public int id;
        public string code;
        public string name;
        public string look;
        public float hp;
        public float value;
        public int cubesOnPath;
        public float overheadSeconds;
        public string notes;
        public string colorHex;
    }

    public sealed class TreasureMaterialDefinition
    {
        public int id;
        public string key;
        public string name;
        public float hp;
        public float value;
        public string colorHex;
        public bool glows;
        public int[] bands;
    }

    public sealed class EquipmentDefinition
    {
        public string key;
        public string name;
        public long repairCost;
        public string partId;
        public string source;
        public float punchDamageMultiplier = 1f;
        public float punchSpeedMultiplier = 1f;
        public int depthLayers = 1;
    }

    public sealed class HeadlampLevel
    {
        public int level;
        public long cost;
        public float rangeMeters;
        public float intensity;
        public float spotAngle;
    }

    public sealed class TierSettings
    {
        public string key;
        public int rubbleCap;
        public int targetFps;
        public int physicsHz;
        public float renderScale;
        public float shadowDistance;
        public float farClip;
        public bool mainLightShadows;
    }

    public sealed class ThermalGuardSettings
    {
        public float checkIntervalSeconds = 5f;
        public float minAverageFpsAt60 = 48f;
        public float minAverageFpsAt30 = 26f;
        public float rubbleCapStepDown = 0.75f;
        public int minimumRubbleCap = 80;
    }

    public sealed class TierDetectionSettings
    {
        public int lowMaxRamMb = 4200;
        public int lowMaxCores = 4;
        public int lowMaxGraphicsMb = 1024;
        public int highMinRamMb = 7200;
        public int highMinCores = 8;
    }

    public sealed class MonetizationSettings
    {
        public float doubleCashDurationSeconds = 300f;
        public float doubleCashCooldownSeconds = 600f;
        public int freeDynamiteSticks = 2;
        public int freeDynamitePerSession = 3;
        public float adFreeGracePeriodSeconds = 300f;
        public string removeAdsProductId = "remove_ads";
        public float removeAdsDoubleCashCooldownSeconds = 1200f;
        public int removeAdsFreeDynamitePerSession = 2;
    }

    public sealed class PacingSettings
    {
        public double patternEfficiency;
        public double tripSeconds;
        public double collectionEfficiency;
        public double targetMinMinutes;
        public double targetMaxMinutes;
        public double healthyRatioLow;
        public double healthyRatioHigh;
        /// <summary>Planned levels per upgrade line at the start of B1..B6 plus the end column (7 entries).</summary>
        public int[][] plannedLevels = new int[UpgradeLines.Count][];
        public double[] expectedTimeInBandMinutes;
        public double[] expectedCashEarnedInBand;
        public double[] expectedCashNeeded;
        public double expectedTotalRunMinutes;
        public double expectedTotalCashEarned;
        public double expectedCashToMaxEverything;
    }

    /// <summary>
    /// All economy and tuning data, loaded from Resources/Data/economy.json.
    /// This is the single source of truth used by every game system.
    /// </summary>
    public sealed class EconomyData
    {
        public readonly UpgradeDefinition[] upgrades = new UpgradeDefinition[UpgradeLines.Count];
        public readonly List<BandDefinition> bands = new List<BandDefinition>();
        public readonly List<TreasureMaterialDefinition> treasureMaterials = new List<TreasureMaterialDefinition>();
        public readonly Dictionary<string, EquipmentDefinition> equipment = new Dictionary<string, EquipmentDefinition>();
        public readonly List<HeadlampLevel> headlampLevels = new List<HeadlampLevel>();
        public readonly Dictionary<string, TierSettings> tiers = new Dictionary<string, TierSettings>();
        public PacingSettings pacing = new PacingSettings();
        public MonetizationSettings monetization = new MonetizationSettings();
        public ThermalGuardSettings thermalGuard = new ThermalGuardSettings();
        public TierDetectionSettings tierDetection = new TierDetectionSettings();

        public float voxelSize = 0.5f;
        public Int3 worldSizeVoxels = new Int3(96, 32, 96);
        public long startingCash;
        public float secondsPerDay = 90f;
        public float sweepRefundRatio = 0.6f;
        public int treasureOneInVoxels = 600;
        public int nookTreasureClusterSize = 6;

        public float tapThresholdSeconds = 0.18f;
        public float fullChargeSeconds = 1.15f;
        public int maxPatternBonusLevels = 2;
        public float maxChargeDamageMultiplier = 2f;

        public float punchReachMeters = 2.4f;
        public float punchInputBufferSeconds = 0.12f;

        public int dynamiteStartingStock = 3;
        public float dynamiteFuseSeconds = 3f;
        public float dynamiteBlastRadiusMeters = 1.75f;
        public float dynamiteBlastDamage = 500f;

        public long dynamitePackCost = 400;
        public int dynamitePackSticks = 3;
        public long autoPunchCost = 1200;
        public long treasureDetectorCost = 650;
        public float treasureDetectorRangeMeters = 6.5f;

        public int[] vacuumVisualTierFromTankLevel = { 1, 4, 7 };

        public UpgradeDefinition Upgrade(UpgradeLine line) => upgrades[(int)line];

        public EquipmentDefinition Equipment(string key)
        {
            if (equipment.TryGetValue(key, out EquipmentDefinition def)) return def;
            throw new KeyNotFoundException("Unknown equipment '" + key + "' in economy data");
        }

        public TierSettings Tier(string key)
        {
            if (tiers.TryGetValue(key, out TierSettings t)) return t;
            throw new KeyNotFoundException("Unknown tier '" + key + "' in economy data");
        }

        public static EconomyData FromJson(string json)
        {
            var root = JsonRead.Obj(MiniJson.Parse(json), "economy.json");
            var data = new EconomyData();

            var upgradesNode = JsonRead.Obj(root, "upgrades");
            for (int i = 0; i < UpgradeLines.Count; i++)
            {
                var node = JsonRead.Obj(upgradesNode, UpgradeLines.JsonKeys[i]);
                var def = new UpgradeDefinition
                {
                    line = (UpgradeLine)i,
                    displayName = JsonRead.Str(node, "displayName", UpgradeLines.JsonKeys[i]),
                    description = JsonRead.Str(node, "description", ""),
                    unit = JsonRead.Str(node, "unit", ""),
                    baseCost = JsonRead.Num(node, "baseCost"),
                    costGrowth = JsonRead.Num(node, "costGrowth"),
                    effects = JsonRead.NumArray(node, "effects"),
                    cumulativeCost = JsonRead.NumArray(node, "cumulativeCost"),
                };
                if (def.effects.Length != UpgradeLines.MaxLevel)
                    throw new FormatException("Upgrade '" + UpgradeLines.JsonKeys[i] + "' must have 10 effect values");
                data.upgrades[i] = def;
            }

            foreach (object o in JsonRead.Arr(root, "bands"))
            {
                var b = JsonRead.Obj(o, "band");
                data.bands.Add(new BandDefinition
                {
                    id = JsonRead.Int(b, "id", 0),
                    code = JsonRead.Str(b, "code", ""),
                    name = JsonRead.Str(b, "name", ""),
                    look = JsonRead.Str(b, "look", ""),
                    hp = (float)JsonRead.Num(b, "hp"),
                    value = (float)JsonRead.Num(b, "value"),
                    cubesOnPath = JsonRead.Int(b, "cubesOnPath", 0),
                    overheadSeconds = (float)JsonRead.Num(b, "overheadSeconds", 0),
                    notes = JsonRead.Str(b, "notes", ""),
                    colorHex = JsonRead.Str(b, "colorHex", "#808080"),
                });
            }
            if (data.bands.Count != 6) throw new FormatException("economy.json must define exactly 6 bands");

            var pacingNode = JsonRead.Obj(root, "pacingModel");
            var p = data.pacing;
            p.patternEfficiency = JsonRead.Num(pacingNode, "patternEfficiency");
            p.tripSeconds = JsonRead.Num(pacingNode, "tripSeconds");
            p.collectionEfficiency = JsonRead.Num(pacingNode, "collectionEfficiency");
            p.targetMinMinutes = JsonRead.Num(pacingNode, "targetMinMinutes");
            p.targetMaxMinutes = JsonRead.Num(pacingNode, "targetMaxMinutes");
            p.healthyRatioLow = JsonRead.Num(pacingNode, "healthyRatioLow");
            p.healthyRatioHigh = JsonRead.Num(pacingNode, "healthyRatioHigh");
            var planned = JsonRead.Obj(pacingNode, "plannedLevelsAtBandStart");
            for (int i = 0; i < UpgradeLines.Count; i++)
            {
                double[] levels = JsonRead.NumArray(planned, UpgradeLines.JsonKeys[i]);
                p.plannedLevels[i] = Array.ConvertAll(levels, d => (int)Math.Round(d));
            }
            var expected = JsonRead.Obj(pacingNode, "spreadsheetResults");
            p.expectedTimeInBandMinutes = JsonRead.NumArray(expected, "timeInBandMinutes");
            p.expectedCashEarnedInBand = JsonRead.NumArray(expected, "cashEarnedInBand");
            p.expectedCashNeeded = JsonRead.NumArray(expected, "cashNeededForNextUpgrades");
            p.expectedTotalRunMinutes = JsonRead.Num(expected, "totalRunMinutes");
            p.expectedTotalCashEarned = JsonRead.Num(expected, "totalCashEarned");
            p.expectedCashToMaxEverything = JsonRead.Num(expected, "cashToMaxEverything");

            data.voxelSize = (float)JsonRead.Num(root, "voxelSize", 0.5);
            var world = JsonRead.OptArr(root, "worldSizeVoxels");
            if (world != null && world.Count == 3)
                data.worldSizeVoxels = new Int3((int)(double)world[0], (int)(double)world[1], (int)(double)world[2]);
            data.startingCash = JsonRead.Long(root, "startingCash", 0);
            data.secondsPerDay = (float)JsonRead.Num(root, "secondsPerDay", 90);
            data.sweepRefundRatio = (float)JsonRead.Num(root, "sweepRefundRatio", 0.6);

            var treasureList = JsonRead.OptArr(root, "treasureMaterials");
            if (treasureList != null)
            {
                foreach (object o in treasureList)
                {
                    var t = JsonRead.Obj(o, "treasure material");
                    var bandsArr = JsonRead.OptArr(t, "bands");
                    data.treasureMaterials.Add(new TreasureMaterialDefinition
                    {
                        id = JsonRead.Int(t, "id", 0),
                        key = JsonRead.Str(t, "key", ""),
                        name = JsonRead.Str(t, "name", ""),
                        hp = (float)JsonRead.Num(t, "hp"),
                        value = (float)JsonRead.Num(t, "value"),
                        colorHex = JsonRead.Str(t, "colorHex", "#FFFFFF"),
                        glows = JsonRead.Bool(t, "glows", false),
                        bands = bandsArr == null ? new int[0] : bandsArr.ConvertAll(x => (int)(double)x).ToArray(),
                    });
                }
            }

            var density = JsonRead.OptObj(root, "treasureDensity");
            data.treasureOneInVoxels = JsonRead.Int(density, "oneInVoxels", 600);
            data.nookTreasureClusterSize = JsonRead.Int(density, "nookClusterSize", 6);

            var charge = JsonRead.OptObj(root, "charge");
            data.tapThresholdSeconds = (float)JsonRead.Num(charge, "tapThresholdSeconds", 0.18);
            data.fullChargeSeconds = (float)JsonRead.Num(charge, "fullChargeSeconds", 1.15);
            data.maxPatternBonusLevels = JsonRead.Int(charge, "maxPatternBonusLevels", 2);
            data.maxChargeDamageMultiplier = (float)JsonRead.Num(charge, "maxDamageMultiplier", 2.0);

            var punch = JsonRead.OptObj(root, "punch");
            data.punchReachMeters = (float)JsonRead.Num(punch, "reachMeters", 2.4);
            data.punchInputBufferSeconds = (float)JsonRead.Num(punch, "inputBufferSeconds", 0.12);

            var dyn = JsonRead.OptObj(root, "dynamite");
            data.dynamiteStartingStock = JsonRead.Int(dyn, "startingStock", 3);
            data.dynamiteFuseSeconds = (float)JsonRead.Num(dyn, "fuseSeconds", 3.0);
            data.dynamiteBlastRadiusMeters = (float)JsonRead.Num(dyn, "blastRadiusMeters", 1.75);
            data.dynamiteBlastDamage = (float)JsonRead.Num(dyn, "blastDamage", 500);

            var store = JsonRead.OptObj(root, "storeItems");
            var pack = JsonRead.OptObj(store, "dynamitePack");
            data.dynamitePackCost = JsonRead.Long(pack, "cost", 400);
            data.dynamitePackSticks = JsonRead.Int(pack, "sticks", 3);
            data.autoPunchCost = JsonRead.Long(JsonRead.OptObj(store, "autoPunch"), "cost", 1200);
            var detector = JsonRead.OptObj(store, "treasureDetector");
            data.treasureDetectorCost = JsonRead.Long(detector, "cost", 650);
            data.treasureDetectorRangeMeters = (float)JsonRead.Num(detector, "rangeMeters", 6.5);
            var lamps = JsonRead.OptArr(store, "headlampLevels");
            if (lamps != null)
            {
                foreach (object o in lamps)
                {
                    var l = JsonRead.Obj(o, "headlamp level");
                    data.headlampLevels.Add(new HeadlampLevel
                    {
                        level = JsonRead.Int(l, "level", 1),
                        cost = JsonRead.Long(l, "cost", 0),
                        rangeMeters = (float)JsonRead.Num(l, "rangeMeters", 12),
                        intensity = (float)JsonRead.Num(l, "intensity", 1.6),
                        spotAngle = (float)JsonRead.Num(l, "spotAngle", 55),
                    });
                }
            }
            if (data.headlampLevels.Count == 0)
                data.headlampLevels.Add(new HeadlampLevel { level = 1, cost = 0, rangeMeters = 12, intensity = 1.6f, spotAngle = 55 });

            var equipmentNode = JsonRead.OptObj(root, "equipment");
            if (equipmentNode != null)
            {
                foreach (var kv in equipmentNode)
                {
                    if (!(kv.Value is Dictionary<string, object> e)) continue;
                    data.equipment[kv.Key] = new EquipmentDefinition
                    {
                        key = kv.Key,
                        name = JsonRead.Str(e, "name", kv.Key),
                        repairCost = JsonRead.Long(e, "repairCost", 0),
                        partId = JsonRead.Str(e, "partId", ""),
                        source = JsonRead.Str(e, "source", ""),
                        punchDamageMultiplier = (float)JsonRead.Num(e, "punchDamageMultiplier", 1.0),
                        punchSpeedMultiplier = (float)JsonRead.Num(e, "punchSpeedMultiplier", 1.0),
                        depthLayers = JsonRead.Int(e, "depthLayers", 1),
                    };
                }
            }

            var vacTiers = JsonRead.OptObj(root, "vacuumVisualTiers");
            var fromLevels = JsonRead.OptArr(vacTiers, "fromTankLevel");
            if (fromLevels != null && fromLevels.Count == 3)
                data.vacuumVisualTierFromTankLevel = fromLevels.ConvertAll(x => (int)(double)x).ToArray();

            var mon = JsonRead.OptObj(root, "monetization");
            var m = data.monetization;
            m.doubleCashDurationSeconds = (float)JsonRead.Num(mon, "doubleCashDurationSeconds", 300);
            m.doubleCashCooldownSeconds = (float)JsonRead.Num(mon, "doubleCashCooldownSeconds", 600);
            m.freeDynamiteSticks = JsonRead.Int(mon, "freeDynamiteSticks", 2);
            m.freeDynamitePerSession = JsonRead.Int(mon, "freeDynamitePerSession", 3);
            m.adFreeGracePeriodSeconds = (float)JsonRead.Num(mon, "adFreeGracePeriodSeconds", 300);
            m.removeAdsProductId = JsonRead.Str(mon, "removeAdsProductId", "remove_ads");
            m.removeAdsDoubleCashCooldownSeconds = (float)JsonRead.Num(mon, "removeAdsDoubleCashCooldownSeconds", 1200);
            m.removeAdsFreeDynamitePerSession = JsonRead.Int(mon, "removeAdsFreeDynamitePerSession", 2);

            var tiersNode = JsonRead.OptObj(root, "tiers");
            foreach (string key in new[] { "low", "mid", "high" })
            {
                var t = JsonRead.OptObj(tiersNode, key);
                if (t == null) throw new FormatException("economy.json is missing tier '" + key + "'");
                data.tiers[key] = new TierSettings
                {
                    key = key,
                    rubbleCap = JsonRead.Int(t, "rubbleCap", 150),
                    targetFps = JsonRead.Int(t, "targetFps", 30),
                    physicsHz = JsonRead.Int(t, "physicsHz", 50),
                    renderScale = (float)JsonRead.Num(t, "renderScale", 1.0),
                    shadowDistance = (float)JsonRead.Num(t, "shadowDistance", 0),
                    farClip = (float)JsonRead.Num(t, "farClip", 30),
                    mainLightShadows = JsonRead.Bool(t, "mainLightShadows", false),
                };
            }
            var guard = JsonRead.OptObj(tiersNode, "thermalGuard");
            data.thermalGuard.checkIntervalSeconds = (float)JsonRead.Num(guard, "checkIntervalSeconds", 5);
            data.thermalGuard.minAverageFpsAt60 = (float)JsonRead.Num(guard, "minAverageFpsAt60", 48);
            data.thermalGuard.minAverageFpsAt30 = (float)JsonRead.Num(guard, "minAverageFpsAt30", 26);
            data.thermalGuard.rubbleCapStepDown = (float)JsonRead.Num(guard, "rubbleCapStepDown", 0.75);
            data.thermalGuard.minimumRubbleCap = JsonRead.Int(guard, "minimumRubbleCap", 80);
            var detect = JsonRead.OptObj(tiersNode, "detection");
            data.tierDetection.lowMaxRamMb = JsonRead.Int(detect, "lowMaxRamMb", 4200);
            data.tierDetection.lowMaxCores = JsonRead.Int(detect, "lowMaxCores", 4);
            data.tierDetection.lowMaxGraphicsMb = JsonRead.Int(detect, "lowMaxGraphicsMb", 1024);
            data.tierDetection.highMinRamMb = JsonRead.Int(detect, "highMinRamMb", 7200);
            data.tierDetection.highMinCores = JsonRead.Int(detect, "highMinCores", 8);

            return data;
        }

        /// <summary>Parses "#RRGGBB" into 0..1 floats. Returns grey on bad input.</summary>
        public static void ParseHexColor(string hex, out float r, out float g, out float b)
        {
            r = g = b = 0.5f;
            if (string.IsNullOrEmpty(hex)) return;
            string h = hex.TrimStart('#');
            if (h.Length != 6) return;
            try
            {
                r = Convert.ToInt32(h.Substring(0, 2), 16) / 255f;
                g = Convert.ToInt32(h.Substring(2, 2), 16) / 255f;
                b = Convert.ToInt32(h.Substring(4, 2), 16) / 255f;
            }
            catch (FormatException)
            {
                r = g = b = 0.5f;
            }
        }
    }
}
