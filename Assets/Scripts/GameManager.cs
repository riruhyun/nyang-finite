using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임 오버 상태를 표현하고, 게임 점수와 UI를 관리하는 게임 매니저
// 씬에는 단 하나의 게임 매니저만 존재할 수 있다.
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 싱글톤을 할당할 전역 변수

    public GameObject heartPrefab; // Heart1 오브젝트를 참조할 변수
    public bool isGameover = false; // 게임 오버 상태
    public Text scoreText; // 점수를 출력할 UI 텍스트
    public GameObject gameoverUI; // 게임 오버시 활성화 할 UI 게임 오브젝트
    private GameObject[] hearts = new GameObject[9]; // 생성된 하트들을 저장할 배열
    private int currentHeartCount = 9; // 현재 표시되는 하트 수

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
        CreateHearts();
    }

    void CreateHearts()
    {
        if (heartPrefab == null)
        {
            Debug.LogError("Heart1 프리팹이 할당되지 않았습니다!");
            return;
        }

        Transform healthBar = heartPrefab.transform.parent;

        Vector3 startPosition = heartPrefab.transform.localPosition;
        float heartSpacing = 90f;

        hearts[0] = heartPrefab;

        for (int i = 1; i < 9; i++)
        {
            GameObject newHeart = Instantiate(heartPrefab, healthBar);
            newHeart.name = "Heart" + (i + 1);


            Vector3 newPosition = startPosition;
            newPosition.x += heartSpacing * i;
            newHeart.transform.localPosition = newPosition;

            hearts[i] = newHeart;
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

    // 플레이어의 체력이 변경되었을 때 하트 UI를 업데이트하는 메서드
    public void UpdateHealth(int health)
    {
        // 체력이 현재 하트 수보다 적으면 하트를 제거
        while (currentHeartCount > health && currentHeartCount > 0)
        {
            currentHeartCount--;
            if (hearts[currentHeartCount] != null)
            {
                hearts[currentHeartCount].SetActive(false);
                Debug.Log($"하트 제거: Heart{currentHeartCount + 1}, 남은 체력: {health}");
            }
        }

        // 체력이 현재 하트 수보다 많으면 하트를 추가 (회복 시)
        while (currentHeartCount < health && currentHeartCount < 9)
        {
            if (hearts[currentHeartCount] != null)
            {
                hearts[currentHeartCount].SetActive(true);
            }
            currentHeartCount++;
        }
    }
}