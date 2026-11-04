using System;
using System.Collections.Generic;
using BreakToEscape.Core.Economy;

namespace BreakToEscape.Core.Services
{
    /// <summary>Rewarded ad provider (for example an ad SDK adapter). Implementations live in the Unity layer.</summary>
    public interface IAdService
    {
        /// <summary>True when a rewarded ad is loaded and can be shown.</summary>
        bool IsRewardedReady { get; }
        /// <summary>True once the player has answered the consent prompt (or none is required).</summary>
        bool ConsentResolved { get; }
        void RequestConsent(Action onResolved);
        /// <summary>Shows a rewarded ad. The callback receives true only if the ad completed and the reward is earned.</summary>
        void ShowRewarded(string placement, Action<bool> onFinished);
    }

    public enum PurchaseOutcome
    {
        Purchased,
        Cancelled,
        Failed,
        Pending
    }

    /// <summary>In-app purchase provider (Play Billing adapter). Implementations live in the Unity layer.</summary>
    public interface IPurchaseService
    {
        bool IsReady { get; }
        /// <summary>Starts a purchase. Report Purchased only after the purchase is verified and acknowledged.</summary>
        void Purchase(string productId, Action<PurchaseOutcome> onFinished);
        /// <summary>Queries owned non-consumable products (restore purchases).</summary>
        void RestorePurchases(Action<IList<string>> onOwnedProducts);
    }

    public interface IAnalyticsService
    {
        void LogEvent(string name, IDictionary<string, object> parameters);
    }

    public interface ICrashService
    {
        void Log(string message);
        void RecordException(Exception exception);
    }

    public sealed class NullAdService : IAdService
    {
        public bool IsRewardedReady => false;
        public bool ConsentResolved => true;
        public void RequestConsent(Action onResolved) => onResolved?.Invoke();
        public void ShowRewarded(string placement, Action<bool> onFinished) => onFinished?.Invoke(false);
    }

