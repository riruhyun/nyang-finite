using System.Collections;
using UnityEngine;

/// <summary>
/// Simple helper to fade a SpriteRenderer or CanvasGroup from opaque to transparent on scene start.
/// Attach to the FadePanel object in Lobby.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FadePanelAutoFade : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private bool reactivateOnEnable = true;

    private SpriteRenderer spriteRenderer;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (!reactivateOnEnable) return;
        StartFadeOut();
    }

    public void StartFadeOut()
    {
        if (spriteRenderer == null) return;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        Color startColor = fadeColor;
        startColor.a = 1f;
        spriteRenderer.color = startColor;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            Color c = fadeColor;
            c.a = 1f - t;
            spriteRenderer.color = c;
            yield return null;
        }

        Color final = fadeColor;
        final.a = 0f;
        spriteRenderer.color = final;
        fadeRoutine = null;
    }
}
