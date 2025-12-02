using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper for spawning enemies (clones) with detailed parameters.
/// Ensures a single shared tracking engine instance can be reused by all clones.
/// </summary>
public static class EnemySpawnHelper
{
  public enum EnemyKind
  {
    Dog,
    Pigeon,
    Cat
  }

  /// <summary>
  /// Spawns enemies with a shared tracking engine.
  /// </summary>
  /// <param name="ownerForSharedEngine">GameObject that will host the shared engine (one instance).</param>
  /// <param name="dogPrefab">Prefab for Dog enemy.</param>
  /// <param name="pigeonPrefab">Prefab for Pigeon enemy (optional if using legacy as pigeon).</param>
  /// <param name="catPrefab">Prefab for Cat enemy.</param>
  /// <param name="count">Number of clones to spawn (positions array takes precedence if provided).</param>
  /// <param name="positions">Optional exact spawn positions; when provided, length determines count.</param>
  /// <param name="enemyKind">Which enemy type to spawn.</param>
  /// <param name="algorithm">Tracking algorithm selection.</param>
  /// <param name="view">Side-view vs top-view.</param>
  /// <param name="useLegacyAsPigeonAI">If true and kind is Pigeon, reuse Dog prefab/AI as pigeon.</param>
  /// <param name="reuseOwnerTrackingEngine">If true, attach/find a single shared engine on owner and reuse.</param>
  /// <param name="parent">Optional parent Transform for the spawned clones.</param>
  /// <returns>List of spawned enemy GameObjects.</returns>
  public static List<GameObject> SpawnEnemies(
    GameObject ownerForSharedEngine,
    GameObject dogPrefab,
    GameObject pigeonPrefab,
    int count,
    Vector3[] positions = null,
    EnemyKind enemyKind = EnemyKind.Dog,
    SharedTrackingEngine.TrackingAlgorithm algorithm = SharedTrackingEngine.TrackingAlgorithm.DirectChase,
    SharedTrackingEngine.ViewMode view = SharedTrackingEngine.ViewMode.SideView2D,
    bool useLegacyAsPigeonAI = false,
    bool reuseOwnerTrackingEngine = true,
    Transform parent = null,
    EnemyKind[] kindsByIndex = null,
    SharedTrackingEngine.TrackingAlgorithm[] algorithmsByIndex = null,
    SharedTrackingEngine.ViewMode[] viewsByIndex = null,
    bool[] canJumpByIndex = null,
    bool? canJumpDefault = null,
    GameObject enemyToastPrefab = null,
    Vector3 enemyToastOffset = default,
    string enemyToastSortingLayer = "",
    int enemyToastSortingOrder = 0,
    bool disableTemplateAfterSpawn = true,
    SpawnDefinition[] spawnDefinitions = null,
    GameObject catPrefab = null
  )
  {
    if (ownerForSharedEngine == null)
    {
      Debug.LogError("EnemySpawnHelper: ownerForSharedEngine is null.");
      return new List<GameObject>();
    }

    // Ensure shared engine exists (single instance on owner)
    SharedTrackingEngine engine = null;
    if (reuseOwnerTrackingEngine)
    {
      engine = ownerForSharedEngine.GetComponent<SharedTrackingEngine>();
      if (engine == null)
      {
        engine = ownerForSharedEngine.AddComponent<SharedTrackingEngine>();
      }

      // Apply defaults for this batch
      engine.defaultAlgorithm = algorithm;
      engine.defaultView = view;
    }

    var spawned = new List<GameObject>();

    // Pre-disable scene instances (templates) so originals stay idle/hidden before spawning.
    if (disableTemplateAfterSpawn)
    {
      if (dogPrefab != null && dogPrefab.scene.IsValid())
      {
        dogPrefab.SetActive(false);
      }
      if (pigeonPrefab != null && pigeonPrefab.scene.IsValid())
      {
        pigeonPrefab.SetActive(false);
      }
      if (catPrefab != null && catPrefab.scene.IsValid())
      {
        catPrefab.SetActive(false);
      }
      if (enemyToastPrefab != null && enemyToastPrefab.scene.IsValid())
      {
        enemyToastPrefab.SetActive(false);
        Object.Destroy(enemyToastPrefab);
      }
    }

    // If positions provided, use them. Otherwise, scatter around owner.
    int spawnCount = positions != null && positions.Length > 0
      ? positions.Length
      : (spawnDefinitions != null && spawnDefinitions.Length > 0 ? spawnDefinitions.Length : Mathf.Max(0, count));
    for (int i = 0; i < spawnCount; i++)
    {
      Vector3 pos;
      SpawnDefinition defForIndex = (spawnDefinitions != null && i < spawnDefinitions.Length) ? spawnDefinitions[i] : null;
      if (positions != null && positions.Length > 0)
      {
        pos = positions[i];
      }
      else if (defForIndex != null && defForIndex.useCustomPosition)
      {
        pos = defForIndex.spawnPosition;
      }
      else
      {
        // simple radial scatter near owner
        var angle = (Mathf.PI * 2f) * (i / Mathf.Max(1f, (float)spawnCount));
        var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.5f;
        pos = ownerForSharedEngine.transform.position + offset;
      }

      // Determine kind and prefab per clone
      EnemyKind effectiveKind = enemyKind;
      if (kindsByIndex != null && i < kindsByIndex.Length)
      {
        effectiveKind = kindsByIndex[i];
      }
      else if (defForIndex != null)
      {
        effectiveKind = defForIndex.enemyKind;
      }

      GameObject prefabForClone = dogPrefab;
      switch (effectiveKind)
      {
        case EnemyKind.Dog:
          prefabForClone = dogPrefab;
          break;
        case EnemyKind.Pigeon:
          prefabForClone = (useLegacyAsPigeonAI || pigeonPrefab == null) ? dogPrefab : pigeonPrefab;
          break;
        case EnemyKind.Cat:
          prefabForClone = catPrefab != null ? catPrefab : dogPrefab;
          break;
      }

      var go = Object.Instantiate(prefabForClone, pos, Quaternion.identity, parent);
      // Ensure spawned clone is active even if the template was kept disabled in the scene.
      if (!go.activeSelf) go.SetActive(true);

      // Optional toast follower/indicator attached to the clone (skip if already present on prefab/instance)
      bool hasToastAlready = go.GetComponentInChildren<ToastHoverTrigger>(true) != null
                             || go.GetComponentInChildren<ToastIndicator>(true) != null
                             || go.transform.Find("EnemyToast") != null;
      if (enemyToastPrefab != null && !hasToastAlready)
      {
        var toast = Object.Instantiate(enemyToastPrefab, go.transform);
        toast.transform.localPosition = enemyToastOffset;
        toast.transform.localRotation = Quaternion.identity;

        // Ensure indicator follows the requested offset instead of its prefab default.
        var indicator = toast.GetComponent<ToastIndicator>();
        if (indicator != null)
        {
          indicator.SetLocalOffset(enemyToastOffset);
          toast.transform.localPosition = enemyToastOffset;
        }

        // Apply optional sorting settings so the toast doesn't occlude the player
        if (!string.IsNullOrEmpty(enemyToastSortingLayer))
        {
          var sr = toast.GetComponent<SpriteRenderer>();
          if (sr != null)
          {
            sr.sortingLayerName = enemyToastSortingLayer;
          }
        }
        // Only apply order when non-zero to avoid overriding prefab defaults unnecessarily
        if (enemyToastSortingOrder != 0)
        {
          var sr = toast.GetComponent<SpriteRenderer>();
          if (sr != null)
          {
            sr.sortingOrder = enemyToastSortingOrder;
          }
        }
        if (!toast.activeSelf) toast.SetActive(true);
      }

      // Attach tracking client and bind shared engine
      var client = go.GetComponent<SharedTrackingClient>();
      if (client == null)
      {
        client = go.AddComponent<SharedTrackingClient>();
      }
      client.engine = engine;
      // Per-clone overrides when arrays are provided; otherwise fall back to batch defaults
      var algo = (algorithmsByIndex != null && i < algorithmsByIndex.Length)
        ? algorithmsByIndex[i]
        : algorithm;
      var viewMode = (viewsByIndex != null && i < viewsByIndex.Length)
        ? viewsByIndex[i]
        : view;

      client.useAlgorithmOverride = true;
      client.algorithmOverride = algo;
      client.useViewModeOverride = true;
      client.viewModeOverride = viewMode;

      // Abilities
      if (canJumpByIndex != null && i < canJumpByIndex.Length)
      {
        client.canJump = canJumpByIndex[i];
      }
      else if (canJumpDefault.HasValue)
      {
        client.canJump = canJumpDefault.Value;
      }

      // Apply spawn definition (stats + toast selection + skin)
      if (spawnDefinitions != null && i < spawnDefinitions.Length)
      {
        ApplySpawnDefinition(go, spawnDefinitions[i]);
      }

      spawned.Add(go);
    }

    // Optionally disable template prefabs/scene instances so only clones stay active.
    if (disableTemplateAfterSpawn)
    {
      if (dogPrefab != null && dogPrefab.scene.IsValid() && dogPrefab.activeInHierarchy)
      {
        dogPrefab.SetActive(false);
      }
      if (pigeonPrefab != null && pigeonPrefab.scene.IsValid() && pigeonPrefab.activeInHierarchy)
      {
        pigeonPrefab.SetActive(false);
      }
      if (catPrefab != null && catPrefab.scene.IsValid() && catPrefab.activeInHierarchy)
      {
        catPrefab.SetActive(false);
      }
      if (enemyToastPrefab != null && enemyToastPrefab.scene.IsValid() && enemyToastPrefab.activeInHierarchy)
      {
        enemyToastPrefab.SetActive(false);
      }
    }

    return spawned;
  }

