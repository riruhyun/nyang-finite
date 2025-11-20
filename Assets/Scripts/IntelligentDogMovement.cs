using UnityEngine;
using Pathfinding;

/// <summary>
/// Dog 적 캐릭터 - 지능적인 경로찾기와 전투 기능을 통합
/// AIPath 컴포넌트를 활용하여 점프하고 이동하며, 플레이어를 추격/공격합니다.
/// </summary>
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Rigidbody2D))]
public class IntelligentDogMovement : Enemy
{
    [Header("Dog Specific Settings")]
    [Tooltip("추격 속도 배율")]
    [SerializeField] private float chaseSpeedMultiplier = 1.2f;

    [Tooltip("공격 범위")]
    [SerializeField] private float attackRange = 1.5f;

    [Tooltip("순찰 범위")]
    [SerializeField] private float patrolRange = 5f;

    [Tooltip("공격 데미지 (하트 1칸 = 1)")]
    [SerializeField] private int attackDamage = 1;

    [Tooltip("공격 애니메이션 지속 시간")]
    [SerializeField] private float attackAnimationDuration = 0.5f;

    [Tooltip("최대 이동 속도")]
    [SerializeField] private float maxSpeed = 5f;

    [Header("Jump Settings")]
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

    [Header("Patrol Settings")]
    [SerializeField] private float patrolWaitTime = 2f;

    // Components
    private AIPath aiPath;

    // State
    private float lastJumpTime;
    private bool isGrounded;
    private bool isAttacking = false;
    private float attackAnimationTimer = 0f;
    private Vector2 spawnPosition;
    private float patrolTimer = 0f;
    private int patrolDirection = 1; // 1: 오른쪽, -1: 왼쪽

    private enum DogState
    {
        Patrol,     // 순찰
        Chase,      // 추격
        Attack,     // 공격
        Idle        // 대기
    }

    private DogState currentState = DogState.Patrol;

    protected override void Awake()
    {
        base.Awake();
        aiPath = GetComponent<AIPath>();

        // AIPath의 자동 이동 비활성화
        if (aiPath != null)
        {
            aiPath.canMove = false;
        }
    }

    protected override void Start()
    {
        base.Start();
        lastJumpTime = -jumpCooldown; // 시작 시 바로 점프 가능
        spawnPosition = transform.position;
    }

    protected override void Update()
    {
        if (!isAlive || isKnockedBack) return;

        // 공격 애니메이션 중이면 타이머 업데이트
        if (isAttacking)
        {
            attackAnimationTimer -= Time.deltaTime;
            if (attackAnimationTimer <= 0f)
            {
                isAttacking = false;
                if (animator != null)
                {
                    animator.SetBool("IsAttacking", false);
                }
            }
            return; // 공격 중에는 다른 행동 하지 않음
        }

        // 플레이어 감지 및 상태 전환
        CheckAndUpdateState();
    }

    void FixedUpdate()
    {
        // 넉백 중이거나 살아있지 않으면 이동하지 않음
        if (!isAlive || isKnockedBack) return;

        CheckGroundStatus();

        // 상태에 따른 행동 처리
        switch (currentState)
        {
            case DogState.Patrol:
                PatrolBehavior();
                break;
            case DogState.Chase:
                ChaseBehavior();
                break;
            case DogState.Attack:
                AttackBehavior();
                break;
            case DogState.Idle:
                IdleBehavior();
                break;
        }
    }

