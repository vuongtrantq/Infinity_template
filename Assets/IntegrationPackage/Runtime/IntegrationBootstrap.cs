using System;
using PartnerIntegration.Ads;
using UnityEngine;

namespace PartnerIntegration
{
    [DisallowMultipleComponent]
    public sealed class IntegrationBootstrap : MonoBehaviour
    {
        public static IntegrationBootstrap Instance { get; private set; }

        [SerializeField] private IntegrationSettings settings;

        private IAdsProvider adsProvider;
        private bool initializeStarted;
        private float lastInterstitialShowTime = -9999f;

        public IntegrationSettings Settings => settings;
        public bool IsInitialized { get; private set; }
        public bool IsAdShowing { get; internal set; }

        public event Action RewardedLoaded;
        public event Action RewardedRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (settings == null)
            {
                settings = IntegrationSettings.Load();
            }

            if (settings.AutoInitialize)
            {
                Initialize();
            }
        }

        public async void Initialize()
        {
            if (initializeStarted)
            {
                return;
            }

            initializeStarted = true;
            AdjustTracker.Initialize(settings);

            if (settings.InitializeFirebase)
            {
                await FirebaseIntegration.InitializeAsync(settings);
            }

            if (settings.InitializeAds)
            {
                InitializeAds();
            }

            if (settings.InitializeIap)
            {
                IapManager.Initialize(settings);
            }

            IsInitialized = true;
        }

        public void InitializeAds()
        {
            adsProvider = settings.ActiveAdNetwork == AdNetwork.AdMob
                ? new AdMobAdsProvider(this, settings)
                : new AppLovinMaxAdsProvider(this, settings);

            adsProvider.Initialize();
        }

        public void ShowBanner()
        {
            adsProvider?.ShowBanner();
        }

        public void HideBanner()
        {
            adsProvider?.HideBanner();
        }

        public bool IsInterstitialReady(string key = "Inter_play_time")
        {
            return adsProvider != null && adsProvider.IsInterstitialReady(key);
        }

        public void ShowInterstitial(string key = "Inter_play_time", Action onClosed = null, Action onFailed = null, bool enforceInterval = true)
        {
            if (adsProvider == null)
            {
                onFailed?.Invoke();
                return;
            }

            if (enforceInterval && Time.realtimeSinceStartup - lastInterstitialShowTime < settings.InterstitialIntervalSeconds)
            {
                onFailed?.Invoke();
                return;
            }

            if (!adsProvider.IsInterstitialReady(key))
            {
                onFailed?.Invoke();
                return;
            }

            lastInterstitialShowTime = Time.realtimeSinceStartup;
            adsProvider.ShowInterstitial(key, onClosed, onFailed);
        }

        public bool IsRewardedReady(string key = "Reward_Revive")
        {
            return adsProvider != null && adsProvider.IsRewardedReady(key);
        }

        public void ShowRewarded(string key, Action<bool> onClosed)
        {
            if (adsProvider == null)
            {
                onClosed?.Invoke(false);
                return;
            }

            AdjustTracker.TrackConfiguredAdClick();
            AnalyticsTracker.LogRewardRequest();
            adsProvider.ShowRewarded(key, onClosed);
        }

        public bool ShowAppOpenIfAvailable()
        {
            if (adsProvider == null || !settings.ShowAppOpen || IsAdShowing)
            {
                return false;
            }

            return adsProvider.ShowAppOpenIfAvailable();
        }

        public bool IsIapReady()
        {
            return IapManager.IsReady;
        }

        public void PurchaseIap(string key, Action<IapPurchaseResult> onComplete = null)
        {
            IapManager.Purchase(key, onComplete);
        }

        public void RestoreIapPurchases(Action<bool> onComplete = null)
        {
            IapManager.RestorePurchases(onComplete);
        }

        internal void NotifyRewardedLoaded()
        {
            RewardedLoaded?.Invoke();
        }

        internal void NotifyRewardedRequested()
        {
            RewardedRequested?.Invoke();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                ShowAppOpenIfAvailable();
            }
        }
    }
}