  /// <summary>
  /// Apply stats, skin, and toast profile based on SpawnDefinition.
  /// </summary>
  private static void ApplySpawnDefinition(GameObject go, SpawnDefinition def)
  {
    if (def == null) return;

    // Enemy stats
    var baseEnemy = go.GetComponent<Enemy>();
    if (baseEnemy != null)
    {
      baseEnemy.ApplyBaseStats(def.moveSpeed, def.maxHealth, def.attackSpeed);
    }
    var dog = go.GetComponent<IntelligentDogMovement>();
    if (dog != null)
    {
      dog.ApplyConfig(new IntelligentDogMovement.Config
      {
        moveSpeed = def.moveSpeed,
        maxHealth = def.maxHealth,
        attackDamage = def.attackDamage,
        attackSpeed = def.attackSpeed,
        maxJumps = def.maxJumps
      });
    }
    // PigeonController uses serialized fields directly, no ApplyConfig needed
    var pigeon = go.GetComponent<PigeonController>();
    if (pigeon != null)
    {
      // Pigeon configuration is done via SerializedFields in the prefab
      // SetupSpawnPosition should be called by the spawner after instantiation
    }

    // Apply skin (if skinId > 1, use override animations)
    int normalizedSkinId = NormalizeSkinId(def.enemyKind, def.skinId);
    if (normalizedSkinId > 1)
    {
      EnemySkinManager.ApplySkin(go, normalizedSkinId);
    }

    // Toast selection
    var toast = go.GetComponentInChildren<ToastStats>(true);
    if (toast != null && def.toastDrops != null && def.toastDrops.Count > 0)
    {
      var profile = PickToastProfile(def.toastDrops);
      if (profile != null)
      {
        toast.SetProfile(profile); // SetProfile 내부에서 BuildRuntimeStats 호출

        // Indicator에 프로필의 toastType을 반영하여 외형/이름이 정의별로 유지되도록 함
        var indicator = toast.GetComponent<ToastIndicator>();
        if (indicator != null)
        {
          indicator.SetToast(profile.toastType);
        }
      }
    }
  }