    public sealed class NullPurchaseService : IPurchaseService
    {
        public bool IsReady => false;
        public void Purchase(string productId, Action<PurchaseOutcome> onFinished) => onFinished?.Invoke(PurchaseOutcome.Failed);
        public void RestorePurchases(Action<IList<string>> onOwnedProducts) => onOwnedProducts?.Invoke(new string[0]);
    }

    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string name, IDictionary<string, object> parameters) { }
    }

    public sealed class NullCrashService : ICrashService
    {
        public void Log(string message) { }
        public void RecordException(Exception exception) { }
    }

    /// <summary>Analytics event names. Parameters are listed next to each name.</summary>
    public static class AnalyticsEvents
    {
        /// <summary>params: tier, adsRemoved</summary>
        public const string SessionStart = "session_start";
        /// <summary>params: band, runMinutes</summary>
        public const string BandReached = "band_reached";
        /// <summary>params: line, level, price</summary>
        public const string UpgradeBought = "upgrade_bought";
        /// <summary>params: placement</summary>
        public const string AdOffered = "ad_offered";
        /// <summary>params: placement, rewarded</summary>
        public const string AdWatched = "ad_watched";
        /// <summary>params: placement</summary>
        public const string AdDeclined = "ad_declined";
        /// <summary>params: runMinutes, cubesBroken</summary>
        public const string EscapeCompleted = "escape_completed";
        /// <summary>params: tier, ramMb, cores, graphicsMb, manualOverride</summary>
        public const string QualityTier = "quality_tier";
        /// <summary>params: equipment</summary>
        public const string EquipmentRestored = "equipment_restored";
        /// <summary>params: productId, outcome</summary>
        public const string PurchaseResult = "purchase_result";
    }

    public enum RewardDecision
    {
        /// <summary>Show a real rewarded ad; grant only when it completes.</summary>
        ShowAd,
        /// <summary>Ads removed (verified purchase): grant at the gentler Remove Ads rate.</summary>
        GrantWithoutAd,
        /// <summary>Development builds only: no ad SDK, grant for testing.</summary>
        GrantTestFallback,
        /// <summary>Offer not available now (cooldown, cap, grace period, or no ad ready in a release build).</summary>
        Unavailable
    }

    public enum PurchaseDecision
    {
        StartPurchase,
        GrantTestFallback,
        AlreadyOwned,
        Unavailable
    }

    /// <summary>
    /// Rewarded-ad and Remove Ads rules, all read from economy.json: double-cash duration and
    /// cooldown, free-dynamite session cap, and the ad-free grace period at the start of play.
    /// The caller passes <c>allowTestFallback</c>, which the Unity layer sets to true only in
    /// editor and development builds, so a release build without an SDK can never grant a reward
    /// or Remove Ads for free.
    /// </summary>
    public sealed class MonetizationRules
    {
        public const string PlacementDoubleCash = "double_cash";
        public const string PlacementFreeDynamite = "free_dynamite";

        private readonly MonetizationSettings settings;

        public bool AdsRemoved { get; private set; }
        public float DoubleCashRemaining { get; private set; }
        public float DoubleCashCooldownRemaining { get; private set; }
        public int FreeDynamiteUsedThisSession { get; private set; }
        public bool DoubleCashActive => DoubleCashRemaining > 0f;

        public MonetizationRules(MonetizationSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public MonetizationSettings Settings => settings;

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            DoubleCashRemaining = Math.Max(0f, DoubleCashRemaining - deltaSeconds);
            DoubleCashCooldownRemaining = Math.Max(0f, DoubleCashCooldownRemaining - deltaSeconds);
        }

        public bool InGracePeriod(float totalPlaySeconds) => totalPlaySeconds < settings.adFreeGracePeriodSeconds;

        public int FreeDynamiteSessionCap => AdsRemoved ? settings.removeAdsFreeDynamitePerSession : settings.freeDynamitePerSession;

        public bool CanOfferDoubleCash(float totalPlaySeconds) =>
            !DoubleCashActive && DoubleCashCooldownRemaining <= 0f && (AdsRemoved || !InGracePeriod(totalPlaySeconds));

        public bool CanOfferFreeDynamite(float totalPlaySeconds) =>
            FreeDynamiteUsedThisSession < FreeDynamiteSessionCap && (AdsRemoved || !InGracePeriod(totalPlaySeconds));

        public RewardDecision DecideReward(string placement, float totalPlaySeconds, bool adReady, bool allowTestFallback)
        {
            bool offer = placement == PlacementDoubleCash ? CanOfferDoubleCash(totalPlaySeconds) : CanOfferFreeDynamite(totalPlaySeconds);
            if (!offer) return RewardDecision.Unavailable;
            if (AdsRemoved) return RewardDecision.GrantWithoutAd;
            if (adReady) return RewardDecision.ShowAd;
            return allowTestFallback ? RewardDecision.GrantTestFallback : RewardDecision.Unavailable;
        }

        /// <summary>Call only after an ad completed (or a permitted grant). Starts the boost and its cooldown.</summary>
        public void GrantDoubleCash()
        {
            DoubleCashRemaining = settings.doubleCashDurationSeconds;
            DoubleCashCooldownRemaining = AdsRemoved ? settings.removeAdsDoubleCashCooldownSeconds : settings.doubleCashCooldownSeconds;
        }

        /// <summary>Call only after an ad completed (or a permitted grant). Returns the sticks to add.</summary>
        public int GrantFreeDynamite()
        {
            FreeDynamiteUsedThisSession++;
            return settings.freeDynamiteSticks;
        }

        public PurchaseDecision DecideRemoveAdsPurchase(bool purchaseServiceReady, bool allowTestFallback)
        {
            if (AdsRemoved) return PurchaseDecision.AlreadyOwned;
            if (purchaseServiceReady) return PurchaseDecision.StartPurchase;
            return allowTestFallback ? PurchaseDecision.GrantTestFallback : PurchaseDecision.Unavailable;
        }

        /// <summary>Set after a verified purchase, a restore, or loading a save.</summary>
        public void SetAdsRemoved(bool removed) => AdsRemoved = removed;

        public void Restore(bool adsRemoved, float doubleCashRemaining, float cooldownRemaining)
        {
            AdsRemoved = adsRemoved;
            DoubleCashRemaining = Math.Max(0f, Math.Min(settings.doubleCashDurationSeconds, doubleCashRemaining));
            DoubleCashCooldownRemaining = Math.Max(0f, cooldownRemaining);
        }

        /// <summary>Extra payout while double cash is active (equal to the base payout).</summary>
        public long BonusFor(long basePayout) => DoubleCashActive && basePayout > 0 ? basePayout : 0;
    }
}
