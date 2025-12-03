using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 씬을 자동으로 생성하는 에디터 도구
/// </summary>
public class TutorialSceneCreator : EditorWindow
{
  private string sceneName = "Tutorial";
  private float groundLength = 100f;
  private float groundStartX = 0f;

  // 프리팹 참조
  private GameObject playerPrefab;
  private GameObject garbageCanPrefab;  // GarbageCan 프리팹으로 변경
  private GameObject enemyPrefab;
  private GameObject toastPrefab;

  // 스프라이트 참조
  private Sprite groundSprite;
  private Sprite wallSprite;
  private Sprite backgroundSprite;

  // 키 스프라이트
  private Sprite keyA;
  private Sprite keyD;
  private Sprite keyW;
  private Sprite keyQ;
  private Sprite keyK;
  private Sprite keyL;
  private Sprite arrowRight;
  private Sprite checkMark;

  [MenuItem("Tools/Tutorial/Create Tutorial Scene")]
  public static void ShowWindow()
  {
    GetWindow<TutorialSceneCreator>("Tutorial Scene Creator");
  }

  private void OnGUI()
  {
    GUILayout.Label("튜토리얼 씬 생성기", EditorStyles.boldLabel);
    EditorGUILayout.Space();

    sceneName = EditorGUILayout.TextField("씬 이름", sceneName);
    groundLength = EditorGUILayout.FloatField("바닥 길이", groundLength);
    groundStartX = EditorGUILayout.FloatField("시작 X 좌표", groundStartX);

    EditorGUILayout.Space();
    GUILayout.Label("프리팹", EditorStyles.boldLabel);
    playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player", playerPrefab, typeof(GameObject), false);
    garbageCanPrefab = (GameObject)EditorGUILayout.ObjectField("GarbageCan", garbageCanPrefab, typeof(GameObject), false);
    enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy (Pigeon)", enemyPrefab, typeof(GameObject), false);
    toastPrefab = (GameObject)EditorGUILayout.ObjectField("Toast", toastPrefab, typeof(GameObject), false);

    EditorGUILayout.Space();
    GUILayout.Label("스프라이트", EditorStyles.boldLabel);
    groundSprite = (Sprite)EditorGUILayout.ObjectField("바닥", groundSprite, typeof(Sprite), false);
    wallSprite = (Sprite)EditorGUILayout.ObjectField("벽", wallSprite, typeof(Sprite), false);
    backgroundSprite = (Sprite)EditorGUILayout.ObjectField("배경", backgroundSprite, typeof(Sprite), false);

    EditorGUILayout.Space();
    GUILayout.Label("키 아이콘", EditorStyles.boldLabel);
    keyA = (Sprite)EditorGUILayout.ObjectField("A Key", keyA, typeof(Sprite), false);
    keyD = (Sprite)EditorGUILayout.ObjectField("D Key", keyD, typeof(Sprite), false);
    keyW = (Sprite)EditorGUILayout.ObjectField("W Key", keyW, typeof(Sprite), false);
    keyQ = (Sprite)EditorGUILayout.ObjectField("Q Key", keyQ, typeof(Sprite), false);
    keyK = (Sprite)EditorGUILayout.ObjectField("K Key", keyK, typeof(Sprite), false);
    keyL = (Sprite)EditorGUILayout.ObjectField("L Key", keyL, typeof(Sprite), false);
    arrowRight = (Sprite)EditorGUILayout.ObjectField("Arrow Right", arrowRight, typeof(Sprite), false);
    checkMark = (Sprite)EditorGUILayout.ObjectField("Check Mark", checkMark, typeof(Sprite), false);

    EditorGUILayout.Space();

    if (GUILayout.Button("자동으로 스프라이트/프리팹 찾기"))
    {
      AutoFindAssets();
    }

    EditorGUILayout.Space();

    if (GUILayout.Button("튜토리얼 씬 생성", GUILayout.Height(40)))
    {
      CreateTutorialScene();
    }
  }

