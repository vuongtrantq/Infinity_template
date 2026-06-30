using System;

namespace PartnerIntegration.Ads
{
    internal interface IAdsProvider
    {
        bool IsInitialized { get; }
        void Initialize();
        void ShowBanner();
        void HideBanner();
        bool IsInterstitialReady(string key);
        void ShowInterstitial(string key, Action onClosed, Action onFailed);
        bool IsRewardedReady(string key);
        void ShowRewarded(string key, Action<bool> onClosed);
        void LoadAppOpen();
        bool ShowAppOpenIfAvailable();
    }
}
