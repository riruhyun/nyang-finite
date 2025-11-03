using UnityEngine;

/// <summary>
/// 플레이어를 계속 부드럽게 따라가는 카메라 컨트롤러
/// X축: 1초 동안 Ease In-Out 이동 (영역 단위로 스냅)
/// Y축: 플레이어를 부드럽게 따라감 (계속 보간)
/// 오른쪽 +6 → +20, 왼쪽 복귀는 -9
/// 왼쪽 -6 → -20, 오른쪽 복귀는 +9
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
    [SerializeField] private float yFollowSpeed = 5f;  // Y축 따라가는 속도 (커질수록 빠름)
    [SerializeField] private float yOffset = 0f;       // Y축 기본 오프셋

    [Header("Transition Settings")]
    [SerializeField] private float transitionTime = 1f;
    [SerializeField] private bool useEaseInOut = true;

    private bool isMovingX = false;
    private int xState = 0; // 1=오른쪽으로 더 앞선 상태, -1=왼쪽으로 더 앞선 상태

    private float xMoveStartTime, xMoveStartPos, xMoveTargetPos;

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
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos;

        float deltaX = player.position.x - currentPos.x;

        // =====================================================
        // X축 (Ease-In-Out 이동)
        // =====================================================
        if (!isMovingX)
        {
            if (xState == 1)
            {
                if (deltaX < -xReturnThreshold)
                    StartCameraMoveX(currentPos.x, currentPos.x - xMoveDistance, -1);
                else if (deltaX > xThreshold)
                    StartCameraMoveX(currentPos.x, currentPos.x + xMoveDistance, 1);
            }
            else if (xState == -1)
            {
                if (deltaX > xReturnThreshold)
                    StartCameraMoveX(currentPos.x, currentPos.x + xMoveDistance, 1);
                else if (deltaX < -xThreshold)
                    StartCameraMoveX(currentPos.x, currentPos.x - xMoveDistance, -1);
            }
            else
            {
                if (deltaX > xThreshold)
                    StartCameraMoveX(currentPos.x, currentPos.x + xMoveDistance, 1);
                else if (deltaX < -xThreshold)
                    StartCameraMoveX(currentPos.x, currentPos.x - xMoveDistance, -1);
            }
        }
        else
        {
            float t = Mathf.Clamp01((Time.time - xMoveStartTime) / transitionTime);
            if (useEaseInOut) t = t * t * (3f - 2f * t);
            newPos.x = Mathf.Lerp(xMoveStartPos, xMoveTargetPos, t);
            if (t >= 1f) isMovingX = false;
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

    private void StartCameraMoveX(float start, float target, int direction)
    {
        isMovingX = true;
        xMoveStartPos = start;
        xMoveTargetPos = target;
        xMoveStartTime = Time.time;
        xState = direction;
        Debug.Log($"➡ 카메라 X 이동 시작 ({(direction == 1 ? "오른쪽" : "왼쪽")}) {start:F2} → {target:F2}");
    }
}
