using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Simple world-space hover box for food pickups. Builds its own UI at runtime and
/// follows a target transform. Shows profile, description, and effect text, plus
/// a one-shot button that flips to a complete sprite.
/// </summary>
public class FoodHoverPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(360f, 420f);
    [SerializeField] private float fadeDuration = 0.15f;

    [Header("Sprites")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite eatButtonSprite;
    [SerializeField] private Sprite completeButtonSprite;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform root;
    private Image profileImage;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private TMP_Text effectText;
    private Button actionButton;
    private Image actionButtonImage;
    private Canvas buttonCanvas;
    private RectTransform buttonCanvasRoot;

    private static FoodHoverPanel activePanel;

    private Transform target;
    private Vector2 worldOffset;
    private float uiScale = 0.01f;
    private string sortingLayerName = "UI";
    private int sortingOrder = 5000;
    private bool pointerInside = false;
    private bool foodHover = false;
    private bool consumed = false;
    private Coroutine fadeRoutine;
    private FoodHoverData data;
    private Action<FoodHoverData> consumedCallback;

    public bool IsPointerInside => pointerInside;

    public void ConfigureSprites(Sprite eatSprite, Sprite completeSprite, Sprite background)
    {
        eatButtonSprite = eatSprite;
        completeButtonSprite = completeSprite;
        backgroundSprite = background;
    }

    public void SetConsumedCallback(Action<FoodHoverData> onConsumed)
    {
        consumedCallback = onConsumed;
    }

    public void Show(FoodHoverData hoverData, Transform followTarget, Vector2 offset, float scale, string layerName, int order)
    {
        data = hoverData;
        target = followTarget;
        worldOffset = offset;
        uiScale = scale;
        sortingLayerName = layerName;
        sortingOrder = order;

        EnsureEventSystem();

        Debug.Log($"[FoodHoverPanel] Show called. currentAlpha={canvasGroup?.alpha}");
        BuildUIIfNeeded();
        UpdateCanvasProps();
        ApplyData();

        // 상태 리셋
        foodHover = true;
        pointerInside = false;
        if (activePanel != null && activePanel != this)
        {
            activePanel.Hide();
        }
        activePanel = this;

        Debug.Log($"[FoodHoverPanel] Calling FadeTo(1f)");
        FadeTo(1f);
    }

    public void Hide()
    {
        Debug.Log($"[FoodHoverPanel] Hide called. foodHover={foodHover}, pointerInside={pointerInside}, consumed={consumed}");
        foodHover = false;
        if (activePanel == this)
        {
            activePanel = null;
        }
        if (!consumed)
        {
            Debug.Log($"[FoodHoverPanel] Calling FadeTo(0f)");
            FadeTo(0f);
        }
    }

    private void Update()
    {
        if (target == null || root == null) return;
        Vector3 newPos = target.position + (Vector3)worldOffset;
        transform.position = newPos;

        // 디버그: 위치 확인 (1초마다)
        if (Time.frameCount % 60 == 0 && canvasGroup != null && canvasGroup.alpha > 0.01f)
        {
            Debug.Log($"[FoodHoverPanel] Position: panel={transform.position}, target={target.position}, offset={worldOffset}, alpha={canvasGroup.alpha}, scale={root.localScale}, active={gameObject.activeSelf}");
        }
    }

    private void BuildUIIfNeeded()
    {
        if (canvas != null) return;

        root = gameObject.GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.sizeDelta = panelSize;
        root.localScale = Vector3.one * uiScale;

        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingLayerName = sortingLayerName;
        canvas.sortingOrder = sortingOrder;

        // ★ GraphicRaycaster 설정 강화
        var raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None; // 다른 오브젝트에 의해 막히지 않음

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Debug.Log($"[FoodHoverPanel] BuildUI complete. Canvas: renderMode={canvas.renderMode}, camera={canvas.worldCamera}, sortingLayer={canvas.sortingLayerName}, sortingOrder={canvas.sortingOrder}, panelSize={root.sizeDelta}, scale={root.localScale}, uiScale={uiScale}");

        // Background panel
        var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(root, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var bg = panelGO.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.9f);
        bg.raycastTarget = false;
        if (backgroundSprite != null)
        {
            bg.sprite = backgroundSprite;
            bg.type = Image.Type.Sliced;
        }

        // Profile image
        var profileGO = new GameObject("Profile", typeof(RectTransform), typeof(Image));
        profileGO.transform.SetParent(panelRect, false);
        var profileRect = profileGO.GetComponent<RectTransform>();
        profileRect.anchorMin = new Vector2(0.5f, 1f);
        profileRect.anchorMax = new Vector2(0.5f, 1f);
        profileRect.pivot = new Vector2(0.5f, 1f);
        profileRect.sizeDelta = new Vector2(110f, 110f);
        profileRect.anchoredPosition = new Vector2(0f, -15f);
        profileImage = profileGO.GetComponent<Image>();
        profileImage.preserveAspect = true;

        // Title text
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(panelRect, false);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(320f, 46f);
        titleRect.anchoredPosition = new Vector2(0f, -132f);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 26f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableWordWrapping = false;
        titleText.alignment = TextAlignmentOptions.Center;

        // Description text
        var descGO = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(panelRect, false);
        var descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0f, 1f);
        descRect.sizeDelta = new Vector2(-40f, 120f);
        descRect.anchoredPosition = new Vector2(20f, -184f);
        descriptionText = descGO.GetComponent<TextMeshProUGUI>();
        descriptionText.fontSize = 22f;
        descriptionText.enableWordWrapping = true;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;

        // Effect text
        var effectGO = new GameObject("Effect", typeof(RectTransform), typeof(TextMeshProUGUI));
        effectGO.transform.SetParent(panelRect, false);
        var effectRect = effectGO.GetComponent<RectTransform>();
        effectRect.anchorMin = new Vector2(0f, 1f);
        effectRect.anchorMax = new Vector2(1f, 1f);
        effectRect.pivot = new Vector2(0f, 1f);
        effectRect.sizeDelta = new Vector2(-40f, 60f);
        effectRect.anchoredPosition = new Vector2(20f, -316f);
        effectText = effectGO.GetComponent<TextMeshProUGUI>();
        effectText.fontSize = 18f;
        effectText.enableWordWrapping = true;
        effectText.color = new Color(0.15f, 0.3f, 0.15f);
        effectText.alignment = TextAlignmentOptions.Left;

        // Action button - 더 큰 크기로 클릭 영역 확대
        if (buttonCanvasRoot == null)
        {
            var buttonCanvasGO = new GameObject("ButtonCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            buttonCanvasRoot = buttonCanvasGO.GetComponent<RectTransform>();
            buttonCanvasRoot.SetParent(root, false);
            buttonCanvasRoot.anchorMin = Vector2.zero;
            buttonCanvasRoot.anchorMax = Vector2.one;
            buttonCanvasRoot.pivot = new Vector2(0.5f, 0.5f);
            buttonCanvasRoot.offsetMin = Vector2.zero;
            buttonCanvasRoot.offsetMax = Vector2.zero;

            buttonCanvas = buttonCanvasGO.GetComponent<Canvas>();
            buttonCanvas.renderMode = RenderMode.WorldSpace;
            buttonCanvas.worldCamera = canvas.worldCamera;
            buttonCanvas.sortingLayerName = sortingLayerName;
            buttonCanvas.overrideSorting = true;
            buttonCanvas.sortingOrder = sortingOrder + 1;

            var buttonRaycaster = buttonCanvasGO.GetComponent<GraphicRaycaster>();
            buttonRaycaster.ignoreReversedGraphics = true;
            buttonRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        var buttonGO = new GameObject("EatButton", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Button));
        buttonGO.transform.SetParent(buttonCanvasRoot, false);
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(240f, 80f); // 190→240, 60→80 (클릭 영역 확대)
        buttonRect.anchoredPosition = new Vector2(0f, 15f);

        actionButtonImage = buttonGO.GetComponent<Image>();
        actionButton = buttonGO.GetComponent<Button>();

        // ★ CRITICAL: Ensure Image can receive raycasts
        if (actionButtonImage != null)
        {
            actionButtonImage.raycastTarget = true;
            actionButtonImage.color = Color.white; // Ensure visible
            actionButtonImage.canvasRenderer.cullTransparentMesh = false;
            Debug.Log($"[FoodHoverPanel] Button Image configured: raycastTarget={actionButtonImage.raycastTarget}, color={actionButtonImage.color}");
        }

        // 버튼 CanvasGroup은 페이드 영향을 받지 않도록 유지
        var buttonCanvasGroup = buttonGO.GetComponent<CanvasGroup>();
        buttonCanvasGroup.alpha = 1f;
        buttonCanvasGroup.ignoreParentGroups = true;

        // ★ 클릭 안정성 향상: Transition을 None으로 설정하여 애니메이션 간섭 제거
        actionButton.transition = UnityEngine.UI.Selectable.Transition.None;
        actionButton.onClick.AddListener(OnEatClicked);
        actionButton.interactable = true;

        Debug.Log($"[FoodHoverPanel] ★★★ Button FULLY created ★★★ GameObject={buttonGO.name}, active={buttonGO.activeInHierarchy}, interactable={actionButton.interactable}, listenerCount={actionButton.onClick.GetPersistentEventCount()}, rect={buttonRect.rect}, worldPos={buttonGO.transform.position}");
    }

    private void UpdateCanvasProps()
    {
        if (root != null)
        {
            root.localScale = Vector3.one * uiScale;
        }

        if (canvas != null)
        {
            canvas.worldCamera = Camera.main;
            canvas.sortingLayerName = sortingLayerName;
            canvas.sortingOrder = sortingOrder;
        }

        if (buttonCanvas != null)
        {
            buttonCanvas.worldCamera = canvas != null ? canvas.worldCamera : Camera.main;
            buttonCanvas.sortingLayerName = sortingLayerName;
            buttonCanvas.sortingOrder = sortingOrder + 1;
        }
    }

    private void ApplyData()
    {
        if (profileImage != null) profileImage.sprite = data.profileSprite;

        if (titleText != null)
        {
            string name = string.IsNullOrEmpty(data.displayName) ? (data.profileSprite != null ? data.profileSprite.name : "Food") : data.displayName;
            titleText.text = name;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrEmpty(data.description) ? "맛이 궁금하다..." : data.description;
        }

        if (effectText != null)
        {
            effectText.text = $"효과: {FormatEffect(data.effectType, data.effectAmount)}";
        }

        if (actionButtonImage != null)
        {
            actionButtonImage.sprite = consumed ? completeButtonSprite : (eatButtonSprite != null ? eatButtonSprite : actionButtonImage.sprite);
            actionButtonImage.preserveAspect = true;
            actionButtonImage.raycastTarget = true; // ★ Ensure raycast target is still enabled
            Debug.Log($"[FoodHoverPanel] Button sprite set: consumed={consumed}, sprite={actionButtonImage.sprite?.name}, raycastTarget={actionButtonImage.raycastTarget}");
        }
        if (actionButton != null)
        {
            actionButton.interactable = !consumed;
            Debug.Log($"[FoodHoverPanel] Button interactable set: {actionButton.interactable} (consumed={consumed}), listenerCount={actionButton.onClick.GetPersistentEventCount()}, GameObject={actionButton.gameObject.name}, active={actionButton.gameObject.activeInHierarchy}");
        }
    }

    private string FormatEffect(FoodEffectType type, int amount)
    {
        switch (type)
        {
            case FoodEffectType.Heal: return $"회복 +{amount}";
            case FoodEffectType.CleanseDebuff: return "디버프 제거";
            case FoodEffectType.Damage: return $"피해 -{amount}";
            case FoodEffectType.Buff: return "버프";
            default: return type.ToString();
        }
    }

    private void OnEatClicked()
    {
        Debug.Log($"[FoodHoverPanel] ★★★ OnEatClicked called! consumed={consumed} ★★★");

        if (consumed)
        {
            Debug.Log("[FoodHoverPanel] Already consumed, ignoring");
            return;
        }

        consumed = true;
        Debug.Log("[FoodHoverPanel] Setting consumed=true, changing button sprite");

        if (actionButtonImage != null && completeButtonSprite != null)
        {
            actionButtonImage.sprite = completeButtonSprite;
            actionButtonImage.preserveAspect = true;
            Debug.Log("[FoodHoverPanel] Button sprite changed to complete");
        }
        if (actionButton != null) actionButton.interactable = false;

        // 음식 숨기기
        if (target != null)
        {
            target.gameObject.SetActive(false);
            Debug.Log($"[FoodHoverPanel] Food hidden: {target.gameObject.name}");
        }

        // 효과 적용은 callback에서 처리 (중복 방지)
        Debug.Log("[FoodHoverPanel] Invoking consumedCallback");
        consumedCallback?.Invoke(data);

        Debug.Log("[FoodHoverPanel] Calling FadeTo(0f)");
        FadeTo(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        Debug.Log("[FoodHoverPanel] OnPointerEnter - mouse entered panel");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        Debug.Log($"[FoodHoverPanel] OnPointerExit - mouse left panel. foodHover={foodHover}, consumed={consumed}");
        // ★ 자동 Fade-out 제거! 클릭으로만 닫도록 변경
        // if (!consumed)
        // {
        //     Debug.Log("[FoodHoverPanel] Fading out panel");
        //     FadeTo(0f);
        // }
    }

    private void FadeTo(float targetAlpha)
    {
        BuildUIIfNeeded();
        UpdateCanvasProps();

        if (canvasGroup == null)
        {
            Debug.LogError("[FoodHoverPanel] FadeTo failed - canvasGroup is null!");
            return;
        }

        // 기존 fade 중지
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        Debug.Log($"[FoodHoverPanel] FadeTo({targetAlpha}) - current alpha={canvasGroup.alpha}");
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        Debug.Log($"[FoodHoverPanel] FadeRoutine start. start={start}, target={targetAlpha}, duration={fadeDuration}");
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, targetAlpha, fadeDuration > 0f ? t / fadeDuration : 1f);
            canvasGroup.alpha = a;
            canvasGroup.blocksRaycasts = a > 0.05f;
            canvasGroup.interactable = a > 0.3f; // 0.95 → 0.3 (더 빨리 클릭 가능)

            // Debug log every 5 frames during fade
            if (Time.frameCount % 5 == 0)
            {
                Debug.Log($"[FoodHoverPanel] Fading... alpha={a:F3}, blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}, buttonInteractable={actionButton?.interactable}");
            }
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.05f;
        canvasGroup.interactable = targetAlpha > 0.3f; // 0.95 → 0.3 (더 빨리 클릭 가능)
        Debug.Log($"[FoodHoverPanel] ★★★ FadeRoutine finished ★★★ Final alpha={canvasGroup.alpha}, blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}, buttonInteractable={actionButton?.interactable}, buttonActive={actionButton?.gameObject.activeInHierarchy}");
    }

    private void EnsureEventSystem()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            Debug.Log($"[FoodHoverPanel] EventSystem found: {eventSystem.name}");
            return;
        }

        var go = new GameObject("EventSystem");
        eventSystem = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Debug.Log("[FoodHoverPanel] ★★★ EventSystem was missing; created default EventSystem for Food UI ★★★");
    }
    public static void HideActivePanel()
    {
        if (activePanel != null)
        {
            var panel = activePanel;
            activePanel = null;
            panel.Hide();
        }
    }
}

public struct FoodHoverData
{
    public string displayName;
    public string description;
    public Sprite profileSprite;
    public FoodEffectType effectType;
    public int effectAmount;
}
