using System.Collections;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Rigidbody2D))]
public class IntelligentDogMovement : Enemy
{
    [System.Serializable]
    public class Config
    {
        public float moveSpeed;
        public float maxHealth;
        public float attackDamage;
        public float attackSpeed;
        public int maxJumps;
    }
    [Header("Dog Specific Settings")]
    [Tooltip("Chase speed multiplier")]
    [SerializeField] private float chaseSpeedMultiplier = 1.2f;

    [Tooltip("Attack range")]
    [SerializeField] private float attackRange = 1.5f;

    [Tooltip("Patrol radius")]
    [SerializeField] private float patrolRange = 5f;

    [Tooltip("Attack damage (1 tile = 1)")]
    [SerializeField] private float attackDamage = 1f;

    [Tooltip("Attack animation duration")]
    [SerializeField] private float attackAnimationDuration = 1.5f; // 3x slower

    [Tooltip("Apply damage near the end of the attack animation")]
    [SerializeField] private float attackHitWindow = 0.3f;

    [Tooltip("Early exit: end attack animation this many seconds before it fully completes")]
    [SerializeField] private float attackEarlyExitTime = 0.2f; // 0.2초 일찍 종료

    [Tooltip("Delay between attack starts (seconds)")]
    [SerializeField] private float attackCooldown = 2.0f;

    [Tooltip("Max horizontal speed")]
    [SerializeField] private float maxSpeed = 5f;

    [Tooltip("Delay before freezing after death animation finishes")]
    [SerializeField] private float deathFreezeDelay = 1.0f;

    [Header("Chase Target Padding")]
    [Tooltip("플레이어 추적 시 목표 Y에 더해줄 패딩. 큰 벽을 만나면 위쪽을 겨냥하도록 높이를 올립니다.")]
    [SerializeField] private float chaseTargetYPadding = 1.0f;

    [Header("Jump Settings")]
    [Tooltip("Jump when vertical gap above this height")]
    [SerializeField] private float jumpHeightThreshold = 2f;

    [Tooltip("Jump when horizontal gap below this distance")]
    [SerializeField] private float jumpDistanceThreshold = 2f;

    [Tooltip("Jump cooldown")]
    [SerializeField] private float jumpCooldown = 0.5f;

    [Header("Ground Check")]
    [Tooltip("Layer mask used for ground checks")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Ground check distance")]
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Tooltip("Ground check offset from character pivot")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

    [Header("Spike Detection")]
    [Tooltip("Layer mask for spike hazards")]
    [SerializeField] private LayerMask spikeLayer;

    [Tooltip("Distance to check for spikes ahead")]
    [SerializeField] private float spikeDetectionDistance = 3f;

    [Tooltip("Spike height (always 1 block)")]
    [SerializeField] private float spikeHeight = 1f;

    [Tooltip("Enable spike avoidance logic (disabled unless explicitly turned on)")]
    [SerializeField] private bool spikeAvoidanceEnabled = false;

    [Header("Pit Handling")]
    [Tooltip("Lock other jumps for a short time after a pit escape attempt")]
    [SerializeField] private float pitJumpLockDuration = 0.35f;

    [Tooltip("Block tall wall-jump calculations while stuck in a pit")]
    [SerializeField] private bool lockWallJumpWhileInPit = true;

    [Header("Path Following")]
    [Tooltip("Distance to consider waypoint reached")]
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Tooltip("How many waypoints to look ahead for steering")]
    [SerializeField] private int lookAheadWaypoints = 2;

    [Header("Fallback Behavior")]
    [Tooltip("Use direct movement if path is unavailable")]
    [SerializeField] private bool useDirectMovementFallback = true;

    [Tooltip("Obstacle detection distance for fallback movement")]
    [SerializeField] private float obstacleDetectionDistance = 1.5f;

    [Header("Backward Obstacle Bypass")]
    [SerializeField] private float backOffset = 0.12f;
    [SerializeField] private float verticalCheckMaxDistance = 1.5f;
    [SerializeField] private float forwardBypassDuration = 0.35f;
    [SerializeField] private float forwardBypassSpeedMultiplier = 1.1f;

    [Header("Airborne Stuck Backoff")]
    [SerializeField] private float backOffDuration = 0.2f;
    [SerializeField] private float backOffSpeedMultiplier = 0.8f;
    [SerializeField] private float stuckPosThreshold = 0.005f;
    [SerializeField] private float stuckCheckInterval = 0.1f;

    [Header("Speed Boost on Stuck")]
    [SerializeField] private float boostedSpeed = 6f;
    [SerializeField] private float speedBoostCooldown = 0.3f;
    [SerializeField] private float speedBoostDeltaThreshold = 0.3f;

    [Header("Grounded Stuck Nudge")]
    [SerializeField] private float forwardNudgeImpulse = 1f;
    [SerializeField] private float forwardNudgeCheckInterval = 0.08f;
    [SerializeField] private float forwardNudgeDeltaThreshold = 0.02f;
    [SerializeField] private float forwardNudgeJumpCooldown = 0.1f;
    [SerializeField] private float apexForwardImpulse = 2f;
    [SerializeField] private float forwardHopImpulse = 1.5f;
    [SerializeField] private int maxForwardHops = 3;
    [SerializeField] private float forwardAirRayDistance = 6f;
    [SerializeField] private float forwardShoveImpulse = 2f;
    [SerializeField] private float forwardJumpRayDistance = 6f;
    [SerializeField] private float forwardJumpImpulse = 2f;
    [Header("Airborne Forward Impulse")]
    [SerializeField] private float airborneForwardCheckInterval = 0.05f;
    [SerializeField] private float airborneForwardRayDistance = 6f;
    [SerializeField] private float airborneForwardImpulse = 2.5f;
    [SerializeField] private float airborneForwardCooldown = 0.3f;
    [Header("Stuck Backstep")]
    [SerializeField] private float backStepDistance = 2f;
    [SerializeField] private float backStepTimeout = 0.35f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Slope Handling")]
    [SerializeField] private float slopeNormalMin = 0.4f;
    [SerializeField] private float flatNormalThreshold = 0.95f;
    [SerializeField] private float slopeSpeedMultiplier = 1.5f;
    [SerializeField] private float slopeSpeedPadding = 3f;
    [SerializeField] private float slopeMinSpeed = 5.5f;
    [SerializeField] private float uphillSpeedBonus = 4f;
    [Header("Death Toast Frames")]
    [SerializeField] private DeathToastFrame[] deathToastFrames = new DeathToastFrame[]
    {
        new DeathToastFrame { time = 0f, offset = new Vector3(0f, 0.8f, 0f), ghostAlpha = 0.25f },
        new DeathToastFrame { time = 0.125f, offset = new Vector3(0.05f, 0.9f, 0f), ghostAlpha = 0.3f },
        new DeathToastFrame { time = 0.25f, offset = new Vector3(0.08f, 0.75f, 0f), ghostAlpha = 0.25f },
        new DeathToastFrame { time = 0.375f, offset = new Vector3(0f, 0.7f, 0f), ghostAlpha = 0.2f }
    };

    [Header("Debug")]
    [SerializeField] private bool debugDog = true;

    [Header("Health UI")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Vector2 healthUiOffset = new Vector2(0f, 1.0f);
    [SerializeField] private Vector3 healthUiScale = new Vector3(0.6f, 0.6f, 1f);
    [SerializeField] private int healthUiSortingOrder = 5000;
    [SerializeField] private Vector3 healthTextLocalPosition = new Vector3(0f, 0.1f, -2f);
    [SerializeField] private float healthTextPadding = 1f;
    [SerializeField] private int healthTextSortingOffset = 50;
    [SerializeField] private Color healthTextColor = Color.white;
    [SerializeField] private string healthUiSortingLayer = "UI";
    [SerializeField] private Font healthFont;
    [SerializeField] private float healthUiNormalAlpha = 0.4f;
    [SerializeField] private float healthUiHitAlpha = 1f;
    [SerializeField] private float healthUiHitDuration = 0.8f;

    [Header("Damage Text")]
    [SerializeField] private Vector3 damageTextOffset = new Vector3(0.6f, 0.4f, -1f);
    [SerializeField] private Color damageTextColor = Color.yellow;
    [SerializeField] private float damageTextDuration = 0.6f;
    [SerializeField] private float damageTextAmplitude = 0.4f;
    [SerializeField] private int damageTextFontSize = 80;
    [SerializeField] private float damageTextCharacterSize = 0.05f;
    [SerializeField] private Font damageTextFont;

    // Components
    private AIPath aiPath;
    private SpriteRenderer heartRenderer;
    private TextMesh healthText;
    private GameObject heartGO;
    private GameObject textGO;
    private bool healthUiInitialized = false;
    private float healthUiShowUntil = -1f;
    private bool hideHealthUiAfterHit = false;
    private bool healthUiHidden = false;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 1f); // ?뚰뙆 ?щ챸??????쒖닔 鍮④컯 ?댄듃
    [SerializeField] private float damageFlashDuration = 0.1f;
    [SerializeField] private int damageFlashCount = 2;
    private Coroutine damageFlashRoutine;
    private Color dogBaseColor = Color.white;
    private Coroutine deathToastRoutine;
    private ToastIndicator cachedToastIndicator;
    // State
    private float lastJumpTime;
    private bool isGrounded;
    private float targetJumpHeight = 0f;
    private bool isAttacking = false;
    private float attackAnimationTimer = 0f;
    private bool attackDamageApplied = false;
    private bool attackHitAttempted = false; // 피해 적용 시도 여부 (범위 무관)
    private Vector2 spawnPosition;
    private float patrolTimer = 0f;
    // 臾댁쟻 ?占쎄컙 愿由ъ슜
    private float invincibilityTime = 0.5f;
    private float lastDamageTime = -10f;

