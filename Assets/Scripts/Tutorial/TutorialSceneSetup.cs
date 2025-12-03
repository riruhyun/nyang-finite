using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 씬을 자동으로 설정하는 헬퍼 스크립트
/// 씬에 추가하면 필요한 UI와 스텝을 자동 생성
/// </summary>
public class TutorialSceneSetup : MonoBehaviour
{
  [Header("Auto Setup")]
  public bool autoSetupOnStart = true;

  [Header("Ground Settings")]
  public float groundLength = 100f;
  public float groundHeight = 2f;
  public Sprite groundSprite;

  [Header("Wall Settings")]
  public float wallHeight = 8f;
  public Sprite wallSprite;

  [Header("Key Sprites")]
  public Sprite keyA;
  public Sprite keyD;
  public Sprite keyW;
  public Sprite keyQ;
  public Sprite keyK;
  public Sprite keyL;
  public Sprite arrowRight;
  public Sprite checkMark;

  [Header("Prefabs")]
  public GameObject playerPrefab;
  public GarbageCan garbageCanPrefab;  // GarbageCan 프리팹으로 변경
  public GameObject enemyPrefab;
  public GameObject toastPrefab;

  [Header("UI Prefab")]
  public GameObject tutorialUIPrefab;

  private TutorialManager tutorialManager;

  private void Start()
  {
    if (autoSetupOnStart)
    {
      SetupTutorialSteps();
    }
  }

  public void SetupTutorialSteps()
  {
    tutorialManager = FindFirstObjectByType<TutorialManager>();
    if (tutorialManager == null)
    {
      Debug.LogError("[TutorialSceneSetup] TutorialManager를 찾을 수 없습니다!");
      return;
    }

    // 키 스프라이트 할당
    tutorialManager.keyA = keyA;
    tutorialManager.keyD = keyD;
    tutorialManager.keyW = keyW;
    tutorialManager.keyQ = keyQ;
    tutorialManager.keyK = keyK;
    tutorialManager.keyL = keyL;
    tutorialManager.arrowSprite = arrowRight;
    tutorialManager.checkSprite = checkMark;

    // 튜토리얼 스텝 생성
    CreateTutorialSteps();

    Debug.Log($"[TutorialSceneSetup] {tutorialManager.tutorialSteps.Count}개의 튜토리얼 스텝 생성 완료");
  }

