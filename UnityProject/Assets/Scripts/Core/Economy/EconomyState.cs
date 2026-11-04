using System;

namespace BreakToEscape.Core.Economy
{
    public enum PurchaseResult
    {
        Success,
        NotEnoughCash,
        AlreadyMaxed,
        AlreadyOwned,
        MissingPart,
        Unavailable
    }

    /// <summary>
    /// The player's economic state: cash, upgrade levels, owned store items and restored
    /// equipment. All prices and effects are read from <see cref="EconomyData"/>; nothing
    /// is hard-coded here. Unity-independent so it can be unit-tested.
    /// </summary>
    public sealed class EconomyState
    {
        public const string WorkGauntlets = "workGauntlets";
        public const string PneumaticHammer = "pneumaticHammer";
        public const string Conveyor = "conveyor";
        public const string RockDrill = "rockDrill";
        public static readonly string[] EquipmentKeys = { WorkGauntlets, PneumaticHammer, Conveyor, RockDrill };

        private readonly EconomyData data;
        private readonly int[] levels = new int[UpgradeLines.Count];

        public long Cash { get; private set; }
        public long LifetimeCashEarned { get; private set; }
        public bool AutoPunchUnlocked { get; private set; }
        public bool TreasureDetectorUnlocked { get; private set; }
        public int HeadlampLevel { get; private set; } = 1;
        public int DynamiteStock { get; private set; }

        /// <summary>Equipment flags. Indexed by <see cref="EquipmentKeys"/>.</summary>
        private readonly bool[] restored = new bool[4];
        /// <summary>Repair parts the player has found. Indexed by <see cref="EquipmentKeys"/>.</summary>
        private readonly bool[] partFound = new bool[4];

        public event Action Changed;

        public EconomyState(EconomyData data)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            for (int i = 0; i < levels.Length; i++) levels[i] = 1;
            Cash = data.startingCash;
            DynamiteStock = data.dynamiteStartingStock;
        }

        public EconomyData Data => data;

        // ------------------------------------------------------------------ levels

        public int Level(UpgradeLine line) => levels[(int)line];

        public void SetLevel(UpgradeLine line, int level)
        {
            levels[(int)line] = Math.Max(1, Math.Min(UpgradeLines.MaxLevel, level));
            Changed?.Invoke();
        }

        public bool IsMaxed(UpgradeLine line) => Level(line) >= UpgradeLines.MaxLevel;

        public long PriceToNext(UpgradeLine line) => data.Upgrade(line).PriceToNext(Level(line));

        public bool CanAfford(long price) => price <= Cash;