    private int patrolDirection = 1;
    private Vector2 lastGroundNormal = Vector2.up;
    private bool onSlope = false;
    private float lastFacingDir = 1f;
    private float forwardRunUntil = -1f;
    private float forwardRunDir = 0f;
    private float nextBypassTime = 0f;
    private float backOffUntil = -1f;
    private float backOffDir = 0f;
    private float lastPosX = 0f;
    private float lastPosCheckTime = -1f;
    private float lastForwardCheckPosX = 0f;
    private float lastForwardCheckTime = -1f;
    private float lastForwardNudgeTime = -1f;
    private float lastForwardNudgePosX = 0f;
    private bool awaitingJumpAfterNudge = false;
    private bool lockForwardUntilGrounded = false;
    private bool pendingApexImpulse = false;
    private float apexImpulseDir = 0f;
    private float lastVerticalVelY = 0f;
    private float prevVerticalVelY = 0f;
    private float apexPeakY = float.MinValue;
    private int stuckForwardHopCount = 0;
    private float lastHopPosX = 0f;
    private bool pendingBackstepJump = false;
    private float backstepTargetX = 0f;
    private float backstepExpireTime = 0f;
    private float backstepDir = 0f;
    private float backstepForwardDir = 0f;
    private bool forwardShoveTried = false;
    private float allowForwardUntil = -1f;
    private float lastAirborneForwardCheckTime = -1f;
    private float lastAirborneForwardImpulseTime = -1f;
    private float jumpStartX = 0f;
    private float jumpStartTime = -1f;
    private bool monitoringJumpEscape = false;
    private bool speedBoosted = false;
    private float speedBoostStartTime = -1f;
    private float speedBoostStartX = 0f;
    private float lastSpeedBoostTime = -1f;
    private float pitJumpExecutedTime = -10f;
    private float fallStartY = 0f;
    private bool trackingFall = false;
    private bool lockNonPitJumpInPit = false;
    private float preFallGroundY = 0f;
    private float pitEscapeTargetY = 0f;
    private float lastImpulseUpTime = -10f;

    // 구덩이 감지용
    private float lastGroundedYPosition = 0f;
    private bool wasGroundedLastFrame = false;
    private Vector2 lastGroundedNormal = Vector2.up; // 마지막 착지한 바닥의 노멀
    private float detectedPitDepth = 0f;
    private bool pitJumpPending = false;
    private bool inPit = false; // 구덩이에 빠져있는지
    // private bool onSpike = false; // Spike 위에 있는지 - SPIKE 비활성화

    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private bool hasJumpBoolParam = false;
    private bool hasJumpTriggerParam = false;

    private enum DogState
    {
        Patrol,
        Chase,
        Attack,
        Idle
    }
    private DogState currentState = DogState.Patrol;

