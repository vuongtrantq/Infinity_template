# Integration Package

Unity integration package for partners: AdMob, AppLovin MAX, Adjust, Unity IAP, Firebase Analytics, Firebase Crashlytics, and Firebase Remote Config.

## Included

- SDK folders copied from `D:\Gun`: `Adjust`, `GoogleMobileAds`, `MaxSdk`, `Plugins`.
- Firebase is installed through `Packages/manifest.json` using official Google UPM tarballs:
  - `com.google.external-dependency-manager`
  - `com.google.firebase.app`
  - `com.google.firebase.analytics`
  - `com.google.firebase.crashlytics`
  - `com.google.firebase.remote-config`
- Runtime wrapper in `Assets/IntegrationPackage/Runtime`.
- Unity IAP package dependency: `com.unity.purchasing` `4.12.2`.
- Editor exporter in `Tools/Integration Package/Export UnityPackage`.
- Default bootstrap prefab generated at `Assets/IntegrationPackage/Prefabs/IntegrationBootstrap.prefab`.
- Default settings generated at `Assets/IntegrationPackage/Resources/IntegrationPackage/IntegrationSettings.asset`.
- Example launcher scenes in `Assets/_Project/Scenes`: `Launcher` initializes the integration then loads `Home`.
- `Home` includes runtime buttons to test banner, interstitial, rewarded, app-open, Analytics, and Adjust.

## Partner setup

1. Import `IntegrationPackage.unitypackage`.
2. Add `IntegrationBootstrap.prefab` to the first scene.
3. Open `IntegrationSettings.asset` and fill real IDs:
   - AdMob app IDs in Google Mobile Ads settings.
   - AdMob banner/inter/reward/app-open unit IDs.
   - AppLovin MAX SDK key and unit IDs.
   - Adjust app token and event tokens.
4. Add the partner Firebase files:
   - Android: `google-services.json`.
   - iOS: `GoogleService-Info.plist`.
5. Add the Firebase UPM dependencies to `Packages/manifest.json` if they are not already present:

```json
"com.google.external-dependency-manager": "https://dl.google.com/games/registry/unity/com.google.external-dependency-manager/com.google.external-dependency-manager-1.2.186.tgz",
"com.google.firebase.app": "https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-13.13.0.tgz",
"com.google.firebase.analytics": "https://dl.google.com/games/registry/unity/com.google.firebase.analytics/com.google.firebase.analytics-13.13.0.tgz",
"com.google.firebase.crashlytics": "https://dl.google.com/games/registry/unity/com.google.firebase.crashlytics/com.google.firebase.crashlytics-13.13.0.tgz",
"com.google.firebase.remote-config": "https://dl.google.com/games/registry/unity/com.google.firebase.remote-config/com.google.firebase.remote-config-13.13.0.tgz"
```

6. Add Unity IAP if it is not already present:

```json
"com.unity.purchasing": "4.12.2"
```

7. Configure IAP product IDs in `IntegrationSettings.asset`.
8. Run `Assets > External Dependency Manager > Android Resolver > Resolve`.
9. For Android build, use a writable Android SDK path or run Unity as administrator if the build prints `Probably the SDK is read-only`.

## Firebase Android notes

- `gradleTemplate.properties` uses AndroidX with Jetifier disabled. Re-enable `android.enableJetifier=true` only if a partner adds an old `com.android.support` dependency.
- Partners should replace Firebase config files with their own project files before release builds.

## Runtime API

```csharp
using PartnerIntegration;

IntegrationBootstrap.Instance.ShowBanner();
IntegrationBootstrap.Instance.HideBanner();
IntegrationBootstrap.Instance.ShowInterstitial("Inter_play_time", OnClosed, OnFailed);
IntegrationBootstrap.Instance.ShowRewarded("Reward_Revive", success => { });
IntegrationBootstrap.Instance.ShowAppOpenIfAvailable();
IntegrationBootstrap.Instance.PurchaseIap("remove_ads", result => { });
IntegrationBootstrap.Instance.RestoreIapPurchases(success => { });

AnalyticsTracker.LogEvent("custom_event");
AdjustTracker.TrackEvent("adjust_event_token");
```

## IAP

Configure products in `IntegrationSettings.asset`:

- `key`: logical key used by game code, for example `remove_ads`.
- `androidId`: Google Play product ID.
- `iosId`: App Store product ID.
- `type`: consumable, non-consumable, or subscription.
- `adjustPurchaseEventToken`: optional Adjust event token for purchase revenue tracking.

## Remote Config keys

Default keys follow the sample project:

- `IS_ADMOB`
- `IS_SHOW_APP_OPEN`
- `INTERVAL_TIME`
- `ADMOB_ANDROID_BANNER_ID`
- `ADMOB_ANDROID_INTERTITIAL_ID`
- `ADMOB_ANDROID_REWARDED_ID`
- `ADMOB_IOS_BANNER_ID`
- `ADMOB_IOS_INTERTITIAL_ID`
- `ADMOB_IOS_REWARDED_ID`

The misspelled `INTERTITIAL` key is kept for compatibility with the sample. You can change keys per ad unit in `IntegrationSettings.asset`.
