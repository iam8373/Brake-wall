using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Services;
using UnityEngine;

/// <summary>
/// Rewarded ads and the Remove Ads purchase, made release-safe:
///  - Rewards are granted only when a real rewarded ad completes, or when ads were removed by a
///    verified purchase (the design's "gentler rate" applies then).
///  - The test fallback that grants rewards or Remove Ads without an SDK is compiled only into the
///    editor and development builds (<see cref="AllowTestFallback"/>). A release build without an
///    SDK shows the offers as unavailable and never grants anything for free.
///  - Durations, cooldown, session cap and the ad-free grace period come from economy.json.
/// SDK adapters (ads, billing) are selected by scripting define; see docs/ANDROID_BUILD_CHECKLIST.md.
/// </summary>
public class MonetizationServicesManager : MonoBehaviour
{
    public static MonetizationServicesManager Instance { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public const bool AllowTestFallback = true;
#else
    public const bool AllowTestFallback = false;
#endif

    [Header("Ad unit / product configuration (fill in when the SDKs are installed)")]
    public string rewardedAdUnitIdAndroid = "";

    public MonetizationRules Rules { get; private set; }
    public bool AdsRemoved => Rules != null && Rules.AdsRemoved;
    public bool DoubleCashActive => Rules != null && Rules.DoubleCashActive;
    public float DoubleCashRemaining => Rules != null ? Rules.DoubleCashRemaining : 0f;
    public float DoubleCashMinutes => Rules != null ? Rules.Settings.doubleCashDurationSeconds / 60f : 5f;

    private IAdService ads;
    private IPurchaseService purchases;
    private EconomyState economy;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(MonetizationRules rules, EconomyState economyState)
    {
        Rules = rules;
        economy = economyState;
        ads = CreateAdService();
        purchases = CreatePurchaseService();
        ads.RequestConsent(() => { });
        purchases.RestorePurchases(ApplyOwnedProducts);
    }

    private IAdService CreateAdService()
    {
#if ADMOB_SDK
        return new AdMobRewardedAdService(rewardedAdUnitIdAndroid);
#else
        return new NullAdService();
#endif
    }

    private IPurchaseService CreatePurchaseService()
    {
#if UNITY_IAP
        return new UnityIapPurchaseService(Rules.Settings.removeAdsProductId);
#else
        return new NullPurchaseService();
#endif
    }

    void Update()
    {
        if (Rules != null) Rules.Tick(Time.deltaTime);
    }

    private float PlaySeconds => GameBootstrap.Instance ? GameBootstrap.Instance.RunSeconds : 0f;

    /// <summary>True if the offer may be shown now (cooldown, cap and grace period respected, and something can deliver it).</summary>
    public bool CanOffer(string placement)
    {
        if (Rules == null) return false;
        return Rules.DecideReward(placement, PlaySeconds, ads.IsRewardedReady, AllowTestFallback) != RewardDecision.Unavailable;
    }

    /// <summary>Runs the reward flow for a placement after the player chose WATCH AD.</summary>
    public void RequestReward(string placement)
    {
        if (Rules == null) return;
        RewardDecision decision = Rules.DecideReward(placement, PlaySeconds, ads.IsRewardedReady, AllowTestFallback);
        switch (decision)
        {
            case RewardDecision.ShowAd:
                ads.ShowRewarded(placement, rewarded =>
                {
                    Analytics(AnalyticsEvents.AdWatched, placement, rewarded);
                    if (rewarded) Grant(placement);
                });
                break;
            case RewardDecision.GrantWithoutAd:
                Grant(placement);
                break;
            case RewardDecision.GrantTestFallback:
                Debug.Log("MonetizationServicesManager: development build, granting '" + placement + "' without an ad SDK.");
                Grant(placement);
                break;
        }
    }

    private void Grant(string placement)
    {
        if (placement == MonetizationRules.PlacementDoubleCash)
        {
            Rules.GrantDoubleCash();
            if (Dispatch.Instance) Dispatch.Instance.Say("double_cash_on", Mathf.RoundToInt(DoubleCashMinutes).ToString());
        }
        else if (placement == MonetizationRules.PlacementFreeDynamite)
        {
            economy.AddDynamite(Rules.GrantFreeDynamite());
            if (Dispatch.Instance) Dispatch.Instance.Say("dynamite_found");
        }
    }

    // ------------------------------------------------------------------ Remove Ads

    public void PurchaseRemoveAds()
    {
        if (Rules == null) return;
        switch (Rules.DecideRemoveAdsPurchase(purchases.IsReady, AllowTestFallback))
        {
            case PurchaseDecision.StartPurchase:
                purchases.Purchase(Rules.Settings.removeAdsProductId, outcome =>
                {
                    Analytics(AnalyticsEvents.PurchaseResult, Rules.Settings.removeAdsProductId, outcome == PurchaseOutcome.Purchased);
                    if (outcome == PurchaseOutcome.Purchased) SetAdsRemoved();
                });
                break;
            case PurchaseDecision.GrantTestFallback:
                Debug.Log("MonetizationServicesManager: development build, granting Remove Ads without billing.");
                SetAdsRemoved();
                break;
        }
    }

    public void RestorePurchases()
    {
        if (purchases != null) purchases.RestorePurchases(ApplyOwnedProducts);
    }

    private void ApplyOwnedProducts(IList<string> owned)
    {
        if (owned == null || Rules == null) return;
        foreach (string id in owned)
            if (id == Rules.Settings.removeAdsProductId) SetAdsRemoved();
    }

    private void SetAdsRemoved()
    {
        if (Rules.AdsRemoved) return;
        Rules.SetAdsRemoved(true);
        if (GameBootstrap.Instance) GameBootstrap.Instance.SaveNow();
    }

    private static void Analytics(string eventName, string id, bool success)
    {
        if (GameBootstrap.Instance) GameBootstrap.Instance.Analytics(eventName, "placement", id, "success", success);
    }
}
