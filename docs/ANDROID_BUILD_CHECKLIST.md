# Break to Escape — Android Build and Google Play Checklist

Work through this top to bottom. Items marked **[verified]** were checked against official Google pages in the November 2026 status report (section 15). Everything else is **[re-verify]**: check the current Play Console and Unity documentation before relying on it, because the rules change.

## 1. Unity Player Settings (Edit > Project Settings > Player > Android tab)

| Setting | Value | Notes |
|---|---|---|
| Company Name, Product Name | Your own | Product Name "Break to Escape" |
| Package Name | Reverse domain you own, e.g. `com.yourstudio.breaktoescape` | Cannot change after the first upload |
| Version / Bundle Version Code | 0.1.0 / 1 | Increase the code on every upload |
| Default Orientation | **Landscape Left** | The scene builder also sets this; untick portrait auto-rotation |
| Scripting Backend | **IL2CPP** | |
| Target Architectures | **ARM64 only** | Untick ARMv7 |
| Minimum API Level | **Android 8.0 (API 26)** | [re-verify] Unity 6.3's own minimum may be higher |
| Target API Level | **API 36 (Android 16)** | **[verified]** required for new apps and updates from 31 August 2026 |
| Active Input Handling (Other Settings) | **Both** for the prototype | Requires an editor restart. The game's Input System path is used when the package is installed |
| Graphics APIs | Vulkan, OpenGLES3 | [re-verify] test both on the low-tier phone |
| Managed Stripping Level | Low at first | Raise to Medium once everything works; check reflection-free code still runs |
| Build App Bundle (Google Play) | **On** (File > Build Profiles > Android) | Produces an .aab |
| Custom Main Manifest | Off unless needed | The vibrator permission is added automatically by Unity when `Handheld.Vibrate` or the Android vibrator is referenced; [re-verify] in the merged manifest |

## 2. URP quality tiers

1. Project Settings > Quality: create three levels named exactly **Low**, **Mid**, **High** (and delete or keep others). Set the Android default to Mid.
2. Run **BreakToEscape > Create URP Tier Assets**. It copies the active URP asset to `Assets/Generated/URP/URP_Low/Mid/High`, sets render scale (0.7 / 0.9 / 1.0) and shadow distance (0 / 12 / 25 m), and assigns each to its quality level.
3. In each URP asset: Low — main light shadows off, additional light shadows off, HDR off, MSAA off; Mid — main light shadows on (one cascade, 1024), soft shadows on; High — two cascades, 2048.
4. In the URP renderer asset, set **Rendering Path = Forward** for the prototype (see `docs/DECISIONS.md` D30).
5. At runtime `DeviceTierScaler` selects the level by name, then sets render scale, shadow distance, frame target, physics step and the rubble cap from `economy.json`.

## 3. 16 KB memory page size

**[verified]** Since 1 November 2025, new apps and updates targeting Android 15+ must support 16 KB page sizes on 64-bit devices (third-party sources quote a later date; treat it as required now).

- Use a Unity 6.3 LTS release whose release notes state 16 KB page-size support.
- Every native plug-in (ads SDK, billing, analytics, crash reporting) must also be 16 KB aligned.
- How to check: in Android Studio, open the built .aab/.apk with **Build > Analyze APK** and look for the 16 KB alignment warning on `lib/arm64-v8a/*.so`; or run `zipalign -c -P 16 -v 4 your.apk`; or upload to internal testing and read Play Console's warnings. Upload an internal-test build **early**; community reports describe Play Console rejecting Unity builds even on versions documented as supported.

## 4. Rewarded ads (Google Mobile Ads) and consent

The game is ready for an ads SDK but none is installed. Without one, a release build shows the offers as unavailable and grants nothing (tested at logic level).

