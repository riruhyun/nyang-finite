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

    [Header("Respawn Fade Settings")]
    [Tooltip("리스폰 시 검은 화면을 유지할 시간 (초)")]
    [SerializeField] private float blackScreenDuration = 1f;

    private bool isFading;
    private bool pendingSceneFadeOut;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 이미 ScreenFadeController가 존재하면 현재 GameObject를 파괴
            Debug.Log($"[ScreenFadeController] 중복 인스턴스 발견, 파괴: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[ScreenFadeController] Awake: Instance 설정, DontDestroyOnLoad 적용");
        Instance = this;

        // 이 Canvas를 DontDestroyOnLoad로 설정하여 씬 전환 시에도 유지
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[ScreenFadeController] CanvasGroup reference missing.");
        }
        else
        {
            Debug.Log($"[ScreenFadeController] CanvasGroup 초기 참조 설정 완료");
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
        Debug.Log($"[ScreenFadeController] Start 호출, fadeOnSceneStart={fadeOnSceneStart}");

        // 씬 시작 시에도 CanvasGroup 참조 확인
        RefindCanvasGroup();

        if (fadeOnSceneStart)
        {
            Debug.Log("[ScreenFadeController] Start에서 Fade-In 시작 (첫 씬 시작)");
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
        Debug.Log($"[ScreenFadeController] HandleSceneLoaded 호출: {scene.name}, pendingSceneFadeOut={pendingSceneFadeOut}");

        // 씬에 중복된 ScreenFadeCanvas가 있으면 제거 (DontDestroyOnLoad와 충돌 방지)
        RemoveDuplicateCanvases();

        // 씬이 로드될 때마다 CanvasGroup 참조를 다시 찾음 (씬 재시작 시 Canvas가 재생성되므로)
        RefindCanvasGroup();

        if (!pendingSceneFadeOut)
        {
            Debug.Log("[ScreenFadeController] pendingSceneFadeOut=false, Fade-In 스킵");
            return;
        }

        Debug.Log($"[ScreenFadeController] 씬 재시작 후 검은 화면 {blackScreenDuration}초 대기 후 Fade-In");
        pendingSceneFadeOut = false;
        SetAlpha(1f);
        StartCoroutine(WaitAndFadeIn());
    }

    /// <summary>
    /// 검은 화면을 일정 시간 유지한 후 Fade-In을 시작합니다.
    /// </summary>
    private IEnumerator WaitAndFadeIn()
    {
        // 검은 화면 유지
        yield return new WaitForSecondsRealtime(blackScreenDuration);

        Debug.Log("[ScreenFadeController] 검은 화면 대기 완료, Fade-In 시작");
        // Fade-In 시작
        yield return FadeToAlpha(0f, true);
    }

    /// <summary>
    /// 씬에 중복된 ScreenFadeCanvas를 제거합니다.
    /// DontDestroyOnLoad로 유지된 Canvas와 씬에 배치된 Canvas의 충돌을 방지합니다.
    /// </summary>
    private void RemoveDuplicateCanvases()
    {
        // 모든 ScreenFadeCanvas 찾기
        ScreenFadeController[] allControllers = FindObjectsOfType<ScreenFadeController>();
        Debug.Log($"[ScreenFadeController] 씬에서 찾은 ScreenFadeController 개수: {allControllers.Length}");

        int removedCount = 0;
        foreach (ScreenFadeController controller in allControllers)
        {
            // 현재 인스턴스가 아니고, 다른 ScreenFadeController가 있으면 파괴
            if (controller != this && controller.gameObject != this.gameObject)
            {
                Debug.LogWarning($"[ScreenFadeController] 중복된 ScreenFadeCanvas 발견, 즉시 파괴: {controller.gameObject.name}");
                DestroyImmediate(controller.gameObject);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            Debug.Log($"[ScreenFadeController] {removedCount}개의 중복 Canvas 제거 완료");
        }
    }

    private IEnumerator FadeToAlpha(float targetAlpha, bool clearFadingFlagWhenComplete)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[ScreenFadeController] fadeCanvasGroup이 null입니다! Fade 불가능");
            yield break;
        }

        float start = fadeCanvasGroup.alpha;
        Debug.Log($"[ScreenFadeController] FadeToAlpha 시작: {start} → {targetAlpha}");

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
        Debug.Log($"[ScreenFadeController] FadeToAlpha 완료: alpha={targetAlpha}");

        if (clearFadingFlagWhenComplete && Mathf.Approximately(targetAlpha, 0f))
        {
            isFading = false;
        }
    }

    /// <summary>
    /// 씬이 로드될 때마다 CanvasGroup 참조를 다시 찾습니다.
    /// 씬 재시작 시 Canvas가 재생성되기 때문에 필요합니다.
    /// </summary>
    private void RefindCanvasGroup()
    {
        Debug.Log($"[ScreenFadeController] RefindCanvasGroup 호출, 현재 fadeCanvasGroup={(fadeCanvasGroup == null ? "null" : "not null")}");

        // fadeCanvasGroup이 파괴된 객체를 가리킬 수 있으므로 항상 다시 체크
        // Unity의 null 체크는 파괴된 객체도 감지함
        if (fadeCanvasGroup != null && fadeCanvasGroup.gameObject != null)
        {
            // 유효한 참조가 있으면 스킵
            Debug.Log($"[ScreenFadeController] fadeCanvasGroup 이미 유효함, alpha={fadeCanvasGroup.alpha}");
            return;
        }

        Debug.Log("[ScreenFadeController] fadeCanvasGroup을 다시 찾는 중...");

        // 현재 GameObject(ScreenFadeCanvas)에서 CanvasGroup 찾기
        fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[ScreenFadeController] CanvasGroup을 찾을 수 없습니다! FadeImage 자식 오브젝트에 CanvasGroup이 있는지 확인하세요.");
        }
        else
        {
            Debug.Log($"[ScreenFadeController] CanvasGroup 참조를 찾았습니다! alpha={fadeCanvasGroup.alpha}");
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
        Debug.Log($"[ScreenFadeController] RespawnWithFade 호출, isFading={isFading}");

        if (isFading)
        {
            Debug.LogWarning("[ScreenFadeController] 이미 페이드 중이므로 리스폰 무시");
            return;
        }

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[ScreenFadeController] 현재 씬: {currentSceneName}");
        StartCoroutine(RespawnSequence(currentSceneName, waitBeforeFade));
    }

    private IEnumerator RespawnSequence(string sceneName, float waitBeforeFade)
    {
        Debug.Log($"[ScreenFadeController] RespawnSequence 시작: {waitBeforeFade}초 대기");
        isFading = true;

        // 죽고 나서 대기 시간 (페이드 없이 가만히)
        yield return new WaitForSecondsRealtime(waitBeforeFade);

        Debug.Log("[ScreenFadeController] Fade-Out 시작 (화면이 검어짐)");
        // 페이드 아웃 (화면이 검어짐)
        yield return FadeToAlpha(1f, false);

        Debug.Log($"[ScreenFadeController] Fade-Out 완료, 씬 로드: {sceneName}");
        // 씬 리로드 플래그 설정
        pendingSceneFadeOut = true;

        // 같은 스테이지 리로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
