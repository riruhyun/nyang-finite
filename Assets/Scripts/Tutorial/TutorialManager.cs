using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 튜토리얼 전체 흐름을 관리하는 매니저
/// 긴 바닥을 따라 이동하면서 각 스텝을 순차적으로 진행
/// </summary>
public class TutorialManager : MonoBehaviour
{
  public static TutorialManager Instance { get; private set; }

  [System.Serializable]
  public class TutorialStep
  {
    public string stepId;
    public string title;
    [TextArea(2, 4)]
    public string description;
    public Sprite keyIcon;           // 키 아이콘 (A, D, W, Q, K, L)
    public Sprite secondKeyIcon;     // 두 번째 키 아이콘 (A+D 같은 경우)
    public TutorialTriggerType triggerType;
    public KeyCode requiredKey;
    public KeyCode secondRequiredKey; // A+D 같은 조합용
    public int requiredCount = 1;    // 몇 번 수행해야 하는지
    public float requiredHoldTime;   // 홀드가 필요한 경우
    public Transform triggerZone;    // 특정 영역에 도달해야 하는 경우
    public bool isCompleted;
  }

  public enum TutorialTriggerType
  {
    KeyPress,           // 특정 키 누르기
    KeyHold,            // 특정 키 홀드
    KeyCombo,           // 키 조합 (A+D)
    ReachPosition,      // 특정 위치 도달
    CollectItem,        // 아이템 획득
    DefeatEnemy,        // 적 처치
    TakeToast,          // 토스트 획득
    WallJump,           // 벽점프
    Custom              // 커스텀 조건
  }

  [Header("Tutorial Steps")]
  public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
  private int currentStepIndex = 0;

  [Header("UI References")]
  public GameObject tutorialPanel;
  public TextMeshProUGUI titleText;
  public TextMeshProUGUI descriptionText;
  public Image keyIconImage;
  public Image secondKeyIconImage;
  public Image arrowImage;
  public Image checkImage;
  public GameObject progressPanel;
  public TextMeshProUGUI progressText;

  [Header("Arrow Settings")]
  public Sprite arrowSprite;
  public float arrowBobSpeed = 2f;
  public float arrowBobAmount = 0.3f;

  [Header("Sprites")]
  public Sprite checkSprite;
  public Sprite keyA;
  public Sprite keyD;
  public Sprite keyW;
  public Sprite keyQ;
  public Sprite keyK;
  public Sprite keyL;

  [Header("Audio")]
  public AudioClip stepCompleteSound;
  public AudioClip tutorialCompleteSound;
  private AudioSource audioSource;

  [Header("Settings")]
  public float panelFadeSpeed = 3f;
  public string nextSceneName = "Stage1";
  public bool autoProgressOnComplete = true;

  // 현재 스텝 진행 상태
  private int currentCount = 0;
  private float currentHoldTime = 0f;
  private bool isHolding = false;
  private bool isTutorialActive = true;
  private bool isTransitioning = false;  // 스텝 전환 중 플래그 (중복 완료 방지)
  private CanvasGroup panelCanvasGroup;

