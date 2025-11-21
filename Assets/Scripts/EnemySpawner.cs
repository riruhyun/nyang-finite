using UnityEngine;

/// <summary>
/// Place on a single GameObject in the scene to spawn enemies on start.
/// Uses EnemySpawnHelper to centralize parameters and ensures a single
/// SharedTrackingEngine instance exists on this spawner object for all clones.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
  [Header("Prefabs")]
  [Tooltip("Dog enemy prefab (required)")]
  public GameObject dogPrefab;

  [Tooltip("Pigeon enemy prefab (optional if using legacy as pigeon)")]
  public GameObject pigeonPrefab;

  [Tooltip("Optional parent Transform for spawned enemies")]
  public Transform spawnParent;

  [Header("Spawn Policy")]
  [Tooltip("Spawn automatically on Start()")]
  public bool spawnOnStart = true;

  [Tooltip("If enabled, spawns one Dog at the shared tracking origin using SideView settings.")]
  public bool spawnDogAtTrackingOrigin = true;

  [Tooltip("Number of enemies to spawn when no explicit spawn points are set")]
  public int count = 3;

  [Tooltip("Scatter radius around this spawner when no explicit spawn points are set")]
  public float scatterRadius = 2f;

  [Tooltip("Optional explicit spawn points. Overrides count when provided")]
  public Transform[] spawnPoints;

  [Header("Per-Clone Tracking Overrides (optional)")]
  [Tooltip("Per-clone enemy kind; aligns with spawn order")]
  public EnemySpawnHelper.EnemyKind[] enemyKindsByIndex;

  [Tooltip("Per-clone algorithm; aligns with spawnPoints or count order")]
  public SharedTrackingEngine.TrackingAlgorithm[] algorithmsByIndex;

  [Tooltip("Per-clone view mode; aligns with spawnPoints or count order")]
  public SharedTrackingEngine.ViewMode[] viewsByIndex;

  [Header("Per-Clone Abilities (optional)")]
  [Tooltip("Per-clone jump toggle; aligns with spawn order")]
  public bool[] canJumpByIndex;

  [Header("Ability Defaults")]
  [Tooltip("Default jump toggle for clones when per-index value is not provided")]
  public bool defaultCanJump = true;

  [Header("Enemy Options")]
  public EnemySpawnHelper.EnemyKind enemyKind = EnemySpawnHelper.EnemyKind.Dog;

  [Tooltip("Reuse older AI logic as Pigeon (reuses Dog prefab)")]
  public bool useLegacyAsPigeonAI = false;

  [Header("Tracking/Movement View")]
  public SharedTrackingEngine.TrackingAlgorithm algorithm = SharedTrackingEngine.TrackingAlgorithm.DirectChase;
  public SharedTrackingEngine.ViewMode view = SharedTrackingEngine.ViewMode.SideView2D;

  [Tooltip("Keep one SharedTrackingEngine on this spawner for all clones")]
  public bool reuseOwnerTrackingEngine = true;

  void Start()
  {
    if (spawnOnStart)
    {
      if (spawnDogAtTrackingOrigin)
      {
        SpawnDogAtTrackingOriginSideView();
      }
      else
      {
        Spawn();
      }
    }
  }

  /// <summary>
  /// Spawn using current inspector settings.
  /// </summary>
  public void Spawn()
  {
    if (dogPrefab == null)
    {
      Debug.LogError("EnemySpawner: dogPrefab is required (used also for legacy pigeon).");
      return;
    }

    Vector3[] positions = null;
    if (spawnPoints != null && spawnPoints.Length > 0)
    {
      positions = new Vector3[spawnPoints.Length];
      for (int i = 0; i < spawnPoints.Length; i++)
      {
        positions[i] = spawnPoints[i] != null ? spawnPoints[i].position : transform.position;
      }
    }
    else
    {
      // Create scattered positions around spawner
      int n = Mathf.Max(0, count);
      positions = new Vector3[n];
      for (int i = 0; i < n; i++)
      {
        float ang = (Mathf.PI * 2f) * (i / Mathf.Max(1f, (float)n));
        Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * scatterRadius;
        positions[i] = transform.position + offset;
      }
    }

    EnemySpawnHelper.SpawnEnemies(
      ownerForSharedEngine: this.gameObject,
      dogPrefab: dogPrefab,
      pigeonPrefab: pigeonPrefab,
      count: positions.Length,
      positions: positions,
      enemyKind: enemyKind,
      algorithm: algorithm,
      view: view,
      useLegacyAsPigeonAI: useLegacyAsPigeonAI,
      reuseOwnerTrackingEngine: reuseOwnerTrackingEngine,
      parent: spawnParent,
      kindsByIndex: enemyKindsByIndex,
      algorithmsByIndex: algorithmsByIndex,
      viewsByIndex: viewsByIndex,
      canJumpByIndex: canJumpByIndex,
      canJumpDefault: defaultCanJump
    );
  }

  /// <summary>
  /// Spawns a single Dog at the shared tracking origin using SideView algorithm settings.
  /// "기본 트래킹 좌표" is interpreted as the SharedTrackingEngine.target position (or spawner position if no target).
  /// </summary>
  public void SpawnDogAtTrackingOriginSideView()
  {
    if (dogPrefab == null)
    {
      Debug.LogError("EnemySpawner: dogPrefab is required for spawning the dog at tracking origin.");
      return;
    }

    // Ensure/locate shared engine on this owner
    SharedTrackingEngine engine = null;
    if (reuseOwnerTrackingEngine)
    {
      engine = GetComponent<SharedTrackingEngine>();
      if (engine == null)
      {
        engine = gameObject.AddComponent<SharedTrackingEngine>();
      }
    }

    // Determine spawn position: engine target or spawner position
    Vector3 origin = (engine != null && engine.target != null) ? engine.target.position : transform.position;

    EnemySpawnHelper.SpawnEnemies(
      ownerForSharedEngine: this.gameObject,
      dogPrefab: dogPrefab,
      pigeonPrefab: pigeonPrefab,
      count: 1,
      positions: new Vector3[] { origin },
      enemyKind: EnemySpawnHelper.EnemyKind.Dog,
      algorithm: SharedTrackingEngine.TrackingAlgorithm.DirectChase,
      view: SharedTrackingEngine.ViewMode.SideView2D,
      useLegacyAsPigeonAI: false,
      reuseOwnerTrackingEngine: reuseOwnerTrackingEngine,
      parent: spawnParent,
      algorithmsByIndex: null,
      viewsByIndex: null,
      canJumpByIndex: null,
      canJumpDefault: defaultCanJump
    );
  }
}