    protected override void Awake()
    {
        base.Awake();
        aiPath = GetComponent<AIPath>();
        cachedToastIndicator = GetComponentInChildren<ToastIndicator>(true);

        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == IsJumpingHash)
                {
                    hasJumpBoolParam = true;
                }
                else if (p.type == AnimatorControllerParameterType.Trigger && p.nameHash == JumpTriggerHash)
                {
                    hasJumpTriggerParam = true;
                }
            }
        }

        if (spriteRenderer != null)
        {
            dogBaseColor = spriteRenderer.color;
        }

        // Disable AIPath auto movement; we drive it manually
        if (aiPath != null)
        {
            aiPath.canMove = false;
        }

        UpdateHealthUiAlphaState();
    }

    protected override void Start()
    {
        base.Start();
        lastJumpTime = -jumpCooldown; // allow immediate jump on start
        spawnPosition = transform.position;
        lastPosX = transform.position.x;
        lastPosCheckTime = Time.time;
        lastAirborneForwardCheckTime = -1f;
        lastAirborneForwardImpulseTime = -airborneForwardCooldown; // 시작 시 즉시 사용 가능
        lastSpeedBoostTime = -speedBoostCooldown; // 시작 시 즉시 사용 가능
        speedBoosted = false;

        CreateHealthUI();
        UpdateHealthUI();

        Debug.Log($"[Dog] Start - maxHealth: {maxHealth}, currentHealth: {currentHealth}, heartSprite: {heartSprite != null}");
    }

    protected override void Update()
    {
        UpdateHealthUiAlphaState();

        // Track vertical velocity for apex detection
        if (rb != null)
        {
            prevVerticalVelY = lastVerticalVelY;
            lastVerticalVelY = rb.linearVelocity.y;
        }

        if (!isAlive || isKnockedBack) return;

        // Attack animation handling
        if (isAttacking)
        {
            attackAnimationTimer -= Time.deltaTime;

            // Deal damage near the end of the animation if still in range
            if (!attackHitAttempted && attackAnimationTimer <= attackHitWindow)
            {
                attackHitAttempted = true; // ★ 시도 여부만 기록 (성공/실패 무관)
                TryApplyAttackDamage();
            }

            // ★ 피해 적용 시도 후에만 조기 종료 가능 (데미지 스킵 방지)
            // 플레이어가 범위 밖이어도 시도만 했으면 조기 종료 가능
            if (attackHitAttempted && attackAnimationTimer <= attackEarlyExitTime)
            {
                isAttacking = false;
                if (animator != null)
                {
                    animator.SetBool("IsAttacking", false);
                    // ★ IsWalking은 설정하지 않음 - 다음 프레임에 MoveHorizontally()가 자동으로 설정
                }
            }
            return; // during attack, skip other updates
        }
        else if (animator != null)
        {
            // Ensure attack pose is cleared while waiting (including cooldown)
            animator.SetBool("IsAttacking", false);
        }

        CheckAndUpdateState();

        TryApplyApexImpulse();
    }

    private void FixedUpdate()
    {
        if (!isAlive || isKnockedBack) return;

        CheckGroundStatus();

        // 점프 후 x변화 모니터링 - 탈출 성공 시 빠른 착지
        MonitorJumpEscape();

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

        // ★ 매 프레임 속도 체크하여 IsWalking 업데이트 (공격 중이 아닐 때만)
        UpdateWalkingAnimation();
    }

    private void UpdateWalkingAnimation()
    {
        if (animator == null || isAttacking) return;

        // 실제 속도 기준으로 Idle/Walk 결정
        bool isMoving = rb != null && (Mathf.Abs(rb.linearVelocity.x) > 0.05f || Mathf.Abs(rb.linearVelocity.y) > 0.05f);
        animator.SetBool("IsWalking", isMoving);
    }

    private void CheckAndUpdateState()
    {
        if (playerTransform == null) return;

        // ★ 공격 중일 때는 상태 변경 금지 (공격 애니메이션이 끝날 때까지 대기)
        if (isAttacking)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 공격 쿨타임 중이거나 타격 불가면 Attack 상태로 진입하지 않음
        bool attackReady = (Time.time - lastAttackTime >= attackCooldown) && CanDealDamageNow();

        if (attackReady && distanceToPlayer <= attackRange)
        {
            // ★ 이미 Attack 상태면 중복 설정 방지
            if (currentState != DogState.Attack)
            {
                currentState = DogState.Attack;
            }
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = DogState.Chase;
        }
        else
        {
            // ?占쎈젅?占쎌뼱 媛먲옙? 踰붿쐞占?踰쀬뼱?占쎈㈃ 利됱떆 Idle
            currentState = DogState.Idle;
        }
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
        {
            patrolDirection *= -1;
        }

        MoveInDirection(new Vector2(patrolDirection, 0));
    }

    private void ChaseBehavior()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 targetPos = playerPos;

        // 벽/높이 우회용으로 필요 시 Y 패딩을 올린다.
        float extraY = 0f;
        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)playerPos - origin;
        bool blockedToPlayer = false;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, toPlayer.magnitude, groundLayer);
            blockedToPlayer = hit.collider != null && hit.collider.transform != playerTransform;
        }

        // 플레이어가 위쪽에 있거나, 경로가 위를 가리키거나, 직선 경로가 막혔을 때 패딩 적용
        bool playerAbove = (playerPos.y - transform.position.y) > 0.5f;
        bool pathAbove = aiPath != null && aiPath.hasPath && (aiPath.steeringTarget.y - transform.position.y) > 0.5f;
        if (blockedToPlayer || pathAbove || playerAbove)
        {
            extraY = chaseTargetYPadding;
        }

        if (extraY > 0f)
        {
            targetPos += new Vector3(0f, extraY, 0f);
        }

        // Set AIPath destination
        if (aiPath != null)
        {
            bool destinationChanged = (aiPath.destination - targetPos).sqrMagnitude > 0.0001f;
            aiPath.destination = targetPos;
            if (destinationChanged && extraY > 0f)
            {
                aiPath.SearchPath(); // 즉시 재계산하여 패딩된 목적지가 반영되도록
            }
        }

        // If already in attack range, switch to Attack state (but check cooldown first)
        float distToPlayer = Vector2.Distance(transform.position, targetPos);
        if (distToPlayer <= attackRange)
        {
            // ★ AttackBehavior()를 직접 호출하지 말고, 쿨타임 체크 후 상태만 변경
            bool attackReady = (Time.time - lastAttackTime >= attackCooldown) && CanDealDamageNow();
            if (attackReady)
            {
                currentState = DogState.Attack;
            }
            // 쿨타임 중이면 Chase 상태 유지 (이동은 계속)
            return;
        }

        // If AIPath has a valid path, follow it
        if (aiPath != null && aiPath.hasPath)
        {
            FollowPath();
        }
        else if (useDirectMovementFallback)
        {
            // Direct movement fallback
            Vector2 directionToPlayer = (targetPos - transform.position).normalized;
            MoveInDirection(directionToPlayer * chaseSpeedMultiplier);
        }
    }

    private void AttackBehavior()
    {
        // 공격 중에는 Update()가 애니/피해 처리하므로 여기서 추가 동작 안 함
        if (isAttacking)
        {
            return;
        }

        // 공격 가능 여부 먼저 판정 (거리/라인오브사이트 + 쿨타임)
        bool canDamageNow = (Time.time - lastAttackTime >= attackCooldown) && CanDealDamageNow();

        // 공격 쿨타임 동안은 Chase 상태로 전환
        bool coolingDown = (Time.time - lastAttackTime < attackCooldown);
        if (coolingDown)
        {
            Debug.Log($"[DOG ATTACK] 쿨타임 중이므로 Attack 상태 벗어남 - 남은 시간: {attackCooldown - (Time.time - lastAttackTime):F2}초");
            // ★ 쿨타임 중에는 Attack 상태에서 빠져나감
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= detectionRange)
            {
                currentState = DogState.Chase;
            }
            else
            {
                currentState = DogState.Idle;
            }

            if (animator != null)
            {
                animator.SetBool("IsAttacking", false);
            }
            // ★ IsWalking은 FixedUpdate의 UpdateWalkingAnimation()에서 자동 설정
            return;
        }

        if (!canDamageNow)
        {
            // 공격 조건이 안 되면 이동/대기 유지
            if (animator != null)
            {
                animator.SetBool("IsAttacking", false);
            }
            // ★ IsWalking은 FixedUpdate의 UpdateWalkingAnimation()에서 자동 설정
            return;
        }

        // 공격 가능 거리 안에 있고, 쿨타임이 끝났을 때만 공격 시도
        PerformAttack();
    }

    private void IdleBehavior()
    {
        // Stop all movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Set idle animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }
    }

    private void CheckGroundStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;

        RaycastHit2D hitRay = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitCircle = Physics2D.CircleCast(checkPosition, 0.1f, Vector2.down, groundCheckDistance, groundLayer);

        bool hasRay = hitRay.collider != null;
        bool hasCircle = hitCircle.collider != null;
        bool wasGroundedBefore = isGrounded;
        isGrounded = hasRay || hasCircle;

        if (animator != null && hasJumpBoolParam)
        {
            animator.SetBool(IsJumpingHash, !isGrounded);
        }

        RaycastHit2D chosen = hasRay ? hitRay : hitCircle;
        if (chosen.collider != null)
        {
            lastGroundNormal = chosen.normal;
            // Unity 2D y-up normal: 1 = flat. Anything below flatNormalThreshold is a slope.
            onSlope = lastGroundNormal.y > slopeNormalMin && lastGroundNormal.y < flatNormalThreshold;
        }
        else
        {
            lastGroundNormal = Vector2.up;
            onSlope = false;
        }

        // 공중으로 전환되며 y가 떨어지기 시작할 때 낙하 시작 y 기록
        if (!isGrounded && wasGroundedBefore && rb != null && rb.linearVelocity.y < -0.01f)
        {
            fallStartY = transform.position.y;
            trackingFall = true;
            preFallGroundY = lastGroundedYPosition;
        }

        // SPIKE 비활성화
        // Spike 위에 있는지 확인
        // if (isGrounded)
        // {
        //     Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        //     Collider2D spikeCollider = Physics2D.OverlapCircle(checkPos, 0.3f, spikeLayer);
        //     onSpike = spikeCollider != null;
        //
        //     if (onSpike && debugDog && Time.frameCount % 60 == 0)
        //     {
        //         Debug.Log($"[DOG][ON-SPIKE] Standing on spike! Escape mode active.");
        //     }
        // }
        // else
        // {
        //     onSpike = false;
        // }

        // 구덩이 감지: 평지에서 평지로 떨어진 경우만
        if (isGrounded && !wasGroundedBefore)
        {
            // 방금 착지한 바닥이 평지인지 확인
            bool currentGroundIsFlat = Mathf.Abs(lastGroundNormal.y - 1f) < 0.1f && !onSlope;
            // 이전 바닥도 평지였는지 확인
            bool previousGroundWasFlat = Mathf.Abs(lastGroundedNormal.y - 1f) < 0.1f;

            Vector2 forwardDir = new Vector2(Mathf.Sign(Mathf.Abs(lastFacingDir) > 0.01f ? lastFacingDir : 1f), 0f);
            bool frontEmptyLanding;
            bool backEmptyLanding;
            bool bothEmptyLanding = BothPitProbesEmpty(checkPosition, forwardDir, out frontEmptyLanding, out backEmptyLanding);

            if (currentGroundIsFlat && previousGroundWasFlat && wasGroundedLastFrame && bothEmptyLanding)
            {
                float yDrop = lastGroundedYPosition - transform.position.y;
                if (yDrop > 0.5f) // 0.5블록 이상 떨어졌다면 구덩이
                {
                    detectedPitDepth = yDrop;
                    pitJumpPending = true;
                    inPit = true; // 구덩이에 빠짐!
                    pitEscapeTargetY = lastGroundedYPosition;
                    lockNonPitJumpInPit = true;
                    if (debugDog)
                    {
                        Debug.Log($"[DOG][PIT-DETECT] Pit detected on landing! Depth={yDrop:F2}, lastY={lastGroundedYPosition:F2}, nowY={transform.position.y:F2}, inPit=true, frontEmpty={frontEmptyLanding}, backEmpty={backEmptyLanding}");
                    }
                }
            }

            // 착지 지점이 평지고 바로 앞이 벽이면, 방금 낙하한 높이 기반으로 얕은 구덩이 탈출용 점프 예약
            bool landedOnFlat = Mathf.Abs(lastGroundNormal.y - 1f) < 0.1f && !onSlope && bothEmptyLanding;
            if (landedOnFlat && trackingFall)
            {
                float fallDepth = Mathf.Max(0f, fallStartY - transform.position.y);
                float wallCheckDistance = 1.5f;

                // 두 위치(발바닥, 몸 중간)에서 모두 검사해 낮은 벽/높은 벽을 놓치지 않도록 함
                Vector2 midOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
                RaycastHit2D wallAhead = Physics2D.Raycast(checkPosition, forwardDir, wallCheckDistance, groundLayer);
                if (wallAhead.collider == null)
                {
                    wallAhead = Physics2D.Raycast(midOrigin, forwardDir, wallCheckDistance, groundLayer);
                }
                if (wallAhead.collider == null)
                {
                    wallAhead = Physics2D.CircleCast(checkPosition, 0.25f, forwardDir, wallCheckDistance, groundLayer);
                }

                // 구덩이 내부인지 확인: 앞은 막혀 있고, 앞 바닥이 비어 있으면 확정
                bool frontEmpty = false;
                bool backEmpty = false;
                bool pitConfirmedByProbe = false;
                if (fallDepth > 0.1f)
                {
                    pitConfirmedByProbe = BothPitProbesEmpty(checkPosition, forwardDir, out frontEmpty, out backEmpty);
                }

                // 얕은 단차에서는 점프하지 않도록 깊이 제한을 둔다.
                float depthFromPreFall = Mathf.Max(0f, preFallGroundY - transform.position.y);
                bool deepEnoughToTreatPit = depthFromPreFall > 0.25f;

                if ((wallAhead.collider != null || pitConfirmedByProbe) && deepEnoughToTreatPit)
                {
                    detectedPitDepth = depthFromPreFall;
                    pitJumpPending = true;
                    inPit = true;
                    pitEscapeTargetY = preFallGroundY;
                    lockNonPitJumpInPit = true;
                    if (debugDog)
                    {
                        string wallName = wallAhead.collider != null ? wallAhead.collider.name : "none";
                        Debug.Log($"[DOG][PIT-DETECT-FALL] pit flagged. fallDepth={fallDepth:F2}, depthFromPreFall={depthFromPreFall:F2}, wall={wallName}, frontEmpty={frontEmpty}, backEmpty={backEmpty}");
                    }
                }
                else if (debugDog)
                {
                    Debug.Log($"[DOG][PIT-CHECK-FAIL] landed flat after fall. fallDepth={fallDepth:F2}, depthFromPreFall={depthFromPreFall:F2}, wallHit={(wallAhead.collider != null)}, frontEmpty={frontEmpty}, backEmpty={backEmpty}, wallDist={wallCheckDistance}");
                }

                trackingFall = false;
            }
        }

        // 구덩이 탈출 확인
        if (inPit && isGrounded)
        {
            float now = Time.time;
            float heightDiffToTarget = pitEscapeTargetY - transform.position.y;

            // 원래 높이까지 돌아왔으면 탈출
            if (heightDiffToTarget <= 0.2f)
            {
                inPit = false;
                lockNonPitJumpInPit = false;
                lastGroundedYPosition = transform.position.y;
                lastGroundedNormal = lastGroundNormal;
                if (debugDog)
                {
                    Debug.Log($"[DOG][PIT-ESCAPE] Escaped from pit! currentY={transform.position.y:F2}, targetY={pitEscapeTargetY:F2}");
                }
            }
            // 오랫동안 못 올라가면 높이 차이만큼 다시 한 번 작은 점프로 시도
            else if ((now - pitJumpExecutedTime) > 0.25f)
            {
                float retryDepth = Mathf.Max(0.15f, heightDiffToTarget); // 최소 0.15f만큼만 점프
                detectedPitDepth = retryDepth;
                pitJumpPending = true;
                pitJumpExecutedTime = now;
                if (debugDog)
                {
                    Debug.Log($"[DOG][PIT-RETRY] Still in pit. retryDepth={retryDepth:F2}, currentY={transform.position.y:F2}, targetY={pitEscapeTargetY:F2}");
                }
            }
            else if (debugDog && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[DOG][IN-PIT] Still in pit. currentY={transform.position.y:F2}, targetY>={pitEscapeTargetY - 0.2f:F2}");
            }
        }

        // 지면에 있을 때만 y 위치 및 노멀 저장 (평지에서만!)
        if (isGrounded)
        {
            bool isFlat = Mathf.Abs(lastGroundNormal.y - 1f) < 0.1f && !onSlope;

            // 평지면 현재 높이를 항상 기록해서 내리막에서도 기준이 내려가도록 함
            if (isFlat && !inPit)
            {
                lastGroundedYPosition = transform.position.y;
                lastGroundedNormal = lastGroundNormal;
            }

            wasGroundedLastFrame = true;
            trackingFall = false;
            fallStartY = transform.position.y;
        }
        else
        {
            wasGroundedLastFrame = false;
        }
    }

    private void FollowPath()
    {
        var path = new System.Collections.Generic.List<Vector3>();
        aiPath.GetRemainingPath(path, out bool stale);

        if (path.Count < 2) return;

        Vector3 nextWaypoint = GetNextWaypoint(path, transform.position);
        if (nextWaypoint == Vector3.zero) return;

        Vector2 direction = (nextWaypoint - transform.position).normalized;

        // ?占쎌そ?占쎌꽌 ?占쎈젅?占쎌뼱源뚳옙???寃쎈줈媛 留됲삍?占쎌떆 ?占쎌쭊 ?占쏀쉶
        float distToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool nearAttack = distToPlayer <= attackRange + 0.5f;
        if (!nearAttack && !pendingApexImpulse && Time.time >= nextBypassTime && (rb == null || rb.linearVelocity.y <= 0.05f))
        {
            TryBackwardObstacleBypass(direction);
            if (forwardRunUntil > Time.time && Mathf.Abs(forwardRunDir) > 0.01f)
            {
                MoveHorizontally(forwardRunDir);
                return;
            }
        }

        // Airborne downward movement: keep a bit of horizontal motion
        if (!isGrounded && direction.y < -0.1f)
        {
            if (Mathf.Abs(direction.x) > 0.01f)
                MoveHorizontally(direction.x);
            return;
        }

        // Jump if needed
        if (ShouldJump(transform.position, nextWaypoint, path) && CanJump())
            Jump();

        // Horizontal movement
        if (Mathf.Abs(direction.x) > 0.01f)
            MoveHorizontally(direction.x);
    }

    private Vector3 GetNextWaypoint(System.Collections.Generic.List<Vector3> path, Vector3 currentPosition)
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

    private bool ShouldJump(Vector3 currentPosition, Vector3 nextWaypoint, System.Collections.Generic.List<Vector3> path)
    {
        if (!isGrounded) return false;

        // 諛붾줈 ??占?Ray) 2釉붾윮 ?占쎈궡???占쎈젅?占쎌뼱媛 ?占쎌쓣???占쏀봽 湲덌옙?
        if (playerTransform != null)
        {
            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)playerTransform.position - origin;
            float distToPlayer = toPlayer.magnitude;
            float angleToUp = Vector2.Angle(Vector2.up, toPlayer);
            // ?占쎈젅?占쎌뼱媛 ?占쎌そ(60?占쎈궡)?占쎄퀬 2釉붾윮 ?占쏀븯占??占쏀봽 湲덌옙?
            if (toPlayer.y > 0f && distToPlayer <= 4f && angleToUp <= 60f)
            {
                RaycastHit2D hitAbove = Physics2D.Raycast(origin, toPlayer.normalized, distToPlayer + 0.1f, groundLayer);
                // ?占쎈젅?占쎌뼱媛 吏곸젒 ?占쎌뿉 ?占쎈뒗 寃쎌슦??李⑤떒 (以묎컙???占쎈Ⅸ ?占쎌븷占??占쎌쓣 ???占쎌쓬)
                if (hitAbove.collider == null || hitAbove.collider.transform == playerTransform)
                {
                    return false;
                }
            }
        }

        // 우선순위 1: 구덩이 점프
        if (pitJumpPending)
        {
            targetJumpHeight = -detectedPitDepth; // 음수로 저장
            pitJumpPending = false;
            pitJumpExecutedTime = Time.time; // 실행 시간 기록
            if (debugDog)
            {
                Debug.Log($"[DOG][PIT-JUMP] Jumping for pit! depth={detectedPitDepth:F2}");
            }
            return true;
        }

        // 구덩이 점프 직후 0.3초 동안만 벽 점프 차단
        bool recentlyJumpedFromPit = (Time.time - pitJumpExecutedTime) < pitJumpLockDuration;
        bool lockHighJumpBecauseOfPit = (lockWallJumpWhileInPit && inPit) || recentlyJumpedFromPit;
        if (lockNonPitJumpInPit && inPit)
        {
            return false; // 구덩이 상태에서는 다른 점프 권한 잠금
        }

        // 우선순위 2: Spike 회피 점프 (Spike 위가 아닐 때만)
        if (spikeAvoidanceEnabled && !recentlyJumpedFromPit && ShouldJumpForSpike(currentPosition, out float spikeJumpHeight))
        {
            targetJumpHeight = spikeJumpHeight;
            if (debugDog)
            {
                Debug.Log($"[DOG][SPIKE-JUMP] Jumping to avoid spike! height={spikeJumpHeight:F2}");
            }
            return true;
        }

        float heightDiff = nextWaypoint.y - currentPosition.y;
        float horizontalDist = Mathf.Abs(nextWaypoint.x - currentPosition.x);

        // 우선순위 3: 벽 점프 (waypoint가 높을 때)
        if (!lockHighJumpBecauseOfPit && heightDiff > jumpHeightThreshold && horizontalDist < jumpDistanceThreshold)
        {
            targetJumpHeight = heightDiff;
            if (debugDog)
            {
                Debug.Log($"[DOG][WALL-JUMP] High waypoint! height={heightDiff:F2}, dist={horizontalDist:F2}");
            }
            return true;
        }

        // 우선순위 4: 앞에 벽이 있을 때
        if (!lockHighJumpBecauseOfPit)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(
                currentPosition,
                (nextWaypoint - currentPosition).normalized,
                Mathf.Min(horizontalDist, jumpDistanceThreshold),
                groundLayer
            );

            if (wallHit.collider != null && wallHit.point.y > currentPosition.y + 0.5f)
            {
                targetJumpHeight = wallHit.point.y - currentPosition.y;
                if (debugDog)
                {
                    Debug.Log($"[DOG][WALL-JUMP] Wall detected by raycast! height={targetJumpHeight:F2}");
                }
                return true;
            }
        }

        return false;
    }

    private bool BothPitProbesEmpty(Vector2 checkPosition, Vector2 forwardDir, out bool frontEmpty, out bool backEmpty)
    {
        Vector2 dirNorm = forwardDir.sqrMagnitude > 0.01f ? forwardDir.normalized : Vector2.right;
        float pitProbeDistance = 1.0f;
        Vector2 pitProbeOriginFront = checkPosition + dirNorm * 0.6f;
        Vector2 pitProbeOriginBack = checkPosition - dirNorm * 0.6f;
        RaycastHit2D pitProbeFront = Physics2D.Raycast(pitProbeOriginFront, Vector2.down, pitProbeDistance, groundLayer);
        RaycastHit2D pitProbeBack = Physics2D.Raycast(pitProbeOriginBack, Vector2.down, pitProbeDistance, groundLayer);
        Debug.DrawRay(pitProbeOriginFront, Vector2.down * pitProbeDistance, Color.blue, 0.5f);
        Debug.DrawRay(pitProbeOriginBack, Vector2.down * pitProbeDistance, Color.cyan, 0.5f);

        frontEmpty = pitProbeFront.collider == null;
        backEmpty = pitProbeBack.collider == null;
        return frontEmpty && backEmpty;
    }

    private bool ShouldJumpForSpike(Vector3 currentPosition, out float requiredJumpHeight)
    {
        requiredJumpHeight = 0f;

        if (!spikeAvoidanceEnabled)
        {
            return false;
        }

        if (rb == null) return false;

        // spikeLayer가 Nothing이면 경고
        if (spikeLayer == 0)
        {
            if (debugDog && Time.frameCount % 180 == 0)
            {
                Debug.LogWarning("[DOG][SPIKE] spikeLayer is not set! Please set it in Inspector.");
            }
            return false;
        }

        // 이동 방향 확인
        float dirX = Mathf.Abs(lastFacingDir) > 0.01f ? Mathf.Sign(lastFacingDir) : 1f;
        Vector2 checkDirection = new Vector2(dirX, 0f);

        // 전방에 Spike가 있는지 raycast로 감지
        Vector2 rayOrigin = (Vector2)currentPosition + groundCheckOffset;
        RaycastHit2D spikeHit = Physics2D.Raycast(rayOrigin, checkDirection, spikeDetectionDistance, spikeLayer);

        if (debugDog)
        {
            Debug.DrawRay(rayOrigin, checkDirection * spikeDetectionDistance,
                spikeHit.collider != null ? Color.red : Color.green, 0.1f);

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DOG][SPIKE-CHECK] rayOrigin={rayOrigin}, dir={checkDirection}, dist={spikeDetectionDistance}, spikeLayer={spikeLayer.value}, hit={spikeHit.collider != null}");
            }
        }

        if (spikeHit.collider == null) return false; // Spike 없음

        // Spike의 collider bounds를 읽어서 가로 길이 계산
        Bounds spikeBounds = spikeHit.collider.bounds;
        float spikeStartX = dirX > 0 ? spikeBounds.min.x : spikeBounds.max.x;
        float spikeEndX = dirX > 0 ? spikeBounds.max.x : spikeBounds.min.x;

        // Spike 시작점까지 거리
        float distanceToSpikeStart = Mathf.Abs(spikeStartX - currentPosition.x);
        // Spike 끝점까지 거리
        float distanceToSpikeEnd = Mathf.Abs(spikeEndX - currentPosition.x);
        // Spike 가로 길이
        float spikeWidth = spikeBounds.size.x;

        // 현재 수평 속도
        float currentSpeedX = Mathf.Abs(rb.linearVelocity.x);
        if (currentSpeedX < 0.1f) currentSpeedX = moveSpeed; // 정지 중이면 기본 속도 사용

        // 점프 후 포물선 계산: Spike 위를 넘어가려면
        // - Spike 전체 위에서 y 좌표가 spikeHeight(1블록) 이상이어야 함
        // - 안전 여유: 1.5블록 높이로 점프 (여유 0.5블록)
        float safetyMargin = 0.5f;
        requiredJumpHeight = spikeHeight + safetyMargin;

        // 점프 타이밍: Spike 끝점을 넘어가도록 계산
        if (rb.gravityScale > 0)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            // 필요한 초기 속도: v = sqrt(2 * g * h)
            float requiredVelocityY = Mathf.Sqrt(2f * gravity * requiredJumpHeight);
            // 점프 후 최고점까지 걸리는 시간: t = v / g
            float timeToApex = requiredVelocityY / gravity;
            // 총 체공 시간 (대략): 2 * timeToApex
            float airTime = 2f * timeToApex;
            // 점프 중 이동 거리
            float jumpDistance = currentSpeedX * airTime;

            // 점프 거리가 Spike 끝점까지 거리보다 커야 안전하게 넘어감
            // 타이밍: Spike 끝점을 기준으로 점프 거리의 40~80% 범위일 때
            float minJumpDist = jumpDistance * 0.4f;
            float maxJumpDist = jumpDistance * 0.8f;

            // Spike 끝점까지 거리가 타이밍 범위 안에 있고,
            // 점프 거리가 Spike를 완전히 넘어갈 수 있을 때
            if (distanceToSpikeEnd >= minJumpDist &&
                distanceToSpikeEnd <= maxJumpDist &&
                jumpDistance > distanceToSpikeEnd + 0.2f) // 0.2블록 여유
            {
                if (debugDog)
                {
                    Debug.Log($"[DOG][SPIKE-CALC] spikeWidth={spikeWidth:F2}, startDist={distanceToSpikeStart:F2}, endDist={distanceToSpikeEnd:F2}, jumpDist={jumpDistance:F2}, range=[{minJumpDist:F2}, {maxJumpDist:F2}]");
                }
                return true;
            }
            else if (debugDog && spikeHit.collider != null && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[DOG][SPIKE-WAIT] Too close/far. spikeWidth={spikeWidth:F2}, endDist={distanceToSpikeEnd:F2}, jumpDist={jumpDistance:F2}, need>{distanceToSpikeEnd + 0.2f:F2}");
            }
        }

        return false;
    }

    private bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime) >= jumpCooldown;
    }

    protected override void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // Calculate jump force based on target height
        float calculatedJumpForce = jumpForce; // default
        if (targetJumpHeight < 0f)
        {
            // 얕은 구덩이: 낙하 깊이에 딱 맞는 낮은 점프(과도 점프 방지)
            float depth = Mathf.Abs(targetJumpHeight);
            float desiredHeight = Mathf.Clamp(depth + 0.1f, 0.2f, 2f); // 떨어진 높이 + 약간의 여유, 최대 2블록
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            calculatedJumpForce = Mathf.Sqrt(2f * gravity * desiredHeight);
            // 너무 큰 값으로 가지 않도록 상한, 너무 작지 않도록 하한 설정
            calculatedJumpForce = Mathf.Clamp(calculatedJumpForce, jumpForce * 0.12f, jumpForce * 0.55f);

            if (debugDog)
            {
                Debug.Log($"[DOG][JUMP] PIT depth={depth:F2}, desiredH={desiredHeight:F2}, calculatedForce={calculatedJumpForce:F2}, baseForce={jumpForce}");
            }
        }
        else if (targetJumpHeight > 0f && targetJumpHeight <= 2f)
        {
            // Spike 회피 점프: 정확한 높이로 계산
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            calculatedJumpForce = Mathf.Sqrt(2f * gravity * targetJumpHeight);

            if (debugDog)
            {
                Debug.Log($"[DOG][JUMP] SPIKE targetHeight={targetJumpHeight:F2}, calculatedForce={calculatedJumpForce:F2}");
            }
        }
        else if (targetJumpHeight > 2f)
        {
            // 위로 올라가는 높은 벽: 물리 공식으로 계산
            // Physics formula: v = sqrt(2 * g * h)
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            calculatedJumpForce = Mathf.Sqrt(2f * gravity * targetJumpHeight) * 3f;

            // Clamp to not exceed base jumpForce
            calculatedJumpForce = Mathf.Clamp(calculatedJumpForce, jumpForce * 0.8f, jumpForce);

            if (debugDog)
            {
                Debug.Log($"[DOG][JUMP] WALL targetHeight={targetJumpHeight:F2}, calculatedForce={calculatedJumpForce:F2}, baseForce={jumpForce}");
            }
        }

        // 구덩이 점프의 경우 더 낮은 상한으로 한번 더 클램프
        if (targetJumpHeight < 0f)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float maxPitImpulse = Mathf.Sqrt(2f * gravity * 2f); // 최대 2블록까지
            calculatedJumpForce = Mathf.Min(calculatedJumpForce, jumpForce * 0.45f, maxPitImpulse);
        }

        if (animator != null)
        {
            if (hasJumpBoolParam) animator.SetBool(IsJumpingHash, true);
            if (hasJumpTriggerParam) animator.SetTrigger(JumpTriggerHash);
        }

        rb.AddForce(Vector2.up * calculatedJumpForce, ForceMode2D.Impulse);

        lastJumpTime = Time.time;
        jumpStartX = rb.position.x;
        jumpStartTime = Time.time;
        monitoringJumpEscape = true;
        targetJumpHeight = 0f; // Reset
        ApplyForwardJumpImpulseIfClear();
    }

    private void ApplyForwardJumpImpulseIfClear()
    {
        if (rb == null) return;

        // backstep 점프의 경우 backstepForwardDir 사용, 아니면 lastFacingDir 사용
        float dir;
        if (pendingApexImpulse && Mathf.Abs(apexImpulseDir) > 0.01f)
        {
            dir = Mathf.Sign(apexImpulseDir);
        }
        else
        {
            dir = Mathf.Abs(lastFacingDir) > 0.01f ? Mathf.Sign(lastFacingDir) : 1f;
        }

        Vector2 origin = (Vector2)transform.position + groundCheckOffset + Vector2.down * groundCheckDistance;
        Vector2 rayDir = new Vector2(dir, 0f);

        Debug.DrawRay(origin, rayDir * forwardJumpRayDistance, Color.blue, 0.2f);
        bool blocked = Physics2D.Raycast(origin, rayDir, forwardJumpRayDistance, groundLayer);
        if (!blocked)
        {
            rb.AddForce(new Vector2(dir * forwardJumpImpulse, 0f), ForceMode2D.Impulse);
            if (debugDog)
            {
                Debug.Log($"[DOG][JUMP-FWD] clear path dir={dir}, impulse={forwardJumpImpulse}, apexDir={apexImpulseDir}");
            }
        }
        else if (debugDog)
        {
            Debug.Log($"[DOG][JUMP-FWD] BLOCKED by raycast, dir={dir}");
        }
    }

    private void MoveHorizontally(float directionX)
    {
        // 속도 부스트 중이면 x좌표 급변 체크 (단, 0.5초 텀 후에)
        if (speedBoosted)
        {
            float boostDuration = Time.time - speedBoostStartTime;
            if (boostDuration >= 0.5f)
            {
                float deltaX = Mathf.Abs(rb.position.x - speedBoostStartX);
                if (deltaX > speedBoostDeltaThreshold)
                {
                    speedBoosted = false;
                    if (debugDog)
                    {
                        Debug.Log($"[DOG][SPEED-RESET] 0.5s passed, x changed significantly ({deltaX:F2}), reset speed to normal");
                    }
                }
            }
        }

        bool slopeActive = isGrounded && (lastGroundNormal.y > 0.1f && lastGroundNormal.y < flatNormalThreshold);

        // 경사면 방향 감지: 오르막 vs 내리막
        bool isUphill = false;
        bool isDownhill = false;
        if (slopeActive && Mathf.Abs(directionX) > 0.01f)
        {
            // directionX * lastGroundNormal.x < 0 → 오르막
            // directionX * lastGroundNormal.x > 0 → 내리막
            float slopeDirection = directionX * lastGroundNormal.x;
            isUphill = slopeDirection < -0.01f;
            isDownhill = slopeDirection > 0.01f;
        }

        // 속도 부스트 중이면 boostedSpeed 사용, 아니면 경사면 방향에 따라 조절
        float baseSpeed;
        float speedMultiplier = 1f;

        if (speedBoosted)
        {
            baseSpeed = boostedSpeed;
        }
        else if (isUphill)
        {
            // 오르막: 중력에 의한 미끄러짐을 이기기 위해 속도 증가
            baseSpeed = moveSpeed + uphillSpeedBonus;
            speedMultiplier = 1.0f;
            if (debugDog)
            {
                Debug.Log($"[DOG][SLOPE-UP] Uphill boost applied. moveSpeed={moveSpeed:F2}, bonus={uphillSpeedBonus:F2}, targetBase={baseSpeed:F2}");
            }
        }
        else if (isDownhill)
        {
            // 내리막: 약간 증가하지만 과도하지 않게
            baseSpeed = moveSpeed * 1.1f;
            speedMultiplier = 1.1f;
        }
        else
        {
            baseSpeed = moveSpeed;
        }

        // 경사면 감지 시 즉시 임펄스 효과 취소
        if (slopeActive)
        {
            if (backOffUntil > Time.time || forwardRunUntil > Time.time)
            {
                backOffUntil = -1f;
                forwardRunUntil = -1f;
                if (debugDog)
                {
                    Debug.Log($"[DOG][SLOPE-CANCEL] slope detected, cancel impulse effects");
                }
            }
        }

        // lastFacingDir 업데이트 - 회피 로직(forwardRun, backOff) 중이 아닐 때만
        bool inBypassMode = (forwardRunUntil > Time.time) || (backOffUntil > Time.time);
        if (Mathf.Abs(directionX) > 0.01f && !inBypassMode)
        {
            float newFacingDir = Mathf.Sign(directionX);
            if (Mathf.Abs(newFacingDir - lastFacingDir) > 0.1f && debugDog)
            {
                Debug.Log($"[DOG][FACING-CHANGE] lastFacing={lastFacingDir:F2} → newFacing={newFacingDir:F2}, directionX={directionX:F2}");
            }
            lastFacingDir = newFacingDir;
        }

        // Airborne forward impulse: if in air and path is clear, immediately push forward
        TryAirborneForwardImpulse(directionX);

        // Airborne stuck detection: trying to move forward but X not changing
        TryAirborneStuckBackoff(directionX);

        // ?占쏀쉶 ?占쎌쭊???占쎌꽦?占쎈릺?占쎌쓣???占쎌꽑 ?占쎌슜 (단, 점프 직후 일정 시간은 제외)
        if (lockForwardUntilGrounded && Time.time > allowForwardUntil)
        {
            // 경사면에 착지하면 즉시 락 해제
            if (isGrounded && slopeActive)
            {
                lockForwardUntilGrounded = false;
                if (debugDog)
                {
                    Debug.Log($"[DOG][SLOPE-UNLOCK] landed on slope, unlock forward movement");
                }
            }
            else
            {
                // Keep horizontal neutral while waiting to land
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
                {
                    lockForwardUntilGrounded = false;
                }
            }
        }
        else if (forwardRunUntil > Time.time && Mathf.Abs(forwardRunDir) > 0.01f)
        {
            float forcedX = moveSpeed * forwardBypassSpeedMultiplier * forwardRunDir;
            rb.linearVelocity = new Vector2(forcedX, rb.linearVelocity.y);
        }
        else if (backOffUntil > Time.time && Mathf.Abs(backOffDir) > 0.01f)
        {
            float backX = moveSpeed * backOffSpeedMultiplier * backOffDir;
            rb.linearVelocity = new Vector2(backX, rb.linearVelocity.y);
        }
        else
        {
            float targetVelocityX = Mathf.Clamp(directionX * baseSpeed * speedMultiplier, -maxSpeed, maxSpeed);

            float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, baseSpeed * Time.fixedDeltaTime * 10f);
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

            // 오르막에서 바닥을 누르는 힘 추가 (미끄러짐 방지)
            if (isUphill && rb != null)
            {
                // 경사면 법선 방향으로 힘을 가해서 바닥에 밀착
                Vector2 normalForce = -lastGroundNormal * 2f;
                rb.AddForce(normalForce, ForceMode2D.Force);
            }
        }

        // Sprite facing - lastFacingDir 기반으로 안정적으로 업데이트
        if (spriteRenderer != null && Mathf.Abs(lastFacingDir) > 0.01f)
        {
            spriteRenderer.flipX = lastFacingDir < 0;
        }

        // ★ IsWalking은 FixedUpdate의 UpdateWalkingAnimation()에서 일괄 처리

        TryGroundedForwardNudge(directionX);
    }

    private void MoveInDirection(Vector2 direction)
    {
        if (isKnockedBack) return;

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            MoveHorizontally(direction.x);
        }

        // ★ IsWalking은 FixedUpdate의 UpdateWalkingAnimation()에서 일괄 처리
    }

    private void TryApplyApexImpulse()
    {
        if (!pendingApexImpulse || rb == null) return;

        apexPeakY = Mathf.Max(apexPeakY, rb.position.y);

        // Detect transition from upward to downward (peak)
        bool atPeak = (prevVerticalVelY > 0f && lastVerticalVelY <= 0f)
                      || (lastVerticalVelY <= 0f && rb.position.y >= apexPeakY - 0.05f);

        if (atPeak)
        {
            // 공중 임펄스 쿨다운 중이면 apex 임펄스 스킵
            float now = Time.time;
            if (now - lastAirborneForwardImpulseTime < airborneForwardCooldown)
            {
                if (debugDog)
                {
                    Debug.Log($"[DOG][APEX] skipped (airborne impulse cooldown)");
                }
                pendingApexImpulse = false;
                return;
            }

            float dir = Mathf.Abs(apexImpulseDir) > 0.01f
                ? Mathf.Sign(apexImpulseDir)
                : (Mathf.Abs(lastFacingDir) > 0.01f ? Mathf.Sign(lastFacingDir) : 1f);
            if (dir != 0f)
            {
                Vector2 origin = (Vector2)transform.position + groundCheckOffset + Vector2.down * groundCheckDistance;
                Vector2 rayDir = new Vector2(dir, 0f);
                bool forwardBlocked = Physics2D.Raycast(origin, rayDir, forwardAirRayDistance, groundLayer);
                if (!forwardBlocked)
                {
                    rb.AddForce(new Vector2(dir * apexForwardImpulse, 0f), ForceMode2D.Impulse);
                    if (debugDog)
                    {
                        Debug.Log($"[DOG][APEX] forward impulse applied dir={dir} force={apexForwardImpulse}");
                    }
                }
            }
            pendingApexImpulse = false;
        }

        if (isGrounded)
        {
            pendingApexImpulse = false;
            apexPeakY = float.MinValue;
        }
    }

    protected override void OnPlayerDetected()
    {
        currentState = DogState.Chase;
    }

    protected override void PerformAttack()
    {
        if (playerTransform == null || isAttacking) return;

        // Respect attack cooldown
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack < attackCooldown)
        {
            Debug.Log($"[DOG ATTACK] 쿨타임 중 - 남은 시간: {attackCooldown - timeSinceLastAttack:F2}초");
            return;
        }

        // 공격 애니 시작 시점에도 실제 타격 가능 조건 확인
        if (!CanDealDamageNow()) return;

        Debug.Log($"[DOG ATTACK] 공격 시작! lastAttackTime={lastAttackTime:F2}, currentTime={Time.time:F2}");
        isAttacking = true;
        attackAnimationTimer = attackAnimationDuration;
        attackDamageApplied = false;
        attackHitAttempted = false; // ★ 피해 적용 시도 플래그 초기화
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
        }
    }

    private void TryApplyAttackDamage()
    {
        if (attackDamageApplied) return;
        if (playerTransform == null) return;

        if (!CanDealDamageNow()) return;

        PlayerController player = playerTransform.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(attackDamage);
            attackDamageApplied = true;
        }
    }

    private bool CanDealDamageNow()
    {
        if (playerTransform == null) return false;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > attackRange) return false;

        // 라인오브사이트: groundLayer에 막히면 공격 보류
        if (groundLayer.value != 0)
        {
            Vector2 origin = transform.position;
            Vector2 dir = ((Vector2)playerTransform.position - origin).normalized;
            float len = distanceToPlayer + 0.1f;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, len, groundLayer);
            if (hit.collider != null && !hit.collider.transform.IsChildOf(playerTransform))
            {
                return false;
            }
        }

        return true;
    }

    private void StartDamageFlash()
    {
        if (spriteRenderer == null) return;
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            spriteRenderer.color = new Color(dogBaseColor.r, dogBaseColor.g, dogBaseColor.b, 1f);
            damageFlashRoutine = null;
        }
        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        dogBaseColor = spriteRenderer.color;
        Color baseSolid = new Color(dogBaseColor.r, dogBaseColor.g, dogBaseColor.b, 1f);
        Color flashTint = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, 1f);
        Color flash = Color.Lerp(baseSolid, flashTint, 0.6f); // ?뚰뙆 蹂寃??놁씠 ?됱긽留?釉붾젋??
        for (int i = 0; i < damageFlashCount; i++)
        {
            spriteRenderer.color = flash;
            yield return new WaitForSeconds(damageFlashDuration * 0.5f);
            spriteRenderer.color = baseSolid;
            yield return new WaitForSeconds(damageFlashDuration * 0.5f);
        }

        spriteRenderer.color = baseSolid;
        damageFlashRoutine = null;
    }

    protected override void ResetKnockback()
    {
        base.ResetKnockback();
        currentState = DogState.Patrol;
    }

    private Vector2 FindAlternativeDirection(Vector2 blockedDirection)
    {
        Vector2 upDirection = new Vector2(blockedDirection.x, 0.5f).normalized;
        if (!Physics2D.Raycast(transform.position, upDirection, obstacleDetectionDistance, groundLayer))
            return upDirection;

        Vector2 downDirection = new Vector2(blockedDirection.x, -0.5f).normalized;
        if (!Physics2D.Raycast(transform.position, downDirection, obstacleDetectionDistance, groundLayer))
            return downDirection;

        return Vector2.zero;
    }

    private void MonitorJumpEscape()
    {
        if (!monitoringJumpEscape || rb == null) return;

        // 착지하면 모니터링 중단
        if (isGrounded)
        {
            monitoringJumpEscape = false;
            return;
        }

        // 점프 시작 후 0.1초 이내는 체크 안함 (초기 임펄스 적용 시간)
        if (Time.time - jumpStartTime < 0.1f)
            return;

        // x 변화가 크면 (0.5블럭 이상) 탈출 성공으로 판단
        float deltaX = Mathf.Abs(rb.position.x - jumpStartX);
        if (deltaX > 0.5f && rb.linearVelocity.y > 0f)
        {
            // y속도를 50%로 줄여서 빠른 착지
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            monitoringJumpEscape = false;
            if (debugDog)
            {
                Debug.Log($"[DOG][JUMP-CUT] escape success! deltaX={deltaX:F2}, cut jump height for fast landing");
            }
        }
    }

    private void TryAirborneForwardImpulse(float directionX)
    {
        if (rb == null) return;
        if (isGrounded) return; // 공중에 있을 때만
        if (Mathf.Abs(directionX) < 0.01f) return;

        float now = Time.time;

        // 쿨다운 체크
        if (now - lastAirborneForwardImpulseTime < airborneForwardCooldown)
            return;

        // 체크 간격
        if (now - lastAirborneForwardCheckTime < airborneForwardCheckInterval)
            return;

        lastAirborneForwardCheckTime = now;

        float dir = Mathf.Sign(directionX);
        Vector2 origin = (Vector2)transform.position + groundCheckOffset + Vector2.down * groundCheckDistance;
        Vector2 rayDir = new Vector2(dir, 0f);

        // 파란색 raycast로 앞 공간 체크 (1~2블럭)
        Debug.DrawRay(origin, rayDir * airborneForwardRayDistance, Color.blue, 0.2f);
        bool pathClear = !Physics2D.Raycast(origin, rayDir, airborneForwardRayDistance, groundLayer);

        if (pathClear)
        {
            // 앞이 충분히 비었으면 즉시 임펄스!
            rb.AddForce(new Vector2(dir * airborneForwardImpulse, 0f), ForceMode2D.Impulse);
            lastAirborneForwardImpulseTime = now;
            if (debugDog)
            {
                Debug.Log($"[DOG][AIR-IMPULSE] instant forward push! dir={dir}, impulse={airborneForwardImpulse}");
            }
        }
    }

    private void TryGroundedForwardNudge(float directionX)
    {
        if (rb == null) return;
        if (!isGrounded) return;
        if (inPit || lockNonPitJumpInPit) return; // 구덩이 상태에서는 다른 점프 유발 로직 금지
        if (Mathf.Abs(directionX) < 0.01f) return;

        // 경사면에서는 갇힘 판정 안함
        if (onSlope) return;

        float now = Time.time;

        // Backstep -> then jump sequence if triggered
        if (pendingBackstepJump)
        {
            bool reached = (backstepDir < 0f && rb.position.x <= backstepTargetX) ||
                           (backstepDir > 0f && rb.position.x >= backstepTargetX);
            if ((reached || now >= backstepExpireTime) && CanJump())
            {
                Jump();
                pendingBackstepJump = false;
                lockForwardUntilGrounded = true;
                allowForwardUntil = Time.time + 0.25f; // 점프 후 0.25초 동안 앞으로 가는 힘 유지
                awaitingJumpAfterNudge = false;
                pendingApexImpulse = true;
                apexImpulseDir = backstepForwardDir;
                stuckForwardHopCount = 0;
                forwardShoveTried = false;
                lastForwardCheckPosX = rb.position.x;
                lastForwardCheckTime = now;
                if (debugDog)
                {
                    Debug.Log($"[DOG][BACKSTEP-JUMP] dir={backstepForwardDir}, allowForward until={allowForwardUntil:F2}");
                }
            }
            return;
        }

        if (lastForwardCheckTime < 0f)
        {
            lastForwardCheckPosX = rb.position.x;
            lastForwardCheckTime = now;
            lastSpeedBoostTime = -speedBoostCooldown; // 즉시 사용 가능
            return;
        }

        if (now - lastForwardCheckTime < forwardNudgeCheckInterval) return;

        float deltaX = Mathf.Abs(rb.position.x - lastForwardCheckPosX);
        if (deltaX < forwardNudgeDeltaThreshold && (now - lastForwardNudgeTime) >= forwardNudgeCheckInterval)
        {
            float dir = Mathf.Sign(directionX);

            // 첫 시도: 속도를 6으로 부스트 (짧게만)
            if (!speedBoosted && now - lastSpeedBoostTime >= speedBoostCooldown)
            {
                speedBoosted = true;
                speedBoostStartTime = now;
                speedBoostStartX = rb.position.x;
                lastSpeedBoostTime = now;
                lastForwardNudgePosX = rb.position.x;
                lastForwardNudgeTime = now;
                if (debugDog)
                {
                    Debug.Log($"[DOG][SPEED-BOOST] speed boosted to {boostedSpeed}");
                }
                return;
            }

            // 두 번째 시도: 0.15초 후에도 실패하면 즉시 backstep → jump! (총 0.35초 안에)
            if (speedBoosted && now - speedBoostStartTime > 0.15f)
            {
                backstepDir = -dir;
                backstepForwardDir = dir;
                backstepTargetX = rb.position.x + backstepDir * backStepDistance;
                backstepExpireTime = now + backStepTimeout;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.AddForce(new Vector2(backstepDir * backStepDistance, 0f), ForceMode2D.Impulse);
                pendingBackstepJump = true;
                lockForwardUntilGrounded = true;
                awaitingJumpAfterNudge = false;
                stuckForwardHopCount = 0;
                forwardShoveTried = false;
                speedBoosted = false; // 속도 부스트 해제
                lastForwardNudgePosX = rb.position.x;
                lastForwardNudgeTime = now;
                if (debugDog)
                {
                    Debug.Log($"[DOG][STUCK-QUICK] 0.15s failed, backstep→jump dir={dir}, targetX={backstepTargetX:F2}");
                }
                return;
            }
        }

        lastForwardCheckPosX = rb.position.x;
        lastForwardCheckTime = now;

        // x좌표가 정상적으로 변하면 부스트 해제 및 상태 리셋 (단, 0.5초 텀 후에)
        if (deltaX >= forwardNudgeDeltaThreshold * 2f)
        {
            stuckForwardHopCount = 0;
            forwardShoveTried = false;
            if (speedBoosted)
            {
                float boostDuration = now - speedBoostStartTime;
                if (boostDuration >= 0.5f)
                {
                    speedBoosted = false;
                    if (debugDog)
                    {
                        Debug.Log($"[DOG][SPEED-RESET] 0.5s passed, movement recovered, reset speed");
                    }
                }
            }
        }

        // 최후의 수단: 0.5초 이상 x 이동이 거의 없으면 위로 작은 임펄스를 반복 적용
        if (deltaX < forwardNudgeDeltaThreshold * 0.5f && (now - lastForwardCheckTime) >= 0.5f)
        {
            if (rb != null && (now - lastImpulseUpTime) >= 0.5f)
            {
                rb.AddForce(Vector2.up * 1f, ForceMode2D.Impulse);
                lastImpulseUpTime = now;
                if (debugDog)
                {
                    Debug.Log($"[DOG][IMPULSE-UP] stuck on wall, apply upward impulse");
                }
            }
        }
    }

    private void TryAirborneStuckBackoff(float inputX)
    {
        if (rb == null) return;
        if (isGrounded) return;
        if (Mathf.Abs(inputX) < 0.01f) return;

        if (Time.time - lastPosCheckTime < stuckCheckInterval)
            return;

        float deltaX = Mathf.Abs(rb.position.x - lastPosX);
        if (deltaX < stuckPosThreshold)
        {
            backOffDir = -Mathf.Sign(inputX);
            backOffUntil = Time.time + backOffDuration;
            if (debugDog)
            {
                Debug.Log($"[DOG][BACKOFF-AIR] stuck deltaX={deltaX:F4}, dir={backOffDir}, until={backOffUntil:F2}");
            }
        }

        lastPosX = rb.position.x;
        lastPosCheckTime = Time.time;
    }

    private void TryBackwardObstacleBypass(Vector2 steeringDir)
    {
        if (playerTransform == null) return;

        float facingX = Mathf.Abs(lastFacingDir) > 0.01f
            ? Mathf.Sign(lastFacingDir)
            : (Mathf.Abs(steeringDir.x) > 0.01f ? Mathf.Sign(steeringDir.x) : 1f);
        Vector2 facing = new Vector2(facingX, 0f);
        Vector2 origin = (Vector2)transform.position - facing * backOffset;

        Vector2 toPlayer = (Vector2)playerTransform.position - origin;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        float toPlayerDist = toPlayer.magnitude;
        Vector2 dirToPlayer = toPlayer.normalized;
        RaycastHit2D hitToPlayer = Physics2D.Raycast(origin, dirToPlayer, toPlayerDist, groundLayer);
        if (debugDog)
        {
            Debug.DrawLine(origin, origin + dirToPlayer * toPlayerDist, hitToPlayer.collider == null ? Color.green : Color.red, 0.2f);
        }
        if (hitToPlayer.collider == null)
            return; // not blocked

        // Cast vertically toward the player (up or down) instead of always upward
        Vector2 verticalDir = toPlayer.y >= 0f ? Vector2.up : Vector2.down;
        RaycastHit2D upHit = Physics2D.Raycast(origin, verticalDir, verticalCheckMaxDistance, groundLayer);
        if (debugDog)
        {
            Debug.DrawLine(origin, origin + verticalDir * verticalCheckMaxDistance, upHit.collider == null ? Color.green : Color.yellow, 0.2f);
        }
        if (upHit.collider == null)
            return; // free space above, no need to bypass

        forwardRunDir = facingX;
        forwardRunUntil = Time.time + forwardBypassDuration;
        nextBypassTime = Time.time + 0.3f;
        if (debugDog)
        {
            Debug.Log($"[DOG][BYPASS] blocked by {hitToPlayer.collider.name}, runDir={forwardRunDir}, until={forwardRunUntil:F2}");
        }
    }

    private void SetHealthUiAlpha(float alpha)
    {
        if (heartRenderer != null)
        {
            Color heartColor = heartRenderer.color;
            heartColor.a = alpha;
            heartRenderer.color = heartColor;
        }

        if (healthText != null)
        {
            Color textColor = healthText.color;
            textColor.a = alpha;
            healthText.color = textColor;
        }
    }

    private void UpdateHealthUiAlphaState()
    {
        if (!healthUiInitialized || healthUiHidden) return;

        float targetAlpha = Time.time <= healthUiShowUntil ? healthUiHitAlpha : healthUiNormalAlpha;
        SetHealthUiAlpha(targetAlpha);

        if (hideHealthUiAfterHit && Time.time > healthUiShowUntil)
        {
            HideHealthUI();
            hideHealthUiAfterHit = false;
        }
    }

    private void HideHealthUI()
    {
        if (healthUiHidden) return;

        if (heartGO != null) heartGO.SetActive(false);
        if (textGO != null) textGO.SetActive(false);
        healthUiHidden = true;
    }

    private void CreateHealthUI()
    {
        if (heartSprite == null)
        {
            Debug.LogWarning("[Dog] heartSprite is null");
            return;
        }

        if (heartGO != null) Destroy(heartGO);
        if (textGO != null) Destroy(textGO);

        heartGO = new GameObject("HealthHeart");
        heartGO.transform.SetParent(transform);
        heartGO.transform.localPosition = healthUiOffset;
        heartGO.transform.localScale = healthUiScale;

        heartRenderer = heartGO.AddComponent<SpriteRenderer>();
        heartRenderer.sprite = heartSprite;
        heartRenderer.sortingLayerName = healthUiSortingLayer;
        heartRenderer.sortingOrder = healthUiSortingOrder;

        // ?띿뒪???꾩튂 ?ㅼ젙 - padding??湲곗〈 x 媛믪뿉 異붽?
        var textPos = healthTextLocalPosition;
        float padding = healthTextPadding;
        textPos.x = textPos.x + padding;  // 湲곗〈 x 媛믪뿉 ?⑤뵫???뷀븿
        textPos.z = Mathf.Approximately(textPos.z, 0f) ? -2f : textPos.z;

        int textSortingOffset = healthTextSortingOffset <= 0 ? 50 : healthTextSortingOffset;

        textGO = new GameObject("HealthText");
        textGO.transform.SetParent(heartGO.transform);
        textGO.transform.localPosition = GetHealthTextPosition();
        textGO.transform.localScale = Vector3.one;
        textGO.transform.localRotation = Quaternion.identity;

        healthText = textGO.AddComponent<TextMesh>();
        healthText.text = currentHealth.ToString("0.##");
        healthText.anchor = TextAnchor.MiddleLeft;
        healthText.alignment = TextAlignment.Left;
        healthText.characterSize = 1f;
        healthText.fontSize = 40;
        healthText.color = healthTextColor;
        healthText.font = healthFont != null
            ? healthFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.richText = false;

        var textRenderer = textGO.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = healthUiSortingLayer;
            textRenderer.sortingOrder = healthUiSortingOrder + textSortingOffset;
            textRenderer.enabled = true;
            textRenderer.material = healthText.font != null
                ? healthText.font.material
                : new Material(Shader.Find("GUI/Text Shader"));
        }

        SetHealthUiAlpha(healthUiNormalAlpha);
        healthUiInitialized = true;
        Debug.Log($"[Dog] Health UI ?앹꽦 - Heart: {heartGO.name}, Text: {healthText.text}, Pos: {textGO.transform.position}, Sorting:{textRenderer?.sortingOrder}");
    }

    private Vector3 GetHealthTextPosition()
    {
        var pos = healthTextLocalPosition;
        pos.x += healthTextPadding;
        pos.z = Mathf.Approximately(pos.z, 0f) ? -2f : pos.z;
        return pos;
    }

    private void UpdateHealthUI()
    {
        if (healthUiHidden) return;

        if (heartSprite != null && heartRenderer == null)
        {
            CreateHealthUI();
        }

        if (heartGO != null)
        {
            heartGO.SetActive(true);
        }
        if (textGO != null)
        {
            textGO.SetActive(true);
            textGO.transform.localPosition = GetHealthTextPosition();
        }

        if (healthText != null)
        {
            float hpValue = Mathf.Max(0f, currentHealth);
            healthText.text = hpValue.ToString("0.##");
            Debug.Log($"[Dog] Health UI update - HP: {hpValue}, TextActive: {textGO?.activeSelf}");
        }

        UpdateHealthUiAlphaState();
    }

    private void SpawnDamageText(float damage)
    {
        StartCoroutine(DamageTextRoutine(damage));
    }

    private IEnumerator DamageTextRoutine(float damage)
    {
        var damageTextGO = new GameObject("DogDamageText");
        damageTextGO.transform.SetParent(transform);
        damageTextGO.transform.localPosition = damageTextOffset;
        damageTextGO.transform.localRotation = Quaternion.identity;
        damageTextGO.transform.localScale = Vector3.one;

        var textMesh = damageTextGO.AddComponent<TextMesh>();
        textMesh.text = $"-{damage:0.#}";
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.characterSize = damageTextCharacterSize;
        textMesh.fontSize = damageTextFontSize;
        textMesh.color = damageTextColor;
        textMesh.richText = false;
        textMesh.font = damageTextFont != null
            ? damageTextFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var renderer = damageTextGO.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = healthUiSortingLayer;
            renderer.sortingOrder = healthUiSortingOrder + 80;
            renderer.material = textMesh.font != null
                ? textMesh.font.material
                : new Material(Shader.Find("GUI/Text Shader"));
        }

        Vector3 baseLocalPos = damageTextOffset;
        float elapsed = 0f;
        while (elapsed < damageTextDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / damageTextDuration);
            float vertical = Mathf.Sin(t * Mathf.PI) * damageTextAmplitude;
            damageTextGO.transform.localPosition = baseLocalPos + new Vector3(0f, vertical, 0f);

            var c = textMesh.color;
            c.a = 1f - t;
            textMesh.color = c;

            yield return null;
        }

        Destroy(damageTextGO);
    }

    public override void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (!isAlive) return;

        // 臾댁쟻 ?占쎄컙 泥댄겕 異뷂옙?
        if (Time.time - lastDamageTime < invincibilityTime)
        {
            Debug.Log($"[Dog] 臾댁쟻 ?占쎄컙 占??占쏙옙?吏 臾댁떆 (?占쏙옙? ?占쎄컙: {invincibilityTime - (Time.time - lastDamageTime):F2}s)");
            return;
        }

        // 臾댁쟻 ?占쎄컙 ?占쎈뜲?占쏀듃
        lastDamageTime = Time.time;

        StartDamageFlash();

        if (!healthUiInitialized)
        {
            CreateHealthUI();
        }

        currentHealth -= damage;
        if (currentHealth < 0f) currentHealth = 0f;
        SpawnDamageText(damage);
        bool lethalHit = currentHealth <= 0f;

        Debug.Log($"[Dog] ?占쏙옙?吏 諛쏆쓬! {damage} (?占쎌옱 泥대젰: {currentHealth}/{maxHealth})");

        float hitDuration = lethalHit ? Mathf.Max(healthUiHitDuration, 1f) : healthUiHitDuration;
        healthUiShowUntil = Time.time + hitDuration;
        hideHealthUiAfterHit = lethalHit;
        UpdateHealthUI();
        SetHealthUiAlpha(healthUiHitAlpha);

        if (lethalHit)
        {
            Die();
            return;
        }

        // Any non-lethal hit should interrupt an ongoing attack and start cooldown.
        InterruptAttack("damaged");

        // Apply knockback from player attacks
        ApplyKnockback(knockbackDirection);
    }

    /// <summary>
    /// Dash 공격에 맞았을 때 공격 취소 및 쿨타임 초기화
    /// </summary>
