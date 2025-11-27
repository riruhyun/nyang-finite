using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체에 빨간 투명 필터를 짧게 깜빡이는 오버레이.
/// DamageFlashOverlay.Flash()를 호출하면 2회 깜빡임.
/// </summary>
public class DamageFlashOverlay : MonoBehaviour
{
    private static DamageFlashOverlay instance;

    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int flashCount = 2;

    private Image overlayImage;
    private Coroutine flashRoutine;

    public static void Flash()
    {
        EnsureInstance();
        if (instance != null)
        {
            instance.StartFlash();
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("DamageFlashOverlay");
        instance = go.AddComponent<DamageFlashOverlay>();
        DontDestroyOnLoad(go);
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
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("DamageFlashCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        var group = canvasGO.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var imageGO = new GameObject("DamageFlashImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        overlayImage = imageGO.AddComponent<Image>();
        var rect = overlayImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        overlayImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        overlayImage.raycastTarget = false;
    }

    private void StartFlash()
    {
        if (overlayImage == null)
        {
            BuildOverlay();
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            yield return FadeTo(flashColor.a, flashDuration * 0.5f);
            yield return FadeTo(0f, flashDuration * 0.5f);
        }

        flashRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (overlayImage == null) yield break;

        float startAlpha = overlayImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (overlayImage == null) return;
        Color c = overlayImage.color;
        c.a = alpha;
        overlayImage.color = c;
    }
}
