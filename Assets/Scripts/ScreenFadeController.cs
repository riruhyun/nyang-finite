using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global screen fade controller that lives across scenes and handles fade-in/out transitions.
/// Attach this to a Canvas with a fullscreen Image/CanvasGroup set to black.
/// </summary>
public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool fadeOnSceneStart = true;

    private bool isFading;
    private bool pendingSceneFadeOut;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[ScreenFadeController] CanvasGroup reference missing.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Start()
    {
        if (fadeOnSceneStart)
        {
            SetAlpha(1f);
            StartCoroutine(FadeToAlpha(0f, false));
            // fadeOnSceneStart = false;
        }
        else
        {
            SetAlpha(0f);
        }
    }

    /// <summary>
    /// Trigger a fade-to-black and load the given scene.
    /// </summary>
    public void FadeToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[ScreenFadeController] Scene name is empty.");
            return;
        }

        if (isFading)
        {
            return;
        }

        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        isFading = true;
        yield return FadeToAlpha(1f, false);
        pendingSceneFadeOut = true;
        SceneManager.LoadScene(sceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pendingSceneFadeOut)
        {
            return;
        }

        pendingSceneFadeOut = false;
        SetAlpha(1f);
        StartCoroutine(FadeToAlpha(0f, true));
    }

    private IEnumerator FadeToAlpha(float targetAlpha, bool clearFadingFlagWhenComplete)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float start = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(start, targetAlpha, t);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;

        if (clearFadingFlagWhenComplete && Mathf.Approximately(targetAlpha, 0f))
        {
            isFading = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (fadeCanvasGroup == null) return;
        fadeCanvasGroup.alpha = alpha;
    }

    /// <summary>
    /// 플레이어 사망 시 리스폰 처리: 대기 -> 페이드 아웃 -> 같은 스테이지 리로드 -> 페이드 인
    /// </summary>
    /// <param name="waitBeforeFade">페이드 아웃 전 대기 시간 (기본 3초)</param>
    public void RespawnWithFade(float waitBeforeFade = 3f)
    {
        if (isFading)
        {
            return;
        }

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        StartCoroutine(RespawnSequence(currentSceneName, waitBeforeFade));
    }

    private IEnumerator RespawnSequence(string sceneName, float waitBeforeFade)
    {
        isFading = true;

        // 죽고 나서 대기 시간 (페이드 없이 가만히)
        yield return new WaitForSecondsRealtime(waitBeforeFade);

        // 페이드 아웃 (화면이 검어짐)
        yield return FadeToAlpha(1f, false);

        // 씬 리로드 플래그 설정
        pendingSceneFadeOut = true;

        // 같은 스테이지 리로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
