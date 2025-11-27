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

    [Header("Stamina Bar Settings")]
    [SerializeField] private SpriteRenderer staminaBar; // 스태미나 바 (SpriteRenderer)
    [SerializeField] private float maxStaminaBarWidth = 1f; // 스태미나 바 최대 X 스케일 (항상 1)

    [Header("Stamina Text Settings")]
    [SerializeField] private TextMesh staminaText; // 현재 스태미나 표기용 텍스트
    [SerializeField] private Color staminaTextColor = Color.white;
    [SerializeField] private Vector3 textLocalOffset = new Vector3(-0.4f, 0.2f, -1f); // 기본: 왼쪽/아래로 조금 이동
    [SerializeField] private int staminaTextFontSize = 48;
    [SerializeField] private Font staminaFont; // 둥근모꼴 등 원하는 폰트 지정 (Dog Health UI와 동일하게 설정 가능)

    [Header("Screen Follow (UI처럼 보이게)")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private Transform cameraTarget; // 비워두면 Camera.main
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 4f, 0f);
    [SerializeField] private bool freezeRotation = true;

    private void Awake()
    {
        // 싱글톤 패턴
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("씬에 두 개 이상의 StaminaManager가 존재합니다!");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentStamina = 0f; // 0부터 시작

        // 스태미나 바 초기화(X 스케일을 0으로 설정)
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
        // 자동 회복
        if (currentStamina < maxStamina)
        {
            float previousStamina = currentStamina;
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            // 1씩 올라갈 때만 로그
            if (Mathf.FloorToInt(previousStamina) < Mathf.FloorToInt(currentStamina))
            {
                Debug.Log($"[스태미나] 회복: {Mathf.FloorToInt(currentStamina)}/{maxStamina}");
            }

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
    /// 스태미나를 사용한다.
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
    /// UI 업데이트
    /// </summary>
    private void UpdateStaminaUI()
    {
        float staminaRatio = maxStamina > 0f ? currentStamina / maxStamina : 0f;

        // 스태미나 바 갱신
        if (staminaBar != null)
        {
            Vector3 newScale = staminaBar.transform.localScale;
            newScale.x = maxStaminaBarWidth * staminaRatio;
            staminaBar.transform.localScale = newScale;
        }

        UpdateStaminaText();
    }

    /// <summary>
    /// 현재 스태미나 비율 (0~1)
    /// </summary>
    public float GetStaminaRatio()
    {
        return maxStamina > 0f ? currentStamina / maxStamina : 0f;
    }

    /// <summary>
    /// 현재 스태미나 값
    /// </summary>
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
        // 지정된 폰트가 없으면 기본 폰트 사용
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
