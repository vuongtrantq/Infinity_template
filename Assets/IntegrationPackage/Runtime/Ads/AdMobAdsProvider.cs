using System;
using System.Collections;
using System.Collections.Generic;

using AdjustSdk;
using GoogleMobileAds.Api;
using UnityEngine;

namespace PartnerIntegration.Ads
{
    internal sealed class AdMobAdsProvider : IAdsProvider
    {
        private const double AppOpenExpireHours = 4.0;

        private readonly IntegrationBootstrap owner;
        private readonly IntegrationSettings settings;
        private readonly Dictionary<string, string> interstitialIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> rewardedIds = new Dictionary<string, string>();
        private readonly Dictionary<string, InterstitialAd> interstitialAds = new Dictionary<string, InterstitialAd>();
        private readonly Dictionary<string, RewardedAd> rewardedAds = new Dictionary<string, RewardedAd>();
        private readonly Dictionary<string, bool> interstitialLoaded = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> rewardedLoaded = new Dictionary<string, bool>();
        private readonly Dictionary<string, Action> interstitialClosedCallbacks = new Dictionary<string, Action>();
        private readonly Dictionary<string, Action> interstitialFailedCallbacks = new Dictionary<string, Action>();
        private readonly Dictionary<string, Action<bool>> rewardedCallbacks = new Dictionary<string, Action<bool>>();
        private readonly Dictionary<string, bool> rewardedEarned = new Dictionary<string, bool>();

        private BannerView bannerView;
        private AppOpenAd appOpenAd;
        private DateTime appOpenLoadTime;
        private bool appOpenShowing;

        public AdMobAdsProvider(IntegrationBootstrap owner, IntegrationSettings settings)
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

            CacheAdUnits();
            MobileAds.Initialize(_ =>
            {
                IsInitialized = true;
                CreateBanner();
                LoadInterstitials();
                LoadRewardedAds();
                LoadAppOpen();
            });
        }

        public void ShowBanner()
        {
            bannerView?.Show();
        }

        public void HideBanner()
        {
            bannerView?.Hide();
        }

        public bool IsInterstitialReady(string key)
        {
            return !string.IsNullOrWhiteSpace(key)
                && interstitialAds.ContainsKey(key)
                && interstitialLoaded.ContainsKey(key)
                && interstitialLoaded[key]
                && interstitialAds[key] != null
                && interstitialAds[key].CanShowAd();
        }

        public void ShowInterstitial(string key, Action onClosed, Action onFailed)
        {
            if (!IsInterstitialReady(key))
            {
                onFailed?.Invoke();
                return;
            }

            interstitialClosedCallbacks[key] = onClosed;
            interstitialFailedCallbacks[key] = onFailed;
            interstitialLoaded[key] = false;
            owner.IsAdShowing = true;

            AnalyticsTracker.LogInterstitialImpression();

            try
            {
                interstitialAds[key].Show();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[IntegrationPackage] AdMob interstitial show failed: " + exception.Message);
                owner.IsAdShowing = false;
                onFailed?.Invoke();
                LoadInterstitial(key);
            }
        }

        public bool IsRewardedReady(string key)
        {
            return !string.IsNullOrWhiteSpace(key)
                && rewardedAds.ContainsKey(key)
                && rewardedLoaded.ContainsKey(key)
                && rewardedLoaded[key]
                && rewardedAds[key] != null
                && rewardedAds[key].CanShowAd();
        }

        public void ShowRewarded(string key, Action<bool> onClosed)
        {
            if (!IsRewardedReady(key))
            {
                onClosed?.Invoke(false);
                return;
            }

            rewardedCallbacks[key] = onClosed;
            rewardedEarned[key] = false;
            rewardedLoaded[key] = false;
            owner.IsAdShowing = true;

            AnalyticsTracker.LogRewardImpression();

            try
            {
                rewardedAds[key].Show(_ =>
                {
                    rewardedEarned[key] = true;
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[IntegrationPackage] AdMob rewarded show failed: " + exception.Message);
                owner.IsAdShowing = false;
                onClosed?.Invoke(false);
                LoadRewarded(key);
            }
        }

        public void LoadAppOpen()
        {
            if (!settings.AdMobAppOpen.HasCurrentId || IsAppOpenAvailable())
            {
                return;
            }

            appOpenAd?.Destroy();
            appOpenAd = null;

            AppOpenAd.Load(settings.AdMobAppOpen.CurrentId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[IntegrationPackage] AdMob app-open load failed: " + error);
                    return;
                }

                appOpenAd = ad;
                appOpenLoadTime = DateTime.UtcNow;
                RegisterAppOpenEvents(ad);
            });
        }

        public bool ShowAppOpenIfAvailable()
        {
            if (!IsAppOpenAvailable() || appOpenShowing)
            {
                LoadAppOpen();
                return false;
            }

            owner.IsAdShowing = true;
            appOpenShowing = true;
            appOpenAd.Show();
            return true;
        }

        private void CacheAdUnits()
        {
            interstitialIds.Clear();
            rewardedIds.Clear();

            CacheUnits(settings.AdMobInterstitials, interstitialIds);
            CacheUnits(settings.AdMobRewarded, rewardedIds);
        }

