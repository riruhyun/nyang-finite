using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles hover detection for a food pickup and spawns/controls the FoodHoverPanel.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FoodHoverHandler : MonoBehaviour
{
    [SerializeField] private Vector2 uiWorldOffset = new Vector2(0f, 1f);
    [SerializeField] private float uiWorldScale = 0.01f;
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 5000;
    [SerializeField] private Sprite eatButtonSprite;
    [SerializeField] private Sprite completeButtonSprite;
    [SerializeField] private Sprite backgroundSprite;
    [Header("Effect Text")]
    [SerializeField] private Vector3 effectTextOffset = new Vector3(0f, 0.8f, -1f);
    [SerializeField] private float effectTextDuration = 0.6f;
    [SerializeField] private float effectTextAmplitude = 0.35f;
    [SerializeField] private int effectTextFontSize = 60;
    [SerializeField] private float effectTextCharacterSize = 0.05f;
    [SerializeField] private Color healTextColor = Color.green;
    [SerializeField] private Color damageTextColor = Color.red;
    [SerializeField] private Font effectTextFont;

    private Camera mainCam;
    private bool hovering = false;
    private bool consumed = false;
    private FoodHoverData data;
    private FoodHoverPanel panel;

    public void Initialize(FoodHoverData hoverData, Vector2 offset, float scale, string layer, int order, Sprite eatSprite, Sprite completeSprite, Sprite background)
    {
        data = hoverData;
        uiWorldOffset = offset;
        uiWorldScale = scale;
        sortingLayerName = layer;
        sortingOrder = order;
        eatButtonSprite = eatSprite;
        completeButtonSprite = completeSprite;
        backgroundSprite = background;
    }

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        if (consumed) return;

        // ★ 클릭 기반으로 변경 (ToastHoverTrigger와 동일한 방식)
        if (Input.GetMouseButtonDown(0)) // 좌클릭
        {
            Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"[FoodHoverHandler] ★ Click detected at {mouseWorldPos}");

            bool clickedOnThis = false;

            // 방법 1: OverlapPoint로 체크
            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);
            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                Debug.Log($"[FoodHoverHandler] ★★★ CLICKED on Food (OverlapPoint) ★★★");
                clickedOnThis = true;
            }

            // 방법 2: CircleCast로 더 넓은 범위 체크
            if (!clickedOnThis)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPos, 0.5f);
                foreach (var col in hits)
                {
                    if (col.gameObject == gameObject)
                    {
                        Debug.Log($"[FoodHoverHandler] ★★★ CLICKED on Food (CircleCast) ★★★");
                        clickedOnThis = true;
                        break;
                    }
                }
            }

            // Food를 클릭했다면 패널 토글
            if (clickedOnThis)
            {
                ToastHoverPanel.HideActivePanel();
                TogglePanel();
            }
            else if (hovering)
            {
                // 다른 곳을 클릭했는데 패널이 열려있으면 닫기 (단, UI 또는 패널 위는 제외)
                bool clickedOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                bool pointerOnPanel = panel != null && panel.IsPointerInside;

                if (!clickedOnUI && !pointerOnPanel)
                {
                    Debug.Log($"[FoodHoverHandler] Clicked outside FoodPanel, closing panel");
                    hovering = false;
                    panel?.Hide();
                }
                else
                {
                    Debug.Log($"[FoodHoverHandler] Clicked on FoodPanel, keeping panel open (clickedOnUI={clickedOnUI}, pointerOnPanel={pointerOnPanel})");
                }
            }
        }
    }

    private void TogglePanel()
    {
        // 패널이 열려있으면 닫고, 닫혀있으면 열기
        if (hovering)
        {
            hovering = false;
            panel?.Hide();
            Debug.Log($"[FoodHoverHandler] Panel closed for Food");
        }
        else
        {
            hovering = true;
            ShowPanel();
            Debug.Log($"[FoodHoverHandler] Panel opened for Food");
        }
    }

    private void ShowPanel()
    {
        EnsurePanel();
        panel.Show(data, transform, uiWorldOffset, uiWorldScale, sortingLayerName, sortingOrder);
    }

    private void EnsurePanel()
    {
        if (panel != null) return;

        var go = new GameObject("FoodHoverPanel");
        panel = go.AddComponent<FoodHoverPanel>();
        // 배경/버튼이 비어 있으면 FoodUI 리소스를 우선 로드, 없으면 StatsBoxUI로 폴백
        if (backgroundSprite == null)
        {
            var bgFood = Resources.LoadAll<Sprite>("FoodUI/FoodPanel");
            var bgToast = Resources.LoadAll<Sprite>("StatsBoxUI/StatsBox");
            if (bgFood != null && bgFood.Length > 0) backgroundSprite = bgFood[0];
            else if (bgToast != null && bgToast.Length > 0) backgroundSprite = bgToast[0];
        }
        if (eatButtonSprite == null)
        {
            var eatFood = Resources.LoadAll<Sprite>("FoodUI/Button_Eat");
            var eatToast = Resources.LoadAll<Sprite>("StatsBoxUI/Button_Eat");
            if (eatFood != null && eatFood.Length > 0) eatButtonSprite = eatFood[0];
            else if (eatToast != null && eatToast.Length > 0) eatButtonSprite = eatToast[0];
        }
        if (completeButtonSprite == null)
        {
            var doneFood = Resources.LoadAll<Sprite>("FoodUI/Button_complete");
            var doneToast = Resources.LoadAll<Sprite>("StatsBoxUI/Button_complete");
            if (doneFood != null && doneFood.Length > 0) completeButtonSprite = doneFood[0];
            else if (doneToast != null && doneToast.Length > 0) completeButtonSprite = doneToast[0];
        }

        panel.ConfigureSprites(eatButtonSprite, completeButtonSprite, backgroundSprite);
        panel.SetConsumedCallback(OnConsumed);
    }

    private void OnConsumed(FoodHoverData consumedData)
    {
        consumed = true;
        ApplyEffectToPlayer(consumedData);
        panel?.Hide();
        enabled = false;
    }

    private void ApplyEffectToPlayer(FoodHoverData consumedData)
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        // 튜토리얼 매니저에 음식 획득 알림
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnItemCollected();
        }

        switch (consumedData.effectType)
        {
            case FoodEffectType.Heal:
                player.Heal(consumedData.effectAmount);
                break;
            case FoodEffectType.Damage:
                player.TakeFoodDamage(consumedData.effectAmount);
                player.PlayDamageFlash();
                break;
            case FoodEffectType.CleanseDebuff:
                SpawnEffectText(player.transform, "Cleanse", healTextColor);
                break;
            case FoodEffectType.Buff:
                SpawnEffectText(player.transform, "Buff", healTextColor);
                break;
        }
    }

    private void SpawnEffectText(Transform target, string text, Color color)
    {
        if (target == null) return;
        StartCoroutine(EffectTextRoutine(target, text, color));
    }

    private System.Collections.IEnumerator EffectTextRoutine(Transform target, string text, Color color)
    {
        var go = new GameObject("FoodEffectText");
        go.transform.SetParent(target);
        go.transform.localPosition = effectTextOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = effectTextCharacterSize;
        tm.fontSize = effectTextFontSize;
        tm.color = color;
        tm.richText = false;
        tm.font = effectTextFont != null ? effectTextFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder + 50;
            renderer.material = tm.font != null ? tm.font.material : new Material(Shader.Find("GUI/Text Shader"));
        }

        Vector3 basePos = effectTextOffset;
        float elapsed = 0f;
        while (elapsed < effectTextDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / effectTextDuration);
            float vertical = Mathf.Sin(t * Mathf.PI) * effectTextAmplitude;
            go.transform.localPosition = basePos + new Vector3(0f, vertical, 0f);

            var c = tm.color;
            c.a = 1f - t;
            tm.color = c;
            yield return null;
        }
        Destroy(go);
    }
}
