using System.Collections.Generic;
using AdjustSdk;

using UnityEngine;

namespace PartnerIntegration
{
    public sealed class IntegrationSettings : ScriptableObject
    {
        public const string ResourcesPath = "IntegrationPackage/IntegrationSettings";

        [Header("Startup")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool initializeFirebase = true;
        [SerializeField] private bool initializeAds = true;
        [SerializeField] private AdNetwork activeAdNetwork = AdNetwork.AdMob;
        [SerializeField] private bool showAppOpen = true;
        [SerializeField] private IntegrationBannerPosition bannerPosition = IntegrationBannerPosition.Bottom;
        [SerializeField] private float interstitialIntervalSeconds = 40f;

        [Header("Remote Config")]
        [SerializeField] private bool fetchRemoteConfig = true;
        [SerializeField] private long remoteConfigCacheSeconds;
        [SerializeField] private string remoteUseAdMobKey = "IS_ADMOB";
        [SerializeField] private string remoteShowAppOpenKey = "IS_SHOW_APP_OPEN";
        [SerializeField] private string remoteInterstitialIntervalKey = "INTERVAL_TIME";

        [Header("AdMob")]
        [SerializeField] private PlatformAdUnit admobBanner = new PlatformAdUnit(
            "Banner",
            "ca-app-pub-3940256099942544/6300978111",
            "ca-app-pub-3940256099942544/2934735716",
            "ADMOB_ANDROID_BANNER_ID",
            "ADMOB_IOS_BANNER_ID");

        [SerializeField] private PlatformAdUnit admobAppOpen = new PlatformAdUnit(
            "AppOpen",
            "ca-app-pub-3940256099942544/9257395921",
            "ca-app-pub-3940256099942544/5575463023",
            "ADMOB_ANDROID_APP_OPEN_ID",
            "ADMOB_IOS_APP_OPEN_ID");

        [SerializeField] private List<PlatformAdUnit> admobInterstitials = new List<PlatformAdUnit>
        {
            new PlatformAdUnit("Inter_play_time", "ca-app-pub-3940256099942544/1033173712", "ca-app-pub-3940256099942544/4411468910", "ADMOB_ANDROID_INTERTITIAL_ID", "ADMOB_IOS_INTERTITIAL_ID")
        };

        [SerializeField] private List<PlatformAdUnit> admobRewarded = new List<PlatformAdUnit>
        {
            new PlatformAdUnit("Reward_Revive", "ca-app-pub-3940256099942544/5224354917", "ca-app-pub-3940256099942544/1712485313", "ADMOB_ANDROID_REWARDED_ID", "ADMOB_IOS_REWARDED_ID")
        };

        [Header("AppLovin MAX")]
        [SerializeField] private string appLovinSdkKey;
        [SerializeField] private PlatformAdUnit appLovinBanner = new PlatformAdUnit("Banner", "", "");
        [SerializeField] private PlatformAdUnit appLovinAppOpen = new PlatformAdUnit("AppOpen", "", "");
        [SerializeField] private List<PlatformAdUnit> appLovinInterstitials = new List<PlatformAdUnit>
        {
            new PlatformAdUnit("Inter_play_time", "", "")
        };
        [SerializeField] private List<PlatformAdUnit> appLovinRewarded = new List<PlatformAdUnit>
        {
            new PlatformAdUnit("Reward_Revive", "", "")
        };

        [Header("Adjust")]
        [SerializeField] private bool initializeAdjust = true;
        [SerializeField] private string adjustAppToken;
        [SerializeField] private AdjustEnvironment adjustEnvironment = AdjustEnvironment.Sandbox;
        [SerializeField] private AdjustLogLevel adjustLogLevel = AdjustLogLevel.Info;
        [SerializeField] private bool adjustSendInBackground = true;
        [SerializeField] private string adjustAdClickEventToken = "9s14t4";
        [SerializeField] private string adjustAdRevenueEventToken = "bmh2rl";
        [SerializeField] private string adjustInterstitialFinishedEventToken = "ukg1l5";

        public bool AutoInitialize => autoInitialize;
        public bool InitializeFirebase => initializeFirebase;
        public bool InitializeAds => initializeAds;
        public AdNetwork ActiveAdNetwork { get => activeAdNetwork; set => activeAdNetwork = value; }
        public bool ShowAppOpen { get => showAppOpen; set => showAppOpen = value; }
        public IntegrationBannerPosition BannerPosition => bannerPosition;
        public float InterstitialIntervalSeconds { get => interstitialIntervalSeconds; set => interstitialIntervalSeconds = Mathf.Max(0f, value); }

        public bool FetchRemoteConfig => fetchRemoteConfig;
        public long RemoteConfigCacheSeconds => remoteConfigCacheSeconds;
        public string RemoteUseAdMobKey => remoteUseAdMobKey;
        public string RemoteShowAppOpenKey => remoteShowAppOpenKey;
        public string RemoteInterstitialIntervalKey => remoteInterstitialIntervalKey;

        public PlatformAdUnit AdMobBanner => admobBanner;
        public PlatformAdUnit AdMobAppOpen => admobAppOpen;
        public IReadOnlyList<PlatformAdUnit> AdMobInterstitials => admobInterstitials;
        public IReadOnlyList<PlatformAdUnit> AdMobRewarded => admobRewarded;

        public string AppLovinSdkKey => appLovinSdkKey;
        public PlatformAdUnit AppLovinBanner => appLovinBanner;
        public PlatformAdUnit AppLovinAppOpen => appLovinAppOpen;
        public IReadOnlyList<PlatformAdUnit> AppLovinInterstitials => appLovinInterstitials;
        public IReadOnlyList<PlatformAdUnit> AppLovinRewarded => appLovinRewarded;

        public bool InitializeAdjust => initializeAdjust;
        public string AdjustAppToken => adjustAppToken;
        public AdjustEnvironment AdjustEnvironment => adjustEnvironment;
        public AdjustLogLevel AdjustLogLevel => adjustLogLevel;
        public bool AdjustSendInBackground => adjustSendInBackground;
        public string AdjustAdClickEventToken => adjustAdClickEventToken;
        public string AdjustAdRevenueEventToken => adjustAdRevenueEventToken;
        public string AdjustInterstitialFinishedEventToken => adjustInterstitialFinishedEventToken;

        public static IntegrationSettings Load()
        {
            var settings = Resources.Load<IntegrationSettings>(ResourcesPath);
            return settings != null ? settings : CreateInstance<IntegrationSettings>();
        }

        public IEnumerable<PlatformAdUnit> GetActiveInterstitialUnits()
        {
            return activeAdNetwork == AdNetwork.AdMob ? admobInterstitials : appLovinInterstitials;
        }

        public IEnumerable<PlatformAdUnit> GetActiveRewardedUnits()
        {
            return activeAdNetwork == AdNetwork.AdMob ? admobRewarded : appLovinRewarded;
        }
    }
}
