using UnityEngine;

/// <summary>
/// Dog Enemy – 패트롤, 추격, 공격, 점프 AI 포함
/// </summary>
public class DogEnemy : Enemy
{
    [Header("Dog Specific Settings")]

    [Tooltip("추격 속도 배율")]
    [SerializeField] private float chaseSpeedMultiplier = 1.2f;

    [Tooltip("공격 범위")]
    [SerializeField] private float attackRange = 1.5f;

    [Tooltip("패트롤 범위")]
    [SerializeField] private float patrolRange = 5f;

    [Tooltip("공격 데미지 (1 = 기본)")]
    [SerializeField] private int attackDamage = 1;

    [Tooltip("공격 애니메이션 지속 시간")]
    [SerializeField] private float attackAnimationDuration = 0.5f;

    [Header("Jump Settings")]
    [Tooltip("점프 힘 (수직)")]
    [SerializeField] private float jumpForce = 700f;

    [Tooltip("점프를 고려하는 최소 Y 높이 차이")]
    [SerializeField] private float jumpHeightThreshold = 1.0f;

    [Tooltip("점프 쿨다운 (초)")]
    [SerializeField] private float jumpCooldown = 1.0f;

    [Tooltip("지면 레이어 마스크")]
  [SerializeField] private LayerMask groundLayer;
  [Tooltip("Optional transform for ground check (use feet)")]
  [SerializeField] private Transform groundCheck;
  [SerializeField] private float groundCheckRadius = 0.12f;
  [SerializeField] private float groundCoyoteTime = 0.05f;

  [Header("Debug")]
  [SerializeField] private bool debugJump = true;

    private float lastJumpTime = -10f;
    private Vector3 nextJumpTargetPosition;
    private bool isAttacking = false;
    private float attackAnimationTimer = 0f;
    private bool isGrounded = false;
  private bool groundedByCollision = false;
  private float lastGroundedAt = -10f;
  private bool jumpLocked = false;
  private bool wasGrounded = false;
  private float lastChaseDir = 0f;
  private float lastJumpDir = 0f;
  [SerializeField] private float chaseDirectionDeadzone = 0.2f;
  [SerializeField] private float chaseStopDistance = 0.05f;
  [SerializeField] private float verticalGapOffsetThreshold = 5f;
  [SerializeField] private float verticalGapXOffset = 20f;
  private bool isCollidingLeftWall = false;
  private bool isCollidingRightWall = false;

    [SerializeField] private float jumpForwardImpulse = 2.5f;
    [SerializeField] private float jumpLaunchToleranceX = 1.8f;
    private float desiredJumpX = float.NaN;
    private float desiredJumpDir = 0f;

    [SerializeField] private float jumpLeadTimeBase = 0.15f;
    [SerializeField] private float jumpLeadTimeBySpeed = 0.03f;
  [SerializeField] private float jumpLeadTimeMax = 0.35f;
  [SerializeField] private float jumpTimingOffset = 0.05f; // small extra lead to time jumps later
  [SerializeField] private int jumpLookAheadSegments = 8;
  [SerializeField] private float steepSlopeRatio = 3f; // dy/dx ratio to treat as wall-like
  [SerializeField] private float steepSlopeMinHeight = 0.15f;
  [SerializeField] private float noJumpWhenPlayerCloserThan = 0.6f;
  [SerializeField] private float airborneForwardBias = 3f; // minimum run-through distance while airborne
  private float airborneRunThroughX = float.NaN;
  private float desiredJumpHeight = 0f;
  private float jumpStartY = 0f;
  private bool pendingRunThrough = false;
  [Header("Debug Path")]
  [SerializeField] private bool debugDrawPath = false;
  [SerializeField] private Color pathGizmoColor = Color.cyan;
  [SerializeField] private float pathPointRadius = 0.05f;
  private readonly System.Collections.Generic.List<Vector3> cachedPathPoints = new System.Collections.Generic.List<Vector3>();
  private bool cachedPathStale = true;

    [Header("Patrol Settings")]
    [Tooltip("패트롤 방향 전환 대기 시간")]
    [SerializeField] private float patrolWaitTime = 2f;

