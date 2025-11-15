using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어의 스태미나를 관리하고 UI로 표시하는 매니저
/// 자동으로 초당 1씩 회복
/// </summary>
public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 0f;
    [SerializeField] private float staminaRegenRate = 2f;

    [Header("UI Settings")]
    [SerializeField] private Image staminaBarFill; // Fill 이미지
    
    private RectTransform staminaBarBGRect; // 스태미나 BG RectTransform 캠싱
[SerializeField] private Text staminaText; // 스태미나 수치 텍스트 (옵션)

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
        
        // StaminaBarBG RectTransform 캠싱
        if (staminaBarFill != null)
        {
            Transform bgTransform = staminaBarFill.transform.parent;
            if (bgTransform != null)
            {
                staminaBarBGRect = bgTransform.GetComponent<RectTransform>();
            }
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
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;
        }
        
        // StaminaBarBG의 가로 길이만 변경 (위치는 고정)
        if (staminaBarBGRect != null)
        {
            // 최대 길이 400, 최소 길이 0
            float maxWidth = 400f;
            float newWidth = (currentStamina / maxStamina) * maxWidth;
            
            // 크기만 변경 (위치는 그대로)
            staminaBarBGRect.sizeDelta = new Vector2(newWidth, staminaBarBGRect.sizeDelta.y);
        }

        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(currentStamina)} / {maxStamina}";
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
