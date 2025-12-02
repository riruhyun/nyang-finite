using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 일시정지를 관리하는 매니저
/// 화면 우측 상단의 일시정지 버튼과 일시정지 오버레이 UI를 제어합니다.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI References")]
    [Tooltip("일시정지 버튼")]
    public Button pauseButton;

    [Tooltip("일시정지 오버레이 패널 (반투명 검은 배경 + 텍스트)")]
    public GameObject pauseOverlay;

    [Tooltip("일시정지 텍스트")]
    public TextMeshProUGUI pauseText;

    [Header("Settings")]
    [Tooltip("일시정지 중 오버레이 배경 색상")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.7f);

    private bool isPaused = false;
    private Image overlayImage;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("씬에 두 개 이상의 PauseManager가 존재합니다!");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // UI 자동 참조 찾기 (Inspector에서 설정되지 않은 경우)
        if (pauseButton == null)
        {
            GameObject pauseButtonObj = GameObject.Find("PauseButton");
            if (pauseButtonObj != null)
            {
                pauseButton = pauseButtonObj.GetComponent<Button>();
            }
        }

        if (pauseOverlay == null)
        {
            pauseOverlay = GameObject.Find("PauseOverlay");
        }

        if (pauseText == null && pauseOverlay != null)
        {
            pauseText = pauseOverlay.GetComponentInChildren<TextMeshProUGUI>();
        }

        // 일시정지 버튼 클릭 이벤트 등록
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        // 오버레이 이미지 컴포넌트 가져오기
        if (pauseOverlay != null)
        {
            overlayImage = pauseOverlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.color = overlayColor;
            }

            // 오버레이 클릭 시 게임 재개
            Button overlayButton = pauseOverlay.GetComponent<Button>();
            if (overlayButton != null)
            {
                overlayButton.onClick.AddListener(ResumeGame);
            }

            // 시작 시 오버레이 비활성화
            pauseOverlay.SetActive(false);
        }
    }

    void Update()
    {
        // ESC 키로도 일시정지 토글 가능
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// 일시정지 상태를 토글합니다.
    /// </summary>
    public void TogglePause()
    {
        // 게임 오버 상태에서는 일시정지 불가
        if (GameManager.instance != null && GameManager.instance.isGameover)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// 게임을 일시정지합니다.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(true);
        }

        Debug.Log("[PauseManager] 게임 일시정지");
    }

    /// <summary>
    /// 게임을 재개합니다.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(false);
        }

        Debug.Log("[PauseManager] 게임 재개");
    }

    /// <summary>
    /// 현재 일시정지 상태를 반환합니다.
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }

    void OnDestroy()
    {
        // 씬 전환 시 timeScale 복구
        Time.timeScale = 1f;

        if (instance == this)
        {
            instance = null;
        }
    }
}
