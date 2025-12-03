using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임 오버 상태를 표현하고, 게임 점수와 UI를 관리하는 게임 매니저
// 씬에는 단 하나의 게임 매니저만 존재할 수 있다.
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 싱글톤을 할당할 전역 변수

    public bool isGameover = false; // 게임 오버 상태
    public Text scoreText; // 점수를 출력할 UI 텍스트
    public GameObject gameoverUI; // 게임 오버시 활성화 할 UI 게임 오브젝트

    [Header("HP Bar Settings")]
    public SpriteRenderer hpBar; // HP 바 (SpriteRenderer)
    public float maxHealth = 9f; // 최대 체력
    public float initialHealth = 9f; // 씬 시작 시 표시할 체력 값

    private float maxHpBarScaleX; // Transform localScale.x 기준 최대값
    private float maxHpBarSizeX;  // SpriteRenderer.size.x 기준 최대값 (Sliced/Tiled 전용)
    private bool useSpriteSize = false;

    // InfoBar는 Frame.png로 칸을 나누는 껍데기
    // 1칸 = maxHealth의 1/9

    private int score = 0; // 게임 점수

    // 게임 시작과 동시에 싱글톤을 구성
    void Awake()
    {
        // 싱글톤 변수 instance가 비어있는가?
        if (instance == null)
        {
            // instance가 비어있다면(null) 그곳에 자기 자신을 할당
            instance = this;
        }
        else
        {
            // instance에 이미 다른 GameManager 오브젝트가 할당되어 있는 경우

            // 씬에 두개 이상의 GameManager 오브젝트가 존재한다는 의미.
            // 싱글톤 오브젝트는 하나만 존재해야 하므로 자신의 게임 오브젝트를 파괴
            Debug.LogWarning("씬에 두개 이상의 게임 매니저가 존재합니다!");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // HP 바 초기화
        if (hpBar != null)
        {
            useSpriteSize = hpBar.drawMode != SpriteDrawMode.Simple;

            if (useSpriteSize)
            {
                maxHpBarSizeX = hpBar.size.x;
                if (maxHpBarSizeX <= Mathf.Epsilon)
                {
                    maxHpBarSizeX = 1f;
                    Debug.LogWarning("[GameManager] hpBar.size.x가 0이어서 기본값 1을 사용합니다.");
                }
                Debug.Log($"HP 바 초기화 (SpriteRenderer.size 사용): width={maxHpBarSizeX}");
            }
            else
            {
                maxHpBarScaleX = Mathf.Abs(hpBar.transform.localScale.x);
                if (maxHpBarScaleX <= Mathf.Epsilon)
                {
                    maxHpBarScaleX = 1f;
                    Debug.LogWarning("[GameManager] hpBar localScale.x가 0이어서 기본값 1을 사용합니다.");
                }
                Debug.Log($"HP 바 초기화 (Transform.localScale 사용): width={maxHpBarScaleX}");
            }

            // 초기 상태에서도 체력 비율이 정확히 반영되도록 한 번 업데이트
            float clampedInitial = Mathf.Clamp(initialHealth, 0f, maxHealth);
            UpdateHealth(clampedInitial);
        }
    }

    void Update()
    {
        // 게임 오버 상태에서 R키를 누르면 씬을 다시 로드
        if (isGameover && Input.GetKeyDown(KeyCode.R))
        {
            // 재시작 시 저장된 상태 클리어
            if (PlayerStateManager.instance != null)
            {
                PlayerStateManager.instance.ClearSavedState();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // 점수를 증가시키는 메서드
    public void AddScore(int newScore)
    {
        if (!isGameover)
        {
            score += newScore;
            scoreText.text = "Score : " + score;
        }
    }

    // 플레이어 캐릭터가 사망시 리스폰을 실행하는 메서드
    public void OnPlayerDead()
    {
        // 토스트만 유지하고 체력/스태미나는 리셋되도록 저장
        if (PlayerStateManager.instance != null)
        {
            PlayerStateManager.instance.SaveToastOnlyForRespawn();
            Debug.Log("[GameManager] 리스폰을 위해 토스트 상태만 저장");
        }

        // ScreenFadeController를 통해 페이드 아웃 -> 3초 대기 -> 페이드 인 -> 리스폰
        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.RespawnWithFade(3f);
        }
        else
        {
            // ScreenFadeController가 없으면 기존 방식으로 fallback
            Debug.LogWarning("[GameManager] ScreenFadeController를 찾을 수 없어 기존 게임오버 처리");
            isGameover = true;
            gameoverUI.SetActive(true);
        }
    }

    // 플레이어의 체력이 변경되었을 때 HP 바 UI를 업데이트하는 메서드
    public void UpdateHealth(float health)
    {
        // HP 바 시스템 업데이트
        if (hpBar != null)
        {
            // InfoBar는 9칸으로 나뉘어져 있음
            // health가 0~9 사이의 값일 때, X 스케일을 비례적으로 조정
            float healthRatio = Mathf.Clamp01(health / maxHealth);

            if (useSpriteSize)
            {
                Vector2 size = hpBar.size;
                size.x = maxHpBarSizeX * healthRatio;
                hpBar.size = size;
            }
            else
            {
                Vector3 newScale = hpBar.transform.localScale;
                newScale.x = maxHpBarScaleX * healthRatio;
                hpBar.transform.localScale = newScale;
            }

            Debug.Log($"HP 바 업데이트: 체력={health}/{maxHealth}, 비율={healthRatio:0.00}");
        }
    }

    /// <summary>
    /// 특정 스테이지(씬)를 로드합니다.
    /// StageTransitionWall에서 호출됩니다.
    /// 플레이어 상태(HP, 스태미나, 토스트)를 저장하고 다음 스테이지에서 복원합니다.
    /// </summary>
    /// <param name="stageName">로드할 스테이지 씬 이름</param>
    public void LoadStage(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
        {
            Debug.LogError("[GameManager] 스테이지 이름이 비어있습니다!");
            return;
        }

        Debug.Log($"[GameManager] 스테이지 로드: {stageName}");

        // 플레이어 상태 저장 (스테이지 전환 전)
        if (PlayerStateManager.instance != null)
        {
            PlayerStateManager.instance.SavePlayerState();
            Debug.Log("[GameManager] 플레이어 상태 저장 완료");
        }
        else
        {
            // PlayerStateManager가 없으면 자동 생성
            var stateManagerGO = new GameObject("PlayerStateManager");
            stateManagerGO.AddComponent<PlayerStateManager>();
            PlayerStateManager.instance.SavePlayerState();
            Debug.Log("[GameManager] PlayerStateManager 자동 생성 및 상태 저장");
        }

        // 게임오버 상태 초기화
        isGameover = false;

        SceneManager.LoadScene(stageName);
    }

    /// <summary>
    /// 현재 스테이지에서 다음 스테이지로 자동 이동합니다.
    /// 예: Stage1 -> Stage2, Stage10 -> Stage11
    /// 플레이어 상태(HP, 스태미나, 토스트)를 저장하고 다음 스테이지에서 복원합니다.
    /// </summary>
    public void LoadNextStage()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 플레이어 상태 저장 (스테이지 전환 전)
        if (PlayerStateManager.instance != null)
        {
            PlayerStateManager.instance.SavePlayerState();
            Debug.Log("[GameManager] 플레이어 상태 저장 완료");
        }
        else
        {
            // PlayerStateManager가 없으면 자동 생성
            var stateManagerGO = new GameObject("PlayerStateManager");
            stateManagerGO.AddComponent<PlayerStateManager>();
            PlayerStateManager.instance.SavePlayerState();
            Debug.Log("[GameManager] PlayerStateManager 자동 생성 및 상태 저장");
        }

        // "Stage" 접두사 확인
        if (currentSceneName.StartsWith("Stage"))
        {
            string numberPart = currentSceneName.Substring(5);
            if (int.TryParse(numberPart, out int currentNumber))
            {
                string nextStage = "Stage" + (currentNumber + 1);
                LoadStage(nextStage);
                return;
            }
        }

        Debug.LogWarning($"[GameManager] 다음 스테이지를 자동으로 계산할 수 없습니다. 현재: {currentSceneName}");
    }
}