  // 플레이어 참조
  private PlayerController playerController;
  private Transform playerTransform;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }
  }

  private void Start()
  {
    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
      audioSource = gameObject.AddComponent<AudioSource>();
    }

    // 플레이어 찾기
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerController = player.GetComponent<PlayerController>();
      playerTransform = player.transform;
    }

    // Canvas Group 설정
    if (tutorialPanel != null)
    {
      panelCanvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
      if (panelCanvasGroup == null)
      {
        panelCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
      }
    }

    // 체크 이미지 초기 비활성화
    if (checkImage != null)
    {
      checkImage.gameObject.SetActive(false);
    }

    // 첫 번째 스텝 시작
    if (tutorialSteps.Count > 0)
    {
      StartStep(0);
    }
    else
    {
      Debug.LogWarning("[TutorialManager] 튜토리얼 스텝이 없습니다!");
    }
  }

  private void Update()
  {
    if (!isTutorialActive || currentStepIndex >= tutorialSteps.Count)
      return;

    // 스텝 전환 중에는 입력 체크 안함
    if (isTransitioning)
      return;

    TutorialStep currentStep = tutorialSteps[currentStepIndex];

    // 이미 완료된 스텝은 스킵
    if (currentStep.isCompleted)
    {
      return;
    }

    // 트리거 타입에 따른 체크
    switch (currentStep.triggerType)
    {
      case TutorialTriggerType.KeyPress:
        CheckKeyPress(currentStep);
        break;
      case TutorialTriggerType.KeyHold:
        CheckKeyHold(currentStep);
        break;
      case TutorialTriggerType.KeyCombo:
        CheckKeyCombo(currentStep);
        break;
      case TutorialTriggerType.ReachPosition:
        CheckReachPosition(currentStep);
        break;
      case TutorialTriggerType.WallJump:
        CheckWallJump(currentStep);
        break;
    }

    // 화살표 애니메이션
    UpdateArrowAnimation();

    // 진행도 업데이트
    UpdateProgressUI(currentStep);
  }

  private void CheckKeyPress(TutorialStep step)
  {
    if (Input.GetKeyDown(step.requiredKey))
    {
      currentCount++;
      Debug.Log($"[Tutorial] {step.stepId}: {currentCount}/{step.requiredCount}");

      if (currentCount >= step.requiredCount)
      {
        CompleteCurrentStep();
      }
    }
  }

  private void CheckKeyHold(TutorialStep step)
  {
    if (Input.GetKey(step.requiredKey))
    {
      if (!isHolding)
      {
        isHolding = true;
        currentHoldTime = 0f;
      }

      currentHoldTime += Time.deltaTime;

      if (currentHoldTime >= step.requiredHoldTime)
      {
        CompleteCurrentStep();
      }
    }
    else
    {
      isHolding = false;
      currentHoldTime = 0f;
    }
  }

  private void CheckKeyCombo(TutorialStep step)
  {
    // A+D 동시 누르기 같은 조합
    bool firstKey = Input.GetKey(step.requiredKey);
    bool secondKey = Input.GetKey(step.secondRequiredKey);

    if (firstKey && secondKey)
    {
      currentHoldTime += Time.deltaTime;
      if (currentHoldTime >= 0.5f) // 0.5초 동시 누르기
      {
        CompleteCurrentStep();
      }
    }
    else
    {
      currentHoldTime = 0f;
    }
  }

  private void CheckReachPosition(TutorialStep step)
  {
    if (step.triggerZone == null || playerTransform == null)
      return;

    float distance = Vector2.Distance(playerTransform.position, step.triggerZone.position);
    if (distance < 1.5f)
    {
      CompleteCurrentStep();
    }
  }

  private void CheckWallJump(TutorialStep step)
  {
    // 벽점프는 PlayerController에서 이벤트로 알려줌
    // 또는 여기서 상태 체크
  }

  private void UpdateArrowAnimation()
  {
    if (arrowImage != null && arrowImage.gameObject.activeSelf)
    {
      float bob = Mathf.Sin(Time.time * arrowBobSpeed) * arrowBobAmount;
      Vector3 pos = arrowImage.rectTransform.anchoredPosition;
      pos.x = bob * 10f; // 좌우로 흔들림
      arrowImage.rectTransform.anchoredPosition = pos;
    }
  }

  private void UpdateProgressUI(TutorialStep step)
  {
    if (progressText == null)
      return;

    if (step.requiredCount > 1)
    {
      progressText.text = $"{currentCount} / {step.requiredCount}";
      if (progressPanel != null)
        progressPanel.SetActive(true);
    }
    else if (step.requiredHoldTime > 0)
    {
      float progress = currentHoldTime / step.requiredHoldTime * 100f;
      progressText.text = $"{progress:F0}%";
      if (progressPanel != null)
        progressPanel.SetActive(true);
    }
    else
    {
      if (progressPanel != null)
        progressPanel.SetActive(false);
    }
  }

  public void StartStep(int index)
  {
    if (index < 0 || index >= tutorialSteps.Count)
      return;

    currentStepIndex = index;
    currentCount = 0;
    currentHoldTime = 0f;
    isHolding = false;

    TutorialStep step = tutorialSteps[index];

    Debug.Log($"[Tutorial] 스텝 시작: {step.stepId} - {step.title}");

    // UI 업데이트
    if (titleText != null)
      titleText.text = step.title;

    if (descriptionText != null)
      descriptionText.text = step.description;

    // 키 아이콘 설정
    if (keyIconImage != null)
    {
      if (step.keyIcon != null)
      {
        keyIconImage.sprite = step.keyIcon;
        keyIconImage.gameObject.SetActive(true);
      }
      else
      {
        keyIconImage.gameObject.SetActive(false);
      }
    }

    // 두 번째 키 아이콘 (조합용)
    if (secondKeyIconImage != null)
    {
      if (step.secondKeyIcon != null)
      {
        secondKeyIconImage.sprite = step.secondKeyIcon;
        secondKeyIconImage.gameObject.SetActive(true);
      }
      else
      {
        secondKeyIconImage.gameObject.SetActive(false);
      }
    }

    // 체크 이미지 숨기기
    if (checkImage != null)
    {
      checkImage.gameObject.SetActive(false);
    }

    // 패널 보이기
    if (tutorialPanel != null)
    {
      tutorialPanel.SetActive(true);
    }
  }

  public void CompleteCurrentStep()
  {
    if (currentStepIndex >= tutorialSteps.Count)
      return;

    // 이미 전환 중이면 무시
    if (isTransitioning)
      return;

    TutorialStep step = tutorialSteps[currentStepIndex];

    // 이미 완료된 스텝이면 무시
    if (step.isCompleted)
      return;

    // 전환 시작
    isTransitioning = true;
    step.isCompleted = true;

    Debug.Log($"[Tutorial] 스텝 완료: {step.stepId}");

    // 완료 효과
    if (audioSource != null && stepCompleteSound != null)
    {
      audioSource.PlayOneShot(stepCompleteSound);
    }

    // 체크마크 표시 및 다음 스텝 진행
    StartCoroutine(ShowCheckAndProgress());
  }

  private IEnumerator ShowCheckAndProgress()
  {
    // 키 아이콘 숨기기
    if (keyIconImage != null)
    {
      keyIconImage.gameObject.SetActive(false);
    }
    if (secondKeyIconImage != null)
    {
      secondKeyIconImage.gameObject.SetActive(false);
    }

    // 체크 이미지 표시
    if (checkImage != null)
    {
      if (checkSprite != null)
      {
        checkImage.sprite = checkSprite;
      }
      checkImage.gameObject.SetActive(true);

      // 체크마크 팝업 애니메이션
      RectTransform checkRect = checkImage.GetComponent<RectTransform>();
      if (checkRect != null)
      {
        Vector3 originalScale = checkRect.localScale;
        checkRect.localScale = Vector3.zero;

        float elapsed = 0f;
        float popDuration = 0.2f;

        while (elapsed < popDuration)
        {
          elapsed += Time.deltaTime;
          float t = elapsed / popDuration;
          float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.2f; // 오버슈트 효과
          if (t > 0.8f)
          {
            scale = Mathf.Lerp(1.2f, 1f, (t - 0.8f) / 0.2f);
          }
          checkRect.localScale = originalScale * scale;
          yield return null;
        }
        checkRect.localScale = originalScale;
      }
    }

    // 설명 텍스트 변경
    if (descriptionText != null)
    {
      descriptionText.text = "완료!";
    }

    Debug.Log("[Tutorial] 체크마크 표시됨, 1초 후 다음 스텝으로...");

    // 1초 대기
    yield return new WaitForSeconds(1f);

    // 다음 스텝으로
    if (autoProgressOnComplete)
    {
      NextStep();
    }
  }

  public void NextStep()
  {
    currentStepIndex++;

    // 전환 완료 플래그 리셋
    isTransitioning = false;

    if (currentStepIndex >= tutorialSteps.Count)
    {
      CompleteTutorial();
    }
    else
    {
      StartStep(currentStepIndex);
    }
  }

  public void CompleteTutorial()
  {
    isTutorialActive = false;

    Debug.Log("[Tutorial] 튜토리얼 완료!");

    if (audioSource != null && tutorialCompleteSound != null)
    {
      audioSource.PlayOneShot(tutorialCompleteSound);
    }

    // 완료 UI 표시
    if (titleText != null)
      titleText.text = "튜토리얼 완료!";

    if (descriptionText != null)
      descriptionText.text = "이제 모험을 시작할 준비가 되었습니다!";

    if (keyIconImage != null)
      keyIconImage.gameObject.SetActive(false);

    if (secondKeyIconImage != null)
      secondKeyIconImage.gameObject.SetActive(false);

    if (checkImage != null)
    {
      checkImage.gameObject.SetActive(true);
    }

    // 잠시 후 다음 씬으로
    StartCoroutine(LoadNextSceneAfterDelay(3f));
  }

  private IEnumerator LoadNextSceneAfterDelay(float delay)
  {
    yield return new WaitForSeconds(delay);

    // 튜토리얼 완료 저장
    PlayerPrefs.SetInt("TutorialCompleted", 1);
    PlayerPrefs.Save();

    // 다음 씬 로드
    if (!string.IsNullOrEmpty(nextSceneName))
    {
      SceneManager.LoadScene(nextSceneName);
    }
  }

  // 외부에서 호출 가능한 완료 메서드들
  public void OnItemCollected()
  {
    if (isTransitioning) return;

    if (currentStepIndex < tutorialSteps.Count &&
        tutorialSteps[currentStepIndex].triggerType == TutorialTriggerType.CollectItem)
    {
      currentCount++;
      if (currentCount >= tutorialSteps[currentStepIndex].requiredCount)
      {
        CompleteCurrentStep();
      }
    }
  }

  public void OnEnemyDefeated()
  {
    if (isTransitioning) return;

    if (currentStepIndex < tutorialSteps.Count &&
        tutorialSteps[currentStepIndex].triggerType == TutorialTriggerType.DefeatEnemy)
    {
      currentCount++;
      if (currentCount >= tutorialSteps[currentStepIndex].requiredCount)
      {
        CompleteCurrentStep();
      }
    }
  }

  public void OnToastTaken()
  {
    if (isTransitioning) return;

    if (currentStepIndex < tutorialSteps.Count &&
        tutorialSteps[currentStepIndex].triggerType == TutorialTriggerType.TakeToast)
    {
      CompleteCurrentStep();
    }
  }

  public void OnWallJumpPerformed()
  {
    if (isTransitioning) return;

    if (currentStepIndex < tutorialSteps.Count &&
        tutorialSteps[currentStepIndex].triggerType == TutorialTriggerType.WallJump)
    {
      currentCount++;
      if (currentCount >= tutorialSteps[currentStepIndex].requiredCount)
      {
        CompleteCurrentStep();
      }
    }
  }

  // 스킵 기능
  public void SkipTutorial()
  {
    PlayerPrefs.SetInt("TutorialCompleted", 1);
    PlayerPrefs.Save();

    if (!string.IsNullOrEmpty(nextSceneName))
    {
      SceneManager.LoadScene(nextSceneName);
    }
  }

  // 현재 스텝 정보 가져오기
  public TutorialStep GetCurrentStep()
  {
    if (currentStepIndex < tutorialSteps.Count)
      return tutorialSteps[currentStepIndex];
    return null;
  }

  public int GetCurrentStepIndex() => currentStepIndex;
  public int GetTotalSteps() => tutorialSteps.Count;
  public bool IsTutorialActive() => isTutorialActive;
}
