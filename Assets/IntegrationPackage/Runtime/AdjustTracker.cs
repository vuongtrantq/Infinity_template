using AdjustSdk;
using UnityEngine;

namespace PartnerIntegration
{
    public static class AdjustTracker
    {
        private static IntegrationSettings settings;
        private static bool initialized;

        public static bool IsInitialized => initialized;

        public static void Initialize(IntegrationSettings integrationSettings)
        {
            settings = integrationSettings;
            initialized = false;

            if (settings == null || !settings.InitializeAdjust || string.IsNullOrWhiteSpace(settings.AdjustAppToken))
            {
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
        }
    }
}
