// 2025-11-10 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
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
    [SerializeField] private float edgeDetectionDistance = 2f; // 가장자리 감지 거리
    [SerializeField] private float cameraMoveDistance = 20f; // 카메라 이동 거리

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
    
    private float playerXAtMoveStart; // 이동 시작 시 플레이어 X 위치
private float xMoveTargetPos;

    private float lastBoundaryX; // 경계를 넘어설 때의 x값 저장

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
        lastBoundaryX = baseCameraX; // 초기 경계값 설정
        Debug.Log($"[카메라] 기준 X 위치 저장: {baseCameraX:F2}");
    }

private void LateUpdate()
    {
        if (player == null) return;

        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos;

        // =====================================================
        // X축 - 가장자리 밀어내기 방식
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
                newPos.x = xMoveTargetPos;
                baseCameraX = newPos.x; // 기준 위치 업데이트
                Debug.Log($"[카메라] 이동 완료! 새 위치: {newPos.x:F2}");
            }
        }
        else
        {
            // 이동 중이 아닐 때 - 가장자리와의 거리 확인
            
            // 카메라는 Orthographic Size = 5 (height), aspect에 따라 width 계산
            Camera cam = GetComponent<Camera>();
            float cameraHeight = cam.orthographicSize * 2f;
            float cameraWidth = cameraHeight * cam.aspect;
            
            float cameraLeftEdge = currentPos.x - cameraWidth / 2f;
            float cameraRightEdge = currentPos.x + cameraWidth / 2f;
            
            float distanceToLeftEdge = player.position.x - cameraLeftEdge;
            float distanceToRightEdge = cameraRightEdge - player.position.x;
            
            // 왼쪽 가장자리에 너무 가까우면 왼쪽으로 이동
            if (distanceToLeftEdge < edgeDetectionDistance)
            {
                float newTargetX = currentPos.x - cameraMoveDistance;
                Debug.Log($"[카메라] 왼쪽 가장자리 감지! 거리: {distanceToLeftEdge:F2}, 이동: {currentPos.x:F2} → {newTargetX:F2}");
                StartCameraMoveX(newTargetX);
            }
            // 오른쪽 가장자리에 너무 가까우면 오른쪽으로 이동
            else if (distanceToRightEdge < edgeDetectionDistance)
            {
                float newTargetX = currentPos.x + cameraMoveDistance;
                Debug.Log($"[카메라] 오른쪽 가장자리 감지! 거리: {distanceToRightEdge:F2}, 이동: {currentPos.x:F2} → {newTargetX:F2}");
                StartCameraMoveX(newTargetX);
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

private void StartCameraMoveX(float targetX)
    {
        isMovingX = true;
        xMoveStartPos = transform.position.x;
        xMoveTargetPos = targetX;
        xMoveStartTime = Time.time;
        Debug.Log($"[카메라] 이동 시작: {xMoveStartPos:F2} → {targetX:F2}");
    }
}
