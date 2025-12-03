using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 튜토리얼 패널 UI를 관리
/// 플레이어를 따라다니며 현재 스텝 정보를 표시
/// </summary>
public class TutorialUI : MonoBehaviour
{
  [Header("Panel Settings")]
  public RectTransform panelRect;
  public float panelOffsetY = 150f;
  public bool followPlayer = false;

  [Header("Animation")]
  public float fadeSpeed = 3f;
  public float scaleAnimSpeed = 5f;
  public float targetScale = 1f;

  [Header("References")]
  public CanvasGroup canvasGroup;
  public TextMeshProUGUI stepCounterText;

  private Camera mainCamera;
  private Transform playerTransform;
  private Vector3 currentVelocity;
  private float currentScale = 0f;
  private bool isVisible = true;

  private void Start()
  {
    mainCamera = Camera.main;

    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerTransform = player.transform;
    }

    if (canvasGroup == null)
    {
      canvasGroup = GetComponent<CanvasGroup>();
    }
  }

  private void Update()
  {
    // 스텝 카운터 업데이트
    UpdateStepCounter();

    // 페이드 애니메이션
    if (canvasGroup != null)
    {
      float targetAlpha = isVisible ? 1f : 0f;
      canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    // 스케일 애니메이션
    currentScale = Mathf.Lerp(currentScale, isVisible ? targetScale : 0f, Time.deltaTime * scaleAnimSpeed);
    transform.localScale = Vector3.one * currentScale;

    // 플레이어 따라다니기
    if (followPlayer && playerTransform != null && mainCamera != null)
    {
      Vector3 screenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
      screenPos.y += panelOffsetY;

      if (panelRect != null)
      {
        panelRect.position = Vector3.SmoothDamp(panelRect.position, screenPos, ref currentVelocity, 0.1f);
      }
    }
  }

  private void UpdateStepCounter()
  {
    if (stepCounterText == null || TutorialManager.Instance == null)
      return;

    int current = TutorialManager.Instance.GetCurrentStepIndex() + 1;
    int total = TutorialManager.Instance.GetTotalSteps();

    stepCounterText.text = $"{current} / {total}";
  }

  public void Show()
  {
    isVisible = true;
    gameObject.SetActive(true);
  }

  public void Hide()
  {
    isVisible = false;
  }

  public void SetFollowPlayer(bool follow)
  {
    followPlayer = follow;
  }
}
