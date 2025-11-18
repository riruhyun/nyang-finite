using UnityEngine;

/// <summary>
/// 플레이어의 스태미나를 관리하고 UI로 표시하는 매니저
/// 자동으로 초당 staminaRegenRate만큼 회복
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
    private float maxStaminaBarWidth = 1f; // 스태미나 바의 최대 X 스케일 (항상 1)

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

        // 스태미나 바 초기화 (X 스케일을 0으로 시작)
        if (staminaBar != null)
        {
            Vector3 scale = staminaBar.transform.localScale;
            scale.x = 0f; // 초기값 0 (스태미나가 0이므로)
            staminaBar.transform.localScale = scale;
            Debug.Log($"스태미나 바 초기화: 최대 X 스케일={maxStaminaBarWidth}, 현재={scale.x}");
        }

        UpdateStaminaUI();
        Debug.Log($"[스태미나] 초기화: {currentStamina}/{maxStamina}");
    }

    private void Update()
    {
        // 자동 회복
        if (currentStamina < maxStamina)
        {
            float previousStamina = currentStamina;
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            // 1이 차면 로그 출력 (디버그용)
            if (Mathf.FloorToInt(previousStamina) < Mathf.FloorToInt(currentStamina))
            {
                Debug.Log($"[스태미나] 회복: {Mathf.FloorToInt(currentStamina)}/{maxStamina}");
            }

            UpdateStaminaUI();
        }
    }

    /// <summary>
    /// 스태미나를 소모하는 메서드
    /// </summary>
    /// <param name="amount">소모할 스태미나 양</param>
    /// <returns>스태미나가 충분하면 true, 부족하면 false</returns>
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
    /// 스태미나를 즉시 회복하는 메서드
    /// </summary>
    /// <param name="amount">회복할 스태미나 양</param>
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
        float staminaRatio = currentStamina / maxStamina;

        // 스태미나 바 업데이트 (SpriteRenderer)
        if (staminaBar != null)
        {
            // Transform의 localScale.x를 조정하여 가로 길이 변경
            Vector3 newScale = staminaBar.transform.localScale;
            newScale.x = maxStaminaBarWidth * staminaRatio;
            staminaBar.transform.localScale = newScale;
        }
    }

    /// <summary>
    /// 현재 스태미나 비율 (0~1)
    /// </summary>
    public float GetStaminaRatio()
    {
        return currentStamina / maxStamina;
    }

    /// <summary>
    /// 현재 스태미나 값
    /// </summary>
    public float GetCurrentStamina()
    {
        return currentStamina;
    }
}
