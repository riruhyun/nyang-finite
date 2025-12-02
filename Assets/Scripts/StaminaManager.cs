using UnityEngine;

/// <summary>
/// Manages player stamina and shows both bar and numeric UI (current only).
/// The UI uses SpriteRenderer/TextMesh so it can move with the scene but stay aligned to the camera.
/// </summary>
public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 0f;
    [SerializeField] private float staminaRegenRate = 2f;
    private float baseStaminaRegenRate = 2f;
    private float staminaRegenBonus = 0f;

    [Header("Stamina Bar Settings")]
    [SerializeField] private SpriteRenderer staminaBar; // ?�태미나 �?(SpriteRenderer)
    [SerializeField] private float maxStaminaBarWidth = 1f; // ?�태미나 �?최�? X ?��???(??�� 1)

    [Header("Stamina Text Settings")]
    [SerializeField] private TextMesh staminaText; // current stamina text
    [SerializeField] private Color staminaTextColor = Color.white;
    [SerializeField] private Vector3 textLocalOffset = new Vector3(-0.4f, 0.2f, -1f); // 기본: ?�쪽/?�래�?조금 ?�동
    [SerializeField] private int staminaTextFontSize = 48;
    [SerializeField] private Font staminaFont; // ?�근모꼴 ???�하???�트 지??(Dog Health UI?� ?�일?�게 ?�정 가??

    [Header("Screen Follow (UI처럼 보이�?")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private Transform cameraTarget; // 비워?�면 Camera.main
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 4f, 0f);
    [SerializeField] private bool freezeRotation = true;

    private void Awake()
    {
        // ?��????�턴
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("?�에 ??�??�상??StaminaManager가 존재?�니??");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentStamina = 0f; // 0부터 시작
        baseStaminaRegenRate = staminaRegenRate;

        // 스태미나 바 초기화 (X 스케일을 0으로 설정)
        if (staminaBar != null)
        {
            Vector3 scale = staminaBar.transform.localScale;
            scale.x = 0f;
            staminaBar.transform.localScale = scale;
        }

        EnsureCameraTarget();
        EnsureStaminaText();
        UpdateStaminaUI();
    }

    private void Update()
    {
        // 자동 회복 (stamina_regen 보너스 적용)
        if (currentStamina < maxStamina)
        {
            float effectiveRegenRate = baseStaminaRegenRate + staminaRegenBonus;
            currentStamina += effectiveRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            UpdateStaminaUI();
        }
    }

    private void LateUpdate()
    {
        if (!followCamera) return;

        EnsureCameraTarget();
        if (cameraTarget != null)
        {
            transform.position = cameraTarget.position + cameraOffset;
            if (freezeRotation)
            {
                transform.rotation = Quaternion.identity;
            }
        }
    }

    /// <summary>
    /// ?�태미나�??�용?�다.
    /// </summary>
    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            currentStamina = Mathf.Max(currentStamina, 0);
            UpdateStaminaUI();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 스태미나를 회복한다.
    /// </summary>
    public void RecoverStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        UpdateStaminaUI();
    }

    /// <summary>
    /// stamina_regen stat 보너스를 적용한다
    /// </summary>
    public void ApplyStaminaRegenBonus(float bonus)
    {
        staminaRegenBonus = bonus;
        Debug.Log($"[StaminaManager] Stamina regen bonus applied: {bonus:F2} (effective rate: {baseStaminaRegenRate + staminaRegenBonus:F2})");
    }

    /// <summary>
    /// UI ?�데?�트
    /// </summary>
    private void UpdateStaminaUI()
    {
        float staminaRatio = maxStamina > 0f ? currentStamina / maxStamina : 0f;

        // ?�태미나 �?갱신
        if (staminaBar != null)
        {
            Vector3 newScale = staminaBar.transform.localScale;
            newScale.x = maxStaminaBarWidth * staminaRatio;
            staminaBar.transform.localScale = newScale;
        }

        UpdateStaminaText();
    }

    /// <summary>
    /// ?�재 ?�태미나 비율 (0~1)
    /// </summary>
    public float GetStaminaRatio()
    {
        return maxStamina > 0f ? currentStamina / maxStamina : 0f;
    }

    /// <summary>
    /// ?�재 ?�태미나 �?    /// </summary>
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    private void EnsureCameraTarget()
    {
        if (cameraTarget == null && Camera.main != null)
        {
            cameraTarget = Camera.main.transform;
        }
    }

    private void EnsureStaminaText()
    {
        if (staminaText != null) return;

        var textGO = new GameObject("StaminaText");
        textGO.transform.SetParent(transform);
        textGO.transform.localPosition = textLocalOffset;
        textGO.transform.localRotation = Quaternion.identity;
        textGO.transform.localScale = Vector3.one;

        staminaText = textGO.AddComponent<TextMesh>();
        staminaText.anchor = TextAnchor.MiddleCenter;
        staminaText.alignment = TextAlignment.Center;
        staminaText.characterSize = 0.1f;
        staminaText.fontSize = staminaTextFontSize;
        staminaText.color = staminaTextColor;
        staminaText.richText = false;
        // 지?�된 ?�트가 ?�으�?기본 ?�트 ?�용
        staminaText.font = staminaFont != null
            ? staminaFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var renderer = textGO.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            string layer = staminaBar != null ? staminaBar.sortingLayerName : "UI";
            int order = staminaBar != null ? staminaBar.sortingOrder + 5 : 100;
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
            renderer.material = staminaText.font != null
                ? staminaText.font.material
                : new Material(Shader.Find("GUI/Text Shader"));
        }
    }

    private void UpdateStaminaText()
    {
        EnsureStaminaText();
        if (staminaText == null) return;

        int currentValue = Mathf.RoundToInt(Mathf.Max(0f, currentStamina));
        staminaText.text = $"{currentValue}";

        staminaText.color = staminaTextColor;
        staminaText.fontSize = staminaTextFontSize;
        staminaText.transform.localPosition = textLocalOffset;

        var renderer = staminaText.GetComponent<MeshRenderer>();
        if (renderer != null && staminaBar != null)
        {
            renderer.sortingLayerName = staminaBar.sortingLayerName;
            renderer.sortingOrder = staminaBar.sortingOrder + 5;
        }
    }
}