    private Vector2 spawnPosition;
    private float patrolTimer = 0f;
    private int patrolDirection = 1; // 1 = 오른쪽, -1 = 왼쪽

    private enum DogState
    {
        Patrol,
        Chase,
        Attack,
        Idle
    }

    private DogState currentState = DogState.Patrol;

    private Vector2 GetFeetPosition()
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            return (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y + 0.02f);
        }
        return (Vector2)transform.position + Vector2.down * 0.15f;
    }

    protected override void Awake()
    {
        base.Awake();
        SetupCapsuleColliderLikePlayer();
        EnsureGroundCheck();
    }

    private void SetupCapsuleColliderLikePlayer()
    {
        // Mirror the player's CapsuleCollider2D configuration for consistent contact normals
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule == null)
        {
            capsule = gameObject.AddComponent<CapsuleCollider2D>();
        }
        capsule.size = new Vector2(0.2800002f, 0.2016948f);
        capsule.offset = new Vector2(-0.006397904f, -0.025f);
        capsule.direction = CapsuleDirection2D.Horizontal;

        if (capsule.sharedMaterial == null)
        {
            var mat = new PhysicsMaterial2D("EnemyPhysics");
            mat.friction = 0.3f;
            mat.bounciness = 0f;
            capsule.sharedMaterial = mat;
        }
    }

    private void EnsureGroundCheck()
    {
        if (groundCheck == null)
        {
            var gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            groundCheck = gc.transform;
        }
    }

    private bool CheckGroundContact()
    {
        // 0) Primary: use the CapsuleCollider2D itself (same spec as Player) for contact or a tiny downward cast
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            if (groundLayer.value != 0)
            {
                if (capsule.IsTouchingLayers(groundLayer)) return true;

                // Tiny downward capsule cast to detect ground just below feet
                Vector2 c = capsule.bounds.center;
                Vector2 s = capsule.bounds.size;
                float extra = 0.03f;
                var hit = Physics2D.CapsuleCast(c, s, CapsuleDirection2D.Vertical, 0f, Vector2.down, extra, groundLayer);
                if (hit.collider != null && hit.collider != capsule && hit.normal.y > 0.4f) return true;
            }
            else
            {
                if (capsule.IsTouchingLayers()) return true;
                Vector2 c = capsule.bounds.center;
                Vector2 s = capsule.bounds.size;
                float extra = 0.03f;
                var hit = Physics2D.CapsuleCast(c, s, CapsuleDirection2D.Vertical, 0f, Vector2.down, extra);
                if (hit.collider != null && hit.collider != capsule && hit.normal.y > 0.4f) return true;
            }
        }

        // 1) Overlap circle around optional groundCheck (if provided)
        if (groundCheck != null)
        {
            Collider2D c = groundLayer.value != 0
                ? Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer)
                : Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius);
            if (c != null && c != capsule) return true;
        }

        // 2) Dual raycasts from left/right feet as a final fallback
        Vector2 center = capsule != null ? (Vector2)capsule.bounds.center : (Vector2)transform.position;
        float halfWidth = capsule != null ? capsule.bounds.extents.x * 0.9f : 0.1f;
        Vector2 feetY = capsule != null
            ? new Vector2(center.x, center.y - capsule.bounds.extents.y + 0.02f)
            : (Vector2)transform.position + Vector2.down * 0.12f;
        Vector2 left = new Vector2(center.x - halfWidth, feetY.y);
        Vector2 right = new Vector2(center.x + halfWidth, feetY.y);
        float rayDist = 0.12f;

        bool HitOK(RaycastHit2D h) => h.collider != null && h.normal.y > 0.4f;

        RaycastHit2D hitL = groundLayer.value != 0
            ? Physics2D.Raycast(left, Vector2.down, rayDist, groundLayer)
            : Physics2D.Raycast(left, Vector2.down, rayDist);
        if (HitOK(hitL) && hitL.collider != capsule) return true;

        RaycastHit2D hitR = groundLayer.value != 0
            ? Physics2D.Raycast(right, Vector2.down, rayDist, groundLayer)
            : Physics2D.Raycast(right, Vector2.down, rayDist);
        if (HitOK(hitR) && hitR.collider != capsule) return true;

        return false;
    }

    private void PerformJump()
    {
        // Fresh ground check to prevent mid-air jumps (ignore coyote time here)
        bool groundContact = groundedByCollision || CheckGroundContact();

        // 1) 지면 체크 (즉시 접지 상태만 허용)
        if (!groundContact)
        {
            if (debugJump) Debug.Log("[DOG][JUMP] Blocked: not grounded (fresh check)");
            return;
        }

        // 1.5) Jump lock (one jump per landing)
        if (jumpLocked)
        {
            if (debugJump) Debug.Log("[DOG][JUMP] Blocked: jump locked until landing");
            return;
        }

        // 2) 쿨다운 체크
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            if (debugJump) Debug.Log("[DOG][JUMP] Blocked by cooldown");
            return;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var trackingClient = GetComponent<SharedTrackingClient>();
            if (trackingClient != null && !trackingClient.canJump)
            {
                if (debugJump) Debug.Log("[DOG][JUMP] Blocked: client can't jump");
                return;
            }

            // 방향 계산: 플레이어 상대 위치를 우선, 그 다음 경로 목표
            float dir = 0f;
            if (playerTransform != null)
            {
                float toPlayerX = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(toPlayerX) > 0.01f)
                    dir = Mathf.Sign(toPlayerX);
            }
            if (Mathf.Approximately(dir, 0f) && Mathf.Abs(desiredJumpDir) > 0.01f)
                dir = Mathf.Sign(desiredJumpDir);
            else if (Mathf.Approximately(dir, 0f) && !float.IsNaN(desiredJumpX))
                dir = Mathf.Sign(desiredJumpX - transform.position.x);

            if (Mathf.Approximately(dir, 0f))
                dir = 1f;

            // 수평 이동 속도 유지
            float baseSpeed = Mathf.Max(Mathf.Abs(rb.linearVelocity.x), moveSpeed);
            rb.linearVelocity = new Vector2(baseSpeed * dir, 0);

            // 실제 점프
            PlayerController.PerformJumpPhysics(rb, jumpForce);
            rb.AddForce(new Vector2(dir * jumpForwardImpulse, 0));

            lastJumpTime = Time.time;
            isGrounded = false;
            jumpLocked = true; // lock until actual ground contact clears it
            lastJumpDir = dir;
            jumpStartY = transform.position.y;
            pendingRunThrough = true; // set run-through after reaching desired height

            if (debugJump) Debug.Log($"[DOG][JUMP] Jump executed at {transform.position}");
        }
    }

    private bool ShouldJumpForPath()
    {
        Vector3 currentPos = transform.position;
        desiredJumpX = float.NaN;
        desiredJumpDir = 0f;
        desiredJumpHeight = 0f;

        if (playerTransform != null)
        {
            float distToPlayer = Vector2.Distance(currentPos, playerTransform.position);
            if (distToPlayer < noJumpWhenPlayerCloserThan)
            {
                if (debugJump) Debug.Log($"[DOG][JUMP] Skipped: player too close ({distToPlayer:F2} < {noJumpWhenPlayerCloserThan})");
                return false;
            }
        }

        var aiPath = GetComponent<Pathfinding.AIPath>();
        if (aiPath == null || !aiPath.hasPath)
        {
            if (debugJump) Debug.Log("[DOG][JUMP] No AIPath or path not ready");
            return false;
        }
        System.Collections.Generic.List<Vector3> pathPoints = cachedPathPoints;
        bool stale = cachedPathStale;
        if (pathPoints.Count < 2 || stale)
        {
            if (debugJump) Debug.Log($"[DOG][JUMP] Path unusable: count={pathPoints.Count}, stale={stale}");
            return false;
        }
        desiredJumpX = float.NaN;
        desiredJumpDir = 0f;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            // Look ahead up to N segments to find the next rising or steep segment
            int start = i;
            int skipped = 0;
            while (start < pathPoints.Count - 1 && skipped < jumpLookAheadSegments)
            {
                Vector3 sa = pathPoints[start];
                Vector3 sb = pathPoints[start + 1];
                float sdy = sb.y - sa.y;
                float sdx = sb.x - sa.x;
                float sslope = Mathf.Abs(sdx) > 0.001f ? sdy / Mathf.Abs(sdx) : float.MaxValue;
                bool sSteep = sdy > steepSlopeMinHeight && sslope >= steepSlopeRatio;
                if (sdy > 0f || sSteep)
                    break;
                start++;
                skipped++;
            }
            if (start >= pathPoints.Count - 1) break;
            i = start;

            // Accumulate continuous upward/steep climb over multiple small segments
            float climb = 0f;
            float midXSum = 0f;
            int midXCount = 0;
            int j = i;
            bool steepForced = false;
            for (; j < pathPoints.Count - 1; j++)
            {
                Vector3 a = pathPoints[j];
                Vector3 b = pathPoints[j + 1];
                float dy = b.y - a.y;
                float dx = b.x - a.x;
                float slope = Mathf.Abs(dx) > 0.001f ? dy / Mathf.Abs(dx) : float.MaxValue;
                bool isSteep = dy > steepSlopeMinHeight && slope >= steepSlopeRatio;
                if (dy <= 0f && !isSteep) break; // stop accumulating when descending or flat unless steep wall

                if (isSteep) steepForced = true;

                climb += dy;
                midXSum += (a.x + b.x) * 0.5f;
                midXCount++;
                if (climb >= jumpHeightThreshold) break;
            }

            bool hasJumpableClimb = climb >= jumpHeightThreshold;
            bool forceBySteep = steepForced && midXCount > 0 && climb >= steepSlopeMinHeight * 0.5f;

            if ((hasJumpableClimb || forceBySteep) && midXCount > 0)
            {
                float jumpX = midXSum / midXCount;
                float currentX = currentPos.x;
                float xDistanceToJump = Mathf.Abs(currentX - jumpX);
                float runTargetX = jumpX;

                // Find the next segment after the vertical climb that has horizontal movement to guide run direction/landing
                int landingIndex = Mathf.Min(j + 1, pathPoints.Count - 1);
                for (int k = landingIndex; k < pathPoints.Count; k++)
                {
                    float dx = pathPoints[k].x - jumpX;
                    if (Mathf.Abs(dx) > 0.05f)
                    {
                        landingIndex = k;
                        break;
                    }
                    // If path starts descending, stop extending the landing search
                    if (k < pathPoints.Count - 1 && pathPoints[k + 1].y < pathPoints[k].y) break;
                }
                runTargetX = pathPoints[landingIndex].x;
                desiredJumpX = runTargetX;
                // Use horizontal change between climb end and landing to pick jump direction even if we're already near the target
                float climbEndX = pathPoints[Mathf.Min(j, pathPoints.Count - 1)].x;
                desiredJumpDir = Mathf.Sign(runTargetX - climbEndX);
                if (Mathf.Approximately(desiredJumpDir, 0f))
                    desiredJumpDir = Mathf.Sign(runTargetX - currentX);
                desiredJumpHeight = climb;

                float predictedX = currentX;
                var rbLocal = GetComponent<Rigidbody2D>();
                if (rbLocal != null)
                {
                    float vx = rbLocal.linearVelocity.x;
                    float timeToClimb = EstimateTimeToReachHeight(climb, rbLocal);
                    float speedX = Mathf.Abs(vx);
                    float speedLead = jumpLeadTimeBase + speedX * jumpLeadTimeBySpeed;
                    float lead = Mathf.Min(jumpLeadTimeMax + jumpTimingOffset, Mathf.Max(timeToClimb + jumpTimingOffset, speedLead));
                    predictedX = currentX + vx * lead;

                    // If we are too far to cover the horizontal gap with current momentum during ascent, wait
                    float reachable = Mathf.Abs(vx) * Mathf.Max(timeToClimb, lead);
                    if (xDistanceToJump > reachable + jumpLaunchToleranceX * 0.5f)
                    {
                        if (debugJump) Debug.Log($"[DOG][JUMP] Waiting (too far): climb={climb:F2}, jumpX={jumpX:F2}, curX={currentX:F2}, gap={xDistanceToJump:F2}, reachable={reachable:F2}, vx={vx:F2}");
                        continue;
                    }
                }

                bool withinPredictedWindow = Mathf.Abs(predictedX - jumpX) < jumpLaunchToleranceX;
                bool withinStaticWindow = xDistanceToJump < jumpLaunchToleranceX;
                bool withinRunWindow = Mathf.Abs(currentX - runTargetX) < jumpLaunchToleranceX;
                bool forceSteepJump = forceBySteep && xDistanceToJump < jumpLaunchToleranceX * 2f; // relax window for steep walls

                if (withinPredictedWindow || withinStaticWindow || withinRunWindow || forceSteepJump)
                {
                    if (debugJump) Debug.Log($"[DOG][JUMP] Need jump: climb={climb:F2}, jumpX={jumpX:F2}, runX={runTargetX:F2}, curX={currentX:F2}, dist={xDistanceToJump:F2}, predX={predictedX:F2}, dir={desiredJumpDir:F2}");
                    nextJumpTargetPosition = pathPoints[Mathf.Min(landingIndex, pathPoints.Count - 1)];
                    return true;
                }
                else if (debugJump)
                {
                    Debug.Log($"[DOG][JUMP] Waiting: climb={climb:F2}, jumpX={jumpX:F2}, runX={runTargetX:F2}, curX={currentX:F2}, xDist={xDistanceToJump:F2}, predX={predictedX:F2}, tol={jumpLaunchToleranceX:F2}");
                }

                // Continue scanning after the accumulated span
                i = j;
            }
        }
        if (debugJump) Debug.Log("[DOG][JUMP] No jumpable segment found");
        return false;
    }

