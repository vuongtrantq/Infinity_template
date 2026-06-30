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

        public static void LogAdMobPaidImpression(double value, string currency, string adUnitId, string placement)
        {
            LogEvent(PaidAdImpression,
                new Parameter("ad_platform", "AdMob"),
                new Parameter("ad_source", "admob"),
                new Parameter("ad_unit_name", adUnitId ?? string.Empty),
                new Parameter("ad_placement", placement ?? string.Empty),
                new Parameter("value", value),
                new Parameter("currency", string.IsNullOrWhiteSpace(currency) ? "USD" : currency));
        }

        public static void LogMaxPaidImpression(double value, string currency, string network, string adUnitId, string placement)
        {
            LogEvent(AdImpression,
                new Parameter("ad_platform", "AppLovin MAX"),
                new Parameter("ad_source", network ?? string.Empty),
                new Parameter("ad_unit_name", adUnitId ?? string.Empty),
                new Parameter("ad_placement", placement ?? string.Empty),
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
