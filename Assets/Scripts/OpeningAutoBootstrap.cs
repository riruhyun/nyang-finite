using UnityEngine;
using UnityEngine.SceneManagement;

public static class OpeningAutoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnSubsystemRegistration()
    {
        Debug.Log("OpeningAutoBootstrap: SubsystemRegistration");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        Debug.Log("OpeningAutoBootstrap: BeforeSceneLoad");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        Debug.Log("OpeningAutoBootstrap: AfterSceneLoad");
        var scene = SceneManager.GetActiveScene();
        if (IsOpeningScene(scene))
        {
            EnsureSetupAndManager();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void AfterAssembliesLoaded()
    {
        // Subscribe to subsequent scene loads at runtime (Lobby -> Game, etc.)
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("OpeningAutoBootstrap: Subscribed to SceneManager.sceneLoaded");
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"OpeningAutoBootstrap: OnSceneLoaded {scene.name} ({mode})");
        if (IsOpeningScene(scene))
        {
            var manager = Object.FindObjectOfType<OpeningSceneManager>();
            if (manager != null)
            {
                manager.ResetOpening();
            }
            EnsureSetupAndManager();
        }
    }

    private static bool IsOpeningScene(Scene scene)
    {
        // Only enable opening bootstrap in a dedicated Opening scene
        return scene.name == "Opening";
    }

    private static void EnsureSetupAndManager()
    {
        var existing = Object.FindObjectOfType<OpeningSceneSetup>();
        if (existing == null)
        {
            var go = new GameObject("OpeningSceneSetup(Auto)");
            go.AddComponent<OpeningSceneSetup>();
            Debug.Log("OpeningAutoBootstrap: Added OpeningSceneSetup automatically.");
        }
        else
        {
            Debug.Log("OpeningAutoBootstrap: Found OpeningSceneSetup in scene.");
        }

        var manager = Object.FindObjectOfType<OpeningSceneManager>();
        if (manager == null)
        {
            var mgo = new GameObject("OpeningSceneManager(Auto)");
            manager = mgo.AddComponent<OpeningSceneManager>();
            Debug.Log("OpeningAutoBootstrap: Created OpeningSceneManager automatically.");
        }
    }
}
