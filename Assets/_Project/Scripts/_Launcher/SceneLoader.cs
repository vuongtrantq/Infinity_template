using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Base.Launcher
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private bool isDontDestroyOnLoad = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (isDontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public Coroutine ChangeScene(string sceneName)
        {
            return StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Single));
        }

        public Coroutine LoadSceneAdditive(string sceneName)
        {
            return StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Additive));
        }

        private static IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
