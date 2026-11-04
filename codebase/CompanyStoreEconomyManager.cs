using System.Collections;
using UnityEngine;
using TMPro;

public class CompanyStoreEconomyManager : MonoBehaviour
{
    public static CompanyStoreEconomyManager Instance { get; private set; }

    [Header("Core References")]
    public FirstPersonToolController toolController;
    public VoxelWireframePreview wireframePreview;
    public Crusher07Controller crusher07;
    public RubbleSuctionManager suctionManager;

    [Header("HUD Text Elements (Matches Wireframe Screen 1)")]
    public TextMeshProUGUI cashText;
    public TextMeshProUGUI incomePerSecText;
    public TextMeshProUGUI tankCounterText;
    public TextMeshProUGUI dayCounterText;
    public TextMeshProUGUI tutorialBannerText;
    public RectTransform tutorialBannerRect;

    [Header("Player Balance & Progression")]
    public int currentCash = 0;
    public int currentDay = 1;
    public float secondsPerDay = 90f;

    [Header("7 Numeric Upgrades (Levels 1-10, Exact Match to Break_to_Escape_Economy.xlsx)")]
    [Range(1, 10)] public int punchPowerLevel = 1;
    [Range(1, 10)] public int punchSpeedLevel = 1;
    [Range(1, 10)] public int punchPatternLevel = 1;
    [Range(1, 10)] public int vacuumSuctionLevel = 1;
    [Range(1, 10)] public int tankCapacityLevel = 1;
    [Range(1, 10)] public int crusherSpeedLevel = 1;
    [Range(1, 10)] public int oreSenseLevel = 1;

    [Header("Restored Equipment (Story Unlocks - GDD Section 8)")]
    public bool workGauntletsRestored = false;
    public bool pneumaticHammerRestored = false;
    public bool rockDrillRestored = false;
    public bool conveyorRestored = false;
    public bool autoPunchUnlocked = false;

    // Exact arrays from Break_to_Escape_Economy.xlsx ('Upgrades' sheet)
    private static readonly float[] PunchPowerTable   = { 10f, 14.5f, 21.025f, 30.48625f, 44.205062f, 64.097341f, 92.941144f, 134.764659f, 195.408755f, 283.342695f };
    private static readonly float[] PunchSpeedTable   = { 1.5f, 1.75f, 2.0f, 2.25f, 2.5f, 2.75f, 3.0f, 3.25f, 3.5f, 3.75f };
    private static readonly int[]   PunchPatternTable = { 5, 9, 13, 21, 29, 37, 49, 61, 77, 97 };
    private static readonly float[] VacuumSuctionTable= { 6f, 7.8f, 10.14f, 13.182f, 17.1366f, 22.27758f, 28.960854f, 37.64911f, 48.943843f, 63.626996f };
    private static readonly int[]   TankCapacityTable = { 60, 90, 130, 190, 270, 380, 530, 740, 1030, 1450 };
    private static readonly float[] CrusherSpeedTable = { 8f, 10.4f, 13.52f, 17.576f, 22.8488f, 29.70344f, 38.614472f, 50.198814f, 65.258458f, 84.835995f };
    private static readonly float[] OreSenseTable     = { 1.0f, 1.08f, 1.16f, 1.24f, 1.32f, 1.40f, 1.48f, 1.56f, 1.64f, 1.72f };

    // Legacy alias for SaveManager compatibility
    public int vacuumTierLevel
    {
        get => tankCapacityLevel;
        set => tankCapacityLevel = Mathf.Clamp(value, 1, 10);
    }

