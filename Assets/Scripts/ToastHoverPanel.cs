using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space hover panel that fades in/out above the toast, shows profile, stats, and a sprite-based load button.
/// </summary>
public class ToastHoverPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform root;
    [SerializeField] private Image profileImage;
    [SerializeField] private Image toastNameImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Image loadingImage;
    [SerializeField] private Sprite[] loadingFrames;
    [SerializeField] private Sprite completeButtonSprite;
    [SerializeField] private Sprite changeButtonSprite;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float loadingAnimationDuration = 1.0f;

    private Transform target;
    private Camera cam;
    private ToastStats toastStats;
    private bool pointerInside = false;
    private bool toastHover = false;
    private Coroutine fadeRoutine;
    private Coroutine loadRoutine;
    private bool isLoading = false;
    private bool hasBeenLoaded = false;
    private Vector2 worldOffset;
    private string sortingLayerName;
    private int sortingOrder;
    private static ToastHoverPanel s_activePanel;

    private void Awake()
    {
        cam = Camera.main;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadButtonClicked);

        // Initialize loading image with first frame and hide
        if (loadingImage != null)
        {
            // Set initial sprite to first frame to avoid white square
            if (loadingFrames != null && loadingFrames.Length > 0)
            {
                loadingImage.sprite = loadingFrames[0];
            }
            loadingImage.gameObject.SetActive(false);
        }

        // Fallback: Load complete button sprite from Resources if not assigned
        if (completeButtonSprite == null)
        {
            Debug.LogWarning("[ToastHoverPanel] completeButtonSprite not assigned in Inspector! Trying to load from Resources...");
            var sprite = UnityEngine.Resources.LoadAll<Sprite>("StatsBoxUI/Button_complete");
            if (sprite != null && sprite.Length > 0)
            {
                completeButtonSprite = sprite[0];
                Debug.Log($"[ToastHoverPanel] Loaded completeButtonSprite from Resources: {completeButtonSprite.name}");
            }
            else
            {
                Debug.LogError("[ToastHoverPanel] Failed to load Button_complete from Resources!");
            }
        }

        // Fallback: Load change button sprite from Resources if not assigned
        if (changeButtonSprite == null)
        {
            var sprite = UnityEngine.Resources.LoadAll<Sprite>("StatsBoxUI/Button_change");
            if (sprite != null && sprite.Length > 0)
            {
                changeButtonSprite = sprite[0];
            }
        }

        ResetLoadingVisual();
    }

    private bool loggedPosition = false;
    private Canvas parentCanvas;

    private void Update()
    {
        if (target == null || cam == null) return;

        // Calculate world position with offset
        Vector3 worldPos = target.position + (Vector3)worldOffset;

        // Cache parent canvas
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            // For WorldSpace Canvas, move the CANVAS (not the panel) to follow the target
            // This preserves the panel's internal RectTransform layout
            parentCanvas.transform.position = worldPos;

            if (!loggedPosition && canvasGroup != null && canvasGroup.alpha > 0.5f)
            {
                loggedPosition = true;
                Debug.Log($"[ToastHoverPanel] WorldSpace positioning: target={target.position}, offset={worldOffset}, worldPos={worldPos}, canvasPos={parentCanvas.transform.position}, panelLocalPos={transform.localPosition}");
            }
        }
        else if (root != null)
        {
            // For ScreenSpace Canvas, convert to screen coordinates and set RectTransform position
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            root.position = screenPos;
        }
    }

    private void OnEnable()
    {
        var canvas = GetComponentInParent<Canvas>();
        Debug.Log($"[ToastHoverPanel] OnEnable called. GameObject={gameObject.name}, Canvas={canvas?.renderMode}, Position={transform.position}, Scale={transform.localScale}");
    }

    public void EnableHoverFromToast(ToastStats stats)
    {
        toastStats = stats;
        ResetLoadingVisual();
    }

    public void Show(ToastStats stats, Vector2 offset, string layer, int order)
    {
        FoodHoverPanel.HideActivePanel();
        if (s_activePanel != null && s_activePanel != this)
        {
            s_activePanel.gameObject.SetActive(false);
        }
        s_activePanel = this;
        toastStats = stats;
        worldOffset = offset;
        sortingLayerName = layer;
        sortingOrder = order;

        target = stats.transform;
        Debug.Log($"[ToastHoverPanel] Show called. target={target.name}, offset={offset}, layer={layer}, order={order}");
        ApplyData(stats);

        // UpdateButtonForActiveToast를 먼저 호출하여 hasBeenLoaded를 리셋
        UpdateButtonForActiveToast();

        // 그 다음 ResetLoadingVisual 호출 (이제 hasBeenLoaded가 리셋된 상태)
        ResetLoadingVisual();

        toastHover = true;
        Debug.Log($"[ToastHoverPanel] Calling FadeTo(1f). Current alpha={canvasGroup?.alpha}");
        FadeTo(1f);
    }

    public void Hide()
    {
        toastHover = false;
        if (!pointerInside)
        {
            FadeTo(0f);
            if (s_activePanel == this)
            {
                s_activePanel = null;
            }
            // 패널을 닫을 때 버튼 상태를 다시 활성화하도록 모든 패널 리프레시
            RefreshAllButtons();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        Debug.Log("[ToastHoverPanel] OnPointerEnter - mouse entered panel");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        Debug.Log($"[ToastHoverPanel] OnPointerExit - mouse left panel. toastHover={toastHover}");
        // ★ 자동 Fade-out 제거! 클릭으로만 닫도록 변경
        // if (!toastHover)
        // {
        //     FadeTo(0f);
        // }
    }

    private void OnLoadButtonClicked()
    {
        Debug.Log($"[ToastHoverPanel] ★★★ OnLoadButtonClicked called! isLoading={isLoading}, hasBeenLoaded={hasBeenLoaded} ★★★");

        if (isLoading || hasBeenLoaded)
        {
            Debug.Log($"[ToastHoverPanel] Button click blocked: isLoading={isLoading}, hasBeenLoaded={hasBeenLoaded}");
            return;
        }

        Debug.Log("[ToastHoverPanel] Starting loading animation");
        if (loadRoutine != null) StopCoroutine(loadRoutine);
        loadRoutine = StartCoroutine(PlayLoadingAnimation());
    }

    private IEnumerator PlayLoadingAnimation()
    {
        isLoading = true;
        if (loadButton != null) loadButton.interactable = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true; // 버튼 클릭 시에도 밖을 클릭하면 닫히도록 Raycast 허용 상태 유지

        // Show loading image GameObject
        if (loadingImage != null)
        {
            loadingImage.gameObject.SetActive(true);
        }

        if (loadingFrames != null && loadingFrames.Length > 0 && loadingImage != null)
        {
            // Calculate frame delay to fit all frames in loadingAnimationDuration (1 second)
            float frameDelay = loadingAnimationDuration / loadingFrames.Length;

            Debug.Log($"[ToastHoverPanel] Playing loading animation: {loadingFrames.Length} frames, {frameDelay:F3}s per frame");

            for (int i = 0; i < loadingFrames.Length; i++)
            {
                loadingImage.sprite = loadingFrames[i];
                yield return new WaitForSeconds(frameDelay);
            }
        }
        else
        {
            Debug.LogWarning($"[ToastHoverPanel] Loading frames not set! Frames={loadingFrames?.Length}, Image={loadingImage}");
            yield return new WaitForSeconds(loadingAnimationDuration);
        }

        // Hide loading image GameObject
        if (loadingImage != null)
        {
            loadingImage.gameObject.SetActive(false);
        }

        // Apply toast stats to player
        toastStats?.ApplyToPlayer();
        var player = GameObject.FindObjectOfType<PlayerController>();
        if (player != null && toastStats != null)
        {
            player.ApplyToastStats(toastStats.GetRuntimeStats(), toastStats.GetToastType().ToString());

            // 튜토리얼 매니저에 토스트 획득 알림
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnToastTaken();
            }
        }
        RefreshAllButtons();

        // Change button to "Complete" state
        if (loadButton != null && completeButtonSprite != null)
        {
            var buttonImage = loadButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Debug.Log($"[ToastHoverPanel] Changing button sprite to Complete. Before={buttonImage.sprite?.name}, After={completeButtonSprite.name}");
                buttonImage.sprite = completeButtonSprite;
            }
            else
            {
                Debug.LogWarning("[ToastHoverPanel] LoadButton has no Image component!");
            }
        }
        else
        {
            Debug.LogWarning($"[ToastHoverPanel] Cannot change to Complete: loadButton={loadButton}, completeButtonSprite={completeButtonSprite}");
        }

        // Reset loading state
        isLoading = false;
        hasBeenLoaded = true;
        if (loadButton != null) loadButton.interactable = false;
    }

    private void ApplyData(ToastStats stats)
    {
        if (profileImage != null) profileImage.sprite = stats.GetProfileSprite();
        if (toastNameImage != null)
        {
            toastNameImage.sprite = stats.GetToastNameSprite();
            toastNameImage.enabled = toastNameImage.sprite != null;
        }
        if (descriptionText != null)
        {
            var font = stats.GetFont();
            if (font != null) descriptionText.font = TMP_FontAsset.CreateFontAsset(font);
            descriptionText.text = string.IsNullOrEmpty(stats.GetDescription()) ? "임시 스탯 설명" : stats.GetDescription();
        }
        if (statsText != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in stats.GetRuntimeStats())
            {
                float val = s.value;
                string sign = s.showSign ? (val >= 0 ? "+" : "-") : "";
                float magnitude = s.showSign ? Mathf.Abs(val) : val;
                string unit = string.IsNullOrEmpty(s.unit) ? "" : s.unit;
                sb.AppendLine($"{s.GetDisplayName()} {sign}{magnitude:0.##}{unit}");
            }
            statsText.text = sb.ToString();
        }
    }

    /// <summary>
    /// PlayerController가 Toast를 적용했을 때 호출 - 버튼 상태를 새로고침
    /// </summary>
    public void RefreshButton()
    {
        Debug.Log($"[ToastHoverPanel] RefreshButton called for {gameObject.name}");
        UpdateButtonForActiveToast();
        ResetLoadingVisual();
    }


    private void UpdateButtonForActiveToast()
    {
        if (loadButton == null)
        {
            Debug.LogWarning("[ToastHoverPanel] UpdateButtonForActiveToast - loadButton is NULL!");
            return;
        }

        var player = GameObject.FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[ToastHoverPanel] UpdateButtonForActiveToast - PlayerController not found!");
            return;
        }

        var btnImage = loadButton.GetComponent<Image>();
        if (btnImage == null)
        {
            Debug.LogWarning("[ToastHoverPanel] UpdateButtonForActiveToast - btnImage is NULL!");
            return;
        }

        // 리소스/프리팹 기반 스프라이트를 먼저 보장
        EnsureButtonSpritesLoaded(btnImage);

        string currentId = player.CurrentToastId;
        string thisId = toastStats != null ? toastStats.GetToastType().ToString() : string.Empty;

        string changeName = changeButtonSprite != null ? changeButtonSprite.name : "null";
        string completeName = completeButtonSprite != null ? completeButtonSprite.name : "null";

        bool isCurrent = !string.IsNullOrEmpty(currentId) && string.Equals(currentId, thisId);
        Debug.Log($"[ToastHoverPanel] UpdateButtonForActiveToast: isCurrent={isCurrent}, currentId={currentId}, thisId={thisId}, btnBefore={btnImage.sprite?.name}, change={changeName}, complete={completeName}");

        if (isCurrent)
        {
            if (completeButtonSprite != null)
            {
                btnImage.sprite = completeButtonSprite;
                btnImage.preserveAspect = true;
                Debug.Log($"[ToastHoverPanel] Set COMPLETE sprite -> {btnImage.sprite?.name}");
            }
            loadButton.interactable = false;
            hasBeenLoaded = true;
        }
        else
        {
            if (changeButtonSprite != null)
            {
                btnImage.sprite = changeButtonSprite;
                btnImage.preserveAspect = true;
                Debug.Log($"[ToastHoverPanel] Set CHANGE sprite -> {btnImage.sprite?.name}");
            }
            else if (loadingFrames != null && loadingFrames.Length > 0 && loadingImage != null)
            {
                // 리소스를 못 찾았을 때 최소한 기본 프레임으로 복구
                loadingImage.sprite = loadingFrames[0];
                Debug.LogWarning("[ToastHoverPanel] changeButtonSprite missing, fell back to loadingFrames[0]");
            }

            loadButton.interactable = true;
            hasBeenLoaded = false;
        }

        isLoading = false;
    }

    private void EnsureButtonSpritesLoaded(Image btnImage = null)
    {
        // 1) 이미 버튼에 할당된 스프라이트를 우선 활용 (Prefab/Scene에 배치된 값)
        if (btnImage != null && btnImage.sprite != null)
        {
            if (changeButtonSprite == null && btnImage.sprite.name.Contains("Button_change"))
            {
                changeButtonSprite = btnImage.sprite;
            }
            if (completeButtonSprite == null && btnImage.sprite.name.Contains("Button_complete"))
            {
                completeButtonSprite = btnImage.sprite;
            }
        }

        // 2) Resources 폴더 시도 (있으면 덮어씀)
        if (completeButtonSprite == null)
        {
            var sprites = UnityEngine.Resources.LoadAll<Sprite>("StatsBoxUI/Button_complete");
            if (sprites != null && sprites.Length > 0) completeButtonSprite = sprites[0];
        }

        if (changeButtonSprite == null)
        {
            var sprites = UnityEngine.Resources.LoadAll<Sprite>("StatsBoxUI/Button_change");
            if (sprites != null && sprites.Length > 0)
            {
                changeButtonSprite = sprites[0];
            }
            else if (btnImage != null && btnImage.sprite != null)
            {
                // 최후: 버튼에 할당된 스프라이트라도 사용
                changeButtonSprite = btnImage.sprite;
            }
            else
            {
                Debug.LogWarning("[ToastHoverPanel] Button_change sprite not found (Resources/StatsBoxUI) and no fallback sprite present");
            }
        }
    }


    public void ForceClose()
    {
        toastHover = false;
        pointerInside = false;
        if (s_activePanel == this)
        {
            s_activePanel = null;
        }
        FadeTo(0f);
    }

    public static void HideActivePanel()
    {
        if (s_activePanel == null) return;
        s_activePanel.gameObject.SetActive(false);
        s_activePanel = null;
    }

    private static void RefreshAllButtons()
    {
        var panels = FindObjectsOfType<ToastHoverPanel>(true);
        foreach (var panel in panels)
        {
            panel?.RefreshButton();
        }
    }

    private void ResetLoadingVisual()
    {
        isLoading = false;
        // ★ REMOVED: Don't override button.interactable here!
        // UpdateButtonForActiveToast() already handles button state correctly
        // if (loadButton != null) loadButton.interactable = !hasBeenLoaded;

        if (loadingImage != null && loadingFrames != null && loadingFrames.Length > 0)
        {
            loadingImage.sprite = hasBeenLoaded ? loadingFrames[loadingFrames.Length - 1] : loadingFrames[0];
        }
        Debug.Log($"[ToastHoverPanel] ResetLoadingVisual: isLoading={isLoading}, hasBeenLoaded={hasBeenLoaded}, button.interactable={loadButton?.interactable}");
    }

    private void FadeTo(float targetAlpha)
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"[ToastHoverPanel] FadeTo failed - canvasGroup is NULL! GameObject={gameObject.name}");
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError($"[ToastHoverPanel] FadeTo failed - GameObject is INACTIVE! GameObject={gameObject.name}");
            gameObject.SetActive(true); // 강제로 활성화
        }

        Debug.Log($"[ToastHoverPanel] FadeTo({targetAlpha}) called. Current alpha={canvasGroup.alpha}, GameObject active={gameObject.activeInHierarchy}");

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null)
        {
            Debug.LogError("[ToastHoverPanel] FadeRoutine - canvasGroup is NULL!");
            yield break;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        Debug.Log($"[ToastHoverPanel] ★★★ FadeRoutine STARTED ★★★ start={start}, target={targetAlpha}, duration={fadeDuration}");

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            canvasGroup.alpha = a;
            canvasGroup.blocksRaycasts = a > 0.01f;
            canvasGroup.interactable = a > 0.99f;

            if (Time.frameCount % 10 == 0) // 10프레임마다
            {
                Debug.Log($"[ToastHoverPanel] Fading... alpha={a:F2}, t={t:F2}/{fadeDuration}");
            }

            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
        canvasGroup.interactable = targetAlpha > 0.99f;
        Debug.Log($"[ToastHoverPanel] ★★★ FadeRoutine FINISHED ★★★ Final alpha={canvasGroup.alpha}");
    }
}
