using System.Collections;
using PartnerIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Base.Launcher
{
    public class LoadingManager : MonoBehaviour
    {
        [Header("Launcher")]
        [SerializeField] private string homeSceneName = "Home";
        [SerializeField] private float minimumLoadingSeconds = 1.5f;
        [SerializeField] private float maxInitWaitSeconds = 8f;
        [SerializeField] private bool logExampleEvents = true;

        [Header("Optional UI")]
        [SerializeField] private Image progressBar;

        private void Awake()
        {
            StartCoroutine(InitializeAndLoadHome());
        }

        private IEnumerator InitializeAndLoadHome()
        {
            var bootstrap = EnsureIntegrationBootstrap();
            var startTime = Time.realtimeSinceStartup;

            while (!bootstrap.IsInitialized && Time.realtimeSinceStartup - startTime < maxInitWaitSeconds)
            {
                UpdateProgress(startTime, maxInitWaitSeconds);
                yield return null;
            }

            if (logExampleEvents)
            {
                AnalyticsTracker.LogEvent("launcher_integration_initialized");
                AdjustTracker.TrackConfiguredAdClick();
            }

            while (Time.realtimeSinceStartup - startTime < minimumLoadingSeconds)
            {
                UpdateProgress(startTime, minimumLoadingSeconds);
                yield return null;
            }

            UpdateProgress(startTime, 0f, 1f);
            SceneManager.LoadScene(homeSceneName);
        }

        private static IntegrationBootstrap EnsureIntegrationBootstrap()
        {
            if (IntegrationBootstrap.Instance != null)
            {
                return IntegrationBootstrap.Instance;
            }

            var gameObject = new GameObject("IntegrationBootstrap");
            return gameObject.AddComponent<IntegrationBootstrap>();
        }

        private void UpdateProgress(float startTime, float duration, float forcedValue = -1f)
        {
            if (progressBar == null)
            {
                return;
            }

            progressBar.fillAmount = forcedValue >= 0f
                ? forcedValue
                : Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / Mathf.Max(0.01f, duration));
        }
    }
}