protected override void Start()
    {
        base.Start();
        spawnPosition = transform.position;
        // Place groundCheck at feet using current collider bounds
        if (groundCheck != null)
        {
            CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
            if (col != null)
            {
                var feetWorld = (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y + 0.02f);
                groundCheck.position = feetWorld;
            }
            else
            {
                groundCheck.position = transform.position + Vector3.down * 0.15f;
            }
        }
    }

    protected override void Update()
    {
        if (!isAlive || isKnockedBack) return;

        UpdateAIDestinationWithOffset();
        CachePathPoints();

        // Only physical collision unlocks jump; ray-based sensing can still mark isGrounded for coyote
        bool groundSense = groundedByCollision || CheckGroundContact();
        if (groundedByCollision)
        {
            jumpLocked = false; // unlock only on real collision
            lastGroundedAt = Time.time;
            airborneRunThroughX = float.NaN; // reset airborne run-through target
            pendingRunThrough = false;
            desiredJumpHeight = 0f;
            lastJumpDir = 0f;
        }
        isGrounded = groundSense || (Time.time - lastGroundedAt <= groundCoyoteTime);

        // When airborne, wait until we reach desired jump height, then set run-through target
        if (!isGrounded && pendingRunThrough && desiredJumpHeight > 0f && Mathf.Abs(lastJumpDir) > 0.01f)
        {
            float climbed = transform.position.y - jumpStartY;
            if (climbed >= desiredJumpHeight - 0.05f)
            {
                float targetX = !float.IsNaN(desiredJumpX) ? desiredJumpX : transform.position.x;
                airborneRunThroughX = targetX + Mathf.Sign(lastJumpDir) * airborneForwardBias;
                pendingRunThrough = false;
            }
        }

        if (debugJump)
        {
            bool byRay = CheckGroundContact();
            var client = GetComponent<SharedTrackingClient>();
            bool canJumpFlag = (client == null) || client.canJump;
            Debug.Log($"[DOG][GROUND] grounded={isGrounded}, byCollision={groundedByCollision}, byRay={byRay}, canJump={canJumpFlag}");
            Debug.Log($"[DOG][UPDATE] state={currentState}, attacking={isAttacking}, alive={isAlive}, knockback={isKnockedBack}");
        }

        bool needJump = ShouldJumpForPath();
        if (debugJump)
            Debug.Log($"[DOG][UPDATE] needJump={needJump}, grounded={isGrounded}, jumpLocked={jumpLocked}");

        // Only try jumping when currently grounded by contact and not locked
        if (needJump && groundedByCollision && !jumpLocked)
            PerformJump();

        if (isAttacking)
        {
            attackAnimationTimer -= Time.deltaTime;
            if (attackAnimationTimer <= 0f)
            {
                isAttacking = false;
                if (animator != null)
                    animator.SetBool("IsAttacking", false);
            }
            // ❌ 여기서 return 하면 안됨
            // return;
        }

        switch (currentState)
        {
            case DogState.Patrol: PatrolBehavior(); break;
            case DogState.Chase: ChaseBehavior(); break;
            case DogState.Attack: AttackBehavior(); break;
            case DogState.Idle: IdleBehavior(); break;
        }

        CheckAndUpdateState();
        wasGrounded = groundedByCollision || CheckGroundContact();
    }

    protected override void Move(Vector2 direction)
    {
        if (rb == null || isKnockedBack) return;

        // Block active input into the contacted wall, but preserve existing velocity (gravity/inertia)
        float desiredX = direction.x * moveSpeed;
        bool blockLeft = isCollidingLeftWall && desiredX < 0;
        bool blockRight = isCollidingRightWall && desiredX > 0;
        if (blockLeft || blockRight)
        {
            desiredX = rb.linearVelocity.x; // keep current X velocity, don't add thrust into wall
        }

        // If airborne with a run-through target, force movement in jump direction until passed
        if (!isGrounded && !float.IsNaN(airborneRunThroughX) && Mathf.Abs(lastJumpDir) > 0.01f)
        {
            float dir = Mathf.Sign(lastJumpDir);
            desiredX = moveSpeed * dir;
            float px = transform.position.x;
            bool reached = dir > 0 ? px >= airborneRunThroughX : px <= airborneRunThroughX;
            if (reached)
            {
                airborneRunThroughX = float.NaN;
            }
        }

        // Apply movement
        rb.linearVelocity = new Vector2(desiredX, rb.linearVelocity.y);

        // Animator handling (copied from base.Move)
        if (animator != null)
        {
            bool isMoving = direction.magnitude > 0.01f;
            animator.SetBool("IsWalking", isMoving);
        }

        if (direction.x != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    private void CheckAndUpdateState()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
            currentState = DogState.Attack;
        else if (distanceToPlayer <= detectionRange)
            currentState = DogState.Chase;
        else
            currentState = DogState.Patrol;
    }

    private void PatrolBehavior()
    {
        patrolTimer += Time.deltaTime;

        if (patrolTimer >= patrolWaitTime)
        {
            patrolDirection *= -1;
            patrolTimer = 0f;
        }

        float distanceFromSpawn = transform.position.x - spawnPosition.x;
        if (Mathf.Abs(distanceFromSpawn) >= patrolRange)
            patrolDirection *= -1;

        Move(new Vector2(patrolDirection, 0));
    }

    private void ChaseBehavior()
    {
        if (playerTransform == null) return;
        Vector2 dir = GetPathChaseDirection();
        Move(dir * chaseSpeedMultiplier);
    }

    private void AttackBehavior()
    {
        Move(Vector2.zero);
        Attack();
    }

    private void IdleBehavior()
    {
        Move(Vector2.zero);
    }

    protected override void OnPlayerDetected()
    {
        currentState = DogState.Chase;
    }

    private Vector2 GetPathChaseDirection()
    {
        // If player is much higher, chase an offset point beside the player to better approach
        if (playerTransform != null)
        {
            float gapY = playerTransform.position.y - transform.position.y;
            if (gapY >= verticalGapOffsetThreshold)
            {
                float offset = playerTransform.position.x >= transform.position.x ? verticalGapXOffset : -verticalGapXOffset;
                float dx = (playerTransform.position.x + offset) - transform.position.x;
                if (Mathf.Abs(dx) < chaseStopDistance)
                    return Vector2.zero;
                float dirX = Mathf.Sign(dx);
                if (Mathf.Abs(dx) < chaseDirectionDeadzone && Mathf.Abs(lastChaseDir) > 0.01f)
                    dirX = lastChaseDir;
                lastChaseDir = dirX;
                return new Vector2(dirX, 0f);
            }
        }

        // Prefer AIPath steering (x-axis only) to respect computed path; fallback to direct chase
        var aiPath = GetComponent<Pathfinding.AIPath>();
        if (aiPath != null && aiPath.hasPath)
        {
            // Look a few points ahead on the remaining path to get a stable horizontal direction
            System.Collections.Generic.List<Vector3> points = cachedPathPoints;
            bool stale = cachedPathStale;
            if (!stale && points.Count >= 2)
            {
                // If player is far above, skip detours and aim at the first rising/steep segment x
                if (playerTransform != null && (playerTransform.position.y - transform.position.y) >= verticalGapOffsetThreshold)
                {
                    for (int idx = 0; idx < points.Count - 1; idx++)
                    {
                        float dy = points[idx + 1].y - points[idx].y;
                        float dx = points[idx + 1].x - points[idx].x;
                        float slope = Mathf.Abs(dx) > 0.001f ? dy / Mathf.Abs(dx) : float.MaxValue;
                        bool steep = dy > 0.05f && slope >= steepSlopeRatio;
                        if (dy > 0f || steep)
                        {
                            float tx = points[idx].x; // x at start of ascent
                            float dirX = Mathf.Sign(tx - transform.position.x);
                            if (Mathf.Abs(tx - transform.position.x) < chaseStopDistance)
                                return Vector2.zero;
                            if (Mathf.Abs(tx - transform.position.x) < chaseDirectionDeadzone && Mathf.Abs(lastChaseDir) > 0.01f)
                                dirX = lastChaseDir;
                            lastChaseDir = dirX;
                            return new Vector2(dirX, 0f);
                        }
                    }
                }

                int lookAhead = Mathf.Min(2, points.Count - 1); // a couple nodes ahead for smoothing
                Vector3 target = points[lookAhead];
                Vector2 toTarget = (Vector2)(target - transform.position);
                float absX = Mathf.Abs(toTarget.x);
                if (absX < chaseStopDistance)
                {
                    return Vector2.zero; // close enough, avoid oscillation
                }
                if (absX > 0.001f)
                {
                    float dirX = Mathf.Sign(toTarget.x); // use full intent toward target x even on diagonal/vertical segments
                    // Deadzone: if we're very close horizontally, keep prior direction to avoid jitter
                    if (Mathf.Abs(toTarget.x) < chaseDirectionDeadzone && Mathf.Abs(lastChaseDir) > 0.01f)
                        dirX = lastChaseDir;
                    lastChaseDir = dirX;
                    return new Vector2(dirX, 0f);
                }
            }

            // Fallback: steeringTarget or desired velocity
            Vector3 steering = aiPath.steeringTarget;
            Vector2 steerDelta = (Vector2)(steering - transform.position);
            float desiredX = Mathf.Abs(steerDelta.x) > 0.001f ? steerDelta.x : aiPath.desiredVelocity.x;
            if (Mathf.Abs(desiredX) < chaseStopDistance)
            {
                return Vector2.zero;
            }
            if (Mathf.Abs(desiredX) > 0.01f)
            {
                float dirX = Mathf.Sign(desiredX);
                if (Mathf.Abs(desiredX) < chaseDirectionDeadzone && Mathf.Abs(lastChaseDir) > 0.01f)
                    dirX = lastChaseDir;
                lastChaseDir = dirX;
                return new Vector2(dirX, 0f);
            }
        }

        if (playerTransform != null)
        {
            Vector2 dir = (playerTransform.position - transform.position).normalized;
            if (Mathf.Abs(dir.x) < chaseStopDistance)
                return Vector2.zero;
            float dirX = dir.x;
            if (Mathf.Abs(dirX) < chaseDirectionDeadzone && Mathf.Abs(lastChaseDir) > 0.01f)
                dirX = lastChaseDir;
            lastChaseDir = Mathf.Sign(dirX);
            return new Vector2(Mathf.Sign(dirX), 0f);
        }

        return Vector2.zero;
    }

    private float EstimateTimeToReachHeight(float height, Rigidbody2D rb)
    {
        if (rb == null) return jumpLeadTimeBase;
        float g = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(rb.gravityScale, 0.001f);
        if (g < 0.001f) return jumpLeadTimeBase;

        float mass = Mathf.Max(rb.mass, 0.01f);
        // AddForce default mode is Force; approximate impulse over one fixed step
        float vy0 = (jumpForce / mass) * Time.fixedDeltaTime;

        // Solve 0.5*g*t^2 - vy0*t + height = 0
        float disc = vy0 * vy0 - 2f * g * height;
        if (disc >= 0f)
        {
            float t = (vy0 - Mathf.Sqrt(disc)) / g;
            if (t > 0f) return t;
        }

        // Fallback: time to apex
        return vy0 / g;
    }

    private void CachePathPoints()
    {
        var aiPath = GetComponent<Pathfinding.AIPath>();
        cachedPathPoints.Clear();
        cachedPathStale = true;
        if (aiPath == null || !aiPath.hasPath) return;
        aiPath.GetRemainingPath(cachedPathPoints, out bool stale);
        cachedPathStale = stale;
    }

    private void UpdateAIDestinationWithOffset()
    {
        var aiPath = GetComponent<Pathfinding.AIPath>();
        if (aiPath == null || playerTransform == null) return;

        float gapY = playerTransform.position.y - transform.position.y;
        Vector3 target = playerTransform.position;
        if (gapY >= verticalGapOffsetThreshold)
        {
            float offset = playerTransform.position.x >= transform.position.x ? verticalGapXOffset : -verticalGapXOffset;
            target.x += offset;
        }

        aiPath.destination = target;
    }

    protected override void PerformAttack()
    {
        if (playerTransform == null || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            isAttacking = true;
            attackAnimationTimer = attackAnimationDuration;

            if (animator != null)
                animator.SetBool("IsAttacking", true);

            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
                Debug.Log($"Dog attacks player for {attackDamage} damage!");
            }
        }
    }

    protected override void ResetKnockback()
    {
        base.ResetKnockback();
        currentState = DogState.Patrol;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGroundCollision(collision);
        CheckWallCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckGroundCollision(collision);
        CheckWallCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        groundedByCollision = false;
        isCollidingLeftWall = false;
        isCollidingRightWall = false;
    }

    private void CheckGroundCollision(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.4f)
            {
                groundedByCollision = true;
                return;
            }
        }
    }

    private void CheckWallCollision(Collision2D collision)
    {
        bool foundLeftWall = false;
        bool foundRightWall = false;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Ignore wall flags if the surface is climbable/ground-like (has upward normal)
            if (contact.normal.y > 0.4f) continue;

            if (contact.normal.x > 0.7f)
            {
                foundLeftWall = true;
            }
            else if (contact.normal.x < -0.7f)
            {
                foundRightWall = true;
            }
        }

        isCollidingLeftWall = foundLeftWall;
        isCollidingRightWall = foundRightWall;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Vector3 spawnPos = Application.isPlaying ? (Vector3)spawnPosition : transform.position;
        Gizmos.DrawLine(spawnPos + Vector3.left * patrolRange, spawnPos + Vector3.right * patrolRange);

        if (debugDrawPath)
        {
            var aiPath = GetComponent<Pathfinding.AIPath>();
            if (aiPath != null)
            {
                System.Collections.Generic.List<Vector3> pts = new System.Collections.Generic.List<Vector3>();
                aiPath.GetRemainingPath(pts, out bool stale);
                if (pts.Count > 1)
                {
                    Gizmos.color = pathGizmoColor;
                    for (int i = 0; i < pts.Count; i++)
                    {
                        Gizmos.DrawSphere(pts[i], pathPointRadius);
                        if (i < pts.Count - 1)
                        {
                            Gizmos.DrawLine(pts[i], pts[i + 1]);
                        }
                    }
                }
            }
        }
    }
}
