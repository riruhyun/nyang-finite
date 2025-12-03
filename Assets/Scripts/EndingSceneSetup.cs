using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ending 씬 전용 연출 셋업. OpeningSceneManager를 재활용하여
/// 엔딩 일러스트/텍스트를 순차적으로 재생한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class EndingSceneSetup : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string endingSceneName = "Ending";
    [SerializeField] private bool allowSkipDuringScenes = true;
    [SerializeField] private bool requireClickAfterEachScene = false;

    [Header("Completion Settings")]
    [SerializeField] private bool loadNextSceneOnComplete = true;
    [SerializeField] private string nextSceneName = "Lobby";
    [SerializeField] private float loadDelay = 0f;

    [Header("Sprite References")]
    [SerializeField] private Sprite endingScene1Sprite;
    [SerializeField] private Sprite endingScene2Sprite;
    [SerializeField] private Sprite endingScene3Sprite;

    [Header("Final Fade")]
    [SerializeField] private float finalFadeDuration = 1f;

    private OpeningSceneManager manager;

    private void Awake()
    {
        manager = GetComponent<OpeningSceneManager>();
        if (manager == null)
        {
            manager = FindFirstObjectByType<OpeningSceneManager>();
        }
        if (manager == null)
        {
            var go = new GameObject("OpeningSceneManager");
            manager = go.AddComponent<OpeningSceneManager>();
        }
    }

    private void Start()
    {
        if (manager == null)
        {
            Debug.LogError("[EndingSceneSetup] OpeningSceneManager missing.");
            return;
        }

        var activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeScene, endingSceneName))
        {
            return;
        }

        manager.ResetOpening();
        ConfigureManagerDefaults();
        SetupEndingSequence();
        manager.StartOpening();
    }

    private void ConfigureManagerDefaults()
    {
        manager.allowClickToAdvance = allowSkipDuringScenes;
        manager.requireClickAfterSceneComplete = requireClickAfterEachScene;
        manager.loadOnComplete = loadNextSceneOnComplete;
        manager.nextSceneName = nextSceneName;
        manager.loadDelay = loadDelay;
        manager.finalSceneWaitOverride = Mathf.Max(0f, finalFadeDuration);
        manager.blockFinalSceneSkip = true;
    }

    private void SetupEndingSequence()
    {
        EnsureSpriteHolder("ending_scene1", endingScene1Sprite);
        EnsureSpriteHolder("ending_scene2", endingScene2Sprite);
        EnsureSpriteHolder("ending_scene3", endingScene3Sprite);

        var scene1 = manager.AddScene(1);
        scene1.SetDuration(2f);
        scene1.RequireClickAfterComplete(false);
        scene1.AddImage("ending_scene1", new Vector3(-2.5f, 0.1f, 0f), 3)
            .Scale(0.85f)
            .ImageFadeIn(1.5f)
            .MoveTo(new Vector3(-1.5f, 0.1f, 0f), 1.5f)
            .FadeOutAtScene(2, 0.5f);

        var scene2 = manager.AddScene(2);
        scene2.SetDuration(2.5f);
        scene2.RequireClickAfterComplete(false);
        scene2.AutoAdvanceAfterComplete(0.5f);
        scene2.AddImage("ending_scene2", new Vector3(1f, 0.5f, 0f), 4)
            .Scale(1.1f)
            .ImageFadeIn(1.5f)
            .MoveTo(new Vector3(0f, 0.5f, 0f), 1.5f)
            .FadeOutAtScene(3, 0.5f);

        var scene3 = manager.AddScene(3);
        scene3.SetDuration(0f);
        scene3.RequireClickAfterComplete(true);
        scene3.AddImage("ending_scene3", new Vector3(0f, 0f, 0f), 5)
            .Scale(1.2f)
            .ImageFadeIn(1.5f)
            .FadeOutAtScene(4, Mathf.Max(0.1f, finalFadeDuration));

        var scene4 = manager.AddScene(4);
        scene4.SetDuration(Mathf.Max(0.1f, finalFadeDuration));
        scene4.RequireClickAfterComplete(false);
        scene4.AutoAdvanceAfterComplete(0f);
    }

    private void EnsureSpriteHolder(string key, Sprite sprite)
    {
        if (string.IsNullOrEmpty(key) || sprite == null) return;
        var existing = FindInSceneIncludingInactive(key);
        if (existing != null) return;

        var go = new GameObject(key, typeof(SpriteRenderer));
        go.transform.SetParent(transform);
        go.SetActive(false);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 0;
    }

    private GameObject FindInSceneIncludingInactive(string targetName)
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var result = FindChildByName(root.transform, targetName);
            if (result != null) return result.gameObject;
        }
        return null;
    }

    private Transform FindChildByName(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var res = FindChildByName(child, targetName);
            if (res != null) return res;
        }
        return null;
    }
}
