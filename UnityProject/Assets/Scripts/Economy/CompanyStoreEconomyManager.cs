using System;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Limits;
using BreakToEscape.Core.Services;
using BreakToEscape.Core.Story;
using UnityEngine;

/// <summary>
/// Unity side of the economy: wraps <see cref="EconomyState"/> (cash, levels, items, equipment),
/// runs the day counter and the income-per-second readout, receives crusher payouts (with the
/// double-cash bonus), and exposes the Company Store actions the UI buttons call. Every price and
/// effect comes from economy.json through <see cref="EconomyState"/>; nothing is duplicated here.
/// </summary>
[DefaultExecutionOrder(-200)]
public class CompanyStoreEconomyManager : MonoBehaviour
{
    public static CompanyStoreEconomyManager Instance { get; private set; }

    [Header("Systems that read economy stats")]
    public FirstPersonToolController toolController;
    public PunchAction punch;
    public HeadlampController headlamp;
    public Crusher07Controller crusher07;
    public RubbleSuctionManager suctionManager;

    public EconomyState State { get; private set; }
    public EconomyData Data { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public float DayTimer { get; private set; }
    public float IncomePerSecond { get; private set; }
    public RateMeter PunchRate { get; } = new RateMeter();

    /// <summary>Raised when a purchase or repair attempt finishes (UI feedback, tutorial, analytics).</summary>
    public event Action<string, PurchaseResult> PurchaseAttempted;
    public event Action<int> DayStarted;
    public event Action StatsApplied;

    private MonetizationRules monetization;
    private long incomeWindow;
    private float incomeTimer;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(EconomyData data, EconomyState state, MonetizationRules monetizationRules)
    {
        Data = data;
        State = state;
        monetization = monetizationRules;
        Crusher07Controller.CubeCrushed += OnCubeCrushed;
        if (punch) punch.Swung += () => PunchRate.Record(Time.time);
    }

    void OnDestroy()
    {
        Crusher07Controller.CubeCrushed -= OnCubeCrushed;
    }

    public void RestoreDay(int day, float timer)
    {
        CurrentDay = Mathf.Max(1, day);
        DayTimer = Mathf.Max(0f, timer);
    }

    void Update()
    {
        if (State == null) return;
        DayTimer += Time.deltaTime;
        if (DayTimer >= Data.secondsPerDay)
        {
            DayTimer = 0f;
            CurrentDay++;
            DayStarted?.Invoke(CurrentDay);
        }
        incomeTimer += Time.deltaTime;
        if (incomeTimer >= 1f)
        {
            IncomePerSecond = incomeWindow / incomeTimer;
            incomeWindow = 0;
            incomeTimer = 0f;
        }
        PunchRate.Tick(Time.time);
    }

    private void OnCubeCrushed(int basePayout, Vector3 position)
    {
        long bonus = monetization != null ? monetization.BonusFor(basePayout) : 0;
        Earn(basePayout + bonus);
    }

    /// <summary>All cash income goes through here (crusher, sweep refunds, treasure).</summary>
    public void Earn(long amount)
    {
        if (amount <= 0 || State == null) return;
        State.Earn(amount);
        incomeWindow += amount;
    }

    /// <summary>Pushes the current stats to every system that uses them. Call after loading and after every purchase.</summary>
    public void ApplyAllStatsToSystems()
    {
        if (State == null) return;
        if (punch) punch.ApplyStats();
        if (suctionManager) suctionManager.ApplyStats();
        if (toolController)
        {
            toolController.SetVacuumUpgradeTier(State.VacuumVisualTier);
            toolController.SetGloveUpgradeTier(State.IsRestored(EconomyState.WorkGauntlets) ? 1 : 0);
        }
        if (headlamp) headlamp.ApplyLevel(State.CurrentHeadlamp);
        StatsApplied?.Invoke();
    }

    // ------------------------------------------------------------------ Company Store actions (UI buttons)

    public void BuyUpgrade(int line)
    {
        if (State == null) return;
        var upgrade = (UpgradeLine)Mathf.Clamp(line, 0, UpgradeLines.Count - 1);
        long price = State.PriceToNext(upgrade);
        PurchaseResult result = State.TryBuyUpgrade(upgrade);
        Finish(UpgradeLines.JsonKey(upgrade), result);
        if (result == PurchaseResult.Success && GameBootstrap.Instance)
            GameBootstrap.Instance.Analytics(AnalyticsEvents.UpgradeBought, "line", UpgradeLines.JsonKey(upgrade), "level", State.Level(upgrade), "price", price);
    }

    public void BuyPunchPowerUpgrade() => BuyUpgrade((int)UpgradeLine.PunchPower);
    public void BuyPunchSpeedUpgrade() => BuyUpgrade((int)UpgradeLine.PunchSpeed);
    public void BuyPunchPatternUpgrade() => BuyUpgrade((int)UpgradeLine.PunchPattern);
    public void BuyVacuumSuctionUpgrade() => BuyUpgrade((int)UpgradeLine.VacuumSuction);
    public void BuyTankCapacityUpgrade() => BuyUpgrade((int)UpgradeLine.TankCapacity);
    public void BuyCrusherSpeedUpgrade() => BuyUpgrade((int)UpgradeLine.CrusherSpeed);
    public void BuyOreSenseUpgrade() => BuyUpgrade((int)UpgradeLine.OreSense);

    public void BuyDynamitePack() { if (State != null) Finish("dynamitePack", State.TryBuyDynamitePack()); }
    public void BuyAutoPunchUpgrade() { if (State != null) Finish("autoPunch", State.TryBuyAutoPunch()); }
    public void BuyTreasureDetector() { if (State != null) Finish("treasureDetector", State.TryBuyTreasureDetector()); }
    public void BuyHeadlampLevel() { if (State != null) Finish("headlamp", State.TryBuyHeadlampLevel()); }

    public void RepairEquipment(string equipmentKey)
    {
        if (State == null) return;
        PurchaseResult result = State.TryRepair(equipmentKey);
        Finish(equipmentKey, result);
        if (result == PurchaseResult.Success && GameBootstrap.Instance)
            GameBootstrap.Instance.Analytics(AnalyticsEvents.EquipmentRestored, "equipment", equipmentKey);
    }

    public void RepairPneumaticHammer() => RepairEquipment(EconomyState.PneumaticHammer);
    public void RepairConveyor() => RepairEquipment(EconomyState.Conveyor);
    public void RepairRockDrill() => RepairEquipment(EconomyState.RockDrill);

    private void Finish(string item, PurchaseResult result)
    {
        if (result == PurchaseResult.Success) ApplyAllStatsToSystems();
        Dispatch dispatch = Dispatch.Instance;
        if (dispatch)
        {
            if (result == PurchaseResult.NotEnoughCash) dispatch.Say("not_enough_cash");
            else if (result == PurchaseResult.MissingPart) dispatch.Say("missing_part");
            else if (result == PurchaseResult.Success && Array.IndexOf(EconomyState.EquipmentKeys, item) >= 0) dispatch.Say("repair_done");
        }
        PurchaseAttempted?.Invoke(item, result);
    }
}
