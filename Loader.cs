using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    private readonly Queue<LoadRequest> loadRequestQueue = null;
    private LoadRequest activeLoadRequest = null;
    private Coroutine activeCoroutine = null;
    private Scene? lastLoadedScene = null;

    public class LoadRequest
    {
        public class LoadScene
        {
            public string SceneName { get; set; }
            public Scene LoadedScene { get; set; }
        }
        public List<LoadScene> Scenes
        {
            get; private set;
        }
        public object CompletionEventArg
        {
            get; private set;
        }
        public delegate void RequestCompletedDelegate(LoadRequest loadRequest);
        public RequestCompletedDelegate OnRequestCompleted
        {
            get; set;
        }

        public LoadRequest(object completionEventArg = null)
        {
            Scenes = new List<LoadScene>();
            CompletionEventArg = completionEventArg;
        }
        public void AddScene(string sceneName)
        {
            LoadScene loadScene = new LoadScene
            {
                SceneName = sceneName
            };
            Scenes.Add(loadScene);
        }
    }

    public Loader()
    {
        loadRequestQueue = new Queue<LoadRequest>();
    }

    public void AddLoadRequest(LoadRequest loadRequest)
    {
        loadRequestQueue.Enqueue(loadRequest);
        if (activeCoroutine == null)
        {
            activeCoroutine = StartCoroutine(ExecuteLoadRequests());
        }
    }

    IEnumerator ExecuteLoadRequests()
    {
        while (loadRequestQueue.Count > 0)
        {
            activeLoadRequest = loadRequestQueue.Dequeue();

            foreach (LoadRequest.LoadScene loadScene in activeLoadRequest.Scenes)
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(loadScene.SceneName, LoadSceneMode.Additive);
                op.allowSceneActivation = false;
                while (op.progress < 0.9f)
                {
                    yield return null;
                }
                op.allowSceneActivation = true;
                yield return null; // finish loading scene, OnSceneLoaded will be called
                loadScene.LoadedScene = lastLoadedScene.Value;
                lastLoadedScene = null;
            }
            if (activeLoadRequest.CompletionEventArg != null)
            {
                foreach (LoadRequest.LoadScene loadScene in activeLoadRequest.Scenes)
                {
                    foreach (GameObject go in loadScene.LoadedScene.GetRootGameObjects())
                    {
                        go.SendMessage("OnLoadRequestCompleted", activeLoadRequest.CompletionEventArg);
                    }
                }
            }

            if (activeLoadRequest.OnRequestCompleted != null)
            {
                activeLoadRequest.OnRequestCompleted(activeLoadRequest);
            }
            activeLoadRequest = null;
        }
        activeCoroutine = null;
    }


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastLoadedScene = scene;
    }

    // Use this for initialization
    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
