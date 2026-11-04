using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One upgrade card on the Company Store screen (wireframe screen 2).</summary>
[System.Serializable]
public class UpgradeCardView
{
    public UpgradeLine line;
    public TextMeshProUGUI title;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI effectText;
    public Button buyButton;
    public TextMeshProUGUI buyLabel;
}

/// <summary>A store row for an item or a repair (dynamite, auto-punch, headlamp, detector, machines, ads).</summary>
[System.Serializable]
public class StoreItemView
{
    public string key;
    public TextMeshProUGUI label;
    public Button button;
}

/// <summary>
/// Fills the Company Store from the economy data: seven upgrade cards (name, Lv a → b, effect,
/// BUY $price) and item rows. Buttons are greyed out when the player cannot afford them or the item
/// is maxed or missing its part. Refreshes on every economy change while the store is open.
/// </summary>
public class StoreUIController : MonoBehaviour
{
    public CompanyStoreEconomyManager economyManager;
    public MonetizationServicesManager monetization;
    public TextMeshProUGUI cashText;
    public List<UpgradeCardView> cards = new List<UpgradeCardView>();
    public List<StoreItemView> items = new List<StoreItemView>();

    private bool dirty = true;

    void OnEnable()
    {
        dirty = true;
        if (economyManager && economyManager.State != null) economyManager.State.Changed += MarkDirty;
    }

    void OnDisable()
    {
        if (economyManager && economyManager.State != null) economyManager.State.Changed -= MarkDirty;
    }

    private void MarkDirty() => dirty = true;

    void Update()
    {
        if (!dirty || !economyManager || economyManager.State == null) return;
        dirty = false;
        Refresh();
    }

    public void Refresh()
    {
        EconomyState s = economyManager.State;
        EconomyData d = economyManager.Data;
        if (cashText) cashText.text = "$ " + s.Cash.ToString("N0");

        foreach (UpgradeCardView card in cards)
        {
            UpgradeDefinition def = d.Upgrade(card.line);
            int level = s.Level(card.line);
            bool maxed = s.IsMaxed(card.line);
            long price = s.PriceToNext(card.line);
            if (card.title) card.title.text = def.displayName.ToUpperInvariant();
            if (card.levelText) card.levelText.text = maxed ? "Lv " + level + " (MAX)" : "Lv " + level + " \u2192 " + (level + 1);
            if (card.effectText) card.effectText.text = maxed ? FormatEffect(card.line, def.EffectAt(level)) : FormatEffect(card.line, def.EffectAt(level)) + " \u2192 " + FormatEffect(card.line, def.EffectAt(level + 1));
            if (card.buyLabel) card.buyLabel.text = maxed ? "MAXED" : "BUY $" + price.ToString("N0");
            if (card.buyButton) card.buyButton.interactable = !maxed && s.CanAfford(price);
        }

        foreach (StoreItemView item in items) RefreshItem(item, s, d);
    }

    private void RefreshItem(StoreItemView item, EconomyState s, EconomyData d)
    {
        string text;
        bool enabled;
        switch (item.key)
        {
            case "dynamitePack":
                text = "DYNAMITE x" + d.dynamitePackSticks + "  $" + d.dynamitePackCost.ToString("N0");
                enabled = s.CanAfford(d.dynamitePackCost);
                break;
            case "autoPunch":
                text = s.AutoPunchUnlocked ? "AUTO-PUNCH (OWNED)" : "AUTO-PUNCH  $" + d.autoPunchCost.ToString("N0");
                enabled = !s.AutoPunchUnlocked && s.CanAfford(d.autoPunchCost);
                break;
            case "treasureDetector":
                text = s.TreasureDetectorUnlocked ? "TREASURE DETECTOR (OWNED)" : "TREASURE DETECTOR  $" + d.treasureDetectorCost.ToString("N0");
                enabled = !s.TreasureDetectorUnlocked && s.CanAfford(d.treasureDetectorCost);
                break;
            case "headlamp":
                text = s.HeadlampMaxed ? "HEADLAMP Lv " + s.HeadlampLevel + " (MAX)" : "HEADLAMP Lv " + s.HeadlampLevel + " \u2192 " + (s.HeadlampLevel + 1) + "  $" + s.HeadlampPriceToNext.ToString("N0");
                enabled = !s.HeadlampMaxed && s.CanAfford(s.HeadlampPriceToNext);
                break;
            case EconomyState.PneumaticHammer:
            case EconomyState.Conveyor:
            case EconomyState.RockDrill:
            {
                EquipmentDefinition def = d.Equipment(item.key);
                if (s.IsRestored(item.key)) { text = def.name.ToUpperInvariant() + " (RESTORED)"; enabled = false; }
                else if (!s.HasPart(item.key)) { text = def.name.ToUpperInvariant() + ": PART MISSING (" + def.source + ")"; enabled = false; }
                else { text = "REPAIR " + def.name.ToUpperInvariant() + "  $" + def.repairCost.ToString("N0"); enabled = s.CanAfford(def.repairCost); }
                break;
            }
            case "removeAds":
                text = monetization && monetization.AdsRemoved ? "ADS REMOVED - THANK YOU" : "REMOVE ADS";
                enabled = monetization && !monetization.AdsRemoved;
                break;
            case "freeDynamiteAd":
                text = "FREE DYNAMITE (WATCH AD)";
                enabled = monetization && monetization.CanOffer(BreakToEscape.Core.Services.MonetizationRules.PlacementFreeDynamite);
                break;
            case "doubleCashAd":
                text = monetization ? "DOUBLE CASH, " + Mathf.RoundToInt(monetization.DoubleCashMinutes) + " MIN (WATCH AD)" : "DOUBLE CASH";
                enabled = monetization && monetization.CanOffer(BreakToEscape.Core.Services.MonetizationRules.PlacementDoubleCash);
                break;
            default:
                return;
        }
        if (item.label) item.label.text = text;
        if (item.button) item.button.interactable = enabled;
    }

    private static string FormatEffect(UpgradeLine line, double value)
    {
        switch (line)
        {
            case UpgradeLine.PunchPower: return value.ToString("0.#") + " dmg";
            case UpgradeLine.PunchSpeed: return value.ToString("0.00") + "/s";
            case UpgradeLine.PunchPattern: return value.ToString("0") + " cubes";
            case UpgradeLine.VacuumSuction: return value.ToString("0.#") + "/s";
            case UpgradeLine.TankCapacity: return value.ToString("0") + " cubes";
            case UpgradeLine.CrusherSpeed: return value.ToString("0.#") + "/s";
            default: return "x" + value.ToString("0.00");
        }
    }

    void LateUpdate()
    {
        // Ad availability changes with time (cooldowns), so refresh twice a second while open.
        if (Time.frameCount % 30 == 0) dirty = true;
    }
}
