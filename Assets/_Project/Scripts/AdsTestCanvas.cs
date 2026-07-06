using PartnerIntegration;
using UnityEngine;
using UnityEngine.UI;

public class AdsTestCanvas : MonoBehaviour
{
    [SerializeField] private string interstitialKey = "Inter_play_time";
    [SerializeField] private string rewardedKey = "Reward_Revive";
    [SerializeField] private string iapProductKey = "remove_ads";
    [SerializeField] private Text statusText;
    [SerializeField] private bool autoBindButtons = true;
    [SerializeField] private bool autoCreateMissingIapButtons = true;

    private bool hasBuyIapButton;
    private bool hasRestoreIapButton;

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

        if (autoCreateMissingIapButtons)
        {
            CreateMissingIapButtons();
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

    public void BuyIap()
    {
        IntegrationBootstrap.Instance.PurchaseIap(iapProductKey, result =>
        {
            SetStatus(result.Success ? "IAP purchased: " + result.Key : "IAP failed: " + result.Message);
        });

        SetStatus("IAP purchase requested");
    }

    public void RestoreIap()
    {
        IntegrationBootstrap.Instance.RestoreIapPurchases(success =>
        {
            SetStatus("IAP restore result: " + success);
        });

        SetStatus("IAP restore requested");
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
            else if (normalizedName.Contains("buyiap") || normalizedName.Contains("purchaseiap") || normalizedName.Contains("removeads"))
            {
                Bind(button, BuyIap);
                hasBuyIapButton = true;
            }
            else if (normalizedName.Contains("restoreiap") || normalizedName.Contains("restorepurchase"))
            {
                Bind(button, RestoreIap);
                hasRestoreIapButton = true;
            }
        }
    }

    private void CreateMissingIapButtons()
    {
        if (hasBuyIapButton && hasRestoreIapButton)
        {
            return;
        }

        var layout = GetComponentInChildren<VerticalLayoutGroup>(true);
        if (layout == null)
        {
            return;
        }

        var template = layout.GetComponentInChildren<Button>(true);
        if (!hasBuyIapButton)
        {
            CreateButton(layout.transform, template, "Buy IAP", BuyIap);
        }

        if (!hasRestoreIapButton)
        {
            CreateButton(layout.transform, template, "Restore IAP", RestoreIap);
        }

        if (statusText != null && statusText.transform.parent == layout.transform)
        {
            statusText.transform.SetAsLastSibling();
        }
    }

    private static void CreateButton(Transform parent, Button template, string label, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = template != null
            ? template.GetComponent<RectTransform>().sizeDelta
            : new Vector2(760f, 92f);

        var image = buttonObject.GetComponent<Image>();
        image.color = template != null && template.targetGraphic is Image templateImage
            ? templateImage.color
            : new Color(0.12f, 0.42f, 0.80f, 1f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        var text = textObject.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 34;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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
