#if ADMOB_SDK
// Rewarded-ad adapter for the Google Mobile Ads Unity plug-in, with the User Messaging Platform
// consent flow. Compiled only when the scripting define ADMOB_SDK is set (after the plug-in is
// installed). NOT TESTED OR UNVERIFIED: written against the plug-in's documented API (RewardedAd.Load,
// CanShowAd, Show; ConsentInformation.Update; ConsentForm.LoadAndShowConsentFormIfRequired). Check
// the names against the installed plug-in version; see docs/ANDROID_BUILD_CHECKLIST.md.
using System;
using BreakToEscape.Core.Services;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public sealed class AdMobRewardedAdService : IAdService
{
    private readonly string adUnitId;
    private RewardedAd loaded;
    private bool initialized;

    public AdMobRewardedAdService(string adUnitId)
    {
        this.adUnitId = adUnitId;
    }

    public bool IsRewardedReady => loaded != null && loaded.CanShowAd();
    public bool ConsentResolved { get; private set; }

    public void RequestConsent(Action onResolved)
    {
        var request = new ConsentRequestParameters();
        ConsentInformation.Update(request, updateError =>
        {
            if (updateError != null) Debug.LogWarning("Consent update failed: " + updateError.Message);
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null) Debug.LogWarning("Consent form failed: " + formError.Message);
                ConsentResolved = true;
                if (ConsentInformation.CanRequestAds()) InitializeAndLoad();
                onResolved?.Invoke();
            });
        });
    }

    private void InitializeAndLoad()
    {
        if (initialized) { Load(); return; }
        MobileAds.Initialize(status => { initialized = true; Load(); });
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(adUnitId)) { Debug.LogWarning("AdMob: rewarded ad unit id is empty."); return; }
        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null) { Debug.LogWarning("AdMob: rewarded load failed: " + error); return; }
            loaded = ad;
        });
    }

    public void ShowRewarded(string placement, Action<bool> onFinished)
    {
        if (!IsRewardedReady) { onFinished?.Invoke(false); return; }
        RewardedAd ad = loaded;
        loaded = null;
        bool rewarded = false;
        ad.OnAdFullScreenContentClosed += () =>
        {
            onFinished?.Invoke(rewarded);
            ad.Destroy();
            Load();
        };
        ad.OnAdFullScreenContentFailed += error =>
        {
            onFinished?.Invoke(false);
            ad.Destroy();
            Load();
        };
        ad.Show(reward => rewarded = true);
    }
}
#endif
