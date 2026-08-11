using Firebase.Analytics;
using UnityEngine;

namespace PartnerIntegration
{
    public static class AnalyticsTracker
    {
        public const string PlayerWatchInter = "player_watch_inter";
        public const string PlayerWatchVideoSuccess = "player_watch_video_success";
        public const string AdRewardImpression = "ad_reward_impression";
        public const string AdRewardRequest = "ad_reward_request";
        public const string AdInterstitialImpression = "ad_interstital_impression";
        public const string AdInterstitialRequest = "ad_interstital_request";
        public const string PaidAdImpression = "paid_ad_impression";
        public const string AdImpression = "ad_impression";
        public const string AdImpressionMax = "ad_impression_max";

        public static void LogEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            TryLog(() => FirebaseAnalytics.LogEvent(eventName));
        }

        public static void LogEvent(string eventName, string parameterName, string parameterValue)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            TryLog(() => FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue));
        }

        public static void LogEvent(string eventName, params Parameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            TryLog(() => FirebaseAnalytics.LogEvent(eventName, parameters));
        }

        public static void LogAdMobPaidImpression(double value, long valueMicros, int precision, string currency, string adUnitId, string placement)
        {
            LogEvent(PaidAdImpression,
                new Parameter("ad_platform", "AdMob"),
                new Parameter("ad_source", "admob"),
                new Parameter("ad_unit_name", adUnitId ?? string.Empty),
                new Parameter("adunitid", adUnitId ?? string.Empty),
                new Parameter("ad_placement", placement ?? string.Empty),
                new Parameter("ad_format", placement ?? string.Empty),
                new Parameter("valuemicros", valueMicros),
                new Parameter("precision", precision),
                new Parameter("value", value),
                new Parameter("adValue", value + (string.IsNullOrWhiteSpace(currency) ? "USD" : currency)),
                new Parameter("currency", string.IsNullOrWhiteSpace(currency) ? "USD" : currency));
        }

        public static void LogMaxPaidImpression(string adFormat, double value, string currency, string network, string adUnitId, string placement)
        {
            LogEvent(AdImpressionMax,
                new Parameter("ad_format", adFormat ?? string.Empty),
                new Parameter("ad_platform", "AppLovin MAX"),
                new Parameter("ad_network", network ?? string.Empty),
                new Parameter("ad_source", network ?? string.Empty),
                new Parameter("ad_unit_id", adUnitId ?? string.Empty),
                new Parameter("ad_unit_name", adUnitId ?? string.Empty),
                new Parameter("placement", placement ?? string.Empty),
                new Parameter("ad_placement", placement ?? string.Empty),
                new Parameter("is_show", 1),
                new Parameter("value", value),
                new Parameter("currency", string.IsNullOrWhiteSpace(currency) ? "USD" : currency));
        }

        public static void LogRewardRequest()
        {
            LogEvent(AdRewardRequest);
        }

        public static void LogRewardImpression()
        {
            LogEvent(AdRewardImpression);
        }

        public static void LogInterstitialRequest()
        {
            LogEvent(AdInterstitialRequest);
        }

        public static void LogInterstitialImpression()
        {
            LogEvent(AdInterstitialImpression);
        }

        public static void LogInterWatched()
        {
            LogEvent(PlayerWatchInter);
        }

        public static void LogRewardWatched(string rewardName)
        {
            LogEvent(PlayerWatchVideoSuccess, "reward", rewardName);
        }

        private static void TryLog(System.Action logAction)
        {
            try
            {
                logAction?.Invoke();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[IntegrationPackage] Firebase Analytics log skipped: " + exception.Message);
            }
        }
    }
}