        public PurchaseResult TryBuyUpgrade(UpgradeLine line)
        {
            if (IsMaxed(line)) return PurchaseResult.AlreadyMaxed;
            long price = PriceToNext(line);
            if (!CanAfford(price)) return PurchaseResult.NotEnoughCash;
            Cash -= price;
            levels[(int)line]++;
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        // ------------------------------------------------------------------ cash

        /// <summary>Adds earned cash (crusher payouts, sweep refunds, treasure). Negative values are ignored.</summary>
        public void Earn(long amount)
        {
            if (amount <= 0) return;
            Cash += amount;
            LifetimeCashEarned += amount;
            Changed?.Invoke();
        }

        /// <summary>Restores cash exactly (used by the save system only).</summary>
        public void RestoreCash(long cash, long lifetimeEarned)
        {
            Cash = Math.Max(0, cash);
            LifetimeCashEarned = Math.Max(Cash, lifetimeEarned);
            Changed?.Invoke();
        }

        private bool TrySpend(long price)
        {
            if (price < 0 || price > Cash) return false;
            Cash -= price;
            return true;
        }

        // ------------------------------------------------------------------ derived stats

        public float PunchDamage
        {
            get
            {
                float dmg = (float)data.Upgrade(UpgradeLine.PunchPower).EffectAt(Level(UpgradeLine.PunchPower));
                if (IsRestored(WorkGauntlets)) dmg *= data.Equipment(WorkGauntlets).punchDamageMultiplier;
                return dmg;
            }
        }

        public float PunchesPerSecond
        {
            get
            {
                float rate = (float)data.Upgrade(UpgradeLine.PunchSpeed).EffectAt(Level(UpgradeLine.PunchSpeed));
                if (IsRestored(PneumaticHammer)) rate *= data.Equipment(PneumaticHammer).punchSpeedMultiplier;
                return rate;
            }
        }

        public int PatternLevel => Level(UpgradeLine.PunchPattern);
        public int PatternCubes => (int)Math.Round(data.Upgrade(UpgradeLine.PunchPattern).EffectAt(PatternLevel));
        public float VacuumCubesPerSecond => (float)data.Upgrade(UpgradeLine.VacuumSuction).EffectAt(Level(UpgradeLine.VacuumSuction));
        public int TankCapacity => (int)Math.Round(data.Upgrade(UpgradeLine.TankCapacity).EffectAt(Level(UpgradeLine.TankCapacity)));
        public float CrusherCubesPerSecond => (float)data.Upgrade(UpgradeLine.CrusherSpeed).EffectAt(Level(UpgradeLine.CrusherSpeed));
        public float OreSenseMultiplier => (float)data.Upgrade(UpgradeLine.OreSense).EffectAt(Level(UpgradeLine.OreSense));

        /// <summary>Voxel layers a punch reaches: 1 normally, or the Rock Drill depth once restored.</summary>
        public int PunchDepthLayers => IsRestored(RockDrill) ? Math.Max(1, data.Equipment(RockDrill).depthLayers) : 1;

        public bool ConveyorActive => IsRestored(Conveyor);

        /// <summary>Vacuum look index: 0 Dust-buster, 1 Ore Hoover, 2 Suction Cannon.</summary>
        public int VacuumVisualTier
        {
            get
            {
                int tank = Level(UpgradeLine.TankCapacity);
                int[] from = data.vacuumVisualTierFromTankLevel;
                if (tank >= from[2]) return 2;
                if (tank >= from[1]) return 1;
                return 0;
            }
        }

        /// <summary>Cash value of a broken cube of a given base value, with Ore Sense applied, minimum $1.</summary>
        public int CubeCashValue(float baseValue) => Math.Max(1, (int)Math.Round(baseValue * OreSenseMultiplier, MidpointRounding.AwayFromZero));

        // ------------------------------------------------------------------ store items

        public PurchaseResult TryBuyDynamitePack()
        {
            if (!TrySpend(data.dynamitePackCost)) return PurchaseResult.NotEnoughCash;
            DynamiteStock += data.dynamitePackSticks;
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        public PurchaseResult TryBuyAutoPunch()
        {
            if (AutoPunchUnlocked) return PurchaseResult.AlreadyOwned;
            if (!TrySpend(data.autoPunchCost)) return PurchaseResult.NotEnoughCash;
            AutoPunchUnlocked = true;
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        public PurchaseResult TryBuyTreasureDetector()
        {
            if (TreasureDetectorUnlocked) return PurchaseResult.AlreadyOwned;
            if (!TrySpend(data.treasureDetectorCost)) return PurchaseResult.NotEnoughCash;
            TreasureDetectorUnlocked = true;
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        public HeadlampLevel CurrentHeadlamp => data.headlampLevels[Math.Max(0, Math.Min(data.headlampLevels.Count - 1, HeadlampLevel - 1))];

        public bool HeadlampMaxed => HeadlampLevel >= data.headlampLevels.Count;

        public long HeadlampPriceToNext => HeadlampMaxed ? 0 : data.headlampLevels[HeadlampLevel].cost;

        public PurchaseResult TryBuyHeadlampLevel()
        {
            if (HeadlampMaxed) return PurchaseResult.AlreadyMaxed;
            if (!TrySpend(HeadlampPriceToNext)) return PurchaseResult.NotEnoughCash;
            HeadlampLevel++;
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        public void AddDynamite(int sticks)
        {
            if (sticks <= 0) return;
            DynamiteStock += sticks;
            Changed?.Invoke();
        }

        public bool TryUseDynamite()
        {
            if (DynamiteStock <= 0) return false;
            DynamiteStock--;
            Changed?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ equipment

        private static int EquipmentIndex(string key)
        {
            int i = Array.IndexOf(EquipmentKeys, key);
            if (i < 0) throw new ArgumentException("Unknown equipment key '" + key + "'");
            return i;
        }

        public bool IsRestored(string equipmentKey) => restored[EquipmentIndex(equipmentKey)];
        public bool HasPart(string equipmentKey) => partFound[EquipmentIndex(equipmentKey)];

        /// <summary>Records that the repair part for a machine was found in the mine.</summary>
        public void FindPart(string equipmentKey)
        {
            partFound[EquipmentIndex(equipmentKey)] = true;
            Changed?.Invoke();
        }

        /// <summary>
        /// Story unlock for the Work Gauntlets: they are taken from the Old Gallery locker,
        /// not bought. Any other machine must go through <see cref="TryRepair"/>.
        /// </summary>
        public bool RestoreFromLocker(string equipmentKey)
        {
            if (equipmentKey != WorkGauntlets) return false;
            int i = EquipmentIndex(equipmentKey);
            if (restored[i]) return false;
            restored[i] = true;
            partFound[i] = true;
            Changed?.Invoke();
            return true;
        }

        public long RepairPrice(string equipmentKey) => data.Equipment(equipmentKey).repairCost;

        /// <summary>Company Store repair: requires the found part and the repair cost.</summary>
        public PurchaseResult TryRepair(string equipmentKey)
        {
            int i = EquipmentIndex(equipmentKey);
            if (equipmentKey == WorkGauntlets) return PurchaseResult.Unavailable;
            if (restored[i]) return PurchaseResult.AlreadyOwned;
            if (!partFound[i]) return PurchaseResult.MissingPart;
            if (!TrySpend(RepairPrice(equipmentKey))) return PurchaseResult.NotEnoughCash;
            restored[i] = true;
            Changed?.Invoke();
            return PurchaseResult.Success;
        }

        // ------------------------------------------------------------------ save support

        public void RestoreItems(bool autoPunch, bool detector, int headlampLevel, int dynamiteStock)
        {
            AutoPunchUnlocked = autoPunch;
            TreasureDetectorUnlocked = detector;
            HeadlampLevel = Math.Max(1, Math.Min(data.headlampLevels.Count, headlampLevel));
            DynamiteStock = Math.Max(0, dynamiteStock);
            Changed?.Invoke();
        }

        public void RestoreEquipment(string equipmentKey, bool isRestored, bool hasPart)
        {
            int i = EquipmentIndex(equipmentKey);
            restored[i] = isRestored;
            partFound[i] = hasPart || isRestored;
            Changed?.Invoke();
        }
    }
}
