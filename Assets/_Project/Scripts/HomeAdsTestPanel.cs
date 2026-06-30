using PartnerIntegration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Base.Home
{
    public class HomeAdsTestPanel : MonoBehaviour
    {
        [SerializeField] private string interstitialKey = "Inter_play_time";
        [SerializeField] private string rewardedKey = "Reward_Revive";

        private Text statusText;

        private void Awake()
        {
            EnsureIntegrationBootstrap();
            EnsureEventSystem();
            CreateUi();
        }

        private void EnsureIntegrationBootstrap()
        {
            if (IntegrationBootstrap.Instance != null)
            {
                return;
            }

            var bootstrap = new GameObject("IntegrationBootstrap");
            bootstrap.AddComponent<IntegrationBootstrap>();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void CreateUi()
        {
            var canvas = new GameObject("Ads Test Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 100;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = CreatePanel(canvas.transform);
            CreateTitle(panel.transform);
            CreateButton(panel.transform, "Show Banner", () =>
            {
                IntegrationBootstrap.Instance.ShowBanner();
                SetStatus("Show banner requested");
            });

            CreateButton(panel.transform, "Hide Banner", () =>
            {
                IntegrationBootstrap.Instance.HideBanner();
                SetStatus("Hide banner requested");
            });

            CreateButton(panel.transform, "Show Inter", () =>
            {
                IntegrationBootstrap.Instance.ShowInterstitial(
                    interstitialKey,
                    () => SetStatus("Interstitial closed"),
                    () => SetStatus("Interstitial not ready / failed"),
                    false);
                SetStatus("Interstitial requested");
            });

            CreateButton(panel.transform, "Show Reward", () =>
            {
                IntegrationBootstrap.Instance.ShowRewarded(rewardedKey, success => SetStatus("Reward result: " + success));
                SetStatus("Reward requested");
            });

            CreateButton(panel.transform, "Show App Open", () =>
            {
                var shown = IntegrationBootstrap.Instance.ShowAppOpenIfAvailable();
                SetStatus("App open shown: " + shown);
            });

            CreateButton(panel.transform, "Log Analytics", () =>
            {
                AnalyticsTracker.LogEvent("home_test_button_click", "button", "analytics");
                SetStatus("Analytics event logged");
            });

            CreateButton(panel.transform, "Log Adjust", () =>
            {
                AdjustTracker.TrackConfiguredAdClick();
                SetStatus("Adjust event requested");
            });

            statusText = CreateText(panel.transform, "Ready", 32, TextAnchor.MiddleCenter);
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(760f, 80f);
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("Ads Test Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(840f, 1040f);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.05f, 0.09f, 0.13f, 0.88f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 36, 36);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private static void CreateTitle(Transform parent)
        {
            var title = CreateText(parent, "Home Ads Test", 46, TextAnchor.MiddleCenter);
            title.color = Color.white;
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 80f);
        }

        private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(760f, 92f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.42f, 0.80f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var text = CreateText(buttonObject.transform, label, 34, TextAnchor.MiddleCenter);
            text.color = Color.white;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string value, int size, TextAnchor alignment)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = size;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(0.84f, 0.91f, 1f, 1f);

            return text;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log("[HomeAdsTestPanel] " + message);
        }
    }
}