        private static void CacheUnits(IReadOnlyList<PlatformAdUnit> units, Dictionary<string, string> target)
        {
            if (units == null)
            {
                return;
            }

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit != null && unit.HasCurrentId)
                {
                    target[unit.Key] = unit.CurrentId;
                }
            }
        }

        private void CreateBanner()
        {
            if (!settings.AdMobBanner.HasCurrentId)
            {
                return;
            }

            bannerView?.Destroy();

            var position = settings.BannerPosition == IntegrationBannerPosition.Top ? AdPosition.Top : AdPosition.Bottom;
            bannerView = new BannerView(settings.AdMobBanner.CurrentId, AdSize.Banner, position);
            bannerView.OnBannerAdLoaded += () => Debug.Log("[IntegrationPackage] AdMob banner loaded");
            bannerView.OnBannerAdLoadFailed += error => Debug.LogWarning("[IntegrationPackage] AdMob banner failed: " + error);
            bannerView.OnAdPaid += value => HandleAdPaid(value, settings.AdMobBanner.CurrentId, "banner");
            bannerView.LoadAd(new AdRequest());
            bannerView.Hide();
        }

        private void LoadInterstitials()
        {
            foreach (var pair in interstitialIds)
            {
                LoadInterstitial(pair.Key);
            }
        }

        private void LoadInterstitial(string key)
        {
            if (!interstitialIds.TryGetValue(key, out var adUnitId) || string.IsNullOrWhiteSpace(adUnitId))
            {
                return;
            }

            AnalyticsTracker.LogInterstitialRequest();

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    interstitialLoaded[key] = false;
                    Debug.LogWarning("[IntegrationPackage] AdMob interstitial load failed: " + error);
                    owner.StartCoroutine(RetryAfterDelay(() => LoadInterstitial(key), 5f));
                    return;
                }

                if (interstitialAds.TryGetValue(key, out var oldAd))
                {
                    oldAd?.Destroy();
                }

                interstitialAds[key] = ad;
                interstitialLoaded[key] = true;
                RegisterInterstitialEvents(key, adUnitId, ad);
            });
        }

        private void RegisterInterstitialEvents(string key, string adUnitId, InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                owner.IsAdShowing = false;
                AdjustTracker.TrackConfiguredInterstitialFinished();
                AnalyticsTracker.LogInterWatched();

                if (interstitialClosedCallbacks.TryGetValue(key, out var callback))
                {
                    callback?.Invoke();
                }

                LoadInterstitial(key);
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                owner.IsAdShowing = false;
                Debug.LogWarning("[IntegrationPackage] AdMob interstitial display failed: " + error);

                if (interstitialFailedCallbacks.TryGetValue(key, out var callback))
                {
                    callback?.Invoke();
                }

                LoadInterstitial(key);
            };

            ad.OnAdPaid += value => HandleAdPaid(value, adUnitId, key);
        }

        private void LoadRewardedAds()
        {
            foreach (var pair in rewardedIds)
            {
                LoadRewarded(pair.Key);
            }
        }

        private void LoadRewarded(string key)
        {
            if (!rewardedIds.TryGetValue(key, out var adUnitId) || string.IsNullOrWhiteSpace(adUnitId))
            {
                return;
            }

            owner.NotifyRewardedRequested();
            AnalyticsTracker.LogRewardRequest();

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    rewardedLoaded[key] = false;
                    Debug.LogWarning("[IntegrationPackage] AdMob rewarded load failed: " + error);
                    owner.StartCoroutine(RetryAfterDelay(() => LoadRewarded(key), 5f));
                    return;
                }

                if (rewardedAds.TryGetValue(key, out var oldAd))
                {
                    oldAd?.Destroy();
                }

                rewardedAds[key] = ad;
                rewardedLoaded[key] = true;
                owner.NotifyRewardedLoaded();
                RegisterRewardedEvents(key, adUnitId, ad);
            });
        }

        private void RegisterRewardedEvents(string key, string adUnitId, RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                owner.IsAdShowing = false;
                var earned = rewardedEarned.ContainsKey(key) && rewardedEarned[key];

                if (earned)
                {
                    AnalyticsTracker.LogRewardWatched(key);
                }

                if (rewardedCallbacks.TryGetValue(key, out var callback))
                {
                    callback?.Invoke(earned);
                }

                LoadRewarded(key);
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                owner.IsAdShowing = false;
                Debug.LogWarning("[IntegrationPackage] AdMob rewarded display failed: " + error);

                if (rewardedCallbacks.TryGetValue(key, out var callback))
                {
                    callback?.Invoke(false);
                }

                LoadRewarded(key);
            };

            ad.OnAdPaid += value => HandleAdPaid(value, adUnitId, key);
        }

        private void RegisterAppOpenEvents(AppOpenAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                appOpenShowing = true;
                owner.IsAdShowing = true;
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                appOpenShowing = false;
                owner.IsAdShowing = false;
                appOpenAd?.Destroy();
                appOpenAd = null;
                LoadAppOpen();
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                appOpenShowing = false;
                owner.IsAdShowing = false;
                Debug.LogWarning("[IntegrationPackage] AdMob app-open display failed: " + error);
                appOpenAd?.Destroy();
                appOpenAd = null;
                LoadAppOpen();
            };

            ad.OnAdPaid += value => HandleAdPaid(value, settings.AdMobAppOpen.CurrentId, "app_open");
        }

        private bool IsAppOpenAvailable()
        {
            return appOpenAd != null
                && appOpenAd.CanShowAd()
                && DateTime.UtcNow - appOpenLoadTime < TimeSpan.FromHours(AppOpenExpireHours);
        }

        private void HandleAdPaid(AdValue adValue, string adUnitId, string placement)
        {
            var revenue = adValue.Value / 1000000d;
            var currency = string.IsNullOrWhiteSpace(adValue.CurrencyCode) ? "USD" : adValue.CurrencyCode;

            AdjustTracker.TrackAdRevenue(AdjustConfig.AdjustAdRevenueSourceAdMob, revenue, currency, "admob", adUnitId, placement);
            AnalyticsTracker.LogAdMobPaidImpression(revenue, currency, adUnitId, placement);
        }

        private static IEnumerator RetryAfterDelay(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