    /// <summary>
    /// 플레이어 위치에 따라 상태 업데이트
    /// </summary>
    private void CheckAndUpdateState()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = DogState.Attack;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = DogState.Chase;
        }
        else
        {
            currentState = DogState.Patrol;
        }
    }

    /// <summary>
    /// 순찰 행동
    /// </summary>
    private void PatrolBehavior()
    {
        patrolTimer += Time.deltaTime;

        // 일정 시간마다 방향 전환
        if (patrolTimer >= patrolWaitTime)
        {
            patrolDirection *= -1;
            patrolTimer = 0f;
        }

        // 스폰 위치 기준 순찰 범위 체크
        float distanceFromSpawn = transform.position.x - spawnPosition.x;
        if (Mathf.Abs(distanceFromSpawn) >= patrolRange)
        {
            patrolDirection *= -1;
        }

        MoveInDirection(new Vector2(patrolDirection, 0));
    }

    /// <summary>
    /// 추격 행동
    /// </summary>
    private void ChaseBehavior()
    {
        if (playerTransform == null) return;

        // AIPath 목적지 설정
        if (aiPath != null)
        {
            aiPath.destination = playerTransform.position;
        }

        // AIPath가 유효한 경로를 가지고 있는지 확인
        if (aiPath != null && aiPath.hasPath)
        {
            FollowPath();
        }
        else if (useDirectMovementFallback)
        {
            // 경로가 없을 때 직접 이동
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            MoveInDirection(directionToPlayer * chaseSpeedMultiplier);
        }
    }

    /// <summary>
    /// 공격 행동
    /// </summary>
    private void AttackBehavior()
    {
        // 공격 실행
        Attack();
    }

    /// <summary>
    /// 대기 행동
    /// </summary>
    private void IdleBehavior()
    {
        // 대기 상태에서는 아무것도 하지 않음
    }

    /// <summary>
    /// 지면 상태 확인
    /// </summary>
    void CheckGroundStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer) ||
                     Physics2D.CircleCast(checkPosition, 0.1f, Vector2.down, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// AIPath의 경로를 따라 이동
    /// </summary>
    void FollowPath()
    {
        var path = new System.Collections.Generic.List<Vector3>();
        aiPath.GetRemainingPath(path, out bool stale);

        if (path.Count < 2) return;

        Vector3 nextWaypoint = GetNextWaypoint(path, transform.position);
        if (nextWaypoint == Vector3.zero) return;

        Vector2 direction = (nextWaypoint - transform.position).normalized;

        // 공중에서 하강 중일 때 수평 이동
        if (!isGrounded && direction.y < -0.1f)
        {
            if (Mathf.Abs(direction.x) > 0.01f)
                MoveHorizontally(direction.x * Mathf.Max(3, moveSpeed));
            return;
        }

        // 점프 판정 및 실행
        if (ShouldJump(transform.position, nextWaypoint, path) && CanJump())
            Jump();

        // 수평 이동
        if (Mathf.Abs(direction.x) > 0.01f)
            MoveHorizontally(direction.x);
    }

    /// <summary>
    /// 경로에서 다음 웨이포인트 가져오기
    /// </summary>
    Vector3 GetNextWaypoint(System.Collections.Generic.List<Vector3> path, Vector3 currentPosition)
    {
        if (path.Count == 0) return Vector3.zero;

        int closestIndex = 0;
        float closestDist = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            float dist = Vector2.Distance(currentPosition, path[i]);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        return path[Mathf.Min(closestIndex + lookAheadWaypoints, path.Count - 1)];
    }

    /// <summary>
    /// 점프가 필요한지 판단
    /// </summary>
    bool ShouldJump(Vector3 currentPosition, Vector3 nextWaypoint, System.Collections.Generic.List<Vector3> path)
    {
        if (!isGrounded) return false;

        float heightDiff = nextWaypoint.y - currentPosition.y;
        float horizontalDist = Mathf.Abs(nextWaypoint.x - currentPosition.x);

        // 높이 차이가 크고 수평 거리가 적당하면 점프
        if (heightDiff > jumpHeightThreshold && horizontalDist < jumpDistanceThreshold)
            return true;

        // 앞에 높은 장애물이 있으면 점프
        RaycastHit2D hit = Physics2D.Raycast(
            currentPosition,
            (nextWaypoint - currentPosition).normalized,
            Mathf.Min(horizontalDist, jumpDistanceThreshold),
            groundLayer
        );

        return hit.collider != null && hit.point.y > currentPosition.y + 0.5f;
    }

    /// <summary>
    /// 점프 가능 여부 확인
    /// </summary>
    bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime) >= jumpCooldown;
    }

    /// <summary>
    /// 점프 실행 (Enemy 클래스의 Jump를 오버라이드하여 y 속도 리셋 및 쿨다운 추가)
    /// </summary>
    protected override void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        base.Jump(); // Enemy 클래스의 jumpForce 사용
        lastJumpTime = Time.time;
    }

    /// <summary>
    /// 수평 이동
    /// </summary>
    void MoveHorizontally(float directionX)
    {
        float targetVelocityX = Mathf.Clamp(directionX * moveSpeed, -maxSpeed, maxSpeed);
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, moveSpeed * Time.fixedDeltaTime * 10f);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        // 스프라이트 방향 설정
        if (Mathf.Abs(directionX) > 0.01f && spriteRenderer != null)
        {
            spriteRenderer.flipX = directionX < 0;
        }
    }

    /// <summary>
    /// 방향으로 이동 (Enemy 클래스의 Move 대신 사용)
    /// </summary>
    void MoveInDirection(Vector2 direction)
    {
        if (isKnockedBack) return;

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            MoveHorizontally(direction.x);
        }

        // 애니메이터 파라미터 업데이트
        if (animator != null)
        {
            bool isMoving = direction.magnitude > 0.01f;
            animator.SetBool("IsWalking", isMoving);
        }
    }

    /// <summary>
    /// 플레이어 감지 시 호출
    /// </summary>
    protected override void OnPlayerDetected()
    {
        currentState = DogState.Chase;
    }

    /// <summary>
    /// 실제 공격 수행
    /// </summary>
    protected override void PerformAttack()
    {
        if (playerTransform == null || isAttacking) return;

        // 플레이어와의 거리 재확인
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            // 공격 상태 시작
            isAttacking = true;
            attackAnimationTimer = attackAnimationDuration;

            // 공격 애니메이션 재생
            if (animator != null)
            {
                animator.SetBool("IsAttacking", true);
            }

            // 플레이어에게 데미지 전달
            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
    }

    /// <summary>
    /// 넉백 상태 해제 시 순찰 상태로 복귀
    /// </summary>
    protected override void ResetKnockback()
    {
        base.ResetKnockback();
        currentState = DogState.Patrol;
    }

    /// <summary>
    /// 장애물 우회를 위한 대체 방향 찾기
    /// </summary>
    Vector2 FindAlternativeDirection(Vector2 blockedDirection)
    {
        Vector2 upDirection = new Vector2(blockedDirection.x, 0.5f).normalized;
        if (!Physics2D.Raycast(transform.position, upDirection, obstacleDetectionDistance, groundLayer))
            return upDirection;

        Vector2 downDirection = new Vector2(blockedDirection.x, -0.5f).normalized;
        if (!Physics2D.Raycast(transform.position, downDirection, obstacleDetectionDistance, groundLayer))
            return downDirection;

        return Vector2.zero;
    }

    /// <summary>
    /// 디버그용 시각화
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // 공격 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 순찰 범위
        Gizmos.color = Color.blue;
        Vector3 spawnPos = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawLine(spawnPos + (Vector3.left * patrolRange), spawnPos + (Vector3.right * patrolRange));

        if (aiPath == null || !aiPath.hasPath) return;

        // 경로 그리기
        var path = new System.Collections.Generic.List<Vector3>();
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
                Gizmos.color = Color.cyan;
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
        }
    }
}
