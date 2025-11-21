using UnityEngine;
using Pathfinding;

/// <summary>
/// A* 그리드 자동 재스캔 시스템
/// Ground가 이동한 후 자동으로 그리드를 업데이트합니다
/// </summary>
public class AstarAutoRescan : MonoBehaviour
{
    [Header("재스캔 설정")]
    [Tooltip("게임 시작 시 자동 재스캔")]
    public bool rescanOnStart = true;
    
    [Tooltip("재스캔 딜레이 (초) - Ground 이동 후 대기 시간")]
    public float rescanDelay = 0.5f;
    
    [Tooltip("Ground 오브젝트 태그")]
    public string groundTag = "Untagged"; // Ground들이 Layer 7에 있음
    
    [Tooltip("Ground 레이어 마스크")]
    public LayerMask groundLayer;
    
    [Header("디버그")]
    [Tooltip("재스캔 로그 출력")]
    public bool showDebugLogs = true;
    
    private bool hasScanned = false;
    private float scanTimer = 0f;
    private Vector3[] initialGroundPositions;
    private Transform[] groundObjects;
    
    private void Start()
    {
        if (rescanOnStart)
        {
            // Ground 오브젝트들의 초기 위치 저장
            FindAndStoreGroundObjects();
            
            // 딜레이 후 재스캔
            scanTimer = rescanDelay;
            
            if (showDebugLogs)
            {
                Debug.Log($"[AstarAutoRescan] {rescanDelay}초 후 재스캔 예약됨");
            }
        }
    }
    
    private void Update()
    {
        // 재스캔 타이머
        if (!hasScanned && scanTimer > 0f)
        {
            scanTimer -= Time.deltaTime;
            
            if (scanTimer <= 0f)
            {
                PerformRescan();
                hasScanned = true;
            }
        }
        
        // Ground 위치 변경 감지 (선택적)
        if (hasScanned && groundObjects != null)
        {
            CheckGroundMovement();
        }
    }
    
    /// <summary>
    /// Ground 오브젝트들 찾기 및 저장
    /// </summary>
    private void FindAndStoreGroundObjects()
    {
        // Layer 7 (Ground)에 있는 모든 오브젝트 찾기
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        System.Collections.Generic.List<Transform> grounds = new System.Collections.Generic.List<Transform>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == 7) // Ground layer
            {
                grounds.Add(obj.transform);
                if (showDebugLogs)
                {
                    Debug.Log($"[AstarAutoRescan] Ground 오브젝트 발견: {obj.name} at {obj.transform.position}");
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
            Debug.Log($"[AstarAutoRescan] 총 {groundObjects.Length}개의 Ground 오브젝트 감지됨");
        }
    }
    
    /// <summary>
    /// A* 그리드 재스캔 실행
    /// </summary>
    private void PerformRescan()
    {
        // AstarPath가 존재하는지 확인
        if (AstarPath.active == null)
        {
            Debug.LogError("[AstarAutoRescan] AstarPath를 찾을 수 없습니다! A* GameObject가 씬에 있는지 확인하세요.");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] A* 그리드 재스캔 시작...");
        }
        
        // 모든 그래프 스캔
        AstarPath.active.Scan();
        
        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] ✅ A* 그리드 재스캔 완료!");
            
            // Ground 위치 출력
            if (groundObjects != null)
            {
                foreach (Transform ground in groundObjects)
                {
                    Debug.Log($"[AstarAutoRescan] {ground.name} 최종 위치: {ground.position}");
                }
            }
        }
    }
    
    /// <summary>
    /// Ground 이동 감지 및 자동 재스캔
    /// </summary>
    private void CheckGroundMovement()
    {
        bool hasMoved = false;
        
        for (int i = 0; i < groundObjects.Length; i++)
        {
            if (groundObjects[i] == null) continue;
            
            Vector3 currentPos = groundObjects[i].position;
            Vector3 initialPos = initialGroundPositions[i];
            
            // 위치가 0.1 이상 차이나면 이동으로 간주
            if (Vector3.Distance(currentPos, initialPos) > 0.1f)
            {
                hasMoved = true;
                initialGroundPositions[i] = currentPos;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[AstarAutoRescan] {groundObjects[i].name} 이동 감지! {initialPos} → {currentPos}");
                }
            }
        }
        
        // Ground가 이동했으면 재스캔
        if (hasMoved)
        {
            if (showDebugLogs)
            {
                Debug.Log("[AstarAutoRescan] Ground 이동 감지! 재스캔 실행...");
            }
            PerformRescan();
        }
    }
    
    /// <summary>
    /// 수동 재스캔 (외부에서 호출 가능)
    /// </summary>
    public void ManualRescan()
    {
        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] 수동 재스캔 요청됨");
        }
        PerformRescan();
    }
    
    /// <summary>
    /// 특정 영역만 재스캔 (최적화)
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
            Debug.Log($"[AstarAutoRescan] 영역 재스캔: {bounds}");
        }
        
        // 특정 영역만 업데이트 (성능 최적화)
        GraphUpdateObject guo = new GraphUpdateObject(bounds);
        AstarPath.active.UpdateGraphs(guo);
        
        if (showDebugLogs)
        {
            Debug.Log("[AstarAutoRescan] ✅ 영역 재스캔 완료!");
        }
    }
    
#if UNITY_EDITOR
    [ContextMenu("즉시 재스캔")]
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
