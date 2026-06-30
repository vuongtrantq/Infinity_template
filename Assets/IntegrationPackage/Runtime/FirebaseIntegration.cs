using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using Firebase.RemoteConfig;
using UnityEngine;

namespace PartnerIntegration
{
    public static class FirebaseIntegration
    {
        public static bool IsReady { get; private set; }
        public static bool IsRemoteConfigReady { get; private set; }

        public static async Task InitializeAsync(IntegrationSettings settings)
        {
            IsReady = false;
            IsRemoteConfigReady = false;

            if (settings == null || !settings.InitializeFirebase)
            {
                return;
            }

            DependencyStatus status;
            try
            {
                status = await FirebaseApp.CheckAndFixDependenciesAsync();
            }
            catch (Exception exception)
            {
                Debug.LogError("[IntegrationPackage] Firebase dependency check failed: " + exception.Message);
                return;
            }

            if (status != DependencyStatus.Available)
            {
                Debug.LogError("[IntegrationPackage] Firebase dependencies unavailable: " + status);
                return;
            }

            try
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }
            catch (Exception exception)
            {
                Debug.LogError("[IntegrationPackage] Firebase Analytics initialization failed: " + exception.Message);
                return;
            }

            IsReady = true;

            if (settings.FetchRemoteConfig)
            {
                await FetchAndApplyRemoteConfig(settings);
            }
        }

        private static async Task FetchAndApplyRemoteConfig(IntegrationSettings settings)
        {
            try
            {
                var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                await remoteConfig.SetDefaultsAsync(BuildDefaults(settings));
                await remoteConfig.FetchAsync(TimeSpan.FromSeconds(settings.RemoteConfigCacheSeconds));
                if (remoteConfig.Info.LastFetchStatus == LastFetchStatus.Success)
                {
                    await remoteConfig.ActivateAsync();
                }

                ApplyRemoteValues(settings, remoteConfig);
                IsRemoteConfigReady = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[IntegrationPackage] Remote Config fetch failed: " + exception.Message);
            }
        }

        private static Dictionary<string, object> BuildDefaults(IntegrationSettings settings)
        {
            var defaults = new Dictionary<string, object>();

            AddDefault(defaults, settings.RemoteUseAdMobKey, settings.ActiveAdNetwork == AdNetwork.AdMob);
            AddDefault(defaults, settings.RemoteShowAppOpenKey, settings.ShowAppOpen);
            AddDefault(defaults, settings.RemoteInterstitialIntervalKey, settings.InterstitialIntervalSeconds);

            AddAdUnitDefault(defaults, settings.AdMobBanner);
            AddAdUnitDefault(defaults, settings.AdMobAppOpen);
            AddAdUnitDefaults(defaults, settings.AdMobInterstitials);
            AddAdUnitDefaults(defaults, settings.AdMobRewarded);
            AddAdUnitDefault(defaults, settings.AppLovinBanner);
            AddAdUnitDefault(defaults, settings.AppLovinAppOpen);
            AddAdUnitDefaults(defaults, settings.AppLovinInterstitials);
            AddAdUnitDefaults(defaults, settings.AppLovinRewarded);

            return defaults;
        }

        private static void ApplyRemoteValues(IntegrationSettings settings, FirebaseRemoteConfig remoteConfig)
        {
            if (TryGetBool(remoteConfig, settings.RemoteUseAdMobKey, out var useAdMob))
            {
                settings.ActiveAdNetwork = useAdMob ? AdNetwork.AdMob : AdNetwork.AppLovinMax;
            }

            if (TryGetBool(remoteConfig, settings.RemoteShowAppOpenKey, out var showAppOpen))
            {
                settings.ShowAppOpen = showAppOpen;
            }

            if (TryGetFloat(remoteConfig, settings.RemoteInterstitialIntervalKey, out var interval))
            {
                settings.InterstitialIntervalSeconds = interval;
            }

            ApplyAdUnitRemoteValue(remoteConfig, settings.AdMobBanner);
            ApplyAdUnitRemoteValue(remoteConfig, settings.AdMobAppOpen);
            ApplyAdUnitRemoteValues(remoteConfig, settings.AdMobInterstitials);
            ApplyAdUnitRemoteValues(remoteConfig, settings.AdMobRewarded);
            ApplyAdUnitRemoteValue(remoteConfig, settings.AppLovinBanner);
            ApplyAdUnitRemoteValue(remoteConfig, settings.AppLovinAppOpen);
            ApplyAdUnitRemoteValues(remoteConfig, settings.AppLovinInterstitials);
            ApplyAdUnitRemoteValues(remoteConfig, settings.AppLovinRewarded);
        }

        private static void AddDefault(Dictionary<string, object> defaults, string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key) && !defaults.ContainsKey(key))
            {
                defaults.Add(key, value);
            }
        }

        private static void AddAdUnitDefaults(Dictionary<string, object> defaults, IReadOnlyList<PlatformAdUnit> units)
        {
            if (units == null)
            {
                return;
            }

            for (var i = 0; i < units.Count; i++)
            {
                AddAdUnitDefault(defaults, units[i]);
            }
        }

        private static void AddAdUnitDefault(Dictionary<string, object> defaults, PlatformAdUnit unit)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.CurrentRemoteConfigKey))
            {
                return;
            }

            AddDefault(defaults, unit.CurrentRemoteConfigKey, unit.CurrentId ?? string.Empty);
        }

        private static void ApplyAdUnitRemoteValues(FirebaseRemoteConfig remoteConfig, IReadOnlyList<PlatformAdUnit> units)
        {
            if (units == null)
            {
                return;
            }

            for (var i = 0; i < units.Count; i++)
            {
                ApplyAdUnitRemoteValue(remoteConfig, units[i]);
            }
        }

        private static void ApplyAdUnitRemoteValue(FirebaseRemoteConfig remoteConfig, PlatformAdUnit unit)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.CurrentRemoteConfigKey))
            {
                return;
            }

            var value = remoteConfig.GetValue(unit.CurrentRemoteConfigKey).StringValue;
            if (!string.IsNullOrWhiteSpace(value))
            {
                unit.SetCurrentPlatformId(value);
            }
        }

        private static bool TryGetBool(FirebaseRemoteConfig remoteConfig, string key, out bool value)
        {
            value = false;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var configValue = remoteConfig.GetValue(key);
            if (configValue.BooleanValue)
            {
                value = true;
                return true;
            }

            return bool.TryParse(configValue.StringValue, out value);
        }

        private static bool TryGetFloat(FirebaseRemoteConfig remoteConfig, string key, out float value)
        {
            value = 0f;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var configValue = remoteConfig.GetValue(key);
            if (Math.Abs(configValue.DoubleValue) > double.Epsilon)
            {
                value = (float)configValue.DoubleValue;
                return true;
            }

            return float.TryParse(configValue.StringValue, out value);
        }
    }
}
