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
    private float maxHpBarWidth; // HP 바의 최대 X 스케일 (초기값 저장)

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
            // 초기 스케일 저장 (최대 체력일 때의 스케일)
            maxHpBarWidth = hpBar.transform.localScale.x;
            Debug.Log($"HP 바 초기화: 초기 X 스케일={maxHpBarWidth}");
        }
    }

    void Update()
    {
        // 게임 오버 상태에서 R키를 누르면 씬을 다시 로드
        if (isGameover && Input.GetKeyDown(KeyCode.R))
        {
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

    // 플레이어 캐릭터가 사망시 게임 오버를 실행하는 메서드
    public void OnPlayerDead()
    {
        isGameover = true;
        gameoverUI.SetActive(true);
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

            // Transform의 localScale.x를 조정하여 가로 길이 변경
            Vector3 newScale = hpBar.transform.localScale;
            newScale.x = maxHpBarWidth * healthRatio;
            hpBar.transform.localScale = newScale;

            Debug.Log($"HP 바 업데이트: 체력={health}/{maxHealth}, X 스케일={newScale.x}/{maxHpBarWidth}");
        }
    }
}
