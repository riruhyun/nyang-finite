using System.Collections;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(Rigidbody2D))]
public class IntelligentDogMovement : Enemy
{
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

    [Tooltip("Delay between attack starts (seconds)")]
    [SerializeField] private float attackCooldown = 2.0f;

    [Tooltip("Max horizontal speed")]
    [SerializeField] private float maxSpeed = 5f;

    [Tooltip("Delay before freezing after death animation finishes")]
    [SerializeField] private float deathFreezeDelay = 1.0f;

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

    [Header("Patrol Settings")]
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Slope Handling")]
    [SerializeField] private float slopeNormalMin = 0.4f;
    [SerializeField] private float flatNormalThreshold = 0.95f;
    [SerializeField] private float slopeSpeedMultiplier = 1.5f;
    [SerializeField] private float slopeSpeedPadding = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugDog = true;
    [SerializeField] private float debugLogInterval = 0.3f;

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
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 1f); // 알파 투명도 대신 순수 빨강 틴트
    [SerializeField] private float damageFlashDuration = 0.1f;
    [SerializeField] private int damageFlashCount = 2;
    private Coroutine damageFlashRoutine;
    private Color dogBaseColor = Color.white;
    // State
    private float lastJumpTime;
    private bool isGrounded;
    private bool isAttacking = false;
    private float attackAnimationTimer = 0f;
    private bool attackDamageApplied = false;
    private float lastAttackTime = -999f;
    private Vector2 spawnPosition;
    private float patrolTimer = 0f;
    // 무적 ?�간 관리용
    private float invincibilityTime = 0.5f;
    private float lastDamageTime = -10f;

    private int patrolDirection = 1;
    private Vector2 lastGroundNormal = Vector2.up;
    private bool onSlope = false;
    private float lastFacingDir = 1f;
    private float forwardRunUntil = -1f;
    private float forwardRunDir = 0f;
    private float backOffUntil = -1f;
    private float backOffDir = 0f;
    private float lastPosX = 0f;
    private float lastPosCheckTime = -1f;

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
        
        CreateHealthUI();
        UpdateHealthUI();
        
        Debug.Log($"[Dog] Start - maxHealth: {maxHealth}, currentHealth: {currentHealth}, heartSprite: {heartSprite != null}");
    }

    protected override void Update()
    {
        UpdateHealthUiAlphaState();

        if (!isAlive || isKnockedBack) return;

        // Attack animation handling
        if (isAttacking)
        {
            attackAnimationTimer -= Time.deltaTime;

            // Deal damage near the end of the animation if still in range
            if (!attackDamageApplied && attackAnimationTimer <= attackHitWindow)
            {
                TryApplyAttackDamage();
            }

            if (attackAnimationTimer <= 0f)
            {
                isAttacking = false;
                if (animator != null)
                {
                    animator.SetBool("IsAttacking", false);
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
    }

    private void FixedUpdate()
    {
        if (!isAlive || isKnockedBack) return;

        CheckGroundStatus();

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
            // ?�레?�어 감�? 범위�?벗어?�면 즉시 Idle
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

        // Set AIPath destination
        if (aiPath != null)
        {
            aiPath.destination = playerTransform.position;
        }

        // If AIPath has a valid path, follow it
        if (aiPath != null && aiPath.hasPath)
        {
            FollowPath();
        }
        else if (useDirectMovementFallback)
        {
            // Direct movement fallback
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            MoveInDirection(directionToPlayer * chaseSpeedMultiplier);
        }
    }

    private void AttackBehavior()
    {
        // Respect cooldown: skip animation/attack if still cooling down
        if (Time.time - lastAttackTime < attackCooldown)
        {
            if (animator != null)
            {
                animator.SetBool("IsAttacking", false);
            }
            return;
        }

        Attack();
    }

    private void IdleBehavior()
    {
    }

    private void CheckGroundStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;

        RaycastHit2D hitRay = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitCircle = Physics2D.CircleCast(checkPosition, 0.1f, Vector2.down, groundCheckDistance, groundLayer);

        bool hasRay = hitRay.collider != null;
        bool hasCircle = hitCircle.collider != null;
        isGrounded = hasRay || hasCircle;

        RaycastHit2D chosen = hasRay ? hitRay : hitCircle;
        if (chosen.collider != null)
        {
            lastGroundNormal = chosen.normal;
            onSlope = lastGroundNormal.y > slopeNormalMin && lastGroundNormal.y < flatNormalThreshold;
        }
        else
        {
            lastGroundNormal = Vector2.up;
            onSlope = false;
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

        // ?�쪽?�서 ?�레?�어까�???경로가 막혔?�시 ?�진 ?�회
        TryBackwardObstacleBypass(direction);
        if (forwardRunUntil > Time.time && Mathf.Abs(forwardRunDir) > 0.01f)
        {
            MoveHorizontally(forwardRunDir);
            return;
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

        // 바로 ??�?Ray) 2블럭 ?�내???�레?�어가 ?�을???�프 금�?
        if (playerTransform != null)
        {
            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)playerTransform.position - origin;
            float distToPlayer = toPlayer.magnitude;
            float angleToUp = Vector2.Angle(Vector2.up, toPlayer);
            // ?�레?�어가 ?�쪽(60?�내)?�고 2블럭 ?�하�??�프 금�?
            if (toPlayer.y > 0f && distToPlayer <= 4f && angleToUp <= 60f)
            {
                RaycastHit2D hitAbove = Physics2D.Raycast(origin, toPlayer.normalized, distToPlayer + 0.1f, groundLayer);
                // ?�레?�어가 직접 ?�에 ?�는 경우??차단 (중간???�른 ?�애�??�을 ???�음)
                if (hitAbove.collider == null || hitAbove.collider.transform == playerTransform)
                {
                    return false;
                }
            }
        }

        float heightDiff = nextWaypoint.y - currentPosition.y;
        float horizontalDist = Mathf.Abs(nextWaypoint.x - currentPosition.x);

        if (heightDiff > jumpHeightThreshold && horizontalDist < jumpDistanceThreshold)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(
            currentPosition,
            (nextWaypoint - currentPosition).normalized,
            Mathf.Min(horizontalDist, jumpDistanceThreshold),
            groundLayer
        );

        return hit.collider != null && hit.point.y > currentPosition.y + 0.5f;
    }

    private bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime) >= jumpCooldown;
    }

    protected override void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        base.Jump(); // Enemy base jumpForce
        lastJumpTime = Time.time;
    }

    private void MoveHorizontally(float directionX)
    {
        bool slopeActive = isGrounded && (onSlope || (lastGroundNormal.y > 0.1f && lastGroundNormal.y < flatNormalThreshold));
        float baseSpeed = slopeActive ? (moveSpeed + slopeSpeedPadding) : moveSpeed;
        float speedMultiplier = slopeActive ? slopeSpeedMultiplier : 1f;

        if (Mathf.Abs(directionX) > 0.01f)
            lastFacingDir = Mathf.Sign(directionX);

        // Airborne stuck detection: trying to move forward but X not changing
        TryAirborneStuckBackoff(directionX);

        // ?�회 ?�진???�성?�되?�을???�선 ?�용
        if (forwardRunUntil > Time.time && Mathf.Abs(forwardRunDir) > 0.01f)
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
        }

        // Sprite facing
        if (Mathf.Abs(directionX) > 0.01f && spriteRenderer != null)
        {
            spriteRenderer.flipX = directionX < 0;
        }
    }

    private void MoveInDirection(Vector2 direction)
    {
        if (isKnockedBack) return;

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            MoveHorizontally(direction.x);
        }

        if (animator != null)
        {
            bool isMoving = direction.magnitude > 0.01f;
            animator.SetBool("IsWalking", isMoving);
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
        if (Time.time - lastAttackTime < attackCooldown) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            isAttacking = true;
            attackAnimationTimer = attackAnimationDuration;
            attackDamageApplied = false;
            lastAttackTime = Time.time;

            if (animator != null)
            {
                animator.SetBool("IsAttacking", true);
            }
        }
    }

    private void TryApplyAttackDamage()
    {
        if (attackDamageApplied) return;
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= attackRange)
        {
            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
                attackDamageApplied = true;
            }
        }
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
        Color flash = Color.Lerp(baseSolid, flashTint, 0.6f); // 알파 변경 없이 색상만 블렌드

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

        RaycastHit2D upHit = Physics2D.Raycast(origin, Vector2.up, verticalCheckMaxDistance, groundLayer);
        if (debugDog)
        {
            Debug.DrawLine(origin, origin + Vector2.up * verticalCheckMaxDistance, upHit.collider == null ? Color.green : Color.yellow, 0.2f);
        }
        if (upHit.collider == null)
            return; // free space above, no need to bypass

        forwardRunDir = facingX;
        forwardRunUntil = Time.time + forwardBypassDuration;
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

        // 텍스트 위치 설정 - padding을 기존 x 값에 추가
        var textPos = healthTextLocalPosition;
        float padding = healthTextPadding;
        textPos.x = textPos.x + padding;  // 기존 x 값에 패딩을 더함
        textPos.z = Mathf.Approximately(textPos.z, 0f) ? -2f : textPos.z;

        int textSortingOffset = healthTextSortingOffset <= 0 ? 50 : healthTextSortingOffset;

        textGO = new GameObject("HealthText");
        textGO.transform.SetParent(heartGO.transform);
        textGO.transform.localPosition = textPos;
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
        Debug.Log($"[Dog] Health UI 생성 - Heart: {heartGO.name}, Text: {healthText.text}, Pos: {textGO.transform.position}, Sorting:{textRenderer?.sortingOrder}");
    }

    private void UpdateHealthUI()
    {
        if (healthUiHidden) return;

        if (heartSprite != null && heartRenderer == null)
        {
            CreateHealthUI();
        }

        bool show = true;

        if (heartGO != null)
        {
            heartGO.SetActive(show);
        }
        if (textGO != null)
        {
            textGO.SetActive(show);
            var textPos = healthTextLocalPosition;
            textPos.x += healthTextPadding;
            textPos.z = Mathf.Approximately(textPos.z, 0f) ? -2f : textPos.z;
            textGO.transform.localPosition = textPos;
        }

        if (healthText != null)
        {
            float hpValue = Mathf.Max(0f, currentHealth);
            healthText.text = hpValue.ToString("0.##");
            Debug.Log($"[Dog] Health UI update - HP: {hpValue}, Show: {show}, TextActive: {textGO?.activeSelf}");
        }

        UpdateHealthUiAlphaState();
    }

    private void SpawnDamageText(float damage)
    {
        StartCoroutine(DamageTextRoutine(damage));
    }

    private IEnumerator DamageTextRoutine(float damage)
    {
        var textGO = new GameObject("DogDamageText");
        textGO.transform.SetParent(transform);
        textGO.transform.localPosition = damageTextOffset;
        textGO.transform.localRotation = Quaternion.identity;
        textGO.transform.localScale = Vector3.one;

        var textMesh = textGO.AddComponent<TextMesh>();
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

        var renderer = textGO.GetComponent<MeshRenderer>();
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
            // Ease in-out: sin curve로 살짝 올랐다가 내려오게
            float vertical = Mathf.Sin(t * Mathf.PI) * damageTextAmplitude;
            textGO.transform.localPosition = baseLocalPos + new Vector3(0f, vertical, 0f);

            // 알파 서서히 감소
            var c = textMesh.color;
            c.a = 1f - t;
            textMesh.color = c;

            yield return null;
        }

        Destroy(textGO);
    }

    public override void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (!isAlive) return;

        // 무적 ?�간 체크 추�?
        if (Time.time - lastDamageTime < invincibilityTime)
        {
            Debug.Log($"[Dog] 무적 ?�간 �??��?지 무시 (?��? ?�간: {invincibilityTime - (Time.time - lastDamageTime):F2}s)");
            return;
        }

        // 무적 ?�간 ?�데?�트
        lastDamageTime = Time.time;

        StartDamageFlash();

        if (!healthUiInitialized)
        {
            CreateHealthUI();
        }

        currentHealth -= damage;
        if (currentHealth < 0f) currentHealth = 0f;
        SpawnDamageText(damage);
                StartDamageFlash();
        bool lethalHit = currentHealth <= 0f;

        Debug.Log($"[Dog] ?��?지 받음! {damage} (?�재 체력: {currentHealth}/{maxHealth})");

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

        // Player 스크래치 등에 의한 넉백은 비활성화
    }

    protected override void Die()
    {
        if (!isAlive) return;
        isAlive = false;

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

        if (aiPath != null)
        {
            aiPath.canMove = false;
            aiPath.isStopped = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // fully stop
        }

        // Disable all colliders to prevent further interaction
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
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
}
