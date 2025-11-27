using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to the blackFade object.
/// When the Lab scene loads for the first time, it fades out from opaque (alpha 1) to transparent (alpha 0).
/// </summary>
public class BlackFadeFadeOut : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private string labSceneName = "Lab";
    [SerializeField] private Vector2 labPosition = new Vector2(-1.92f, -5.05f);

    private SpriteRenderer spriteRenderer;
    private static bool hasLabFadedOut = false;
    private bool hasFadedThisInstance = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("[BlackFadeFadeOut] SpriteRenderer not found on blackFade object!");
            enabled = false;
            return;
        }

        // Reset flag when scene loads (for testing when starting directly in Lab)
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == labSceneName)
        {
            hasLabFadedOut = false;
        }
    }

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Fade out when entering Lab scene (first time or direct start)
        if (currentScene == labSceneName && !hasLabFadedOut && !hasFadedThisInstance)
        {
            transform.position = new Vector3(labPosition.x, labPosition.y, transform.position.z);
            SetAlpha(1f); // Start fully opaque
            Debug.Log("[BlackFadeFadeOut] Starting fade out in Lab scene");
            StartCoroutine(FadeOut());
            hasLabFadedOut = true;
            hasFadedThisInstance = true;
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Debug.Log($"[BlackFadeFadeOut] Starting fade out - duration: {fadeDuration}s");

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = EaseInOutCubic(progress);
            float alpha = Mathf.Lerp(1f, 0f, eased);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);
        Debug.Log("[BlackFadeFadeOut] Fade out complete");
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = c;
        }
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    // Reset the flag when the application quits (for testing in editor)
    private void OnApplicationQuit()
    {
        hasLabFadedOut = false;
    }
}