public void CancelAttackFromDash()
    {
        InterruptAttack("dash hit");
    }

/// <summary>
    /// Interrupt current attack and force cooldown.
    /// </summary>
    public void InterruptAttack(string reason = "")
    {
        isAttacking = false;
        attackAnimationTimer = 0f;
        attackHitAttempted = false;
        attackDamageApplied = false;
        lastAttackTime = Time.time; // reset cooldown
        if (debugDog)
        {
            Debug.Log($"[DOG] Attack interrupted ({reason})");
        }

        // 애니메이터 상태 초기화
        if (animator != null)
        {
            animator.SetBool("Attack", false);
            animator.SetBool("IsAttacking", false);
        }
    }


    protected override void Die()
    {
        if (!isAlive) return;
        isAlive = false;

        // Notify toast hover UI about death so hover panel can show.
        var toastTrigger = GetComponentInChildren<ToastHoverTrigger>(true);
        if (toastTrigger != null)
        {
            toastTrigger.SetOwnerDead();
        }

        UpdateHealthUI();

        if (animator != null)
        {
            // Stop all movement/attack flags before playing death
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsWalking", false);
            animator.speed = 1f;
            animator.enabled = true;
            animator.Play("Death", 0, 0f);
            StartCoroutine(FreezeAfterDeath());
        }

        isAttacking = false;
        currentState = DogState.Idle;
        if (deathToastRoutine != null)
        {
            StopCoroutine(deathToastRoutine);
        }
        deathToastRoutine = StartCoroutine(PlayDeathToastSequence());

        if (aiPath != null)
        {
            aiPath.canMove = false;
            aiPath.isStopped = true;
        }

        // Keep rigidbody dynamic so dead enemy can fall if in air
        // Only freeze horizontal movement to prevent sliding
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Keep vertical velocity for falling
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            // gravityScale stays at 3 so it falls naturally
        }

        // Keep colliders enabled so dead enemies don't overlap
        // Only disable trigger collider (BoxCollider2D for attack detection)
        var hoverTrigger = GetComponentInChildren<ToastHoverTrigger>(true);
        var hoverCollider = hoverTrigger != null ? hoverTrigger.GetComponent<Collider2D>() : null;
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col == hoverCollider) continue; // keep toast hover collider alive for interaction
            // Only disable trigger colliders (attack detection)
            if (col.isTrigger)
            {
                col.enabled = false;
            }
            // Keep non-trigger colliders (body collision) enabled to prevent overlap
        }
        if (hoverCollider != null)
        {
            hoverCollider.enabled = true;
        }
        // Ensure hover renderer stays hidden
        if (hoverTrigger != null)
        {
            hoverTrigger.SetOwnerDead();
        }

        forwardRunUntil = -1f;
        forwardRunDir = 0f;
        nextBypassTime = float.MaxValue;
        backOffUntil = -1f;
        awaitingJumpAfterNudge = false;
        lockForwardUntilGrounded = false;
        pendingApexImpulse = false;
        pendingBackstepJump = false;
        speedBoosted = false;
    }

    private IEnumerator FreezeAfterDeath()
    {
        yield return new WaitForSeconds(deathFreezeDelay);
        if (animator != null)
        {
            animator.speed = 0f;
            animator.enabled = false;
        }
    }

    private IEnumerator PlayDeathToastSequence()
    {
        var indicator = EnsureToastIndicator();
        if (indicator == null || deathToastFrames == null || deathToastFrames.Length == 0)
        {
            yield break;
        }

        // Determine direction multiplier based on facing direction
        float directionMultiplier = Mathf.Sign(lastFacingDir);

        float lastTime = 0f;
        foreach (var frame in deathToastFrames)
        {
            float wait = frame.time - lastTime;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            // Apply direction to x offset (flip for left-facing)
            Vector3 adjustedOffset = new Vector3(
                frame.offset.x * directionMultiplier,
                frame.offset.y,
                frame.offset.z
            );

            indicator.SetLocalOffset(adjustedOffset);
            if (frame.ghostAlpha > 0f)
            {
                indicator.SpawnGhost(frame.ghostAlpha, 0.3f);
            }
            lastTime = frame.time;
        }
    }

    private ToastIndicator EnsureToastIndicator()
    {
        if (cachedToastIndicator == null)
        {
            cachedToastIndicator = GetComponentInChildren<ToastIndicator>(true);
        }
        return cachedToastIndicator;
    }

    // Called by spawn helper/configurator
    public void ApplyConfig(Config config)
    {
        if (config == null) return;
        if (config.moveSpeed > 0f)
        {
            moveSpeed = config.moveSpeed;
            maxSpeed = config.moveSpeed; // keep maxSpeed in sync
        }
        if (config.maxHealth > 0f)
        {
            maxHealth = config.maxHealth;
            currentHealth = config.maxHealth;
        }
        if (config.attackDamage > 0f) attackDamage = config.attackDamage;
        // ★ attackSpeed는 attackCooldown과 충돌하므로 무시
        // Inspector에서 직접 attackCooldown을 설정하세요
        if (config.attackSpeed > 0f)
        {
            Debug.LogWarning($"[DOG CONFIG] attackSpeed={config.attackSpeed}는 무시됨. Inspector에서 attackCooldown={attackCooldown:F2}초 사용");
        }
        // maxJumps not used in this controller; placeholder for future jump logic
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Vector3 spawnPos = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawLine(spawnPos + (Vector3.left * patrolRange), spawnPos + (Vector3.right * patrolRange));

        if (aiPath == null || !aiPath.hasPath) return;

        var path = new System.Collections.Generic.List<Vector3>();
        aiPath.GetRemainingPath(path, out bool stale);

        if (path != null && path.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }

            Vector3 nextWaypoint = GetNextWaypoint(path, transform.position);
            if (nextWaypoint != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(nextWaypoint, 0.3f);
                Gizmos.DrawLine(transform.position, nextWaypoint);
            }
        }

        if (Application.isPlaying)
        {
            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(checkPosition, checkPosition + Vector2.down * groundCheckDistance);
        }
    }

    [System.Serializable]
    private struct DeathToastFrame
    {
        public float time;
        public Vector3 offset;
        public float ghostAlpha;
    }
}