    private float dayTimer = 0f;
    private float autoPunchTimer = 0f;
    private int cashEarnedThisSecond = 0;
    private float incomeSampleTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Crusher07Controller.OnCashEarned += AddCash;
    }

    void OnDisable()
    {
        Crusher07Controller.OnCashEarned -= AddCash;
    }

    void Start()
    {
        ApplyAllStatsToSystems();
        UpdateHUD();
    }

    void Update()
    {
        dayTimer += Time.deltaTime;
        if (dayTimer >= secondsPerDay)
        {
            dayTimer = 0f;
            currentDay++;
            if (dayCounterText) dayCounterText.text = $"DAY {currentDay}";
        }

        incomeSampleTimer += Time.deltaTime;
        if (incomeSampleTimer >= 1f)
        {
            if (incomePerSecText) incomePerSecText.text = $"+${cashEarnedThisSecond}/s";
            cashEarnedThisSecond = 0;
            incomeSampleTimer = 0f;
        }

        if (tankCounterText && toolController)
        {
            tankCounterText.text = $"TANK {toolController.currentTankCount}/{toolController.maxTankCapacity}";
        }

        if (autoPunchUnlocked && toolController)
        {
            float punchesPerSec = GetCurrentPunchesPerSecond();
            autoPunchTimer += Time.deltaTime;
            if (autoPunchTimer >= (1f / Mathf.Max(0.5f, punchesPerSec)))
            {
                autoPunchTimer = 0f;
                toolController.OnPunchButtonDown();
                toolController.OnPunchButtonUp();
            }
        }
    }

    // Exact step cost formula from Break_to_Escape_Economy.xlsx: baseCost * (2.2 ^ (currentLevel - 1))
    public int GetNextLevelCost(int baseCost, int currentLevel)
    {
        if (currentLevel >= 10) return 0;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(2.2f, currentLevel - 1));
    }

    public float GetCurrentPunchDamage()
    {
        float dmg = PunchPowerTable[Mathf.Clamp(punchPowerLevel - 1, 0, 9)];
        if (workGauntletsRestored) dmg *= 1.15f;
        return dmg;
    }

    public float GetCurrentPunchesPerSecond()
    {
        float spd = PunchSpeedTable[Mathf.Clamp(punchSpeedLevel - 1, 0, 9)];
        if (pneumaticHammerRestored) spd *= 1.25f;
        return spd;
    }

    public int GetCurrentPatternCubes() => PunchPatternTable[Mathf.Clamp(punchPatternLevel - 1, 0, 9)];
    public float GetCurrentVacuumCubesPerSec() => VacuumSuctionTable[Mathf.Clamp(vacuumSuctionLevel - 1, 0, 9)];
    public int GetCurrentTankCapacity() => TankCapacityTable[Mathf.Clamp(tankCapacityLevel - 1, 0, 9)];
    public float GetCurrentCrusherSpeed() => CrusherSpeedTable[Mathf.Clamp(crusherSpeedLevel - 1, 0, 9)];
    public float GetCurrentOreSenseMultiplier() => OreSenseTable[Mathf.Clamp(oreSenseLevel - 1, 0, 9)];

    public void ApplyAllStatsToSystems()
    {
        if (toolController)
        {
            toolController.basePunchPower = GetCurrentPunchDamage();
            toolController.maxTankCapacity = GetCurrentTankCapacity();

            float spd = GetCurrentPunchesPerSecond();
            toolController.jabThrustTime = Mathf.Clamp(0.08f / spd, 0.025f, 0.07f);
            toolController.jabReturnTime = Mathf.Clamp(0.16f / spd, 0.05f, 0.14f);

            // Visual vacuum tier (1-3: Dust-Buster, 4-6: Ore Hoover, 7-10: Suction Cannon)
            int vacMeshTier = (tankCapacityLevel >= 7) ? 2 : (tankCapacityLevel >= 4 ? 1 : 0);
            toolController.SetVacuumUpgradeTier(vacMeshTier);
        }

        if (wireframePreview)
        {
            int patCubes = GetCurrentPatternCubes();
            wireframePreview.basePatternRadius = (patCubes >= 21) ? 3 : (patCubes >= 9 ? 2 : 1);
        }

        if (crusher07)
        {
            crusher07.crushesPerSecond = GetCurrentCrusherSpeed();
            crusher07.autoConveyorUnlocked = conveyorRestored || crusherSpeedLevel >= 5;
        }

        if (suctionManager)
        {
            suctionManager.suctionCurveDuration = Mathf.Clamp(1.2f / GetCurrentVacuumCubesPerSec(), 0.06f, 0.25f);
        }
    }

    public void AddCash(int amount)
    {
        currentCash += amount;
        if (amount > 0)
        {
            cashEarnedThisSecond += amount;
            if (UIScreenAndAccessibilityManager.Instance)
                UIScreenAndAccessibilityManager.Instance.NotifyFirstCubeCrushed();
        }
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (cashText) cashText.text = $"$ {currentCash:N0}";
        if (dayCounterText) dayCounterText.text = $"DAY {currentDay}";
    }

    // =========================================================================
    // 7 COMPANY STORE UPGRADE BUTTONS (Matches Wireframe Screen 2 & Economy Sheet)
    // =========================================================================

    public void BuyPunchPowerUpgrade()
    {
        int cost = GetNextLevelCost(150, punchPowerLevel);
        if (punchPowerLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        punchPowerLevel++;
        if (punchPowerLevel >= 3) { workGauntletsRestored = true; if (toolController) toolController.SetGloveUpgradeTier(1); }
        OnAnyUpgradeBought();
    }

    public void BuyPunchSpeedUpgrade()
    {
        int cost = GetNextLevelCost(200, punchSpeedLevel);
        if (punchSpeedLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        punchSpeedLevel++;
        OnAnyUpgradeBought();
    }

    public void BuyPunchPatternUpgrade()
    {
        int cost = GetNextLevelCost(250, punchPatternLevel);
        if (punchPatternLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        punchPatternLevel++;
        OnAnyUpgradeBought();
    }

    public void BuyVacuumSuctionUpgrade()
    {
        int cost = GetNextLevelCost(175, vacuumSuctionLevel);
        if (vacuumSuctionLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        vacuumSuctionLevel++;
        OnAnyUpgradeBought();
    }

    public void BuyTankCapacityUpgrade()
    {
        int cost = GetNextLevelCost(125, tankCapacityLevel);
        if (tankCapacityLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        tankCapacityLevel++;
        OnAnyUpgradeBought();
    }

    // Alias for SceneAutoBuilderEditor button binding
    public void BuyVacuumUpgrade() => BuyTankCapacityUpgrade();

    public void BuyCrusherSpeedAndConveyor()
    {
        int cost = GetNextLevelCost(225, crusherSpeedLevel);
        if (crusherSpeedLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        crusherSpeedLevel++;
        OnAnyUpgradeBought();
    }

    public void BuyOreSenseUpgrade()
    {
        int cost = GetNextLevelCost(375, oreSenseLevel);
        if (oreSenseLevel >= 10 || currentCash < cost) return;
        currentCash -= cost;
        oreSenseLevel++;
        OnAnyUpgradeBought();
    }

    public void BuyDynamitePack()
    {
        int cost = 400;
        if (currentCash < cost) return;
        currentCash -= cost;
        if (toolController) toolController.dynamiteStock += 3;
        UpdateHUD();
    }

    public void BuyAutoPunchUpgrade()
    {
        int cost = 1200;
        if (currentCash < cost || autoPunchUnlocked) return;
        currentCash -= cost;
        autoPunchUnlocked = true;
        UpdateHUD();
    }

    private void OnAnyUpgradeBought()
    {
        ApplyAllStatsToSystems();
        UpdateHUD();
        if (UIScreenAndAccessibilityManager.Instance)
            UIScreenAndAccessibilityManager.Instance.NotifyFirstUpgradePurchased();
    }

    public void ShowTutorialCaption(string message)
    {
        if (!tutorialBannerText || !tutorialBannerRect) return;
        StopAllCoroutines();
        StartCoroutine(AnimateBannerRoutine(message));
    }

    private IEnumerator AnimateBannerRoutine(string message)
    {
        tutorialBannerText.text = message;
        tutorialBannerRect.gameObject.SetActive(true);

        Vector2 hiddenPos = new Vector2(0f, 120f);
        Vector2 shownPos = new Vector2(0f, -28f);

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            tutorialBannerRect.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, Mathf.SmoothStep(0f, 1f, t / 0.25f));
            yield return null;
        }

        yield return new WaitForSeconds(3.4f);

        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            tutorialBannerRect.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, Mathf.SmoothStep(0f, 1f, t / 0.25f));
            yield return null;
        }

        tutorialBannerRect.gameObject.SetActive(false);
    }
}
