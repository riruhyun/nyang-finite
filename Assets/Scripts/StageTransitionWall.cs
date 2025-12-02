using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플랫폼 양 끝에 배치하여 플레이어가 닿으면 다음 스테이지로 이동하는 벽
/// 
/// 사용법:
/// 1. 빈 GameObject 생성 후 이 스크립트 추가
/// 2. BoxCollider2D가 자동으로 추가됨 (Is Trigger = true)
/// 3. 플랫폼 양 끝에 배치
/// 4. 다음 스테이지 이름 설정 (비워두면 자동으로 다음 번호 스테이지로 이동)
/// 
/// GameManager 연동:
/// - GameManager.instance.LoadNextStage() 호출하여 스테이지 전환
/// - GameManager.instance.LoadStage("StageName") 으로 특정 스테이지 로드 가능
/// </summary>
public class StageTransitionWall : MonoBehaviour
{
    [Header("Stage Settings")]
    [Tooltip("다음 스테이지 씬 이름 (비워두면 현재 스테이지 번호 + 1로 자동 계산)")]
    public string nextStageName = "";

    [Tooltip("전환 가능한 플레이어 태그")]
    public string playerTag = "Player";

    [Header("Visual Settings")]
    [Tooltip("Gizmo 색상 (에디터에서만 보임)")]
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.5f);

    private BoxCollider2D boxCollider;
    private bool hasTriggered = false; // 중복 전환 방지

    private void Awake()
    {
        // BoxCollider2D 자동 추가 및 설정
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;

        // 기본 크기 설정 (필요에 따라 인스펙터에서 조정)
        if (boxCollider.size == Vector2.one)
        {
            boxCollider.size = new Vector2(0.5f, 3f); // 벽 형태: 좁고 높게
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 전환 중이거나 게임오버면 무시
        if (hasTriggered) return;
        if (GameManager.instance != null && GameManager.instance.isGameover) return;

        // 플레이어 태그 확인
        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;
            LoadNextStage();
        }
    }

    private void LoadNextStage()
    {
        // 다음 스테이지 이름이 지정되어 있으면 해당 스테이지로 이동
        if (!string.IsNullOrEmpty(nextStageName))
        {
            Debug.Log($"[StageTransition] 지정된 스테이지로 이동: {nextStageName}");

            if (GameManager.instance != null)
            {
                GameManager.instance.LoadStage(nextStageName);
            }
            else
            {
                SceneManager.LoadScene(nextStageName);
            }
            return;
        }

        // 현재 스테이지 이름에서 번호 추출하여 다음 스테이지로 이동
        string currentSceneName = SceneManager.GetActiveScene().name;
        string nextStage = GetNextStageName(currentSceneName);

        if (!string.IsNullOrEmpty(nextStage))
        {
            Debug.Log($"[StageTransition] 다음 스테이지로 이동: {currentSceneName} -> {nextStage}");

            if (GameManager.instance != null)
            {
                GameManager.instance.LoadStage(nextStage);
            }
            else
            {
                SceneManager.LoadScene(nextStage);
            }
        }
        else
        {
            Debug.LogWarning($"[StageTransition] 다음 스테이지를 찾을 수 없습니다. 현재: {currentSceneName}");
        }
    }

    /// <summary>
    /// 현재 스테이지 이름에서 다음 스테이지 이름을 계산
    /// 예: Stage1 -> Stage2, Stage10 -> Stage11
    /// </summary>
    private string GetNextStageName(string currentName)
    {
        // "Stage" 접두사 확인
        if (!currentName.StartsWith("Stage")) return null;

        // 숫자 부분 추출
        string numberPart = currentName.Substring(5); // "Stage" 다음부터
        if (int.TryParse(numberPart, out int currentNumber))
        {
            return "Stage" + (currentNumber + 1);
        }

        return null;
    }

    // 에디터에서 벽 위치를 시각적으로 확인
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);
            Gizmos.DrawWireCube(col.offset, col.size);
        }
        else
        {
            // 콜라이더가 없으면 기본 크기로 표시
            Gizmos.DrawCube(transform.position, new Vector3(0.5f, 3f, 0.1f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.offset, col.size);
        }
    }
}
