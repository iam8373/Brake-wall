using System;
using System.Collections;
using UnityEngine;

public class MonetizationServicesManager : MonoBehaviour
{
    public static MonetizationServicesManager Instance { get; private set; }

    [Header("References")]
    public CompanyStoreEconomyManager economyManager;
    public FirstPersonToolController toolController;

    [Header("Monetization State")]
    public bool adsRemoved = false;
    public bool isDoubleCashActive = false;
    public float doubleCashDurationSeconds = 120f;

    // Hook your Google AdMob / Unity LevelPlay Rewarded Ad SDK and Google Play Billing Library 8.0+ callbacks here
    public event Action<Action> OnRequestShowRewardedAd;
    public event Action<string, Action> OnRequestPurchaseIAP;

    public const string IAP_REMOVE_ADS_ID = "com.breaktoescape.removeads";

    private float doubleCashRemaining = 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Crusher07Controller.OnCashEarned += HandleDoubleCashBonus;
    }

    void OnDisable()
    {
        Crusher07Controller.OnCashEarned -= HandleDoubleCashBonus;
    }

    void Update()
    {
        if (isDoubleCashActive)
        {
            doubleCashRemaining -= Time.deltaTime;
            if (doubleCashRemaining <= 0f)
            {
                isDoubleCashActive = false;
            }
        }
    }

    private void HandleDoubleCashBonus(int basePayout)
    {
        // When Double Cash boost is active, award an extra 1x payout on every crushed cube
        if (isDoubleCashActive && economyManager != null)
        {
            economyManager.AddCash(basePayout);
        }
    }

    // Bind to "2X CASH (WATCH AD)" UI Button
    public void OnTapRewardedAdDoubleCash()
    {
        if (adsRemoved || OnRequestShowRewardedAd == null)
        {
            GrantDoubleCashReward();
            return;
        }

        OnRequestShowRewardedAd.Invoke(GrantDoubleCashReward);
    }

    private void GrantDoubleCashReward()
    {
        isDoubleCashActive = true;
        doubleCashRemaining = doubleCashDurationSeconds;
        if (economyManager)
            economyManager.ShowTutorialCaption("2X CRUSHER CASH ACTIVE FOR 2 MINUTES!");
    }

    // Bind to "FREE DYNAMITE (WATCH AD)" UI Button
    public void OnTapRewardedAdFreeDynamite()
    {
        if (adsRemoved || OnRequestShowRewardedAd == null)
        {
            GrantFreeDynamiteReward();
            return;
        }

        OnRequestShowRewardedAd.Invoke(GrantFreeDynamiteReward);
    }

    private void GrantFreeDynamiteReward()
    {
        if (toolController)
        {
            toolController.dynamiteStock += 2;
        }
        if (economyManager)
        {
            economyManager.ShowTutorialCaption("+2 DYNAMITE STICKS ADDED TO HOTBAR!");
        }
    }

    // Bind to "REMOVE ADS" IAP Store Button (Google Play Billing Library 8.0+)
    public void OnTapPurchaseRemoveAds()
    {
        if (adsRemoved) return;

        if (OnRequestPurchaseIAP != null)
        {
            OnRequestPurchaseIAP.Invoke(IAP_REMOVE_ADS_ID, () =>
            {
                adsRemoved = true;
                if (SaveManager.Instance) SaveManager.Instance.SaveGameLocal();
            });
        }
        else
        {
            // Editor test fallback
            adsRemoved = true;
            if (SaveManager.Instance) SaveManager.Instance.SaveGameLocal();
        }
    }
}
