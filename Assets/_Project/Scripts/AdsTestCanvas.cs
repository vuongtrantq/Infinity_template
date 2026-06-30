using PartnerIntegration;
using UnityEngine;
using UnityEngine.UI;

public class AdsTestCanvas : MonoBehaviour
{
    [SerializeField] private string interstitialKey = "Inter_play_time";
    [SerializeField] private string rewardedKey = "Reward_Revive";
    [SerializeField] private Text statusText;
    [SerializeField] private bool autoBindButtons = true;

    private void Awake()
    {
        EnsureIntegrationBootstrap();

        if (statusText == null)
        {
            var texts = GetComponentsInChildren<Text>(true);
            if (texts.Length > 0)
            {
                statusText = texts[texts.Length - 1];
            }
        }

        if (autoBindButtons)
        {
            BindButtonsByName();
        }

        SetStatus("Ready");
    }

    public void ShowBanner()
    {
        IntegrationBootstrap.Instance.ShowBanner();
        SetStatus("Show banner requested");
    }

    public void HideBanner()
    {
        IntegrationBootstrap.Instance.HideBanner();
        SetStatus("Hide banner requested");
    }

    public void ShowInterstitial()
    {
        IntegrationBootstrap.Instance.ShowInterstitial(
            interstitialKey,
            () => SetStatus("Interstitial closed"),
            () => SetStatus("Interstitial not ready / failed"),
            false);

        SetStatus("Interstitial requested");
    }

    public void ShowReward()
    {
        IntegrationBootstrap.Instance.ShowRewarded(rewardedKey, success =>
        {
            SetStatus("Reward result: " + success);
        });

        SetStatus("Reward requested");
    }

    public void ShowAppOpen()
    {
        var shown = IntegrationBootstrap.Instance.ShowAppOpenIfAvailable();
        SetStatus("App open shown: " + shown);
    }

    public void LogAnalytics()
    {
        AnalyticsTracker.LogEvent("home_test_button_click", "button", "analytics");
        SetStatus("Analytics event logged");
    }

    public void LogAdjust()
    {
        AdjustTracker.TrackConfiguredAdClick();
        SetStatus("Adjust event requested");
    }

    private static void EnsureIntegrationBootstrap()
    {
        if (IntegrationBootstrap.Instance != null)
        {
            return;
        }

        var bootstrap = new GameObject("IntegrationBootstrap");
        bootstrap.AddComponent<IntegrationBootstrap>();
    }

    private void BindButtonsByName()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var normalizedName = Normalize(button.gameObject.name);

            if (normalizedName.Contains("showbanner"))
            {
                Bind(button, ShowBanner);
            }
            else if (normalizedName.Contains("hidebanner"))
            {
                Bind(button, HideBanner);
            }
            else if (normalizedName.Contains("showinter"))
            {
                Bind(button, ShowInterstitial);
            }
            else if (normalizedName.Contains("showreward"))
            {
                Bind(button, ShowReward);
            }
            else if (normalizedName.Contains("showappopen"))
            {
                Bind(button, ShowAppOpen);
            }
            else if (normalizedName.Contains("loganalytics"))
            {
                Bind(button, LogAnalytics);
            }
            else if (normalizedName.Contains("logadjust"))
            {
                Bind(button, LogAdjust);
            }
        }
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log("[AdsTestCanvas] " + message);
    }
}