  private static int NormalizeSkinId(EnemyKind kind, int requested)
  {
    if (requested <= 1) return 1;
    switch (kind)
    {
      case EnemyKind.Dog:
        return Mathf.Clamp(requested, 1, 4);
      case EnemyKind.Cat:
        return Mathf.Clamp(requested, 1, 3);
      default:
        return 1;
    }
  }

  private static ToastStatProfile PickToastProfile(List<ToastDropConfig> drops)
  {
    float total = 0f;
    foreach (var d in drops)
    {
      if (d != null && d.profile != null && d.weight > 0f) total += d.weight;
    }
    if (total <= 0f) return null;
    float roll = Random.Range(0f, total);
    float cumulative = 0f;
    foreach (var d in drops)
    {
      if (d == null || d.profile == null || d.weight <= 0f) continue;
      cumulative += d.weight;
      if (roll <= cumulative)
      {
        return d.profile;
      }
    }
    return null;
  }

  [System.Serializable]
  public class SpawnDefinition
  {
    public EnemyKind enemyKind = EnemyKind.Dog;
    public int skinId = 0;
    public float moveSpeed = 2f;
    public float maxHealth = 100f;
    public float attackDamage = 1f;
    public float attackSpeed = 1f;
    public int maxJumps = 1;
    [Tooltip("이 항목을 true로 하면 아래 spawnPosition을 절대 좌표로 사용합니다.")]
    public bool useCustomPosition = false;
    [Tooltip("useCustomPosition이 true일 때 사용할 월드 좌표")]
    public Vector3 spawnPosition;
    public List<ToastDropConfig> toastDrops = new List<ToastDropConfig>();
  }

  [System.Serializable]
  public class ToastDropConfig
  {
    [Tooltip("ToastStatProfile asset to apply to the spawned enemy toast.")]
    public ToastStatProfile profile;
    [Tooltip("Weight/probability; higher = more likely.")]
    public float weight = 1f;
  }
}
