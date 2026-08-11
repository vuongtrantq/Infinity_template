using AdjustSdk;
using UnityEngine;

namespace PartnerIntegration
{
    public static class AdjustTracker
    {
        private static IntegrationSettings settings;
        private static bool initialized;
        private static bool warnedMissingInitialization;

        public static bool IsInitialized => initialized;

        public static void Initialize(IntegrationSettings integrationSettings)
        {
            settings = integrationSettings;
            initialized = false;

            if (settings == null || !settings.InitializeAdjust || string.IsNullOrWhiteSpace(settings.AdjustAppToken))
            {
                initialized = HasConfiguredAdjustComponent();
                if (initialized)
                {
                    Debug.Log("[IntegrationPackage] Adjust initialized by scene Adjust component. Ad revenue will use existing Adjust SDK instance.");
                    return;
                }

                if (settings != null && settings.InitializeAdjust && string.IsNullOrWhiteSpace(settings.AdjustAppToken))
                {
                    Debug.LogWarning("[IntegrationPackage] Adjust skipped: app token is empty and no configured Adjust component was found. Ad revenue will not be sent to Adjust.");
                }
                return;
            }

            var config = new AdjustConfig(settings.AdjustAppToken, settings.AdjustEnvironment, settings.AdjustLogLevel == AdjustLogLevel.Suppress);
            config.LogLevel = settings.AdjustLogLevel;
            config.IsSendingInBackgroundEnabled = settings.AdjustSendInBackground;
            Adjust.InitSdk(config);
            initialized = true;
        }

        public static void TrackEvent(string eventToken)
        {
            if (string.IsNullOrWhiteSpace(eventToken))
            {
                return;
            }

            Adjust.TrackEvent(new AdjustEvent(eventToken));
        }

        public static void TrackPurchase(string eventToken, double revenue, string currency, string transactionId = null)
        {
            if (string.IsNullOrWhiteSpace(eventToken))
            {
                return;
            }

            var adjustEvent = new AdjustEvent(eventToken);
            adjustEvent.SetRevenue(revenue, currency);

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                adjustEvent.TransactionId = transactionId;
            }

            Adjust.TrackEvent(adjustEvent);
        }

        public static void TrackConfiguredAdClick()
        {
            TrackEvent(settings != null ? settings.AdjustAdClickEventToken : null);
        }

        public static void TrackConfiguredAdRevenueEvent()
        {
            TrackEvent(settings != null ? settings.AdjustAdRevenueEventToken : null);
        }

        public static void TrackConfiguredInterstitialFinished()
        {
            TrackEvent(settings != null ? settings.AdjustInterstitialFinishedEventToken : null);
        }

        public static void TrackAdRevenue(string source, double revenue, string currency, string network = null, string unit = null, string placement = null)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            if (!initialized && !HasConfiguredAdjustComponent())
            {
                if (!warnedMissingInitialization)
                {
                    warnedMissingInitialization = true;
                    Debug.LogWarning("[IntegrationPackage] Adjust ad revenue skipped: Adjust is not initialized.");
                }
                return;
            }

            var adRevenue = new AdjustAdRevenue(source);
            adRevenue.SetRevenue(revenue, string.IsNullOrWhiteSpace(currency) ? "USD" : currency);

            if (!string.IsNullOrWhiteSpace(network))
            {
                adRevenue.AdRevenueNetwork = network;
            }

            if (!string.IsNullOrWhiteSpace(unit))
            {
                adRevenue.AdRevenueUnit = unit;
            }

            if (!string.IsNullOrWhiteSpace(placement))
            {
                adRevenue.AdRevenuePlacement = placement;
            }

            Adjust.TrackAdRevenue(adRevenue);
            TrackConfiguredAdRevenueEvent();
            Debug.Log("[IntegrationPackage] Adjust ad revenue tracked: " + source + " " + revenue + " " + (string.IsNullOrWhiteSpace(currency) ? "USD" : currency) + " unit=" + unit + " placement=" + placement);
        }

        private static bool HasConfiguredAdjustComponent()
        {
#if UNITY_2023_1_OR_NEWER
            var adjust = Object.FindFirstObjectByType<Adjust>();
#else
            var adjust = Object.FindObjectOfType<Adjust>();
#endif
            return adjust != null && !adjust.startManually && !string.IsNullOrWhiteSpace(adjust.appToken);
        }
    }
}
