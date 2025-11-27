using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles interaction-based scene transitions (pressE proximity -> VentilationShaft)
/// and a one-time fade-out setup when first entering the Lab scene.
/// A small bootstrap ensures this exists without needing to place it in every scene.
/// </summary>
public class SceneTransitionController : MonoBehaviour
{
    private const string DefaultInstanceName = "SceneTransitionController";

    [Header("Interaction Target")]
    [SerializeField] private string pressEObjectName = "pressE";
    [SerializeField] private float interactRangeX = 2f;

    [Header("Fade Target")]
    [SerializeField] private string blackFadeObjectName = "blackFade";
    [SerializeField] private string[] fallbackFadeObjectNames = new[] { "BlackFade" };
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Scene Names")]
    [SerializeField] private string ventilationSceneName = "VentilationShaft";
    [SerializeField] private string labSceneName = "Lab";

    [Header("Lab Fade Settings (first entry only)")]
    [SerializeField] private Vector2 labBlackFadePosition = new Vector2(-1.92f, -5.05f);

    private static SceneTransitionController instance;
    private bool isTransitioning;
    private bool labFadeDone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject(DefaultInstanceName);
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransitionController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Handle cases where the game starts directly in the Lab scene.
        HandleLabFade(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Update()
    {
        if (isTransitioning) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (!TryGetPressETarget(out Transform pressE)) return;
        if (!TryGetPlayer(out Transform player)) return;

        float dx = Mathf.Abs(player.position.x - pressE.position.x);
        if (dx > interactRangeX) return;

        StartCoroutine(FadeAndLoadVentilation());
    }

    private IEnumerator FadeAndLoadVentilation()
    {
        isTransitioning = true;
        var fadeTarget = FindFadeTarget();
        if (fadeTarget.Exists)
        {
            // Ensure blackFade starts at alpha 0 (transparent), then fade in to 1 (opaque)
            SetAlpha(fadeTarget, 0f);
            yield return FadeTo(fadeTarget, 1f, fadeDuration);
        }
        else
        {
            Debug.LogWarning("[SceneTransitionController] blackFade target not found; loading without fade.");
        }

        SceneManager.LoadScene(ventilationSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isTransitioning = false;

        HandleLabFade(scene);
    }

    private void HandleLabFade(Scene scene)
    {
        if (labFadeDone) return;
        if (scene.name != labSceneName) return;

        var fadeTarget = FindFadeTarget();
        if (fadeTarget.Exists)
        {
            Transform t = fadeTarget.Transform;
            if (t != null)
            {
                Vector3 newPos = new Vector3(labBlackFadePosition.x, labBlackFadePosition.y, t.position.z);
                t.position = newPos;
            }
            SetAlpha(fadeTarget, 1f);
            StartCoroutine(FadeTo(fadeTarget, 0f, fadeDuration));
        }
        else
        {
            Debug.LogWarning("[SceneTransitionController] blackFade target not found in Lab; cannot fade out.");
        }

        labFadeDone = true;
    }

    private bool TryGetPlayer(out Transform player)
    {
        player = null;
        var controller = FindObjectOfType<PlayerController>();
        if (controller != null)
        {
            player = controller.transform;
            return true;
        }

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            player = tagged.transform;
            return true;
        }

        return false;
    }

    private bool TryGetPressETarget(out Transform pressE)
    {
        pressE = null;
        var go = GameObject.Find(pressEObjectName);
        if (go == null) return false;
        pressE = go.transform;
        return true;
    }

    private FadeTarget FindFadeTarget()
    {
        GameObject go = GameObject.Find(blackFadeObjectName);
        if (go == null && fallbackFadeObjectNames != null)
        {
            for (int i = 0; i < fallbackFadeObjectNames.Length; i++)
            {
                if (string.IsNullOrEmpty(fallbackFadeObjectNames[i])) continue;
                go = GameObject.Find(fallbackFadeObjectNames[i]);
                if (go != null) break;
            }
        }
        if (go == null) return default;

        return new FadeTarget
        {
            Sprite = go.GetComponent<SpriteRenderer>(),
            Canvas = go.GetComponent<CanvasGroup>(),
            Image = go.GetComponent<Image>()
        };
    }

    private IEnumerator FadeTo(FadeTarget target, float targetAlpha, float duration)
    {
        float startAlpha = GetAlpha(target);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float eased = EaseInOutCubic(progress);
            float a = Mathf.Lerp(startAlpha, targetAlpha, eased);
            SetAlpha(target, a);
            yield return null;
        }
        SetAlpha(target, targetAlpha);
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private static float GetAlpha(FadeTarget target)
    {
        if (target.Sprite != null) return target.Sprite.color.a;
        if (target.Image != null) return target.Image.color.a;
        if (target.Canvas != null) return target.Canvas.alpha;
        return 1f;
    }

    private static void SetAlpha(FadeTarget target, float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (target.Sprite != null)
        {
            Color c = target.Sprite.color;
            c.a = alpha;
            target.Sprite.color = c;
        }

        if (target.Image != null)
        {
            Color c = target.Image.color;
            c.a = alpha;
            target.Image.color = c;
        }

        if (target.Canvas != null)
        {
            target.Canvas.alpha = alpha;
        }
    }

    private struct FadeTarget
    {
        public SpriteRenderer Sprite;
        public CanvasGroup Canvas;
        public Image Image;
        public bool Exists => Sprite != null || Canvas != null || Image != null;
        public Transform Transform => Sprite != null ? Sprite.transform : Canvas != null ? Canvas.transform : Image != null ? Image.transform : null;
    }
}
