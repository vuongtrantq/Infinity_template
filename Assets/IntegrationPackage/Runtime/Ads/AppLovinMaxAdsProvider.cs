using System;
using System.Collections;
using System.Collections.Generic;
using AdjustSdk;
using UnityEngine;

namespace PartnerIntegration.Ads
{
    internal sealed class AppLovinMaxAdsProvider : IAdsProvider
    {
        private readonly IntegrationBootstrap owner;
        private readonly IntegrationSettings settings;
        private readonly Dictionary<string, string> interstitialIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> rewardedIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> interstitialKeyByAdUnit = new Dictionary<string, string>();
        private readonly Dictionary<string, string> rewardedKeyByAdUnit = new Dictionary<string, string>();
        private readonly Dictionary<string, int> interstitialRetryByAdUnit = new Dictionary<string, int>();
        private readonly Dictionary<string, int> rewardedRetryByAdUnit = new Dictionary<string, int>();
        private readonly Dictionary<string, Action> interstitialClosedCallbacks = new Dictionary<string, Action>();
        private readonly Dictionary<string, Action> interstitialFailedCallbacks = new Dictionary<string, Action>();
        private readonly Dictionary<string, Action<bool>> rewardedCallbacks = new Dictionary<string, Action<bool>>();
        private readonly Dictionary<string, bool> rewardedEarned = new Dictionary<string, bool>();

        private bool callbacksRegistered;

