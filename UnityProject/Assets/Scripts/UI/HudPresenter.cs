using BreakToEscape.Core.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Play-screen readouts (wireframe screen 1): cash and income per second, tank gauge, day, dynamite
/// count on the hotbar, the selected hotbar slot, and the double-cash timer. Text is only rewritten
/// when a value changes, to avoid per-frame string allocations.
/// </summary>
public class HudPresenter : MonoBehaviour
{
    public TextMeshProUGUI cashText;
    public TextMeshProUGUI incomeText;
    public TextMeshProUGUI tankText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI dynamiteSlotText;
    public TextMeshProUGUI doubleCashText;
    public Image[] hotbarSlotImages = new Image[4];
    public Color slotNormal = new Color(0.12f, 0.12f, 0.12f, 0.92f);
    public Color slotSelected = new Color(0.95f, 0.78f, 0.1f, 0.95f);

    public CompanyStoreEconomyManager economyManager;
    public RubbleSuctionManager suction;
    public FirstPersonToolController tools;
    public MonetizationServicesManager monetization;

    private long shownCash = -1;
    private int shownIncome = -1, shownTank = -1, shownTankCap = -1, shownDay = -1, shownDynamite = -1, shownBoost = -1;

    void Start()
    {
        if (tools) tools.SlotChanged += HighlightSlot;
        HighlightSlot(FirstPersonToolController.HotbarSlot.Fists);
    }

    void OnDestroy()
    {
        if (tools) tools.SlotChanged -= HighlightSlot;
    }

    private void HighlightSlot(FirstPersonToolController.HotbarSlot slot)
    {
        for (int i = 0; i < hotbarSlotImages.Length; i++)
            if (hotbarSlotImages[i]) hotbarSlotImages[i].color = i == (int)slot ? slotSelected : slotNormal;
    }

    void Update()
    {
        if (!economyManager || economyManager.State == null) return;
        EconomyState s = economyManager.State;
        if (s.Cash != shownCash && cashText) { shownCash = s.Cash; cashText.text = "$ " + s.Cash.ToString("N0"); }
        int income = Mathf.RoundToInt(economyManager.IncomePerSecond * 10f);
        if (income != shownIncome && incomeText) { shownIncome = income; incomeText.text = "+$" + (income / 10f).ToString("0.0") + " / s"; }
        if (suction && suction.Tank != null && tankText)
        {
            int held = suction.Tank.Count, cap = suction.Tank.Capacity;
            if (held != shownTank || cap != shownTankCap) { shownTank = held; shownTankCap = cap; tankText.text = "TANK " + held + " / " + cap; }
        }
        if (economyManager.CurrentDay != shownDay && dayText) { shownDay = economyManager.CurrentDay; dayText.text = "DAY " + shownDay; }
        if (s.DynamiteStock != shownDynamite && dynamiteSlotText) { shownDynamite = s.DynamiteStock; dynamiteSlotText.text = "DYN x" + shownDynamite; }
        if (monetization && doubleCashText)
        {
            int seconds = monetization.DoubleCashActive ? Mathf.CeilToInt(monetization.DoubleCashRemaining) : 0;
            if (seconds != shownBoost)
            {
                shownBoost = seconds;
                doubleCashText.gameObject.SetActive(seconds > 0);
                if (seconds > 0) doubleCashText.text = "2X CASH " + (seconds / 60) + ":" + (seconds % 60).ToString("00");
            }
        }
    }
}
