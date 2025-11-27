using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to the PressE object.
/// When player is within range (X-axis distance <= 2) and presses E key,
/// it fades in the blackFade object and transitions to VentilationShaft scene.
/// </summary>
public class PressEInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRangeX = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Fade Settings")]
    [SerializeField] private string blackFadeObjectName = "blackFade";
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Scene Transition")]
    [SerializeField] private string targetSceneName = "VentilationShaft";

    private bool isTransitioning = false;

    private void Update()
    {
        if (isTransitioning) return;
        if (!Input.GetKeyDown(interactionKey)) return;

        Transform player = GetPlayerTransform();
        if (player == null) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        if (distanceX <= interactionRangeX)
        {
            StartCoroutine(FadeInAndTransition());
        }
    }

    private IEnumerator FadeInAndTransition()
    {
        isTransitioning = true;

        GameObject blackFadeObj = GameObject.Find(blackFadeObjectName);
        if (blackFadeObj == null)
        {
            Debug.LogWarning($"[PressEInteraction] {blackFadeObjectName} object not found! Transitioning without fade.");
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        SpriteRenderer spriteRenderer = blackFadeObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[PressEInteraction] SpriteRenderer not found on {blackFadeObjectName}! Transitioning without fade.");
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        // Fade in from transparent (alpha 0) to opaque (alpha 1)
        yield return FadeIn(spriteRenderer, fadeDuration);

        // Load the next scene
        SceneManager.LoadScene(targetSceneName);
    }

    private IEnumerator FadeIn(SpriteRenderer spriteRenderer, float duration)
    {
        // Start from transparent
        SetAlpha(spriteRenderer, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInOutCubic(progress);
            float alpha = Mathf.Lerp(0f, 1f, eased);
            SetAlpha(spriteRenderer, alpha);
            yield return null;
        }

        SetAlpha(spriteRenderer, 1f);
    }

    private void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
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

    private Transform GetPlayerTransform()
    {
        // Try to find PlayerController first
        PlayerController controller = FindObjectOfType<PlayerController>();
        if (controller != null)
        {
            return controller.transform;
        }

        // Fallback: find by "Player" tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }

        return null;
    }
}
