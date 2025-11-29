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
    LegacyPigeon // Use legacy/alternate AI for pigeon
  }

  /// <summary>
  /// Spawns enemies with a shared tracking engine.
  /// </summary>
  /// <param name="ownerForSharedEngine">GameObject that will host the shared engine (one instance).</param>
  /// <param name="dogPrefab">Prefab for Dog enemy (can be reused for LegacyPigeon if desired).</param>
  /// <param name="pigeonPrefab">Prefab for Pigeon enemy (optional if using legacy as pigeon).</param>
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
    Vector3 enemyToastOffset = default
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

    // If positions provided, use them. Otherwise, scatter around owner.
    int spawnCount = positions != null && positions.Length > 0 ? positions.Length : Mathf.Max(0, count);
    for (int i = 0; i < spawnCount; i++)
    {
      Vector3 pos;
      if (positions != null && positions.Length > 0)
      {
        pos = positions[i];
      }
      else
      {
        // simple radial scatter near owner
        var angle = (Mathf.PI * 2f) * (i / Mathf.Max(1f, (float)spawnCount));
        var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.5f;
        pos = ownerForSharedEngine.transform.position + offset;
      }

      // Determine kind and prefab per clone
      var k = (kindsByIndex != null && i < kindsByIndex.Length) ? kindsByIndex[i] : enemyKind;
      GameObject prefabForClone = dogPrefab;
      switch (k)
      {
        case EnemyKind.Dog:
          prefabForClone = dogPrefab;
          break;
        case EnemyKind.Pigeon:
          prefabForClone = (useLegacyAsPigeonAI || pigeonPrefab == null) ? dogPrefab : pigeonPrefab;
          break;
        case EnemyKind.LegacyPigeon:
          prefabForClone = dogPrefab; // explicitly reuse dog as legacy pigeon
          break;
      }

      var go = Object.Instantiate(prefabForClone, pos, Quaternion.identity, parent);
      // Ensure spawned clone is active even if the template was kept disabled in the scene.
      if (!go.activeSelf) go.SetActive(true);

      // Optional toast follower/indicator attached to the clone
      if (enemyToastPrefab != null)
      {
        var toast = Object.Instantiate(enemyToastPrefab, go.transform);
        toast.transform.localPosition = enemyToastOffset;
        toast.transform.localRotation = Quaternion.identity;
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

      spawned.Add(go);
    }

    return spawned;
  }
}
