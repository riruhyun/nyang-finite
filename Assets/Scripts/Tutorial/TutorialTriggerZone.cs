using UnityEngine;

/// <summary>
/// 플레이어가 특정 영역에 진입하면 튜토리얼 이벤트 발생
/// </summary>
public class TutorialTriggerZone : MonoBehaviour
{
  public enum TriggerAction
  {
    NextStep,           // 다음 스텝으로
    CompleteStep,       // 현재 스텝 완료
    ShowMessage,        // 메시지 표시
    SpawnEnemy,         // 적 스폰
    SpawnFood,          // 음식 스폰
    SpawnToast          // 토스트 스폰
  }

  [Header("Trigger Settings")]
  public TriggerAction action = TriggerAction.CompleteStep;
  public bool triggerOnce = true;
  public string customMessage;

  [Header("Spawn Settings")]
  public GameObject spawnPrefab;
  public Transform spawnPoint;

  [Header("Visual")]
  public bool showGizmo = true;
  public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

  private bool hasTriggered = false;
  private BoxCollider2D boxCollider;

  private void Awake()
  {
    boxCollider = GetComponent<BoxCollider2D>();
    if (boxCollider != null)
    {
      boxCollider.isTrigger = true;
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (triggerOnce && hasTriggered)
      return;

    if (!other.CompareTag("Player"))
      return;

    hasTriggered = true;
    ExecuteAction();
  }

  private void ExecuteAction()
  {
    switch (action)
    {
      case TriggerAction.NextStep:
        if (TutorialManager.Instance != null)
        {
          TutorialManager.Instance.NextStep();
        }
        break;

      case TriggerAction.CompleteStep:
        if (TutorialManager.Instance != null)
        {
          TutorialManager.Instance.CompleteCurrentStep();
        }
        break;

      case TriggerAction.ShowMessage:
        Debug.Log($"[Tutorial] 메시지: {customMessage}");
        break;

      case TriggerAction.SpawnEnemy:
      case TriggerAction.SpawnFood:
      case TriggerAction.SpawnToast:
        if (spawnPrefab != null)
        {
          Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
          Instantiate(spawnPrefab, pos, Quaternion.identity);
        }
        break;
    }
  }

  public void ResetTrigger()
  {
    hasTriggered = false;
  }

  private void OnDrawGizmos()
  {
    if (!showGizmo)
      return;

    Gizmos.color = gizmoColor;

    BoxCollider2D col = GetComponent<BoxCollider2D>();
    if (col != null)
    {
      Vector3 center = transform.position + (Vector3)col.offset;
      Vector3 size = col.size;
      Gizmos.DrawCube(center, size);

      // 테두리
      Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
      Gizmos.DrawWireCube(center, size);
    }
    else
    {
      Gizmos.DrawCube(transform.position, Vector3.one);
    }
  }
}
