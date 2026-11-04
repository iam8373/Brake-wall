#if UNITY_IAP
// Remove Ads purchase adapter for Unity In-App Purchasing (Google Play Billing underneath).
// Compiled only when the scripting define UNITY_IAP is set. NOT TESTED OR UNVERIFIED: written
// against the Unity IAP 4.x listener API (UnityPurchasing.Initialize, IDetailedStoreListener,
// ConfirmPendingPurchase). Unity IAP 5 changed this API; if 5.x is installed, rewrite this adapter
// against its StoreController API. Confirm the bundled Play Billing Library is version 8 or newer.
// Purchases must be verified (receipt validation on a server or Unity's validator) before
// "Purchased" is reported; see docs/ANDROID_BUILD_CHECKLIST.md.
using System;
using System.Collections.Generic;
using BreakToEscape.Core.Services;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public sealed class UnityIapPurchaseService : IPurchaseService, IDetailedStoreListener
{
    private readonly string removeAdsId;
    private IStoreController controller;
    private Action<PurchaseOutcome> pending;
    private Action<IList<string>> pendingRestore;

    public UnityIapPurchaseService(string removeAdsProductId)
    {
        removeAdsId = removeAdsProductId;
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(removeAdsId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public bool IsReady => controller != null;

    public void Purchase(string productId, Action<PurchaseOutcome> onFinished)
    {
        if (controller == null) { onFinished?.Invoke(PurchaseOutcome.Failed); return; }
        pending = onFinished;
        controller.InitiatePurchase(productId);
    }

    public void RestorePurchases(Action<IList<string>> onOwnedProducts)
    {
        if (controller == null) { pendingRestore = onOwnedProducts; return; }
        onOwnedProducts?.Invoke(Owned());
    }

    private IList<string> Owned()
    {
        var owned = new List<string>();
        Product p = controller.products.WithID(removeAdsId);
        if (p != null && p.hasReceipt) owned.Add(removeAdsId);
        return owned;
    }

    public void OnInitialized(IStoreController storeController, IExtensionProvider extensions)
    {
        controller = storeController;
        if (pendingRestore != null) { pendingRestore(Owned()); pendingRestore = null; }
    }

    public void OnInitializeFailed(InitializationFailureReason error) => Debug.LogWarning("IAP init failed: " + error);
    public void OnInitializeFailed(InitializationFailureReason error, string message) => Debug.LogWarning("IAP init failed: " + error + " " + message);

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        // TODO(P1-6): validate args.purchasedProduct.receipt before granting (server-side verification recommended).
        if (args.purchasedProduct.definition.id == removeAdsId)
        {
            pending?.Invoke(PurchaseOutcome.Purchased);
            pending = null;
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        pending?.Invoke(reason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);
        pending = null;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
    {
        pending?.Invoke(description.reason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);
        pending = null;
    }
}
#endif
