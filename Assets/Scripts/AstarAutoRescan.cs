using UnityEngine;
using Pathfinding;
using System.Text;

// Disable A* path logs globally on load
static class AstarLogSilencer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void DisablePathLogs()
    {
        if (AstarPath.active != null)
        {
            AstarPath.active.logPathResults = PathLog.None;
        }
    }
}

/// <summary>
/// A* 그리드를 자동으로 스캔합니다.
/// - 시작 시 지연 스캔
/// - Ground 오브젝트 이동 감지 시 스캔
/// - 플레이어 X축을 따라 그리드 중심을 이동시키며 주기 스캔
/// </summary>
public class AstarAutoRescan : MonoBehaviour
{
    [Header("리스캔 설정")]
    [Tooltip("게임 시작 후 자동 리스캔 여부")]
    public bool rescanOnStart = true;

    [Tooltip("초기 리스캔 대기 시간(초)")]
    public float rescanDelay = 0.5f;

    [Header("그래프 선택")]
    [Tooltip("따라갈 GridGraph 인덱스 (기본 0)")]
    public int gridGraphIndex = 0;
    [Tooltip("그래프 이름으로 선택 (우선순위: 이름 > 인덱스)")]
    public string gridGraphName = "Dog Graph";

    [Header("Ground 이동 감지 리스캔 (끄는 것이 기본)")]
    [Tooltip("Ground 오브젝트 이동을 감지하여 리스캔할지 여부 (끄면 Ground 이동으로 인한 스캔을 막음)")]
    public bool useGroundMovementRescan = false;
    [Tooltip("Ground 오브젝트 태그(옵션)")]
    public string groundTag = "Untagged";
    [Tooltip("Ground 레이어 마스크(옵션)")]
    public LayerMask groundLayer;

    [Header("디버그")]
    [Tooltip("디버그 로그 출력")]
    public bool showDebugLogs = true;

    private bool hasScanned = false;
    private float scanTimer = 0f;
    private Vector3[] initialGroundPositions;
    private Transform[] groundObjects;
    private bool loggedNoGraph = false;
    private bool loggedNoTarget = false;
    private bool loggedGraphsOnStart = false;
    private bool hasLastUpdateCenter = false;
    private Vector2 lastUpdateCenter;

    private void Start()
    {
        if (rescanOnStart)
        {
            FindAndStoreGroundObjects();
            scanTimer = rescanDelay;

            if (showDebugLogs)
            {
                Debug.Log($"[AstarAutoRescan] {rescanDelay}초 뒤 리스캔 예약됨");
            }
        }

        if (showDebugLogs && !loggedGraphsOnStart)
        {
            loggedGraphsOnStart = true;
            Debug.Log($"[AstarAutoRescan] 그래프 목록: {GetGraphNames()}");
        }

        if (AstarPath.active != null)
        {
            AstarPath.active.logPathResults = PathLog.None;
        }
    }

    private void Update()
    {
        // 예약 리스캔 외에는 자동 이동/스캔 없음

        // 초기 리스캔 타이머
        if (!hasScanned && scanTimer > 0f)
        {
            scanTimer -= Time.deltaTime;

            if (scanTimer <= 0f)
            {
                PerformRescan();
                hasScanned = true;
            }
        }

        // Ground 위치 변화 감지 (옵션)
        if (useGroundMovementRescan && hasScanned && groundObjects != null)
        {
            CheckGroundMovement();
        }
    }

    /// <summary>
    /// Ground 오브젝트 찾기 및 초기 위치 저장
    /// </summary>
    private void FindAndStoreGroundObjects()
    {
        if (!useGroundMovementRescan)
        {
            groundObjects = null;
            initialGroundPositions = null;
            return;
        }

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        System.Collections.Generic.List<Transform> grounds = new System.Collections.Generic.List<Transform>();

        foreach (GameObject obj in allObjects)
        {
            bool matchLayer = (groundLayer.value & (1 << obj.layer)) != 0;
            bool matchTag = string.IsNullOrEmpty(groundTag) || groundTag == "Untagged" || obj.CompareTag(groundTag);

            if (matchLayer || matchTag)
            {
                grounds.Add(obj.transform);
                if (showDebugLogs)
                {
                    Debug.Log($"[AstarAutoRescan] Ground 후보 발견: {obj.name} at {obj.transform.position}");
                }
            }
        }

        groundObjects = grounds.ToArray();
        initialGroundPositions = new Vector3[groundObjects.Length];

        for (int i = 0; i < groundObjects.Length; i++)
        {
            initialGroundPositions[i] = groundObjects[i].position;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[AstarAutoRescan] 초기 {groundObjects.Length}개의 Ground 후보 기록");
        }
    }

    /// <summary>
    /// A* 전체 리스캔
    /// </summary>
    private void PerformRescan()
    {
        if (AstarPath.active == null)
        {
            Debug.LogError("[AstarAutoRescan] AstarPath를 찾을 수 없습니다! 씬에 AstarPath가 있는지 확인하세요.");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] A* 리스캔 수행...");
        }