1. Create an AdMob account and app; create one **Rewarded** ad unit. Never use live ad units while testing; use Google's test ad unit ids.
2. Install the Google Mobile Ads Unity plug-in (via its .unitypackage or OpenUPM). Resolve Android dependencies (External Dependency Manager).
3. Add your AdMob **App ID** in the plug-in's settings (Assets > Google Mobile Ads > Settings).
4. Add the scripting define **`ADMOB_SDK`** (Project Settings > Player > Other Settings > Scripting Define Symbols). This compiles `Assets/Scripts/Services/AdMobRewardedAdService.cs`.
5. Put the rewarded ad unit id in `MonetizationServicesManager.rewardedAdUnitIdAndroid` (on `_Systems`).
6. Consent: the adapter runs the User Messaging Platform flow (`ConsentInformation.Update`, then `ConsentForm.LoadAndShowConsentFormIfRequired`) before loading ads. Create the consent message (GDPR and US states) in AdMob > Privacy & messaging. [re-verify] the plug-in's current API names; the adapter is **not tested or unverified**.
7. Rules already enforced from data: double cash 5 min, at most once per 10 min; free dynamite at most 3 per session; no offers in the first 5 minutes; WATCH AD and NO THANKS are the same size.
8. Test on a device: reward only after the ad completes; closing early gives nothing.

## 5. Remove Ads purchase (Google Play Billing)

1. In Play Console create the in-app product **`remove_ads`** (one-time, non-consumable). The id is in `economy.json` → `monetization.removeAdsProductId`.
2. Install **Unity In-App Purchasing** from the Package Manager. **Use a version that bundles Google Play Billing Library 8 or newer** — **[verified]** version 7 is no longer accepted for new apps and updates; version 8's deadline is 31 August 2027. [re-verify] which Unity IAP release bundles Billing 8.
3. Add the scripting define **`UNITY_IAP`**. This compiles `UnityIapPurchaseService.cs`, written against the Unity IAP 4.x listener API. If Unity IAP 5.x is installed, rewrite the adapter for its newer API (the interface `IPurchaseService` stays the same). **Not tested or unverified.**
4. **Purchase verification:** before reporting "Purchased", validate the receipt (Unity's `CrossPlatformValidator` with your Google public key, or better a small server that calls the Google Play Developer API). This is marked `TODO(P1-6)` in the adapter.
5. **Acknowledge** purchases (Unity IAP does this when `ProcessPurchase` returns `Complete`); unacknowledged purchases are refunded by Google after 3 days. [re-verify]
6. **Restore purchases:** the game calls `RestorePurchases` at start-up and from the store's RESTORE PURCHASES row; owned `remove_ads` sets the flag and saves it.
7. Test with licence testers (Play Console > Settings > License testing) on an internal track build.

## 6. Analytics and crash reporting

Interfaces exist (`IAnalyticsService`, `ICrashService`); development builds log analytics events to the console, release builds use null services. Event list: `session_start`, `band_reached`, `upgrade_bought`, `ad_offered`, `ad_watched`, `ad_declined`, `escape_completed`, `quality_tier`, `equipment_restored`, `purchase_result` (parameters in `Core/Services/Services.cs`). To integrate a provider (for example Firebase Analytics and Crashlytics), write an adapter that implements the interface and select it in `GameBootstrap.Awake`. Declare the data in the Data Safety form.

## 7. Signing and upload

1. Create an upload keystore (Project Settings > Player > Publishing Settings > Keystore Manager). Back it up in two places; never commit it.
2. Enrol in **Play App Signing** (Google holds the app signing key).
3. Build the .aab and upload to **Internal testing** first.

## 8. Testing tracks

- Internal testing → **closed testing** → production with a staged rollout.
- **[verified]** Personal developer accounts created after 13 November 2023 must run a closed test with **at least 12 testers opted in continuously for 14 days** before applying for production access. Recruit testers early.
- Read the **pre-launch report** for crashes and accessibility warnings on every upload.

## 9. Store listing and policy forms [re-verify all]

- App name, short and full description (original wording).
- Icon 512 × 512, feature graphic 1024 × 500, at least 4 landscape phone screenshots from the real build (see `docs/ASSET_REQUIREMENTS.md`).
- Privacy policy URL (required; ads and analytics collect data).
- **Data Safety** form: advertising id, crash logs, app interactions (if analytics is added), purchase history.
- **Content rating** questionnaire (expect mild cartoon violence at most).
- **Ads declaration**: the app contains ads.
- Target audience: not primarily children (affects ads SDK settings).
- Final originality check: no text, art, UI or sound copied from any other game.

## 10. Performance gates before release

- Gate 1: punching holds 30 fps with 100 rubble cubes on the low-tier phone.
- Gate 5: 20-minute heat test on all three phones with no crash and a stable frame rate.
- Use the in-game rate overlay (Settings > Show Rate Overlay) and the Unity Profiler over USB.
