using UnityEngine;

/// <summary>
/// 플레이어를 계속 부드럽게 따라가는 카메라 컨트롤러
/// X축: 기준 위치 기반으로 정확하게 이동 (오차 누적 방지)
/// Y축: 플레이어를 부드럽게 따라감
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform player;

    [Header("X-axis Settings")]
    [SerializeField] private float xThreshold = 6f;
    [SerializeField] private float xReturnThreshold = 9f;
    [SerializeField] private float xMoveDistance = 20f;

    [Header("Y-axis Settings")]
    [SerializeField] private float yFollowSpeed = 5f;
    [SerializeField] private float yOffset = 0f;

    [Header("Transition Settings")]
    [SerializeField] private float transitionTime = 1f;
    [SerializeField] private bool useEaseInOut = true;

    private bool isMovingX = false;
    private int xState = 0;
    private float baseCameraX = 0f;

    private float xMoveStartTime;
    private float xMoveStartPos;
    private float xMoveTargetPos;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) player = p.transform;
            else
            {
                Debug.LogError("CameraController: Player를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }
        
        baseCameraX = transform.position.x;
        Debug.Log($"[카메라] 기준 X 위치 저장: {baseCameraX:F2}");
    }

private void LateUpdate()
    {
        if (player == null) return;

        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos;

        // 현재 카메라 영역의 중심에서 플레이어까지의 거리
        float currentCenterX = baseCameraX + xState * xMoveDistance;
        float deltaX = player.position.x - currentCenterX;

        // =====================================================
        // X축 (계속 확장 가능한 이동)
        // =====================================================
        if (isMovingX)
        {
            // 이동 중
            float t = Mathf.Clamp01((Time.time - xMoveStartTime) / transitionTime);
            if (useEaseInOut) t = t * t * (3f - 2f * t);
            newPos.x = Mathf.Lerp(xMoveStartPos, xMoveTargetPos, t);
            
            if (t >= 1f)
            {
                isMovingX = false;
                // 이동 완료 후 정확한 위치로 설정
                newPos.x = xMoveTargetPos;
                Debug.Log($"[카메라] 이동 완료! 최종 위치: {newPos.x:F2}, xState={xState}, player.x={player.position.x:F2}, delta={(player.position.x - (baseCameraX + xState * xMoveDistance)):F2}");
            }
        }
        else
        {
            // 이동 중이 아닐 때만 새로운 이동 트리거 확인
            // 안정적인 임계값: 카메라가 플레이어를 놓치지 않도록
            float safeThreshold = xThreshold;
            float safeReturnThreshold = xMoveDistance - xThreshold - 2f; // 안전 마진
            
            if (deltaX > safeThreshold)
            {
                // 오른쪽으로 이동
                float newTargetX = currentCenterX + xMoveDistance;
                int newDirection = xState + 1;
                Debug.Log($"[카메라] 오른쪽 이동 트리거! deltaX={deltaX:F2} > {safeThreshold:F2}, player.x={player.position.x:F2}");
                StartCameraMoveX(newTargetX, newDirection);
            }
            else if (deltaX < -safeReturnThreshold)
            {
                // 왼쪽으로 이동 (플레이어가 충분히 뒤로 갔을 때만)
                float newTargetX = currentCenterX - xMoveDistance;
                int newDirection = xState - 1;
                Debug.Log($"[카메라] 왼쪽 이동 트리거! deltaX={deltaX:F2} < -{safeReturnThreshold:F2}, player.x={player.position.x:F2}");
                StartCameraMoveX(newTargetX, newDirection);
            }
        }

        // =====================================================
        // Y축 (부드럽게 따라오기)
        // =====================================================
        float targetY = player.position.y + yOffset;
        newPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * yFollowSpeed);

        // =====================================================
        // 최종 적용
        // =====================================================
        newPos.z = currentPos.z;
        transform.position = newPos;
    }

private void StartCameraMoveX(float targetX, int direction)
    {
        isMovingX = true;
        xMoveStartPos = transform.position.x;
        xMoveTargetPos = targetX;
        xMoveStartTime = Time.time;
        xState = direction;
        Debug.Log($"[카메라] X 이동: {xMoveStartPos:F2} → {targetX:F2}, xState: {xState}, player.x={player.position.x:F2}, currentCenter={(baseCameraX + (xState - (direction - xState)) * xMoveDistance):F2}, deltaX={(player.position.x - (baseCameraX + (xState - (direction - xState)) * xMoveDistance)):F2}");
    }
}