        public AppLovinMaxAdsProvider(IntegrationBootstrap owner, IntegrationSettings settings)
        {
            this.owner = owner;
            this.settings = settings;
        }

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.AppLovinSdkKey))
            {
                Debug.LogWarning("[IntegrationPackage] AppLovin MAX SDK key is empty.");
                return;
            }

            CacheAdUnits();
            RegisterCallbacks();

            MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
            {
                IsInitialized = true;
                InitializeBanner();
                LoadInterstitials();
                LoadRewardedAds();
                LoadAppOpen();
            };

            MaxSdk.SetSdkKey(settings.AppLovinSdkKey);
            MaxSdk.InitializeSdk();
        }

        public void ShowBanner()
        {
            if (settings.AppLovinBanner.HasCurrentId)
            {
                MaxSdk.ShowBanner(settings.AppLovinBanner.CurrentId);
            }
        }

        public void HideBanner()
        {
            if (settings.AppLovinBanner.HasCurrentId)
            {
                MaxSdk.HideBanner(settings.AppLovinBanner.CurrentId);
            }
        }

        public bool IsInterstitialReady(string key)
        {
            return interstitialIds.TryGetValue(key, out var adUnitId) && MaxSdk.IsInterstitialReady(adUnitId);
        }

        public void ShowInterstitial(string key, Action onClosed, Action onFailed)
        {
            if (!interstitialIds.TryGetValue(key, out var adUnitId) || !MaxSdk.IsInterstitialReady(adUnitId))
            {
                onFailed?.Invoke();
                return;
            }

            interstitialClosedCallbacks[adUnitId] = onClosed;
            interstitialFailedCallbacks[adUnitId] = onFailed;
            owner.IsAdShowing = true;
            AnalyticsTracker.LogInterstitialImpression();
            MaxSdk.ShowInterstitial(adUnitId);
        }

        public bool IsRewardedReady(string key)
        {
            return rewardedIds.TryGetValue(key, out var adUnitId) && MaxSdk.IsRewardedAdReady(adUnitId);
        }

        public void ShowRewarded(string key, Action<bool> onClosed)
        {
            if (!rewardedIds.TryGetValue(key, out var adUnitId) || !MaxSdk.IsRewardedAdReady(adUnitId))
            {
                onClosed?.Invoke(false);
                return;
            }

            rewardedCallbacks[adUnitId] = onClosed;
            rewardedEarned[adUnitId] = false;
            owner.IsAdShowing = true;
            AnalyticsTracker.LogRewardImpression();
            MaxSdk.ShowRewardedAd(adUnitId);
        }

        public void LoadAppOpen()
        {
            if (settings.AppLovinAppOpen.HasCurrentId)
            {
                MaxSdk.LoadAppOpenAd(settings.AppLovinAppOpen.CurrentId);
            }
        }

        public bool ShowAppOpenIfAvailable()
        {
            if (!settings.AppLovinAppOpen.HasCurrentId)
            {
                return false;
            }

            var adUnitId = settings.AppLovinAppOpen.CurrentId;
            if (!MaxSdk.IsAppOpenAdReady(adUnitId))
            {
                MaxSdk.LoadAppOpenAd(adUnitId);
                return false;
            }

            owner.IsAdShowing = true;
            MaxSdk.ShowAppOpenAd(adUnitId);
            return true;
        }

        private void CacheAdUnits()
        {
            CacheUnits(settings.AppLovinInterstitials, interstitialIds, interstitialKeyByAdUnit);
            CacheUnits(settings.AppLovinRewarded, rewardedIds, rewardedKeyByAdUnit);
        }

        private static void CacheUnits(IReadOnlyList<PlatformAdUnit> units, Dictionary<string, string> byKey, Dictionary<string, string> keyByAdUnit)
        {
            byKey.Clear();
            keyByAdUnit.Clear();

            if (units == null)
            {
                return;
            }

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit != null && unit.HasCurrentId)
                {
                    byKey[unit.Key] = unit.CurrentId;
                    keyByAdUnit[unit.CurrentId] = unit.Key;
                }
            }
        }

        private void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            callbacksRegistered = true;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaid;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaid;

            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaid;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenHidden;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        }

        private void InitializeBanner()
        {
            if (!settings.AppLovinBanner.HasCurrentId)
            {
                return;
            }

            var position = settings.BannerPosition == IntegrationBannerPosition.Top
                ? MaxSdkBase.AdViewPosition.TopCenter
                : MaxSdkBase.AdViewPosition.BottomCenter;

            MaxSdk.CreateBanner(settings.AppLovinBanner.CurrentId, new MaxSdkBase.AdViewConfiguration(position));
            MaxSdk.SetBannerBackgroundColor(settings.AppLovinBanner.CurrentId, Color.black);
            MaxSdk.HideBanner(settings.AppLovinBanner.CurrentId);
        }

        private void LoadInterstitials()
        {
            foreach (var pair in interstitialIds)
            {
                interstitialRetryByAdUnit[pair.Value] = 0;
                LoadInterstitial(pair.Value);
            }
        }

        private void LoadInterstitial(string adUnitId)
        {
            AnalyticsTracker.LogInterstitialRequest();
            MaxSdk.LoadInterstitial(adUnitId);
        }

        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            interstitialRetryByAdUnit[adUnitId] = 0;
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            var retryAttempt = NextRetryAttempt(interstitialRetryByAdUnit, adUnitId);
            owner.StartCoroutine(RetryAfterDelay(() => LoadInterstitial(adUnitId), RetryDelay(retryAttempt)));
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            owner.IsAdShowing = false;

            if (interstitialFailedCallbacks.TryGetValue(adUnitId, out var callback))
            {
                callback?.Invoke();
            }

            LoadInterstitial(adUnitId);
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            owner.IsAdShowing = false;
            AdjustTracker.TrackConfiguredInterstitialFinished();
            AnalyticsTracker.LogInterWatched();

            if (interstitialClosedCallbacks.TryGetValue(adUnitId, out var callback))
            {
                callback?.Invoke();
            }

            LoadInterstitial(adUnitId);
        }

        private void LoadRewardedAds()
        {
            foreach (var pair in rewardedIds)
            {
                rewardedRetryByAdUnit[pair.Value] = 0;
                LoadRewarded(pair.Value);
            }
        }

        private void LoadRewarded(string adUnitId)
        {
            owner.NotifyRewardedRequested();
            AnalyticsTracker.LogRewardRequest();
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            rewardedRetryByAdUnit[adUnitId] = 0;
            owner.NotifyRewardedLoaded();
        }

        private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            var retryAttempt = NextRetryAttempt(rewardedRetryByAdUnit, adUnitId);
            owner.StartCoroutine(RetryAfterDelay(() => LoadRewarded(adUnitId), RetryDelay(retryAttempt)));
        }

        private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            owner.IsAdShowing = false;

            if (rewardedCallbacks.TryGetValue(adUnitId, out var callback))
            {
                callback?.Invoke(false);
            }

            LoadRewarded(adUnitId);
        }

        private void OnRewardedReceivedReward(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            rewardedEarned[adUnitId] = true;
        }

        private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            owner.IsAdShowing = false;
            var earned = rewardedEarned.ContainsKey(adUnitId) && rewardedEarned[adUnitId];

            if (earned)
            {
                var rewardName = rewardedKeyByAdUnit.TryGetValue(adUnitId, out var key) ? key : adUnitId;
                AnalyticsTracker.LogRewardWatched(rewardName);
            }

            if (rewardedCallbacks.TryGetValue(adUnitId, out var callback))
            {
                callback?.Invoke(earned);
            }

            LoadRewarded(adUnitId);
        }

        private void OnAppOpenHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            owner.IsAdShowing = false;
            MaxSdk.LoadAppOpenAd(adUnitId);
        }

        private void OnAppOpenDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            owner.IsAdShowing = false;
            MaxSdk.LoadAppOpenAd(adUnitId);
        }

        private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var revenue = adInfo.Revenue;
            var network = adInfo.NetworkName;
            var placement = string.IsNullOrWhiteSpace(adInfo.Placement) ? adUnitId : adInfo.Placement;

            AdjustTracker.TrackAdRevenue(
                AdjustConfig.AdjustAdRevenueSourceAppLovinMAX,
                revenue,
                "USD",
                network,
                adInfo.AdUnitIdentifier,
                placement);

            AnalyticsTracker.LogMaxPaidImpression(revenue, "USD", network, adInfo.AdUnitIdentifier, placement);
        }

        private static int NextRetryAttempt(Dictionary<string, int> retries, string adUnitId)
        {
            if (!retries.TryGetValue(adUnitId, out var value))
            {
                value = 0;
            }

            value++;
            retries[adUnitId] = value;
            return value;
        }

        private static float RetryDelay(int retryAttempt)
        {
            return Mathf.Pow(2f, Mathf.Min(6, retryAttempt));
        }

        private static IEnumerator RetryAfterDelay(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