        AstarPath.active.Scan();

        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] A* 리스캔 완료");
        }
    }

    /// <summary>
    /// Ground 이동 감지 시 리스캔
    /// </summary>
    private void CheckGroundMovement()
    {
        bool hasMoved = false;

        for (int i = 0; i < groundObjects.Length; i++)
        {
            if (groundObjects[i] == null) continue;

            Vector3 currentPos = groundObjects[i].position;
            Vector3 initialPos = initialGroundPositions[i];

            if (Vector3.Distance(currentPos, initialPos) > 0.1f)
            {
                hasMoved = true;
                initialGroundPositions[i] = currentPos;

                if (showDebugLogs)
                {
                    Debug.Log($"[AstarAutoRescan] {groundObjects[i].name} 이동 감지: {initialPos} -> {currentPos}");
                }
            }
        }

        if (hasMoved)
        {
            if (showDebugLogs)
            {
                Debug.Log("[AstarAutoRescan] Ground 이동 감지: 리스캔 실행");
            }
            PerformRescan();
        }
    }

    // 사용할 GridGraph 가져오기 (인덱스 기반)
    private GridGraph GetGridGraph()
    {
        if (AstarPath.active == null) return null;

        var graphs = AstarPath.active.data.graphs;
        if (graphs == null || graphs.Length == 0) return null;

        // 이름 우선
        if (!string.IsNullOrWhiteSpace(gridGraphName))
        {
            foreach (var g in graphs)
            {
                GridGraph gg = g as GridGraph;
                if (gg != null && gg.name == gridGraphName)
                {
                    return gg;
                }
            }
        }

        // 첫 GridGraph로 폴백
        foreach (var g in graphs)
        {
            GridGraph gg = g as GridGraph;
            if (gg != null) return gg;
        }

        int idx = Mathf.Clamp(gridGraphIndex, 0, graphs.Length - 1);
        return graphs[idx] as GridGraph;
    }

    // 플레이어 주변 영역만 부분 업데이트 (전체 Scan 대신)
    private void UpdatePartialArea(GridGraph gg, float targetX, float targetY)
    {
        if (AstarPath.active == null) return;

        float sizeX = gg.nodeSize * gg.width;
        float sizeY = gg.nodeSize * gg.depth;
        var bounds = new Bounds(new Vector3(targetX, targetY, gg.center.z), new Vector3(sizeX, sizeY, gg.nodeSize));

        var guo = new GraphUpdateObject(bounds)
        {
            updatePhysics = true
        };

        AstarPath.active.UpdateGraphs(guo);

        if (showDebugLogs)
        {
            Debug.Log($"[AstarAutoRescan] 부분 업데이트: center=({targetX:F2},{targetY:F2}), size=({sizeX:F2},{sizeY:F2})");
        }
    }

    /// <summary>
    /// 임의 위치/크기로 그래프를 부분 갱신(전체 스캔 없이)합니다.
    /// 필요하면 recenterGraph=true로 설정해 그래프 중심도 이동시킬 수 있습니다.
    /// </summary>
    public void UpdateAreaAtPosition(Vector3 worldCenter, float width, float height, bool recenterGraph = false)
    {
        GridGraph gg = GetGridGraph();
        if (gg == null || AstarPath.active == null) return;

        float clampedWidth = Mathf.Max(width, gg.nodeSize);
        float clampedHeight = Mathf.Max(height, gg.nodeSize);

        if (recenterGraph)
        {
            gg.center = new Vector3(worldCenter.x, worldCenter.y, gg.center.z);
            gg.UpdateTransform();
            hasLastUpdateCenter = true;
            lastUpdateCenter = new Vector2(worldCenter.x, worldCenter.y);
        }

        var bounds = new Bounds(new Vector3(worldCenter.x, worldCenter.y, gg.center.z),
            new Vector3(clampedWidth, clampedHeight, gg.nodeSize));

        var guo = new GraphUpdateObject(bounds)
        {
            updatePhysics = true
        };

        AstarPath.active.UpdateGraphs(guo);

        if (showDebugLogs)
        {
            Debug.Log($"[AstarAutoRescan] 임의 영역 부분 업데이트: center=({worldCenter.x:F2},{worldCenter.y:F2}), size=({clampedWidth:F2},{clampedHeight:F2}), recenter={recenterGraph}");
        }
    }

    /// <summary>
    /// 노드 개수를 기준으로 영역을 부분 갱신합니다.
    /// </summary>
    public void UpdateAreaAtNodeCounts(Vector3 worldCenter, int widthNodes, int heightNodes, bool recenterGraph = false)
    {
        GridGraph gg = GetGridGraph();
        if (gg == null) return;

        float w = Mathf.Max(widthNodes * gg.nodeSize, gg.nodeSize);
        float h = Mathf.Max(heightNodes * gg.nodeSize, gg.nodeSize);
        UpdateAreaAtPosition(worldCenter, w, h, recenterGraph);
    }

    private string GetGraphNames()
    {
        if (AstarPath.active == null || AstarPath.active.data.graphs == null) return "없음";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < AstarPath.active.data.graphs.Length; i++)
        {
            var g = AstarPath.active.data.graphs[i];
            if (g == null) continue;
            sb.Append($"[{i}] {g.GetType().Name} '{g.name}' ");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 수동 리스캔
    /// </summary>
    public void ManualRescan()
    {
        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] 수동 리스캔 요청");
        }
        PerformRescan();
    }

    /// <summary>
    /// 특정 영역만 리스캔
    /// </summary>
    public void RescanArea(Bounds bounds)
    {
        if (AstarPath.active == null)
        {
            Debug.LogError("[AstarAutoRescan] AstarPath를 찾을 수 없습니다!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[AstarAutoRescan] 부분 영역 리스캔 {bounds}");
        }

        GraphUpdateObject guo = new GraphUpdateObject(bounds);
        AstarPath.active.UpdateGraphs(guo);
    }

#if UNITY_EDITOR
    [ContextMenu("즉시 리스캔")]
    private void EditorRescan()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[AstarAutoRescan] 플레이 모드에서만 실행 가능합니다!");
            return;
        }
        ManualRescan();
    }

    [ContextMenu("Ground 오브젝트 찾기")]
    private void EditorFindGrounds()
    {
        FindAndStoreGroundObjects();
    }
#endif
}
