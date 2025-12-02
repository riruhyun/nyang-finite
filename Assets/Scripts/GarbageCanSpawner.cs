using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple world-position spawner for GarbageCan prefabs. Configure a list of entries in the inspector
/// to drop cans at fixed world coordinates (similar to how dogs are spawned).
/// </summary>
public class GarbageCanSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("World position where the garbage can should be spawned.")]
        public Vector2 worldPosition;

        [Tooltip("Optional prefab override for this entry. Uses the spawner's default prefab if null.")]
        public GarbageCan prefabOverride;

        [Tooltip("Optional Y-axis rotation for the spawned can (e.g., 180 to face left).")]
        public float yRotation;

        [Tooltip("Spawn this entry when the spawner runs?")]
        public bool spawnOnStart = true;

        [Tooltip("Optional drop overrides for this spawn. Leave empty to use prefab defaults.")]
        public List<GarbageCan.FoodDropEntry> dropOverrides = new List<GarbageCan.FoodDropEntry>();
    }

    [Header("Defaults")]
    [SerializeField] private GarbageCan defaultPrefab;
    [SerializeField] private Transform parentContainer;
    [SerializeField] private bool spawnOnAwake = true;

    [Header("Entries")]
    [SerializeField] private List<SpawnEntry> entries = new List<SpawnEntry>();

    private readonly List<GarbageCan> spawnedCans = new List<GarbageCan>();

    private void Awake()
    {
        if (spawnOnAwake)
        {
            SpawnAll();
        }
    }

    /// <summary>
    /// Spawn all configured entries that are marked to spawn.
    /// </summary>
    public void SpawnAll()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry entry = entries[i];
            if (entry == null || !entry.spawnOnStart)
            {
                continue;
            }

            SpawnSingle(entry);
        }
    }

    /// <summary>
    /// Spawn a single entry immediately, even if spawnOnStart is false.
    /// </summary>
    public GarbageCan SpawnSingle(SpawnEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("[GarbageCanSpawner] Tried to spawn null entry");
            return null;
        }

        GarbageCan prefab = entry.prefabOverride != null ? entry.prefabOverride : defaultPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[GarbageCanSpawner] No prefab configured for entry at " + entry.worldPosition);
            return null;
        }

        Vector3 spawnPos = new Vector3(entry.worldPosition.x, entry.worldPosition.y, prefab.transform.position.z);
        Quaternion rotation = Quaternion.identity;
        if (!Mathf.Approximately(entry.yRotation, 0f))
        {
            rotation = Quaternion.Euler(0f, entry.yRotation, 0f);
        }

        Transform parent = parentContainer != null ? parentContainer : transform;
        GarbageCan can = Instantiate(prefab, spawnPos, rotation, parent);
        if (entry.dropOverrides != null && entry.dropOverrides.Count > 0)
        {
            can.OverrideDrops(entry.dropOverrides);
        }
        spawnedCans.Add(can);
        return can;
    }

    /// <summary>
    /// Get a read-only list of all cans spawned by this spawner.
    /// </summary>
    public IReadOnlyList<GarbageCan> GetSpawnedCans() => spawnedCans;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (entries == null) return;
        Gizmos.color = Color.yellow;
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            Vector3 pos = new Vector3(entry.worldPosition.x, entry.worldPosition.y, 0f);
            Gizmos.DrawWireCube(pos, new Vector3(0.5f, 0.8f, 0.5f));
        }
    }
#endif

}