  private void CreateTutorialSteps()
  {
    tutorialManager.tutorialSteps.Clear();

    // Step 1: 오른쪽 이동 (D키)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "move_right",
      title = "오른쪽으로 이동",
      description = "D 키를 눌러 오른쪽으로 이동하세요.",
      keyIcon = keyD,
      triggerType = TutorialManager.TutorialTriggerType.KeyHold,
      requiredKey = KeyCode.D,
      requiredHoldTime = 1f
    });

    // Step 2: 왼쪽 이동 (A키)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "move_left",
      title = "왼쪽으로 이동",
      description = "A 키를 눌러 왼쪽으로 이동하세요.",
      keyIcon = keyA,
      triggerType = TutorialManager.TutorialTriggerType.KeyHold,
      requiredKey = KeyCode.A,
      requiredHoldTime = 1f
    });

    // Step 3: 점프 (W키)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "jump",
      title = "점프",
      description = "W 키를 눌러 점프하세요.\n길게 누르면 더 높이 점프합니다!",
      keyIcon = keyW,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.W,
      requiredCount = 3
    });

    // Step 4: 대시 (Q키)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "dash",
      title = "대시",
      description = "Q 키를 눌러 대시하세요.\n빠르게 이동하며 적을 공격할 수 있습니다!",
      keyIcon = keyQ,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.Q,
      requiredCount = 2
    });

    // Step 5: 할퀴기 공격 (K키)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "scratch",
      title = "할퀴기",
      description = "K 키를 눌러 할퀴기 공격을 하세요.",
      keyIcon = keyK,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.K,
      requiredCount = 3
    });

    // Step 6: 냥펀치 (L키)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "punch",
      title = "냥펀치",
      description = "L 키를 눌러 냥펀치를 날리세요.\n적을 밀쳐낼 수 있습니다!",
      keyIcon = keyL,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.L,
      requiredCount = 3
    });

    // Step 7: 벽점프
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "wall_jump",
      title = "아크 점프(벽점프)",
      description = "`",
      keyIcon = keyW,
      triggerType = TutorialManager.TutorialTriggerType.WallJump,
      requiredKey = KeyCode.W,
      requiredCount = 2
    });

    // Step 8: 쓰레기통에서 음식 획득
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "collect_food",
      title = "쓰레기통 뒤지기",
      description = "쓰레기통을 공격해서 음식을 얻고,\n음식을 먹어 체력을 회복하세요!",
      keyIcon = keyK,
      triggerType = TutorialManager.TutorialTriggerType.CollectItem,
      requiredCount = 1
    });

    // Step 9: 적 처치 (선택적)
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "defeat_enemy",
      title = "적 처치",
      description = "비둘기를 공격해서 물리치세요!",
      keyIcon = keyK,
      secondKeyIcon = keyL,
      triggerType = TutorialManager.TutorialTriggerType.DefeatEnemy,
      requiredCount = 1
    });

    // Step 10: 토스트 획득
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "take_toast",
      title = "토스트 장착",
      description = "토스트를 획득하면 특별한 능력을 얻습니다!",
      triggerType = TutorialManager.TutorialTriggerType.TakeToast,
      requiredCount = 1
    });

    // Step 11: 끝까지 이동
    tutorialManager.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "reach_end",
      title = "목표 지점 도달",
      description = "오른쪽 끝까지 이동하세요!",
      keyIcon = arrowRight,
      triggerType = TutorialManager.TutorialTriggerType.ReachPosition,
      requiredCount = 1
    });
  }

  /// <summary>
  /// 에디터에서 씬 구성 요소 자동 생성
  /// </summary>
  [ContextMenu("Create Tutorial Scene Objects")]
  public void CreateSceneObjects()
  {
    // 긴 바닥 생성
    CreateGround();

    // 벽점프용 벽 생성
    CreateWalls();

    // 트리거 존 생성
    CreateTriggerZones();

    Debug.Log("[TutorialSceneSetup] 씬 오브젝트 생성 완료");
  }

  private void CreateGround()
  {
    GameObject ground = new GameObject("TutorialGround");
    ground.transform.position = new Vector3(groundLength / 2f, -1f, 0f);

    SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
    if (groundSprite != null)
    {
      sr.sprite = groundSprite;
      sr.drawMode = SpriteDrawMode.Tiled;
      sr.size = new Vector2(groundLength, groundHeight);
    }
    else
    {
      // 기본 스프라이트 없으면 색상만 설정
      sr.color = new Color(0.4f, 0.3f, 0.2f);
    }

    BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
    col.size = new Vector2(groundLength, groundHeight);

    ground.layer = LayerMask.NameToLayer("Ground");
  }

  private void CreateWalls()
  {
    // 벽점프 구간 (X = 40 ~ 50)
    float wallX = 45f;

    // 왼쪽 벽
    CreateWall("LeftWall", new Vector3(wallX - 3f, wallHeight / 2f, 0f));

    // 오른쪽 벽
    CreateWall("RightWall", new Vector3(wallX + 3f, wallHeight / 2f, 0f));
  }

  private void CreateWall(string name, Vector3 position)
  {
    GameObject wall = new GameObject(name);
    wall.transform.position = position;

    SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
    if (wallSprite != null)
    {
      sr.sprite = wallSprite;
      sr.drawMode = SpriteDrawMode.Tiled;
      sr.size = new Vector2(1f, wallHeight);
    }
    else
    {
      sr.color = new Color(0.5f, 0.5f, 0.5f);
    }

    BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
    col.size = new Vector2(1f, wallHeight);

    wall.layer = LayerMask.NameToLayer("Ground");
  }

  private void CreateTriggerZones()
  {
    // 각 튜토리얼 구간에 트리거 존 생성
    float[] zonePositions = { 10f, 20f, 30f, 40f, 55f, 70f, 85f, 95f };

    for (int i = 0; i < zonePositions.Length; i++)
    {
      GameObject zone = new GameObject($"TriggerZone_{i}");
      zone.transform.position = new Vector3(zonePositions[i], 2f, 0f);

      BoxCollider2D col = zone.AddComponent<BoxCollider2D>();
      col.size = new Vector2(5f, 6f);
      col.isTrigger = true;

      TutorialTriggerZone trigger = zone.AddComponent<TutorialTriggerZone>();
      trigger.action = TutorialTriggerZone.TriggerAction.CompleteStep;
    }
  }
}
