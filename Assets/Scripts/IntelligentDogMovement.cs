using UnityEngine;
using Pathfinding;

/// <summary>
/// AIPath 컴포넌트를 활용하여 Dog가 지능적으로 점프하고 이동하는 스크립트
/// AIPath의 경로 정보를 읽어 수동으로 이동과 점프를 제어합니다.
/// </summary>
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Rigidbody2D))]
public class IntelligentDogMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("최대 이동 속도")]
    [SerializeField] private float maxSpeed = 5f;

    [Header("Jump Settings")]
    [Tooltip("점프력")]
    [SerializeField] private float jumpForce = 21f;

    [Tooltip("경로상 다음 지점과의 높이 차이가 이 값보다 크면 점프")]
    [SerializeField] private float jumpHeightThreshold = 2f;

    [Tooltip("경로상 다음 지점과의 수평 거리가 이 값보다 작으면 점프")]
    [SerializeField] private float jumpDistanceThreshold = 2f;

    [Tooltip("점프 쿨다운 시간")]
    [SerializeField] private float jumpCooldown = 0.5f;

    [Header("Ground Check")]
    [Tooltip("지면 체크 레이어")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("지면 체크 거리")]
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Tooltip("지면 체크 시작 오프셋")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

    [Header("Path Following")]
    [Tooltip("경로상 다음 웨이포인트까지의 도달 거리")]
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Tooltip("경로를 얼마나 앞서 볼 것인지 (웨이포인트 개수)")]
    [SerializeField] private int lookAheadWaypoints = 2;

    [Header("Fallback Behavior")]
    [Tooltip("경로가 없을 때 직선 이동 사용")]
    [SerializeField] private bool useDirectMovementFallback = true;

    [Tooltip("직선 이동 시 장애물 감지 거리")]
    [SerializeField] private float obstacleDetectionDistance = 1.5f;

    // Components
    private AIPath aiPath;
    private Rigidbody2D rb;
    private DogEnemy dogEnemy;

    // State
    private float lastJumpTime;
    private int currentWaypointIndex = 0;
    private bool isGrounded;

    void Awake()
    {
        aiPath = GetComponent<AIPath>();
        rb = GetComponent<Rigidbody2D>();
        dogEnemy = GetComponent<DogEnemy>();

        // AIPath의 자동 이동 비활성화
        if (aiPath != null)
        {
            aiPath.canMove = false;
        }
    }

    void Start()
    {
        lastJumpTime = -jumpCooldown; // 시작 시 바로 점프 가능
    }

    void FixedUpdate()
    {
        // 넉백 중이거나 살아있지 않으면 이동하지 않음
        if (dogEnemy != null && (!dogEnemy.IsAlive() || dogEnemy.isKnockedBack))
        {
            return;
        }

        CheckGroundStatus();

        // AIPath가 유효한 경로를 가지고 있는지 확인
        if (aiPath == null || !aiPath.hasPath)
        {
            // 경로가 없을 때 직접 이동 시도
            if (useDirectMovementFallback)
            {
                MoveDirectlyToTarget();
            }
            return;
        }

        // 경로 따라가기
        FollowPath();
    }

    /// <summary>
    /// 지면 상태 확인
    /// </summary>
    void CheckGroundStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;

        // Raycast로 지면 체크
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;

        // CircleCast로 추가 체크 (더 안정적)
        if (!isGrounded)
        {
            hit = Physics2D.CircleCast(checkPosition, 0.1f, Vector2.down, groundCheckDistance, groundLayer);
            isGrounded = hit.collider != null;
        }
    }

    /// <summary>
    /// AIPath의 경로를 따라 이동
    /// </summary>
    void FollowPath()
    {
        // 경로의 웨이포인트 가져오기
        System.Collections.Generic.List<Vector3> path = new System.Collections.Generic.List<Vector3>();
        aiPath.GetRemainingPath(path, out bool stale);

        if (path == null || path.Count < 2)
        {
            return;
        }

        // 현재 위치와 다음 웨이포인트 찾기
        Vector3 currentPosition = transform.position;
        Vector3 nextWaypoint = GetNextWaypoint(path, currentPosition);

        if (nextWaypoint == Vector3.zero)
        {
            return;
        }

        // 다음 웨이포인트로의 방향 계산
        Vector2 direction = (nextWaypoint - currentPosition).normalized;
        float distanceToNext = Vector2.Distance(currentPosition, nextWaypoint);

        // 점프가 필요한지 확인
        bool shouldJump = ShouldJump(currentPosition, nextWaypoint, path);

        if (shouldJump && CanJump())
        {
            Jump();
        }

        // 수평 이동
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            MoveHorizontally(direction.x);
        }
    }

    /// <summary>
    /// 경로에서 다음 웨이포인트 가져오기
    /// </summary>
    Vector3 GetNextWaypoint(System.Collections.Generic.List<Vector3> path, Vector3 currentPosition)
    {
        if (path.Count == 0) return Vector3.zero;

        // 현재 위치에서 가장 가까운 웨이포인트 찾기
        float closestDist = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < path.Count; i++)
        {
            float dist = Vector2.Distance(currentPosition, path[i]);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        // 다음 웨이포인트 반환 (lookAheadWaypoints만큼 앞서 봄)
        int targetIndex = Mathf.Min(closestIndex + lookAheadWaypoints, path.Count - 1);
        return path[targetIndex];
    }

    /// <summary>
    /// 점프가 필요한지 판단
    /// </summary>
    bool ShouldJump(Vector3 currentPosition, Vector3 nextWaypoint, System.Collections.Generic.List<Vector3> path)
    {
        if (!isGrounded)
        {
            return false; // 공중에 있으면 점프 불가
        }

        float heightDifference = nextWaypoint.y - currentPosition.y;
        float horizontalDistance = Mathf.Abs(nextWaypoint.x - currentPosition.x);

        // 높이 차이가 threshold보다 크고, 수평 거리가 적당하면 점프
        if (heightDifference > jumpHeightThreshold && horizontalDistance < jumpDistanceThreshold)
        {
            return true;
        }

        // 경로상 장애물이 있는지 확인 (앞을 봤을 때 높은 벽이 있는지)
        Vector2 rayOrigin = currentPosition;
        Vector2 rayDirection = (nextWaypoint - currentPosition).normalized;
        float rayDistance = Mathf.Min(horizontalDistance, jumpDistanceThreshold);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, groundLayer);

        // 앞에 장애물이 있고, 그 장애물이 현재 위치보다 높으면 점프
        if (hit.collider != null && hit.point.y > currentPosition.y + 0.5f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 점프 가능 여부 확인
    /// </summary>
    bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime) >= jumpCooldown;
    }

    /// <summary>
    /// 점프 실행
    /// </summary>
    void Jump()
    {
        if (rb == null) return;

        // 현재 y 속도를 0으로 리셋하고 점프
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        lastJumpTime = Time.time;

        Debug.Log($"Dog jumped at {transform.position}");
    }

    /// <summary>
    /// 수평 이동
    /// </summary>
    void MoveHorizontally(float directionX)
    {
        if (rb == null) return;

        // 가속도를 이용한 부드러운 이동
        float targetVelocityX = directionX * moveSpeed;
        float currentVelocityX = rb.linearVelocity.x;

        // 속도 제한
        targetVelocityX = Mathf.Clamp(targetVelocityX, -maxSpeed, maxSpeed);

        // 부드러운 가속
        float newVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, moveSpeed * Time.fixedDeltaTime * 10f);

        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    /// <summary>
    /// 경로가 없을 때 직접 타겟으로 이동 (폴백)
    /// </summary>
    void MoveDirectlyToTarget()
    {
        if (aiPath == null || aiPath.destination == null)
        {
            return;
        }

        Vector3 targetPosition = aiPath.destination;
        Vector3 currentPosition = transform.position;
        Vector2 directionToTarget = (targetPosition - currentPosition).normalized;

        // 앞에 장애물이 있는지 확인
        RaycastHit2D obstacleHit = Physics2D.Raycast(
            currentPosition,
            directionToTarget,
            obstacleDetectionDistance,
            groundLayer
        );

        // 장애물이 있고 높이가 낮으면 점프 시도
        if (obstacleHit.collider != null)
        {
            float heightDiff = obstacleHit.point.y - currentPosition.y;

            // 장애물이 점프 가능한 높이면 점프
            if (heightDiff > 0.3f && heightDiff < jumpHeightThreshold * 1.5f && CanJump())
            {
                Jump();
            }
            // 장애물이 너무 높으면 우회 시도
            else if (heightDiff > jumpHeightThreshold * 1.5f)
            {
                // 위나 아래로 우회 경로 찾기
                Vector2 alternativeDirection = FindAlternativeDirection(directionToTarget);
                if (alternativeDirection != Vector2.zero)
                {
                    MoveHorizontally(alternativeDirection.x);
                    return;
                }
            }
        }

        // 기본 직선 이동
        if (Mathf.Abs(directionToTarget.x) > 0.01f)
        {
            MoveHorizontally(directionToTarget.x);
        }

        // 목표가 위에 있으면 점프
        float verticalDiff = targetPosition.y - currentPosition.y;
        if (verticalDiff > jumpHeightThreshold && CanJump())
        {
            Jump();
        }
    }

    /// <summary>
    /// 장애물 우회를 위한 대체 방향 찾기
    /// </summary>
    Vector2 FindAlternativeDirection(Vector2 blockedDirection)
    {
        // 위쪽 체크
        Vector2 upDirection = new Vector2(blockedDirection.x, 0.5f).normalized;
        RaycastHit2D upHit = Physics2D.Raycast(
            transform.position,
            upDirection,
            obstacleDetectionDistance,
            groundLayer
        );

        if (upHit.collider == null)
        {
            return upDirection;
        }

        // 아래쪽 체크
        Vector2 downDirection = new Vector2(blockedDirection.x, -0.5f).normalized;
        RaycastHit2D downHit = Physics2D.Raycast(
            transform.position,
            downDirection,
            obstacleDetectionDistance,
            groundLayer
        );

        if (downHit.collider == null)
        {
            return downDirection;
        }

        return Vector2.zero; // 우회 경로 없음
    }

    /// <summary>
    /// 디버그용 시각화
    /// </summary>
    void OnDrawGizmos()
    {
        if (aiPath == null || !aiPath.hasPath) return;

        // 경로 그리기
        System.Collections.Generic.List<Vector3> path = new System.Collections.Generic.List<Vector3>();
        aiPath.GetRemainingPath(path, out bool stale);

        if (path != null && path.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }

            // 다음 웨이포인트 표시
            Vector3 nextWaypoint = GetNextWaypoint(path, transform.position);
            if (nextWaypoint != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(nextWaypoint, 0.3f);
                Gizmos.DrawLine(transform.position, nextWaypoint);
            }
        }

        // 지면 체크 시각화
        if (Application.isPlaying)
        {
            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(checkPosition, checkPosition + Vector2.down * groundCheckDistance);

            // 직접 이동 모드일 때 타겟 방향 표시
            if (useDirectMovementFallback && (!aiPath.hasPath) && aiPath.destination != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, aiPath.destination);

                // 장애물 감지 범위 표시
                Vector2 directionToTarget = (aiPath.destination - transform.position).normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, (Vector2)transform.position + directionToTarget * obstacleDetectionDistance);
            }
        }
    }
}