  private void AutoFindAssets()
  {
    // 프리팹 찾기
    string[] playerGuids = AssetDatabase.FindAssets("Player t:Prefab", new[] { "Assets/Prefabs" });
    if (playerGuids.Length > 0)
      playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(playerGuids[0]));

    string[] pigeonGuids = AssetDatabase.FindAssets("Pigeon t:Prefab", new[] { "Assets/Prefabs" });
    if (pigeonGuids.Length > 0)
      enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(pigeonGuids[0]));

    // GarbageCan 프리팹 찾기
    string[] garbageGuids = AssetDatabase.FindAssets("Garbage t:Prefab", new[] { "Assets/Prefabs" });
    if (garbageGuids.Length > 0)
      garbageCanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(garbageGuids[0]));

    // 키 스프라이트 찾기
    keyA = LoadSprite("Assets/UI/Keys/A_KEY.png");
    keyD = LoadSprite("Assets/UI/Keys/D_KEY.png");
    keyW = LoadSprite("Assets/UI/Keys/W_KEY.png");
    keyQ = LoadSprite("Assets/UI/Keys/Q_KEY.png");
    keyK = LoadSprite("Assets/UI/Keys/K_KEY.png");
    keyL = LoadSprite("Assets/UI/Keys/L_KEY.png");
    arrowRight = LoadSprite("Assets/UI/Keys/Arrow_Right.png");
    checkMark = LoadSprite("Assets/UI/Keys/check.png");

    // 바닥 스프라이트 찾기
    groundSprite = LoadSprite("Assets/Backgrounds/stage1/Ground2.png");
    backgroundSprite = LoadSprite("Assets/Backgrounds/background.png");

    Debug.Log("[TutorialSceneCreator] 에셋 자동 탐색 완료!");
  }

  private Sprite LoadSprite(string path)
  {
    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
  }

  private void CreateTutorialScene()
  {
    // 새 씬 생성
    var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

    // ================== 기본 구성 요소 ==================

    // 카메라 설정
    Camera mainCamera = Camera.main;
    if (mainCamera != null)
    {
      mainCamera.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
      mainCamera.orthographic = true;
      mainCamera.orthographicSize = 5f;
    }

    // ================== 배경 ==================
    if (backgroundSprite != null)
    {
      GameObject bg = new GameObject("Background");
      SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
      bgSr.sprite = backgroundSprite;
      bgSr.sortingOrder = -100;
      bg.transform.position = new Vector3(groundLength / 2f, 0f, 10f);
    }

    // ================== 긴 바닥 생성 ==================
    GameObject ground = new GameObject("Ground1");
    ground.transform.position = new Vector3(groundStartX + groundLength / 2f, -1f, 0f);
    ground.layer = LayerMask.NameToLayer("Ground");

    SpriteRenderer groundSr = ground.AddComponent<SpriteRenderer>();
    if (groundSprite != null)
    {
      groundSr.sprite = groundSprite;
      groundSr.drawMode = SpriteDrawMode.Tiled;
      groundSr.size = new Vector2(groundLength, 2f);
    }
    else
    {
      groundSr.color = new Color(0.4f, 0.3f, 0.2f);
    }
    groundSr.sortingOrder = -10;

    BoxCollider2D groundCol = ground.AddComponent<BoxCollider2D>();
    groundCol.size = new Vector2(groundLength, 2f);

    // ================== 벽점프 구간 (x = 50~60) ==================
    CreateWall("WallLeft", new Vector3(52f, 3f, 0f), 8f);
    CreateWall("WallRight", new Vector3(58f, 3f, 0f), 8f);

    // ================== 플레이어 스폰 ==================
    if (playerPrefab != null)
    {
      GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
      player.transform.position = new Vector3(groundStartX + 2f, 1f, 0f);
      player.name = "Player";
    }

    // ================== 쓰레기통 배치 (x = 70) ==================
    if (garbageCanPrefab != null)
    {
      GameObject garbageCan = (GameObject)PrefabUtility.InstantiatePrefab(garbageCanPrefab);
      garbageCan.transform.position = new Vector3(70f, 0.5f, 0f);
      garbageCan.name = "TutorialGarbageCan";
    }
    else
    {
      // 쓰레기통 프리팹이 없으면 간단한 오브젝트 생성
      CreatePlaceholder("GarbageCanPlaceholder", new Vector3(70f, 0.5f, 0f), Color.gray);
    }

    // ================== 적 배치 (x = 80) ==================
    if (enemyPrefab != null)
    {
      GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
      enemy.transform.position = new Vector3(80f, 1f, 0f);
      enemy.name = "TutorialEnemy";
    }
    else
    {
      CreatePlaceholder("EnemyPlaceholder", new Vector3(80f, 1f, 0f), Color.red);
    }

    // ================== 토스트 배치 (x = 90) ==================
    if (toastPrefab != null)
    {
      GameObject toast = (GameObject)PrefabUtility.InstantiatePrefab(toastPrefab);
      toast.transform.position = new Vector3(90f, 1f, 0f);
      toast.name = "TutorialToast";
    }
    else
    {
      CreatePlaceholder("ToastPlaceholder", new Vector3(90f, 1f, 0f), Color.yellow);
    }

    // ================== 튜토리얼 매니저 ==================
    GameObject tutorialManagerObj = new GameObject("TutorialManager");
    TutorialManager tm = tutorialManagerObj.AddComponent<TutorialManager>();

    // 키 스프라이트 할당
    tm.keyA = keyA;
    tm.keyD = keyD;
    tm.keyW = keyW;
    tm.keyQ = keyQ;
    tm.keyK = keyK;
    tm.keyL = keyL;
    tm.arrowSprite = arrowRight;
    tm.checkSprite = checkMark;

    // ================== 튜토리얼 스텝 생성 ==================
    CreateTutorialSteps(tm);

    // ================== 트리거 존 생성 ==================
    CreateTriggerZone("EndZone", new Vector3(groundLength - 2f, 2f, 0f), new Vector2(4f, 6f));

    // ================== UI Canvas ==================
    CreateTutorialUI(tm);

    // 씬 저장
    string scenePath = $"Assets/Scenes/{sceneName}.unity";
    EditorSceneManager.SaveScene(newScene, scenePath);

    Debug.Log($"[TutorialSceneCreator] 튜토리얼 씬 생성 완료: {scenePath}");

    // Build Settings에 씬 추가
    AddSceneToBuildSettings(scenePath);
  }

  private void CreateWall(string name, Vector3 position, float height)
  {
    GameObject wall = new GameObject(name);
    wall.transform.position = position;
    wall.layer = LayerMask.NameToLayer("Ground");

    SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
    if (wallSprite != null)
    {
      sr.sprite = wallSprite;
      sr.drawMode = SpriteDrawMode.Tiled;
      sr.size = new Vector2(1f, height);
    }
    else
    {
      sr.color = new Color(0.5f, 0.5f, 0.5f);
    }
    sr.sortingOrder = -5;

    BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
    col.size = new Vector2(1f, height);
  }

  private void CreatePlaceholder(string name, Vector3 position, Color color)
  {
    GameObject obj = new GameObject(name);
    obj.transform.position = position;

    SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
    sr.color = color;

    CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
    col.radius = 0.5f;
    col.isTrigger = true;
  }

  private void CreateTriggerZone(string name, Vector3 position, Vector2 size)
  {
    GameObject zone = new GameObject(name);
    zone.transform.position = position;

    BoxCollider2D col = zone.AddComponent<BoxCollider2D>();
    col.size = size;
    col.isTrigger = true;

    TutorialTriggerZone trigger = zone.AddComponent<TutorialTriggerZone>();
    trigger.action = TutorialTriggerZone.TriggerAction.CompleteStep;
  }

  private void CreateTutorialSteps(TutorialManager tm)
  {
    tm.tutorialSteps = new List<TutorialManager.TutorialStep>();

    // Step 1: 오른쪽 이동
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "move_right",
      title = "오른쪽으로 이동",
      description = "D 키를 눌러 오른쪽으로 이동하세요.",
      keyIcon = keyD,
      triggerType = TutorialManager.TutorialTriggerType.KeyHold,
      requiredKey = KeyCode.D,
      requiredHoldTime = 1f
    });

    // Step 2: 왼쪽 이동
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "move_left",
      title = "왼쪽으로 이동",
      description = "A 키를 눌러 왼쪽으로 이동하세요.",
      keyIcon = keyA,
      triggerType = TutorialManager.TutorialTriggerType.KeyHold,
      requiredKey = KeyCode.A,
      requiredHoldTime = 1f
    });

    // Step 3: 점프
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "jump",
      title = "점프",
      description = "W 키를 눌러 점프하세요.\n길게 누르면 더 높이 점프!",
      keyIcon = keyW,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.W,
      requiredCount = 3
    });

    // Step 4: 대시
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "dash",
      title = "대시",
      description = "Q 키를 눌러 대시하세요.\n빠르게 이동하며 적을 공격!",
      keyIcon = keyQ,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.Q,
      requiredCount = 2
    });

    // Step 5: 할퀴기
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "scratch",
      title = "할퀴기",
      description = "K 키를 눌러 할퀴기 공격!",
      keyIcon = keyK,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.K,
      requiredCount = 3
    });

    // Step 6: 냥펀치
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "punch",
      title = "냥펀치",
      description = "L 키를 눌러 냥펀치!\n적을 밀쳐낼 수 있어요.",
      keyIcon = keyL,
      triggerType = TutorialManager.TutorialTriggerType.KeyPress,
      requiredKey = KeyCode.L,
      requiredCount = 3
    });

    // Step 7: 벽점프
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "wall_jump",
      title = "벽점프",
      description = "벽에 붙어서 W 키로 벽점프!",
      keyIcon = keyW,
      triggerType = TutorialManager.TutorialTriggerType.WallJump,
      requiredKey = KeyCode.W,
      requiredCount = 2
    });

    // Step 8: 쓰레기통에서 음식 획득
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "collect_food",
      title = "쓰레기통 뒤지기",
      description = "쓰레기통을 공격해서 음식을 얻고,\n음식을 먹어 체력을 회복하세요!",
      keyIcon = keyK,
      triggerType = TutorialManager.TutorialTriggerType.CollectItem,
      requiredCount = 1
    });

    // Step 9: 적 처치
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
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
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "take_toast",
      title = "토스트 장착",
      description = "토스트를 획득하면 특별한 능력!",
      triggerType = TutorialManager.TutorialTriggerType.TakeToast,
      requiredCount = 1
    });

    // Step 11: 끝까지 이동
    tm.tutorialSteps.Add(new TutorialManager.TutorialStep
    {
      stepId = "reach_end",
      title = "목표 도달",
      description = "오른쪽 끝까지 이동하세요!",
      keyIcon = arrowRight,
      triggerType = TutorialManager.TutorialTriggerType.ReachPosition,
      requiredCount = 1
    });
  }

  private void CreateTutorialUI(TutorialManager tm)
  {
    // Canvas 생성
    GameObject canvasObj = new GameObject("TutorialCanvas");
    Canvas canvas = canvasObj.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 100;

    canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

    // 튜토리얼 패널
    GameObject panel = new GameObject("TutorialPanel");
    panel.transform.SetParent(canvasObj.transform, false);

    RectTransform panelRect = panel.AddComponent<RectTransform>();
    panelRect.anchorMin = new Vector2(0.5f, 0.8f);
    panelRect.anchorMax = new Vector2(0.5f, 0.8f);
    panelRect.sizeDelta = new Vector2(500f, 150f);
    panelRect.anchoredPosition = Vector2.zero;

    UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
    panelImage.color = new Color(0f, 0f, 0f, 0.8f);

    // 제목 텍스트
    GameObject titleObj = new GameObject("TitleText");
    titleObj.transform.SetParent(panel.transform, false);
    RectTransform titleRect = titleObj.AddComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0f, 0.6f);
    titleRect.anchorMax = new Vector2(1f, 1f);
    titleRect.offsetMin = new Vector2(10f, 0f);
    titleRect.offsetMax = new Vector2(-10f, -10f);

    TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
    titleText.text = "튜토리얼";
    titleText.fontSize = 36;
    titleText.alignment = TMPro.TextAlignmentOptions.Center;
    titleText.color = Color.white;

    // 설명 텍스트
    GameObject descObj = new GameObject("DescriptionText");
    descObj.transform.SetParent(panel.transform, false);
    RectTransform descRect = descObj.AddComponent<RectTransform>();
    descRect.anchorMin = new Vector2(0f, 0f);
    descRect.anchorMax = new Vector2(1f, 0.6f);
    descRect.offsetMin = new Vector2(80f, 10f);
    descRect.offsetMax = new Vector2(-10f, 0f);

    TMPro.TextMeshProUGUI descText = descObj.AddComponent<TMPro.TextMeshProUGUI>();
    descText.text = "설명";
    descText.fontSize = 24;
    descText.alignment = TMPro.TextAlignmentOptions.Left;
    descText.color = Color.white;

    // 키 아이콘
    GameObject keyIconObj = new GameObject("KeyIcon");
    keyIconObj.transform.SetParent(panel.transform, false);
    RectTransform keyIconRect = keyIconObj.AddComponent<RectTransform>();
    keyIconRect.anchorMin = new Vector2(0f, 0f);
    keyIconRect.anchorMax = new Vector2(0f, 0.6f);
    keyIconRect.sizeDelta = new Vector2(64f, 64f);
    keyIconRect.anchoredPosition = new Vector2(45f, 35f);

    UnityEngine.UI.Image keyIconImage = keyIconObj.AddComponent<UnityEngine.UI.Image>();
    keyIconImage.preserveAspect = true;

    // 체크 이미지 (중앙에 크게)
    GameObject checkObj = new GameObject("CheckImage");
    checkObj.transform.SetParent(panel.transform, false);
    RectTransform checkRect = checkObj.AddComponent<RectTransform>();
    checkRect.anchorMin = new Vector2(0.5f, 0.5f);
    checkRect.anchorMax = new Vector2(0.5f, 0.5f);
    checkRect.sizeDelta = new Vector2(80f, 80f);
    checkRect.anchoredPosition = Vector2.zero;

    UnityEngine.UI.Image checkImage = checkObj.AddComponent<UnityEngine.UI.Image>();
    checkImage.preserveAspect = true;
    checkImage.color = new Color(0.3f, 1f, 0.3f, 1f); // 연두색 틴트
    if (checkMark != null)
      checkImage.sprite = checkMark;
    checkObj.SetActive(false);

    // 진행도 텍스트
    GameObject progressObj = new GameObject("ProgressText");
    progressObj.transform.SetParent(panel.transform, false);
    RectTransform progressRect = progressObj.AddComponent<RectTransform>();
    progressRect.anchorMin = new Vector2(1f, 0f);
    progressRect.anchorMax = new Vector2(1f, 0f);
    progressRect.sizeDelta = new Vector2(100f, 30f);
    progressRect.anchoredPosition = new Vector2(-60f, 20f);

    TMPro.TextMeshProUGUI progressText = progressObj.AddComponent<TMPro.TextMeshProUGUI>();
    progressText.text = "0/3";
    progressText.fontSize = 20;
    progressText.alignment = TMPro.TextAlignmentOptions.Right;
    progressText.color = Color.yellow;

    // TutorialManager에 UI 참조 연결
    tm.tutorialPanel = panel;
    tm.titleText = titleText;
    tm.descriptionText = descText;
    tm.keyIconImage = keyIconImage;
    tm.checkImage = checkImage;
    tm.progressText = progressText;
  }

  private void AddSceneToBuildSettings(string scenePath)
  {
    var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

    // 이미 추가되어 있는지 확인
    foreach (var scene in scenes)
    {
      if (scene.path == scenePath)
      {
        Debug.Log($"[TutorialSceneCreator] 씬이 이미 Build Settings에 있습니다: {scenePath}");
        return;
      }
    }

    scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    EditorBuildSettings.scenes = scenes.ToArray();
    Debug.Log($"[TutorialSceneCreator] Build Settings에 씬 추가됨: {scenePath}");
  }
}
