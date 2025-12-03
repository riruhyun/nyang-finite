using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private enum AnimationState
    {
        Idle,
        Walk,
        Jump,
        Die,
        Dash,
        Scratch,
        Punch
    }

    [Header("Audio Settings")]
    public AudioClip deathClip;
    public AudioClip damageClip; // ?��?지 받을 ???�운??
    [SerializeField] private AudioClip scratchAudioClip;

    [Header("Health Settings")]
    public float maxHealth = 9f; // 최�? 체력
    private float currentHealth; // ?�재 체력
    public float invincibilityTime = 1.5f; // 무적 ?�간
    private float lastDamageTime = -10f; // 마�?�??��?지�?받�? ?�간

    [Header("Movement Settings")]
    public float jumpForce = 35f;
    private float baseJumpForce;
    public float moveSpeed = 5f;
    private float baseMoveSpeed;
    [SerializeField] private float jumpHoldForcePerSecond = 1300f; // 추가 점프 힘 (1초 기준)

    [Header("Toast Stat Modifiers")]
    private float currentDefense = 0f;         // defense: 받는 피해 감소
    private float currentThorns = 0f;          // thorns: 반격 데미지
    private float currentNutrition = 0f;       // nutrition: 음식 회복 보너스
    private float currentFriction = 0f;        // friction: 감속 증가
    private float currentHaste = 0f;           // haste: 쿨다운 감소
    private float currentAgility = 0f;         // agility: momentum build time 감소
    private float currentSprint = 0f;          // sprint: base/max momentum 증가
    private float currentPoise = 0f;           // poise: 넉백 거리 감소 (받는 넉백)
    private float currentInvincibility = 0f;   // invincibility: 무적 시간 증가
    private float currentAttack = 0f;          // attack: 공격력 증가
    private float currentStaminaRegen = 0f;    // stamina_regen: 스태미나 회복 속도 증가
    private float currentKnockback = 0f;       // knockback: 넉백력 증가 (주는 넉백)
    private float currentDashForce = 0f;       // dashForce: Dash 힘 증가
    private float currentDashRange = 0f;       // dashRange: Dash 거리 증가
    private float currentReap = 0f;            // reap: 처치 시 회복 확률
    [Header("Slope Handling")]
    [SerializeField] private float slopeNormalMin = 0.4f; // 경사�?55???�함) ?�상?�면 바닥 취급
    [SerializeField] private float slopeSpeedMultiplier = 1.2f;
    [SerializeField] private float flatFriction = 0.3f;
    [SerializeField] private float slopeFriction = 0f;
    [SerializeField] private float flatNormalThreshold = 0.95f;

    [Header("Input Override")]
    [SerializeField] private MonoBehaviour inputSourceBehaviour;
    private IPlayerInputSource inputSource;

    [Header("Momentum Settings")]
    public float baseMomentum = 0.5f;
    public float maxMomentum = 2.0f;
    public float momentumBuildTime = 1.0f;
    public float momentumDecayAmount = 2.5f;

    [Header("Animation Settings")]
    public float walkMomentumThreshold = 0.5f;
    public float dashDuration = 0.5f; // Dash 애니메이션 지속 시간
    public float dashForce = 3f; // Dash 임펄스 힘
    public float dashCooldown = 1.5f; // Dash 쿨다운 시간

    [Header("Dash Combat Settings")]
    [SerializeField] private BoxCollider2D dashHitbox;
    [SerializeField] private LayerMask dashDamageLayers;
    [SerializeField] private float dashDamage = 3f;
    [SerializeField] private float dashStaminaCost = 5f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Scratch Settings")]
    [SerializeField] private BoxCollider2D scratchHitbox;
    [SerializeField] private LayerMask scratchDamageLayers;
    [SerializeField] private float scratchDamage = 1.5f;
    [SerializeField] private float scratchDuration = 0.25f; // Target ~0.25s total
    [SerializeField] private string scratchAnimationName = "Scratch";
    [SerializeField] private float scratchStaminaCost = 3f;

    [Header("Punch Settings")]
    [SerializeField] private BoxCollider2D punchHitbox;
    [SerializeField] private LayerMask punchDamageLayers;
    [SerializeField] private float punchDamage = 1f;
    [SerializeField] private float punchKnockbackForce = 3f; // Base knockback force
    [SerializeField] private float punchStaminaCost = 2f;

    [Header("Wall Jump Settings")]
    [SerializeField] private float wallJumpVerticalSpeed = 12f; // 벽차기 수직 속도
    [SerializeField] private float wallJumpHorizontalSpeed = 8f; // 벽차기 수평 속도 (반동)
    [SerializeField] private float wallSlideSpeed = 2f; // 벽에서 미끄러지는 최대 속도
    [SerializeField] private float wallJumpCooldown = 0.35f; // 벽차기 쿨다운
    [SerializeField] private float wallJumpControlLockTime = 0.2f; // 벽차기 후 조작 제한 시간

    // 벽차기 상태
    private bool isWallSliding = false;
    private float lastWallJumpTime = -1f;

    // ==================== 스테이지 자동 전환 ====================
    // Ground1의 오른쪽 끝에 도달하면 다음 스테이지로 이동
    private float stageEndX = float.MaxValue;  // Ground1의 오른쪽 끝 x좌표
    private bool stageTransitionTriggered = false;  // 중복 전환 방지
    [Header("Punch Settings")]
    [SerializeField] private float punchDuration = 0.3f; // Punch 애니메이션 지속 시간
    [SerializeField] private string punchAnimationName = "Punch";

    // 물리 ?�태
    private bool isGrounded = false;
    private bool isDead = false;
    private bool onSlope = false;
    private bool isScratching = false;

    // 관???�스??
    private float leftMomentum = 0f;
    private float rightMomentum = 0f;
    private float leftKeyHoldTime = 0f;
    private float rightKeyHoldTime = 0f;

    // �?충돌 ?�태
    private bool isCollidingLeftWall = false;
    private bool isCollidingRightWall = false;

    // ?�프 grace time (?�프 직후 착�? 감�? 무시)
    private float jumpGraceTime = 0.1f;
    [SerializeField] private float jumpCooldown = 0.25f;
    private float lastJumpTime = -1f;

    // ?�니메이???�태 관�?
    private AnimationState currentAnimState = AnimationState.Idle;
    private AnimationState previousAnimState = AnimationState.Idle;
    private float animationTransitionDelay = 0.1f;
    private float lastAnimationChangeTime = 0f;
    private float pendingJumpResumeNormalizedTime = -1f;
    private bool useLegacyJumpForceScaling = false;

    // Dash 상태 관리
    private bool isDashing = false;
    private float dashStartTime = 0f;
    private float lastDashTime = -10f; // 마지막 Dash 시간

    // Punch 상태 관리
    private bool isPunching = false;
    private float punchEndTime = -1f;
    private float punchStartTime = -1f;

    // 컴포?�트
    private Rigidbody2D playerRigidbody;
    private Animator animator;
    private AudioSource playerAudio;
    private SpriteRenderer playerSprite;
    private Coroutine damageFlashRoutine;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 1f); // 알파 변경 없이 순수 빨강 틴트
    [SerializeField] private float damageFlashDuration = 0.1f;
    [SerializeField] private int damageFlashCount = 2;
    private Color playerBaseColor = Color.white;
    private string activeToastId = null;
    public string CurrentToastId => activeToastId;

    [Header("Toast Visuals")]
    [SerializeField] private ToastIndicator playerToastIndicator;
    [SerializeField] private SpriteRenderer playerToastIndicatorRenderer;
    [SerializeField] private ToastProfileFollower[] toastProfileFollowers;
    [SerializeField] private ToastProfileSprite[] toastProfileSprites;
    [SerializeField] private ToastProfileUI[] toastProfileUIElements;
    private ToastIndicator.ToastType? currentToastType = null;

    /// <summary>
    /// 플레이어가 현재 공격 중인지 확인 (Scratch, Punch, Dash)
    /// 비둘기 등의 적이 토스트 탈취 시 공격 판정에 사용
    /// </summary>
    public bool IsAttacking()
    {
        return isScratching || isPunching || isDashing;
    }

    // ==================== 상태 저장/복원 (스테이지 전환용) ====================

    /// <summary>
    /// 플레이어 상태를 저장하기 위한 구조체
    /// </summary>
    [System.Serializable]
    public struct PlayerSaveData
    {
        public float health;
        public string toastId;
        public float moveSpeedBonus;
        public float jumpForceBonus;
        public float defense;
        public float thorns;
        public float nutrition;
        public float friction;
        public float haste;
        public float agility;
        public float sprint;
        public float poise;
        public float invincibility;
        public float attack;
        public float staminaRegen;
        public float knockback;
        public float dashForce;
        public float dashRange;
        public float reap;
    }

    /// <summary>
    /// 현재 플레이어 상태를 저장 데이터로 반환
    /// </summary>
    public PlayerSaveData GetSaveData()
    {
        return new PlayerSaveData
        {
            health = currentHealth,
            toastId = activeToastId ?? "",
            moveSpeedBonus = moveSpeed - baseMoveSpeed,
            jumpForceBonus = jumpForce - baseJumpForce,
            defense = currentDefense,
            thorns = currentThorns,
            nutrition = currentNutrition,
            friction = currentFriction,
            haste = currentHaste,
            agility = currentAgility,
            sprint = currentSprint,
            poise = currentPoise,
            invincibility = currentInvincibility,
            attack = currentAttack,
            staminaRegen = currentStaminaRegen,
            knockback = currentKnockback,
            dashForce = currentDashForce,
            dashRange = currentDashRange,
            reap = currentReap
        };
    }

    /// <summary>
    /// 저장된 데이터로 플레이어 상태 복원
    /// </summary>
    public void LoadSaveData(PlayerSaveData data)
    {
        // 체력 복원
        currentHealth = data.health;
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHealth(currentHealth);
        }

        // 토스트 스탯 복원
        activeToastId = string.IsNullOrEmpty(data.toastId) ? null : data.toastId;
        moveSpeed = baseMoveSpeed + data.moveSpeedBonus;
        jumpForce = baseJumpForce + data.jumpForceBonus;
        currentDefense = data.defense;
        currentThorns = data.thorns;
        currentNutrition = data.nutrition;
        currentFriction = data.friction;
        currentHaste = data.haste;
        currentAgility = data.agility;
        currentSprint = data.sprint;
        currentPoise = data.poise;
        currentInvincibility = data.invincibility;
        currentAttack = data.attack;
        currentStaminaRegen = data.staminaRegen;
        currentKnockback = data.knockback;
        currentDashForce = data.dashForce;
        currentDashRange = data.dashRange;
        currentReap = data.reap;

        // StaminaManager에 stamina_regen 보너스 적용
        if (StaminaManager.instance != null && currentStaminaRegen != 0f)
        {
            StaminaManager.instance.ApplyStaminaRegenBonus(currentStaminaRegen);
        }

        Debug.Log($"[PlayerController] 상태 복원 완료: HP={currentHealth}, Toast={activeToastId}");
    }

    /// <summary>
    /// 토스트를 제거 (비둘기에게 빼앗겼을 때 호출)
    /// </summary>
    public void RemoveToast()
    {
        if (string.IsNullOrEmpty(activeToastId))
        {
            Debug.Log("[PlayerController] 제거할 토스트가 없습니다.");
            return;
        }

        string removedToastId = activeToastId;

        // 토스트 스탯 초기화
        moveSpeed = baseMoveSpeed;
        jumpForce = baseJumpForce;
        currentDefense = 0f;
        currentThorns = 0f;
        currentNutrition = 0f;
        currentFriction = 0f;
        currentHaste = 0f;
        currentAgility = 0f;
        currentSprint = 0f;
        currentPoise = 0f;
        currentInvincibility = 0f;
        currentAttack = 0f;
        currentStaminaRegen = 0f;
        currentKnockback = 0f;
        currentDashForce = 0f;

        // StaminaManager stamina_regen 보너스 리셋
        if (StaminaManager.instance != null)
        {
            StaminaManager.instance.ApplyStaminaRegenBonus(0f);
        }

        activeToastId = null;
        UpdateToastVisuals(null);

        Debug.Log($"[PlayerController] 토스트 제거됨: {removedToastId}");

        // 모든 ToastHoverPanel 버튼 상태 업데이트
        RefreshAllToastPanelButtons();
    }

    [Header("Food Effect Text")]
    [SerializeField] private Vector3 foodEffectTextOffset = new Vector3(0f, 0.8f, -1f);
    [SerializeField] private float foodEffectTextDuration = 0.6f;
    [SerializeField] private float foodEffectTextAmplitude = 0.35f;
    [SerializeField] private int foodEffectTextFontSize = 60;
    [SerializeField] private float foodEffectTextCharacterSize = 0.05f;
    [SerializeField] private Color foodHealTextColor = Color.green;
    [SerializeField] private Color foodDamageTextColor = Color.red;
    [SerializeField] private Font foodEffectTextFont;
    private readonly Collider2D[] scratchHits = new Collider2D[8];
    private readonly Collider2D[] punchHits = new Collider2D[8];
    private readonly Collider2D[] dashHits = new Collider2D[8];
    private float accumulatedJumpForce = 0f; // 점프 중 누적된 힘
    private bool wasAirborneBeforeScratch = false;
    private float savedJumpNormalizedTime = 0f;
    private float savedJumpLength = 0.85f;
    private float scratchEndTime = -1f;
    private float scratchStartTime = -1f;
    private bool scratchStoppedAnimator = false;
    private AnimationClip scratchClip;
    private float scratchPlaySpeed = 1f;
    private AnimationClip punchClip;
    private PhysicsMaterial2D runtimeMaterial;
    private bool wasOnSlope = false; // ?�전 ?�레?�에 경사�??��??��? 추적

    // ?�니메이???�라미터 ?�시
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private const float LegacyJumpForceThreshold = 100f;

    private void Awake()
    {
        InitializeInputSource();
    }

    private void Start()
    {
        SetupColliders();
        playerRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();
        if (playerAudio == null)
        {
            playerAudio = GetComponentInChildren<AudioSource>();
        }
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
        {
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        }
        if (playerSprite != null)
        {
            playerBaseColor = playerSprite.color;
        }
        if (playerToastIndicator == null)
        {
            playerToastIndicator = GetComponentInChildren<ToastIndicator>(true);
        }
        if (playerToastIndicator != null && playerToastIndicatorRenderer == null)
        {
            playerToastIndicatorRenderer = playerToastIndicator.GetComponent<SpriteRenderer>();
        }
        useLegacyJumpForceScaling = jumpForce >= LegacyJumpForceThreshold;
        baseMoveSpeed = moveSpeed;
        baseJumpForce = jumpForce;
        if (useLegacyJumpForceScaling)
        {
            Debug.Log($"[JUMP] Legacy jumpForce detected ({jumpForce}). Using fixedDeltaTime scaling. Set jumpForce below {LegacyJumpForceThreshold} to switch to impulse units.");
        }
        CacheScratchClip();
        CachePunchClip();

        // ü�� �ʱ�ȭ
        currentHealth = maxHealth;
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHealth(currentHealth);
        }

        // 스테이지 끝 좌표 초기화 (Ground1 기준)
        InitializeStageEndPosition();

        if (animator == null)
        {
            Debug.LogError("Animator component not found on " + gameObject.name);
        }
        if (playerRigidbody == null)
        {
            Debug.LogError("Rigidbody2D component not found on " + gameObject.name);
        }

        SetAnimationState(AnimationState.Idle);
    }

    private void SetupColliders()
    {
        Transform oldHead = transform.Find("HeadCollider");
        Transform oldBody = transform.Find("BodyCollider");
        Transform oldFeet = transform.Find("FeetCollider");

        if (oldHead != null) DestroyImmediate(oldHead.gameObject);
        if (oldBody != null) DestroyImmediate(oldBody.gameObject);
        if (oldFeet != null) DestroyImmediate(oldFeet.gameObject);

        CapsuleCollider2D capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null)
        {
            capsuleCollider.size = new Vector2(0.12f, 0.18f);
            capsuleCollider.offset = new Vector2(0, 0f);
            capsuleCollider.direction = CapsuleDirection2D.Vertical;

            // Physics Material ?�성 (경사면에???�당??미끄?��??�록)
            if (capsuleCollider.sharedMaterial == null)
            {
                PhysicsMaterial2D physicsMat = new PhysicsMaterial2D("PlayerPhysics");
                physicsMat.friction = flatFriction;  // 기본 ?��? 마찰
                physicsMat.bounciness = 0f;
                capsuleCollider.sharedMaterial = physicsMat;
                runtimeMaterial = physicsMat;
            }
            else
            {
                runtimeMaterial = capsuleCollider.sharedMaterial;
                runtimeMaterial.friction = flatFriction;
            }
        }

        Debug.Log("?�레?�어 충돌�?최적???�료 (friction=0.3, 55??경사�?지??");
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        // 스테이지 끝에 도달했는지 체크
        CheckIfReachedStageEnd();

        // ★ Scratch 상태 먼저 처리 (최고 우선순위)
        UpdateScratchState();

        // 벽 슬라이드 상태 업데이트
        UpdateWallSlide();

        // ★ Punch 상태 처리
        UpdatePunchState();

        // Scratch나 Punch 중이면 다른 입력 처리 안함
        if (!isScratching && !isPunching)
        {
            HandleScratchInput();
            HandlePunchInput();
            HandleMovementInput();
            HandleJumpInput();
            HandleDashInput();
        }

        UpdateAnimationState();
    }

    private void HandleMovementInput()
    {
        bool isPressingLeft = inputSource.GetMoveLeft();
        bool isPressingRight = inputSource.GetMoveRight();

        // ?�어지???�태?��? ?�인 (공중 + ?�강)
        bool isFalling = !isGrounded && playerRigidbody.linearVelocity.y < -0.1f;

        // 벽차기 직후 조작 제한 (아크 궤적 유지)
        bool isWallJumpLocked = Time.time - lastWallJumpTime < wallJumpControlLockTime;

        // 왼쪽 키 처리 (왼쪽 벽에 붙어있거나 떨어지는 중이면 무시)
        if (isPressingLeft && !isCollidingLeftWall && !isFalling && !isWallJumpLocked)
        {
            leftKeyHoldTime += Time.deltaTime;
            // agility: momentum build time 감소, sprint: base/max momentum 증가
            float effectiveBuildTime = Mathf.Max(0.1f, momentumBuildTime - currentAgility);
            float effectiveBaseMomentum = baseMomentum + (currentSprint * 0.5f);
            float effectiveMaxMomentum = maxMomentum + currentSprint;
            float momentumProgress = Mathf.Clamp01(leftKeyHoldTime / effectiveBuildTime);
            leftMomentum = Mathf.Lerp(effectiveBaseMomentum, effectiveMaxMomentum, momentumProgress);
            transform.localScale = new Vector3(-5f, 5f, 5f);
        }
        else
        {
            if (leftMomentum > 0)
            {
                // friction: 감속 증가
                float effectiveDecay = momentumDecayAmount + currentFriction;
                leftMomentum -= effectiveDecay * Time.deltaTime * 10f;
                leftMomentum = Mathf.Max(leftMomentum, 0f);
            }
            leftKeyHoldTime = 0f;
        }

        // 오른쪽 키 처리 (오른쪽 벽에 붙어있거나 떨어지는 중이면 무시)
        if (isPressingRight && !isCollidingRightWall && !isFalling && !isWallJumpLocked)
        {
            rightKeyHoldTime += Time.deltaTime;
            // agility: momentum build time 감소, sprint: base/max momentum 증가
            float effectiveBuildTime = Mathf.Max(0.1f, momentumBuildTime - currentAgility);
            float effectiveBaseMomentum = baseMomentum + (currentSprint * 0.5f);
            float effectiveMaxMomentum = maxMomentum + currentSprint;
            float momentumProgress = Mathf.Clamp01(rightKeyHoldTime / effectiveBuildTime);
            rightMomentum = Mathf.Lerp(effectiveBaseMomentum, effectiveMaxMomentum, momentumProgress);
            transform.localScale = new Vector3(5f, 5f, 5f);
        }
        else
        {
            if (rightMomentum > 0)
            {
                // friction: 감속 증가
                float effectiveDecay = momentumDecayAmount + currentFriction;
                rightMomentum -= effectiveDecay * Time.deltaTime * 10f;
                rightMomentum = Mathf.Max(rightMomentum, 0f);
            }
            rightKeyHoldTime = 0f;
        }

        ApplyMovement();
    }

    private void ApplyMovement()
    {
        // Dash 중에는 이동 무시 (임펄스 보존)
        if (isDashing)
        {
            return;
        }

        float finalHorizontalSpeed = 0f;

        if (leftMomentum > 0)
        {
            finalHorizontalSpeed -= moveSpeed * leftMomentum;
        }

        if (rightMomentum > 0)
        {
            finalHorizontalSpeed += moveSpeed * rightMomentum;
        }

        if (onSlope && isGrounded)
        {
            finalHorizontalSpeed *= slopeSpeedMultiplier;
        }

        if (leftMomentum > 0 || rightMomentum > 0)
        {
            Vector2 movement = new Vector2(finalHorizontalSpeed, playerRigidbody.linearVelocity.y);
            playerRigidbody.linearVelocity = movement;
        }
    }

    private void HandleJumpInput()
    {
        if (isScratching)
        {
            return;
        }

        bool jumpHeldInput = inputSource.GetJumpHeld();
        bool jumpDownInput = inputSource.GetJumpDown();
        bool jumpUpInput = inputSource.GetJumpUp();

        if (jumpDownInput)
        {
            // 벽차기 체크 (공중에서 벽에 붙어있을 때)
            if (!isGrounded && (isCollidingLeftWall || isCollidingRightWall))
            {
                // 쿨다운 체크 (haste 적용)
                float effectiveWallJumpCooldown = Mathf.Max(0.05f, wallJumpCooldown - currentHaste);
                if (Time.time - lastWallJumpTime >= effectiveWallJumpCooldown)
                {
                    PerformWallJump();
                    return;
                }
            }

            bool slopeGround = IsOnJumpableSlope();

            // 경사면에서 y가 음수로 찍히면 점프 허용하고 있으면 아예 빼도 됨
            bool isFallingFromCliff = playerRigidbody.linearVelocity.y < -0.1f && IsOnFlatGround();

            bool canJump = (isGrounded || slopeGround) && !isFallingFromCliff;
            // haste 적용: jump 쿨다운 감소
            float effectiveJumpCooldown = Mathf.Max(0.05f, jumpCooldown - currentHaste);
            bool jumpOffCooldown = lastJumpTime < 0f || (Time.time - lastJumpTime) >= effectiveJumpCooldown;

            Debug.Log($"[JUMP INPUT] W! grounded={isGrounded}, onSlope={slopeGround}, velY={playerRigidbody.linearVelocity.y:F2}, canJump={canJump}, offCooldown={jumpOffCooldown}");

            if (canJump && jumpOffCooldown)
            {
                accumulatedJumpForce = 0f; // 점프 시작 시 초기화
                PerformJump();
            }
            else if (canJump && !jumpOffCooldown)
            {
                float wait = Mathf.Max(0f, jumpCooldown - (Time.time - lastJumpTime));
                Debug.Log($"[JUMP INPUT] Jump cooldown active ({wait:F2}s remaining)");
            }
        }
        else if (jumpHeldInput && !isGrounded && playerRigidbody.linearVelocity.y > 0)
        {
            // 점프 키를 누르고 있는 동안 추가 힘 적용 (스태미나 소모)
            float forceToAdd = jumpHoldForcePerSecond * Time.deltaTime; // 1초 동안 jumpHoldForcePerSecond 만큼 추가
            accumulatedJumpForce += forceToAdd;

            // 200 단위마다 스태미나 1 소모
            int staminaCostCount = Mathf.FloorToInt(accumulatedJumpForce / 200f);
            float requiredStamina = staminaCostCount;

            if (StaminaManager.instance != null)
            {
                if (StaminaManager.instance.GetCurrentStamina() >= requiredStamina)
                {
                    // 스태미나가 충분하면 힘 추가
                    playerRigidbody.AddForce(new Vector2(0, forceToAdd));

                    // 200 단위를 넘을 때마다 스태미나 소모
                    int previousStaminaCost = Mathf.FloorToInt((accumulatedJumpForce - forceToAdd) / 200f);
                    if (staminaCostCount > previousStaminaCost)
                    {
                        StaminaManager.instance.UseStamina(1f);
                        Debug.Log($"[JUMP] 스태미나 1 소모 (누적: {accumulatedJumpForce:F1})");
                    }
                }
                else
                {
                    // 스태미나 부족 시 키 입력 무시 (힘 추가 중단)
                    Debug.Log("[JUMP] 스태미나 부족으로 점프 힘 추가 중단!");
                }
            }
        }
        else if (jumpUpInput && playerRigidbody.linearVelocity.y > 0)
        {
            playerRigidbody.linearVelocity = new Vector2(
                playerRigidbody.linearVelocity.x,
                playerRigidbody.linearVelocity.y * 0.5f
            );
            accumulatedJumpForce = 0f; // 키를 떼면 리셋
        }
    }

    private void HandleDashInput()
    {
        // Q 키를 누르면 Dash 실행 (바라보는 방향으로 임펄스)
        if (inputSource.GetDashDown())
        {
            // Scratch/Punch 중에는 Dash 불가
            if (isScratching || isPunching)
            {
                Debug.Log($"[DASH] Dash 차단: isScratching={isScratching}, isPunching={isPunching}");
                return;
            }

            // 쿨다운 체크 (haste 적용)
            float effectiveDashCooldown = Mathf.Max(0.1f, dashCooldown - currentHaste);
            float timeSinceLastDash = Time.time - lastDashTime;
            if (timeSinceLastDash < effectiveDashCooldown)
            {
                Debug.Log($"[DASH] Dash 쿨다운 중: {effectiveDashCooldown - timeSinceLastDash:F2}초 남음");
                return;
            }

            // 스태미나 체크
            if (StaminaManager.instance != null && !StaminaManager.instance.UseStamina(dashStaminaCost))
            {
                Debug.Log($"[DASH] 스태미나 부족! 필요: {dashStaminaCost}");
                return;
            }

            // Dash 실행
            isDashing = true;
            dashStartTime = Time.time;
            lastDashTime = Time.time;
            SetAnimationState(AnimationState.Dash);

            // ★ 바라보는 방향으로 임펄스 적용 (DashForce + DashRange 스탯 적용)
            float dashDirection = transform.localScale.x > 0 ? 1f : -1f; // 오른쪽: 1, 왼쪽: -1
            float effectiveDashForce = dashForce + currentDashForce + currentDashRange;
            if (playerRigidbody != null)
            {
                playerRigidbody.AddForce(new Vector2(dashDirection * effectiveDashForce, 0), ForceMode2D.Impulse);
            }

            // GarbageCan도 대시 시작 시 타격 처리
            TryHitGarbageCanForward();

            // Enemy 데미지 처리
            ApplyDashDamage();

            Debug.Log($"[DASH] Dash 실행! 방향={dashDirection}, 힘={effectiveDashForce}, velocity={playerRigidbody?.linearVelocity}");
        }

        // Dash 지속 시간이 지나면 Dash 상태 해제
        if (isDashing && Time.time - dashStartTime >= dashDuration)
        {
            isDashing = false;
            // 애니메이션 즉시 전환 허용
            lastAnimationChangeTime = Time.time - animationTransitionDelay;
            SetAnimationState(DetermineAnimationState());
            Debug.Log("[DASH] Dash 종료!");
        }

        if (isDashing)
        {
            TryHitGarbageCanForward(1.2f); // 필요 시 더 키워도 됩니다.
        }
    }

    private Vector2 GetFeetPos()
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            return (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y + 0.02f);
        }
        // ?�시 콜라?�더 ?�으�??��?
        return (Vector2)transform.position + Vector2.down * 0.15f;
    }

    /// <summary>
    /// 55???�하 경사�??�에 ?�으�?true
    /// </summary>
    private bool IsOnJumpableSlope()
    {
        Vector2 origin = GetFeetPos();
        float dist = 0.6f; // ?�짝 ?�유

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            dist,
            groundLayer
        );

        // ?�버�?찍어보면 바로 ?�낌 ??
        if (hit.collider != null)
        {
            float ny = hit.normal.y;
            // 55???�하 ??normal.y > cos(55°) ??0.57
            bool ok = ny > 0.57f;
            // Debug.DrawRay(origin, Vector2.down * dist, ok ? Color.green : Color.yellow, 0.2f);
            return ok;
        }

        return false;
    }


    private bool IsOnFlatGround()
    {
        Vector2 origin = GetFeetPos();
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            0.25f,
            groundLayer
        );

        if (hit.collider != null)
        {
            return hit.normal.y > 0.95f;
        }

        return false;
    }


    private float CalculateJumpImpulse()
    {
        float legacyImpulse = jumpForce;
        if (useLegacyJumpForceScaling)
        {
            legacyImpulse = jumpForce * Time.fixedDeltaTime;
        }
        return Mathf.Max(0.01f, legacyImpulse);
    }

    private void PerformJump()
    {
        // 스태미나가 1 이상이어야 점프 가능
        if (StaminaManager.instance != null && StaminaManager.instance.GetCurrentStamina() < 1f)
        {
            Debug.Log("[JUMP] 스태미나 부족으로 점프 불가!");
            return;
        }

        isGrounded = false;
        lastJumpTime = Time.time;
        accumulatedJumpForce = 0f; // 점프 시작 시 초기화

        playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, 0);
        float jumpImpulse = CalculateJumpImpulse();
        playerRigidbody.AddForce(new Vector2(0, jumpImpulse), ForceMode2D.Impulse);

        if (playerAudio != null)
        {
            playerAudio.Play();
        }

        SetAnimationState(AnimationState.Jump);
        Debug.Log($"[JUMP] 점프 실행! Time={Time.time:F3}");
    }

    /// <summary>
    /// 벽차기 수행 - 벽 반대 방향으로 아크 점프처럼 대각선으로 튀어오름
    /// </summary>
    private void PerformWallJump()
    {
        lastWallJumpTime = Time.time;
        isWallSliding = false;

        // 벽 방향에 따라 반대 방향으로 점프
        float horizontalDirection = isCollidingLeftWall ? 1f : -1f;

        // 속도를 직접 설정하여 즉각적인 아크 점프 느낌
        playerRigidbody.linearVelocity = new Vector2(
            wallJumpHorizontalSpeed * horizontalDirection,
            wallJumpVerticalSpeed
        );

        // 스프라이트 방향 전환 (벽차기 방향으로)
        transform.localScale = new Vector3(horizontalDirection * 5f, 5f, 5f);

        // 모멘텀 초기화
        leftMomentum = 0f;
        rightMomentum = 0f;
        leftKeyHoldTime = 0f;
        rightKeyHoldTime = 0f;

        // 벽 충돌 상태 초기화
        isCollidingLeftWall = false;
        isCollidingRightWall = false;

        if (playerAudio != null)
        {
            playerAudio.Play();
        }

        SetAnimationState(AnimationState.Jump);
        Debug.Log($"[WALL JUMP] 벽차기 수행! direction={horizontalDirection}, velocity=({wallJumpHorizontalSpeed * horizontalDirection}, {wallJumpVerticalSpeed})");

        // 튜토리얼 매니저에 벽점프 알림
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnWallJumpPerformed();
        }
    }

    /// <summary>
    /// 벽 슬라이드 상태 업데이트 - 벽에 붙어있을 때 천천히 미끄러지도록
    /// </summary>
    private void UpdateWallSlide()
    {
        // 벽 슬라이드 조건: 공중 + 벽에 붙어있음 + 하강 중
        bool canWallSlide = !isGrounded &&
                            (isCollidingLeftWall || isCollidingRightWall) &&
                            playerRigidbody.linearVelocity.y < 0;

        if (canWallSlide)
        {
            if (!isWallSliding)
            {
                isWallSliding = true;
                Debug.Log($"[WALL SLIDE] 벽 슬라이드 시작! leftWall={isCollidingLeftWall}, rightWall={isCollidingRightWall}");
            }

            // 하강 속도 제한 (천천히 미끄러지도록)
            if (playerRigidbody.linearVelocity.y < -wallSlideSpeed)
            {
                playerRigidbody.linearVelocity = new Vector2(
                    playerRigidbody.linearVelocity.x,
                    -wallSlideSpeed
                );
            }
        }
        else
        {
            if (isWallSliding)
            {
                isWallSliding = false;
                Debug.Log("[WALL SLIDE] 벽 슬라이드 종료");
            }
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null || isDead) return;

        // If not scratching but animator somehow sits on Scratch, snap back
        if (!isScratching)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(scratchAnimationName))
            {
                AnimationState safeState = DetermineAnimationState();
                if (safeState != AnimationState.Scratch)
                {
                    SetAnimationState(safeState);
                }
            }
        }

        // If not punching but animator somehow sits on Punch, snap back
        if (!isPunching)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(punchAnimationName))
            {
                AnimationState safeState = DetermineAnimationState();
                if (safeState != AnimationState.Punch)
                {
                    SetAnimationState(safeState);
                }
            }
        }

        // Scratch 중일 때는 애니메이션 상태 변경 완전 차단
        if (isScratching)
        {
            // Scratch 상태가 아니라면 강제로 Scratch로 전환
            if (currentAnimState != AnimationState.Scratch)
            {
                SetAnimationState(AnimationState.Scratch);
            }
            return;
        }

        // Punch 중일 때는 애니메이션 상태 변경 완전 차단
        if (isPunching)
        {
            // Punch 상태가 아니라면 강제로 Punch로 전환
            if (currentAnimState != AnimationState.Punch)
            {
                SetAnimationState(AnimationState.Punch);
            }
            return;
        }

        AnimationState targetState = DetermineAnimationState();

        if (targetState != currentAnimState)
        {
            if (Time.time - lastAnimationChangeTime >= animationTransitionDelay)
            {
                SetAnimationState(targetState);
            }
        }
    }

    private AnimationState DetermineAnimationState()
    {
        if (isDead)
        {
            return AnimationState.Die;
        }

        // Scratch 중일 때는 무조건 Scratch 상태 유지
        if (isScratching)
        {
            return AnimationState.Scratch;
        }

        // Punch 중일 때는 무조건 Punch 상태 유지
        if (isPunching)
        {
            return AnimationState.Punch;
        }

        // Dash 중일 때는 Dash 상태 유지
        if (isDashing)
        {
            return AnimationState.Dash;
        }

        if (!isGrounded)
        {
            return AnimationState.Jump;
        }

        float maxMomentum = Mathf.Max(leftMomentum, rightMomentum);
        if (maxMomentum > walkMomentumThreshold)
        {
            return AnimationState.Walk;
        }

        return AnimationState.Idle;
    }

    private void SetAnimationState(AnimationState newState)
    {
        if (animator == null || newState == currentAnimState) return;

        previousAnimState = currentAnimState;
        currentAnimState = newState;
        lastAnimationChangeTime = Time.time;

        animator.ResetTrigger(JumpHash);
        animator.ResetTrigger(DieHash);
        animator.ResetTrigger(DashHash);

        switch (currentAnimState)
        {
            case AnimationState.Idle:
                animator.SetBool(GroundedHash, true);
                animator.SetBool(IsMovingHash, false);
                break;

            case AnimationState.Walk:
                animator.SetBool(GroundedHash, true);
                animator.SetBool(IsMovingHash, true);
                break;

            case AnimationState.Jump:
                animator.SetBool(GroundedHash, false);
                animator.SetBool(IsMovingHash, false);
                if (pendingJumpResumeNormalizedTime >= 0f)
                {
                    float resume = Mathf.Repeat(pendingJumpResumeNormalizedTime, 1f);
                    animator.Play("Jump", 0, resume);
                    pendingJumpResumeNormalizedTime = -1f;
                }
                else
                {
                    animator.SetTrigger(JumpHash);
                }
                break;

            case AnimationState.Die:
                animator.SetBool(GroundedHash, false);
                animator.SetBool(IsMovingHash, false);
                animator.SetTrigger(DieHash);
                break;

            case AnimationState.Dash:
                animator.SetTrigger(DashHash);
                break;

            case AnimationState.Scratch:
                // ★ Scratch는 Play()로 직접 재생하여 Animator 상태 머신 우회
                // ★ 모든 Bool 파라미터를 초기화하여 자동 전환 방지
                animator.SetBool(GroundedHash, false);
                animator.SetBool(IsMovingHash, false);
                animator.Play(scratchAnimationName, 0, 0f);
                scratchStoppedAnimator = false;
                Debug.Log($"[SCRATCH ANIM] Scratch 애니메이션 강제 재생 시작");
                break;

            case AnimationState.Punch:
                // ★ Punch는 Play()로 직접 재생
                animator.SetBool(GroundedHash, false);
                animator.SetBool(IsMovingHash, false);
                animator.Play(punchAnimationName, 0, 0f);
                Debug.Log($"[PUNCH ANIM] Punch 애니메이션 재생 시작");
                break;
        }
    }

    // ?��?지�?받는 메서??
    private void StartDamageFlash()
    {
        if (playerSprite == null) return;
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            playerSprite.color = new Color(playerBaseColor.r, playerBaseColor.g, playerBaseColor.b, 1f);
            damageFlashRoutine = null;
        }
        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (playerSprite == null) yield break;
        playerBaseColor = playerSprite.color;
        Color baseSolid = new Color(playerBaseColor.r, playerBaseColor.g, playerBaseColor.b, 1f);
        Color flashTint = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, 1f);
        Color flash = Color.Lerp(baseSolid, flashTint, 0.6f); // 알파 건드리지 않고 색상만 블렌드

        for (int i = 0; i < damageFlashCount; i++)
        {
            playerSprite.color = flash;
            yield return new WaitForSeconds(damageFlashDuration * 0.5f);
            playerSprite.color = baseSolid;
            yield return new WaitForSeconds(damageFlashDuration * 0.5f);
        }

        playerSprite.color = baseSolid;
        damageFlashRoutine = null;
    }

    // 외부에서 플레이어 틴트 플래시를 강제로 실행할 때 사용
    public void PlayDamageFlash()
    {
        StartDamageFlash();
    }

    public void TakeDamage(float damage, GameObject attacker = null, float knockbackForce = 0f)
    {
        // 무적 시간 체크 (invincibility stat 적용)
        float effectiveInvincibilityTime = invincibilityTime + currentInvincibility;
        if (Time.time - lastDamageTime < effectiveInvincibilityTime)
        {
            Debug.Log($"무적 시간 중 데미지 무시 (효과적 무적시간: {effectiveInvincibilityTime:F2}s)");
            return;
        }

        if (isDead)
        {
            return;
        }

        // Defense 적용: 받는 피해 감소
        float finalDamage = Mathf.Max(0f, damage - currentDefense);

        // 데미지 적용
        currentHealth -= finalDamage;
        lastDamageTime = Time.time;
        StartDamageFlash();

        Debug.Log($"플레이어 데미지 받음! 원본:{damage:F1}, 최종:{finalDamage:F1} (방어:{currentDefense:F1}), 현재 체력: {currentHealth}/{maxHealth}");

        // Poise 적용: 넉백 저항 (받는 넉백 감소)
        if (knockbackForce > 0f && attacker != null && playerRigidbody != null)
        {
            float effectiveKnockback = Mathf.Max(0f, knockbackForce - currentPoise);
            Vector2 knockbackDir = (transform.position - attacker.transform.position).normalized;
            playerRigidbody.AddForce(knockbackDir * effectiveKnockback, ForceMode2D.Impulse);
            Debug.Log($"[Poise] 넉백: 원본={knockbackForce:F1}, 감소 후={effectiveKnockback:F1} (poise={currentPoise:F1})");
        }

        // Thorns 적용: 반격 데미지
        if (currentThorns > 0f && attacker != null)
        {
            var enemy = attacker.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(currentThorns, Vector2.zero);
                Debug.Log($"[Thorns] 반격 데미지 {currentThorns:F1} → {attacker.name}");
            }
        }

        // 데미지 사운드 재생
        if (playerAudio != null && damageClip != null)
        {
            playerAudio.PlayOneShot(damageClip);
        }

        // GameManager에 체력 업데이트 알림
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHealth(currentHealth);
        }

        // 체력??0 ?�하�??�망
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        SetAnimationState(AnimationState.Die);

        if (playerAudio != null && deathClip != null)
        {
            playerAudio.clip = deathClip;
            playerAudio.Play();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.bodyType = RigidbodyType2D.Static;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnPlayerDead();
        }
    }

    // ==================== Food Effects ====================

    /// <summary>
    /// 음식 데미지: 체력이 1 아래로 내려가지 않도록 clamps.
    /// </summary>
    public float TakeFoodDamage(float damage)
    {
        if (isDead || damage <= 0f) return 0f;
        float before = currentHealth;
        currentHealth -= damage;
        if (currentHealth < 1f) currentHealth = 1f; // 음식 데미지로는 사망하지 않음
        if (GameManager.instance != null) GameManager.instance.UpdateHealth(currentHealth);
        float applied = before - currentHealth;
        SpawnFoodEffectText($"-{applied:0.#}", foodDamageTextColor);
        return applied;
    }

    /// <summary>
    /// 음식 회복: 최대체력까지 회복 (nutrition stat 적용)
    /// </summary>
    public float Heal(float amount)
    {
        if (isDead || amount <= 0f) return 0f;
        float before = currentHealth;
        // nutrition: 음식 회복 시 추가 회복
        float effectiveHeal = amount + currentNutrition;
        currentHealth = Mathf.Min(maxHealth, currentHealth + effectiveHeal);
        if (GameManager.instance != null) GameManager.instance.UpdateHealth(currentHealth);
        float applied = currentHealth - before;
        SpawnFoodEffectText($"+{applied:0.#}", foodHealTextColor);
        Debug.Log($"[Heal] 회복량: {amount:F1} + nutrition:{currentNutrition:F1} = {effectiveHeal:F1}, 실제 적용: {applied:F1}");
        return applied;
    }

    /// <summary>
    /// Reap: 적 처치 시 확률적으로 회복
    /// </summary>
    public void OnEnemyKilled()
    {
        if (currentReap <= 0f) return;

        // (5 + n*5)% 확률로 n/2만큼 회복
        float reapChance = 5f + (currentReap * 5f);
        float roll = Random.Range(0f, 100f);

        if (roll <= reapChance)
        {
            float healAmount = currentReap * 0.5f;
            float before = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
            if (GameManager.instance != null) GameManager.instance.UpdateHealth(currentHealth);
            float applied = currentHealth - before;
            SpawnFoodEffectText($"+{applied:0.#} Reap!", foodHealTextColor);
            Debug.Log($"[Reap] 적 처치 회복 발동! 확률: {reapChance:F1}%, 회복량: {applied:F1}");
        }
    }

    private void SpawnFoodEffectText(string text, Color color)
    {
        StartCoroutine(FoodEffectTextRoutine(text, color));
    }

    private IEnumerator FoodEffectTextRoutine(string text, Color color)
    {
        var go = new GameObject("FoodEffectText_Player");
        go.transform.SetParent(transform);
        go.transform.localPosition = foodEffectTextOffset;
        go.transform.localRotation = Quaternion.identity;
        // 부모 스케일이 뒤집혀도 텍스트는 정방향으로 보이도록 보정
        float flipX = Mathf.Sign(transform.lossyScale.x) < 0 ? -1f : 1f;
        go.transform.localScale = new Vector3(flipX, 1f, 1f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = foodEffectTextCharacterSize;
        tm.fontSize = foodEffectTextFontSize;
        tm.color = color;
        tm.richText = false;
        tm.font = foodEffectTextFont != null ? foodEffectTextFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = 5100;
            renderer.material = tm.font != null ? tm.font.material : new Material(Shader.Find("GUI/Text Shader"));
        }

        Vector3 basePos = foodEffectTextOffset;
        float elapsed = 0f;
        while (elapsed < foodEffectTextDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / foodEffectTextDuration);
            float vertical = Mathf.Sin(t * Mathf.PI) * foodEffectTextAmplitude;
            go.transform.localPosition = basePos + new Vector3(0f, vertical, 0f);

            var c = tm.color;
            c.a = 1f - t;
            tm.color = c;
            yield return null;
        }

        Destroy(go);
    }

    // ==================== Toast Effects ====================
    public void ApplyToastStats(System.Collections.Generic.List<RuntimeStat> stats, string toastId)
    {
        // 기본값으로 리셋
        moveSpeed = baseMoveSpeed;
        jumpForce = baseJumpForce;
        currentDefense = 0f;
        currentThorns = 0f;
        currentNutrition = 0f;
        currentFriction = 0f;
        currentHaste = 0f;
        currentAgility = 0f;
        currentSprint = 0f;
        currentPoise = 0f;
        currentInvincibility = 0f;
        currentAttack = 0f;
        currentStaminaRegen = 0f;
        currentKnockback = 0f;
        currentDashForce = 0f;
        currentDashRange = 0f;
        currentReap = 0f;

        if (stats != null)
        {
            foreach (var s in stats)
            {
                switch (s.statType)
                {
                    case StatType.Speed:
                        moveSpeed += s.value;
                        break;
                    case StatType.Jump:
                        jumpForce += s.value;
                        break;
                    case StatType.StaminaRegen:
                        currentStaminaRegen += s.value;
                        break;
                    case StatType.Attack:
                        currentAttack += s.value;
                        break;
                    case StatType.Defense:
                        currentDefense += s.value;
                        break;
                    case StatType.Invincibility:
                        currentInvincibility += s.value;
                        break;
                    case StatType.Haste:
                        currentHaste += s.value;
                        break;
                    case StatType.Agility:
                        currentAgility += s.value;
                        break;
                    case StatType.Sprint:
                        currentSprint += s.value;
                        break;
                    case StatType.Poise:
                        currentPoise += s.value;
                        break;
                    case StatType.Thorns:
                        currentThorns += s.value;
                        break;
                    case StatType.Nutrition:
                        currentNutrition += s.value;
                        break;
                    case StatType.Friction:
                        currentFriction += s.value;
                        break;
                    case StatType.Knockback:
                        currentKnockback += s.value;
                        break;
                    case StatType.DashForce:
                        currentDashForce += s.value;
                        break;
                    case StatType.DashRange:
                        currentDashRange += s.value;
                        break;
                    case StatType.Reap:
                        currentReap += s.value;
                        break;
                }
            }
        }

        // StaminaManager에 stamina_regen 적용
        if (StaminaManager.instance != null && currentStaminaRegen != 0f)
        {
            StaminaManager.instance.ApplyStaminaRegenBonus(currentStaminaRegen);
        }

        activeToastId = toastId;
        if (!string.IsNullOrEmpty(toastId) && System.Enum.TryParse(toastId, out ToastIndicator.ToastType parsedToast))
        {
            UpdateToastVisuals(parsedToast);
        }
        else
        {
            UpdateToastVisuals(null);
        }
        Debug.Log($"[PlayerController] ApplyToastStats: toastId={toastId}, speed={moveSpeed}, jump={jumpForce}, " +
                  $"attack={currentAttack}, defense={currentDefense}, haste={currentHaste}, " +
                  $"agility={currentAgility}, sprint={currentSprint}, stamina_regen={currentStaminaRegen}");

        // ★ 모든 ToastHoverPanel들의 버튼 상태를 업데이트
        RefreshAllToastPanelButtons();
    }

    private void RefreshAllToastPanelButtons()
    {
        // 씬의 모든 ToastHoverPanel을 찾아서 버튼 업데이트
        var allPanels = FindObjectsOfType<ToastHoverPanel>(true); // includeInactive = true
        Debug.Log($"[PlayerController] ★★★ RefreshAllToastPanelButtons: Found {allPanels.Length} panels ★★★");

        if (allPanels.Length == 0)
        {
            Debug.LogWarning("[PlayerController] No ToastHoverPanel found! Panels might not be instantiated yet.");
        }

        foreach (var panel in allPanels)
        {
            Debug.Log($"[PlayerController] Refreshing panel: {panel.gameObject.name}, active={panel.gameObject.activeInHierarchy}");
            panel.RefreshButton();
        }

        Debug.Log($"[PlayerController] RefreshAllToastPanelButtons complete. Active toast: {activeToastId}");
    }

    private void UpdateToastVisuals(ToastIndicator.ToastType? toastType)
    {
        currentToastType = toastType;
        EnsureToastIndicatorReference();

        if (toastType.HasValue)
        {
            if (playerToastIndicator != null)
            {
                playerToastIndicator.SetToast(toastType.Value);
            }
            if (playerToastIndicatorRenderer != null)
            {
                playerToastIndicatorRenderer.enabled = true;
            }
        }
        else
        {
            if (playerToastIndicatorRenderer != null)
            {
                playerToastIndicatorRenderer.enabled = false;
            }
        }

        ToastProfileSprite.ToastProfileType spriteType = toastType.HasValue
            ? MapToProfileSpriteType(toastType.Value)
            : ToastProfileSprite.ToastProfileType.Jam;
        ToastProfileUI.ToastType uiType = toastType.HasValue
            ? MapToProfileUIType(toastType.Value)
            : ToastProfileUI.ToastType.Jam;

        if (toastProfileFollowers != null && toastProfileFollowers.Length > 0)
        {
            foreach (var follower in toastProfileFollowers)
            {
                if (follower == null) continue;
                if (toastType.HasValue) follower.SetToast(toastType.Value);
                follower.SetProfile(spriteType);
            }
        }

        if (toastProfileSprites != null && toastProfileSprites.Length > 0)
        {
            foreach (var sprite in toastProfileSprites)
            {
                sprite?.SetProfile(spriteType);
            }
        }

        if (toastProfileUIElements != null && toastProfileUIElements.Length > 0)
        {
            foreach (var ui in toastProfileUIElements)
            {
                ui?.SetProfile(uiType);
            }
        }
    }

    private void EnsureToastIndicatorReference()
    {
        if (playerToastIndicator == null)
        {
            playerToastIndicator = GetComponentInChildren<ToastIndicator>(true);
        }
        if (playerToastIndicatorRenderer == null && playerToastIndicator != null)
        {
            playerToastIndicatorRenderer = playerToastIndicator.GetComponent<SpriteRenderer>();
        }
    }

    private static ToastProfileSprite.ToastProfileType MapToProfileSpriteType(ToastIndicator.ToastType toastType)
    {
        switch (toastType)
        {
            case ToastIndicator.ToastType.Butter:
                return ToastProfileSprite.ToastProfileType.Butter;
            case ToastIndicator.ToastType.Crispy:
                return ToastProfileSprite.ToastProfileType.Crispy;
            case ToastIndicator.ToastType.Herb:
                return ToastProfileSprite.ToastProfileType.Hub;
            case ToastIndicator.ToastType.Raven:
                return ToastProfileSprite.ToastProfileType.Raven;
            case ToastIndicator.ToastType.Admin:
                return ToastProfileSprite.ToastProfileType.Admin;
            case ToastIndicator.ToastType.Jam:
            default:
                return ToastProfileSprite.ToastProfileType.Jam;
        }
    }

    private static ToastProfileUI.ToastType MapToProfileUIType(ToastIndicator.ToastType toastType)
    {
        switch (toastType)
        {
            case ToastIndicator.ToastType.Butter:
                return ToastProfileUI.ToastType.Butter;
            case ToastIndicator.ToastType.Crispy:
                return ToastProfileUI.ToastType.Crispy;
            case ToastIndicator.ToastType.Herb:
                return ToastProfileUI.ToastType.Hub;
            case ToastIndicator.ToastType.Raven:
                return ToastProfileUI.ToastType.Raven;
            case ToastIndicator.ToastType.Admin:
                return ToastProfileUI.ToastType.Admin;
            case ToastIndicator.ToastType.Jam:
            default:
                return ToastProfileUI.ToastType.Jam;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Dead") && !isDead)
        {
            Die();
        }
    }

    // ==================== 스테이지 전환 메서드 ====================

    /// <summary>
    /// 게임 시작 시 Ground1의 오른쪽 끝 x좌표를 찾아 저장
    /// </summary>
    private void InitializeStageEndPosition()
    {
        GameObject ground1 = GameObject.Find("Ground1");

        if (ground1 == null)
        {
            Debug.LogWarning("[Stage] Ground1을 찾을 수 없습니다!");
            return;
        }

        // Ground1의 오른쪽 끝 x좌표 가져오기
        stageEndX = GetRightEdgeX(ground1);

        if (stageEndX != float.MaxValue)
        {
            Debug.Log($"[Stage] 스테이지 끝 x좌표: {stageEndX}");
        }
    }

    /// <summary>
    /// 오브젝트의 오른쪽 끝 x좌표 반환
    /// </summary>
    private float GetRightEdgeX(GameObject obj)
    {
        // 1순위: 직접 붙은 Collider
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider != null)
        {
            return collider.bounds.max.x;
        }

        // 2순위: 직접 붙은 Renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.max.x;
        }

        // 3순위: 자식들의 Collider 합산
        Collider2D[] childColliders = obj.GetComponentsInChildren<Collider2D>();
        if (childColliders.Length > 0)
        {
            float maxX = float.MinValue;
            foreach (var col in childColliders)
            {
                maxX = Mathf.Max(maxX, col.bounds.max.x);
            }
            return maxX;
        }

        Debug.LogWarning("[Stage] Ground1의 크기를 계산할 수 없습니다!");
        return float.MaxValue;
    }

    /// <summary>
    /// 플레이어가 스테이지 끝에 도달했는지 매 프레임 체크
    /// </summary>
    private void CheckIfReachedStageEnd()
    {
        // 전환 불가능한 상태 체크
        if (stageTransitionTriggered) return;
        if (stageEndX == float.MaxValue) return;
        if (GameManager.instance != null && GameManager.instance.isGameover) return;

        // 플레이어가 스테이지 끝에 도달했는지 확인
        bool reachedEnd = transform.position.x >= stageEndX - 0.1f;

        if (reachedEnd)
        {
            stageTransitionTriggered = true;
            Debug.Log($"[Stage] 스테이지 끝 도달! (플레이어: {transform.position.x}, 끝: {stageEndX})");

            GameManager.instance?.LoadNextStage();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGroundContact(collision);
        CheckWallCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckGroundContact(collision);
        CheckWallCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!IsContactingGround())
        {
            isGrounded = false;
            Debug.Log("[GROUND] 공중 상태");

            if (wasOnSlope && playerRigidbody != null)
            {
                leftMomentum = 0f;
                rightMomentum = 0f;
                leftKeyHoldTime = 0f;
                rightKeyHoldTime = 0f;
                playerRigidbody.linearVelocity = new Vector2(0f, playerRigidbody.linearVelocity.y);
            }
            wasOnSlope = false;
        }

        CheckWallCollisionExit(collision);
        onSlope = false;
        ResetFrictionIfNeeded();
    }

    private void CheckGroundContact(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // ?�쪽?�로 ?�한 충돌면만 바닥?�로 ?�식 (normal.y > slopeNormalMin)
            if (contact.normal.y > slopeNormalMin)
            {
                // ?�평??바닥?��? 경사면인지 ?�단
                bool isFlat = contact.normal.y > 0.95f;

                if (isFlat)
                {
                    // ?�평??바닥: grace time 체크
                    float timeSinceJump = Time.time - lastJumpTime;
                    if (timeSinceJump < jumpGraceTime)
                    {
                        Debug.Log($"[GRACE] Grace time �?({timeSinceJump:F3}s < {jumpGraceTime}s)");
                        return;
                    }
                }
                // 경사면�? grace time 무시?�고 즉시 착�?!

                // 착�?!
                if (!isGrounded)
                {
                    Debug.Log($"[GROUND] 착�?! (normal.y={contact.normal.y:F2}, flat={isFlat}, angle={(Mathf.Acos(contact.normal.y) * Mathf.Rad2Deg):F1}°)");
                }
                isGrounded = true;
                onSlope = !isFlat;
                wasOnSlope = onSlope;
                ApplySlopeFriction(contact.normal.y);
                return;
            }
        }
    }

    private bool IsContactingGround()
    {
        Vector2 origin = GetFeetPos();
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            0.2f,
            groundLayer
        );
        if (hit.collider != null && hit.normal.y > 0.3f)
        {
            onSlope = hit.normal.y <= 0.95f && hit.normal.y > slopeNormalMin;
            wasOnSlope = onSlope;
            ApplySlopeFriction(hit.normal.y);
            return true;
        }
        return false;
    }

    private void CheckWallCollision(Collision2D collision)
    {
        bool foundLeftWall = false;
        bool foundRightWall = false;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > slopeNormalMin)
            {
                onSlope = true;
                ApplySlopeFriction(contact.normal.y);
                continue;
            }
            if (contact.normal.x > 0.7f)
            {
                foundLeftWall = true;
                leftMomentum = 0f;
                leftKeyHoldTime = 0f;
                if (playerRigidbody.linearVelocity.x < 0)
                {
                    playerRigidbody.linearVelocity = new Vector2(0, playerRigidbody.linearVelocity.y);
                }
            }
            else if (contact.normal.x < -0.7f)
            {
                foundRightWall = true;
                rightMomentum = 0f;
                rightKeyHoldTime = 0f;
                if (playerRigidbody.linearVelocity.x > 0)
                {
                    playerRigidbody.linearVelocity = new Vector2(0, playerRigidbody.linearVelocity.y);
                }
            }
        }

        isCollidingLeftWall = foundLeftWall;
        isCollidingRightWall = foundRightWall;
    }

    private void CheckWallCollisionExit(Collision2D collision)
    {
        isCollidingLeftWall = false;
        isCollidingRightWall = false;
    }

    private void ApplySlopeFriction(float normalY)
    {
        if (runtimeMaterial == null) return;
        bool isSlope = normalY > slopeNormalMin && normalY < flatNormalThreshold;
        float target = isSlope ? slopeFriction : flatFriction;
        if (!Mathf.Approximately(runtimeMaterial.friction, target))
        {
            runtimeMaterial.friction = target;
        }
    }

    private void ResetFrictionIfNeeded()
    {
        if (runtimeMaterial == null) return;
        if (!Mathf.Approximately(runtimeMaterial.friction, flatFriction))
        {
            runtimeMaterial.friction = flatFriction;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerRigidbody != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    // Shared jump physics for AI/enemies to reuse (vertical portion only)
    public static void PerformJumpPhysics(Rigidbody2D body, float jumpForce)
    {
        if (body == null) return;
        var v = body.linearVelocity;
        body.linearVelocity = new Vector2(v.x, 0);
        body.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    private void CacheScratchClip()
    {
        if (animator == null) return;
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return;

        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name == scratchAnimationName)
            {
                scratchClip = clip;
                // 루프 ?�정?�라??1???�생?�도�?WrapMode 변�??�도
                clip.wrapMode = WrapMode.Once;
                break;
            }
        }
    }

    private void CachePunchClip()
    {
        if (animator == null) return;
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return;

        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name == punchAnimationName)
            {
                punchClip = clip;
                // Punch 애니메이션도 한 번만 재생되도록 WrapMode 설정
                clip.wrapMode = WrapMode.Once;
                Debug.Log($"[PUNCH CACHE] Punch clip cached and set to WrapMode.Once");
                break;
            }
        }
    }

    private void HandleScratchInput()
    {
        if (isScratching || isDead)
        {
            return;
        }

        if (inputSource.GetScratchDown())
        {
            StartScratch();
        }
    }

    private void StartScratch()
    {
        // 이미 Scratch 중이면 무시
        if (isScratching)
        {
            Debug.Log("[SCRATCH] 이미 Scratch 중입니다.");
            return;
        }

        // 스태미나 체크
        if (StaminaManager.instance != null && !StaminaManager.instance.UseStamina(scratchStaminaCost))
        {
            Debug.Log($"[SCRATCH] 스태미나 부족! 필요: {scratchStaminaCost}");
            return;
        }

        // 이동/점프/대시 입력 무시 상태로 전환
        leftMomentum = 0f;
        rightMomentum = 0f;
        leftKeyHoldTime = 0f;
        rightKeyHoldTime = 0f;

        // Scratch 시작 전에 현재 Jump 애니메이션 상태를 먼저 저장
        wasAirborneBeforeScratch = !isGrounded;
        if (wasAirborneBeforeScratch && animator != null)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            // Jump 애니메이션 중이었다면 정확한 시간을 저장
            if (currentState.IsName("Jump"))
            {
                savedJumpNormalizedTime = currentState.normalizedTime;
                savedJumpLength = Mathf.Max(0.01f, currentState.length);
                Debug.Log($"[SCRATCH] Jump 상태 저장: normalizedTime={savedJumpNormalizedTime:F3}, length={savedJumpLength:F3}");
            }
            else
            {
                // Jump 애니메이션이 아니면 0부터 시작
                savedJumpNormalizedTime = 0f;
                savedJumpLength = 0.85f; // 기본 Jump 애니메이션 길이
            }
        }

        float clipLen = scratchClip != null ? scratchClip.length : scratchDuration;
        float targetDuration = Mathf.Max(0.01f, scratchDuration);

        if (animator != null)
        {
            animator.speed = (clipLen > 0f) ? (clipLen / targetDuration) : 1f;
            animator.ResetTrigger(JumpHash);
            animator.ResetTrigger(DieHash);
            animator.ResetTrigger(DashHash);
        }
        // ★ 중요: isScratching을 true로 설정하는 타이밍
        isScratching = true;
        scratchStartTime = Time.time;
        scratchEndTime = Time.time + targetDuration;
        scratchStoppedAnimator = false;

        // Scratch 애니메이션 재생 (이 시점에 이미 isScratching = true)
        SetAnimationState(AnimationState.Scratch);
        if (playerAudio != null && scratchAudioClip != null)
        {
            playerAudio.PlayOneShot(scratchAudioClip);
        }
        ApplyScratchDamage();

        Debug.Log($"[SCRATCH] 시작! 공중={wasAirborneBeforeScratch}, scratchEndTime={scratchEndTime:F3}");
    }

    private void UpdateScratchState()
    {
        if (!isScratching)
        {
            return;
        }

        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            // ★ Scratch 애니메이션 상태 강제 유지 - 매 프레임 체크
            if (!state.IsName(scratchAnimationName))
            {
                // Animator가 다른 상태로 전환하려고 하면 강제로 Scratch로 되돌림
                float timeSinceStart = Time.time - scratchStartTime;
                if (timeSinceStart > 0.1f) // 최초 transition은 허용
                {
                    Debug.LogWarning($"[SCRATCH] Animator가 {state.ToString()}로 전환 시도! 강제로 Scratch로 복귀");
                    animator.Play(scratchAnimationName, 0, Mathf.Min(timeSinceStart / scratchDuration, 0.99f));
                }
            }
            else
            {
                // Scratch 애니메이션이 완전히 끝났는지 확인
                if (state.normalizedTime >= 0.99f)
                {
                    Debug.Log($"[SCRATCH] 애니메이션 완료 감지: normalizedTime={state.normalizedTime:F3}");
                    EndScratch();
                    return;
                }

                // ★ 디버깅: 현재 진행 상황 출력
                if (Time.frameCount % 10 == 0) // 10프레임마다
                {
                    Debug.Log($"[SCRATCH PROGRESS] normalizedTime={state.normalizedTime:F3}, time={Time.time - scratchStartTime:F3}");
                }
            }
        }

        // 시간 기반 종료 (안전장치)
        if (Time.time >= scratchEndTime)
        {
            Debug.Log($"[SCRATCH] 시간 초과로 종료: Time.time={Time.time:F3}, scratchEndTime={scratchEndTime:F3}");
            EndScratch();
        }
    }

    private void EndScratch()
    {
        if (!isScratching)
        {
            Debug.Log("[SCRATCH] 이미 종료된 상태입니다.");
            return;
        }

        Debug.Log($"[SCRATCH] 종료 시작! 공중={wasAirborneBeforeScratch}, isGrounded={isGrounded}");

        // ★ 중요: isScratching을 먼저 false로 설정
        isScratching = false;

        if (scratchStoppedAnimator && animator != null)
        {
            animator.speed = 1f;
        }
        scratchStoppedAnimator = false;
        scratchEndTime = -1f;

        // Scratch 종료 후 공중이면 Jump 애니메이션으로 복귀
        if (wasAirborneBeforeScratch && !isGrounded)
        {
            // 저장했던 normalizedTime으로 복귀
            pendingJumpResumeNormalizedTime = savedJumpNormalizedTime;
            Debug.Log($"[SCRATCH] Jump 복귀 예약: normalizedTime={pendingJumpResumeNormalizedTime:F3}");
        }
        else
        {
            pendingJumpResumeNormalizedTime = -1f;
        }

        wasAirborneBeforeScratch = false;

        // ★ 애니메이션 전환 딜레이를 리셋하여 즉시 전환 가능하도록
        lastAnimationChangeTime = Time.time - animationTransitionDelay;

        // 현재 상태에 맞는 애니메이션으로 전환
        AnimationState nextState = DetermineAnimationState();
        Debug.Log($"[SCRATCH] 다음 상태 결정: {nextState}, leftMomentum={leftMomentum:F2}, rightMomentum={rightMomentum:F2}");

        // ★ 즉시 전환 (딜레이 없이)
        SetAnimationState(nextState);

        Debug.Log($"[SCRATCH] 종료 완료! currentAnimState={currentAnimState}");
    }

    // ==================== Punch Functions ====================

    private void HandlePunchInput()
    {
        if (isPunching || isDead)
        {
            return;
        }

        if (inputSource.GetPunchDown())
        {
            StartPunch();
        }
    }

    private void StartPunch()
    {
        if (isPunching)
        {
            Debug.Log("[PUNCH] 이미 Punch 중입니다.");
            return;
        }

        // 스태미나 체크
        if (StaminaManager.instance != null && !StaminaManager.instance.UseStamina(punchStaminaCost))
        {
            Debug.Log($"[PUNCH] 스태미나 부족! 필요: {punchStaminaCost}");
            return;
        }

        // 이동 입력 무시 상태로 전환
        leftMomentum = 0f;
        rightMomentum = 0f;
        leftKeyHoldTime = 0f;
        rightKeyHoldTime = 0f;

        // Scratch와 동일하게 animator.speed 조절
        float clipLen = punchClip != null ? punchClip.length : punchDuration;
        float targetDuration = Mathf.Max(0.01f, punchDuration);

        if (animator != null)
        {
            animator.speed = (clipLen > 0f) ? (clipLen / targetDuration) : 1f;
            animator.ResetTrigger(JumpHash);
            animator.ResetTrigger(DieHash);
            animator.ResetTrigger(DashHash);
        }

        isPunching = true;
        punchStartTime = Time.time;
        punchEndTime = Time.time + targetDuration;

        // Punch 애니메이션 재생
        SetAnimationState(AnimationState.Punch);

        // GarbageCan 타격 처리
        TryHitGarbageCanForward();

        // Enemy damage 처리
        ApplyPunchDamage();

        Debug.Log($"[PUNCH] 시작! clipLen={clipLen:F3}, targetDuration={targetDuration:F3}, speed={animator?.speed:F2}, punchEndTime={punchEndTime:F3}");
    }

    private void UpdatePunchState()
    {
        if (!isPunching)
        {
            return;
        }

        // 디버그: 현재 상태 출력
        if (Time.frameCount % 10 == 0 && animator != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[PUNCH UPDATE] Time.time={Time.time:F3}, punchEndTime={punchEndTime:F3}, remaining={punchEndTime - Time.time:F3}, animState={stateInfo.ToString()}, isPunching={isPunching}");
        }

        // 시간 기반 종료 (Dash와 동일한 방식)
        if (Time.time >= punchEndTime)
        {
            Debug.Log($"[PUNCH] 시간 종료: Time.time={Time.time:F3}, punchEndTime={punchEndTime:F3}");
            EndPunch();
        }
    }

    private void EndPunch()
    {
        if (!isPunching)
        {
            Debug.Log("[PUNCH] 이미 종료된 상태입니다.");
            return;
        }

        Debug.Log($"[PUNCH] 종료 시작! isGrounded={isGrounded}");

        isPunching = false;
        punchEndTime = -1f;

        // animator.speed를 원래대로 복구
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // 애니메이션 전환 딜레이를 리셋하여 즉시 전환 가능하도록
        lastAnimationChangeTime = Time.time - animationTransitionDelay;

        // 현재 상태에 맞는 애니메이션으로 전환
        AnimationState nextState = DetermineAnimationState();
        Debug.Log($"[PUNCH] 다음 상태 결정: {nextState}");

        // 즉시 전환 (딜레이 없이)
        SetAnimationState(nextState);

        Debug.Log($"[PUNCH] 종료 완료! currentAnimState={currentAnimState}");
    }

    // ==================== End Punch Functions ====================

    // GarbageCan을 전방으로 타격 시도 (K/L/Shift 시작 시 사용)
    private void TryHitGarbageCanForward(float radius = 0.8f)
    {
        Vector2 dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + dir * 0.25f;
        var hits = Physics2D.OverlapCircleAll(origin, radius);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            var can = hit.GetComponentInParent<GarbageCan>();
            if (can != null)
            {
                can.OnHit();
                break;
            }
        }
    }

    private void ApplyScratchDamage()
    {
        if (scratchHitbox == null)
        {
            // ?�동 ?�색 ?�도
            Transform child = transform.Find("ScratchHitbox");
            if (child != null)
            {
                scratchHitbox = child.GetComponent<BoxCollider2D>();
            }
            if (scratchHitbox == null)
            {
                Debug.LogWarning("Scratch hitbox is not assigned on PlayerController.");
                return;
            }
        }

        Vector2 center = scratchHitbox.bounds.center;
        Vector2 size = scratchHitbox.bounds.size;
        int mask = scratchDamageLayers.value == 0 ? ~0 : scratchDamageLayers.value;
        System.Array.Clear(scratchHits, 0, scratchHits.Length);
        int hitCount = Physics2D.OverlapBoxNonAlloc(center, size, 0f, scratchHits, mask);
        Vector2 knockbackDir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        // Apply knockback stat to scratch
        float scratchKnockbackForce = 2f; // Base scratch knockback
        float totalScratchKnockback = scratchKnockbackForce + currentKnockback;
        Vector2 scratchKnockback = knockbackDir * totalScratchKnockback;

        // ?��? 공격???�들??추적?�기 ?�한 HashSet
        System.Collections.Generic.HashSet<int> alreadyHitEnemies = new System.Collections.Generic.HashSet<int>();


        for (int i = 0; i < hitCount && i < scratchHits.Length; i++)
        {
            Collider2D hit = scratchHits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // Enemy 처리                                                                                                                          
            IntelligentDogMovement dog = hit.GetComponentInParent<IntelligentDogMovement>();
            if (dog != null)
            {
                int dogInstanceID = dog.GetInstanceID();
                if (!alreadyHitEnemies.Contains(dogInstanceID))
                {
                    alreadyHitEnemies.Add(dogInstanceID);
                    float finalDamage = scratchDamage + currentAttack;
                    dog.TakeDamage(finalDamage, scratchKnockback);
                }
                continue;
            }

            // GarbageCan 처리                                                                                                                     
            var can = hit.GetComponentInParent<GarbageCan>();
            if (can != null)
            {
                can.OnHit();
            }
        }
    }

    private void ApplyPunchDamage()
    {
        if (punchHitbox == null)
        {
            // Auto-search attempt
            Transform child = transform.Find("PunchHitbox");
            if (child != null)
            {
                punchHitbox = child.GetComponent<BoxCollider2D>();
            }
            if (punchHitbox == null)
            {
                Debug.LogWarning("Punch hitbox is not assigned on PlayerController.");
                return;
            }
        }

        Vector2 center = punchHitbox.bounds.center;
        Vector2 size = punchHitbox.bounds.size;
        int mask = punchDamageLayers.value == 0 ? ~0 : punchDamageLayers.value;
        System.Array.Clear(punchHits, 0, punchHits.Length);
        int hitCount = Physics2D.OverlapBoxNonAlloc(center, size, 0f, punchHits, mask);
        Vector2 knockbackDir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        // Apply knockback stat to increase knockback distance (additive)
        float totalKnockback = punchKnockbackForce + currentKnockback;
        Vector2 knockbackForce = knockbackDir * totalKnockback;

        System.Collections.Generic.HashSet<int> alreadyHitEnemies = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < hitCount && i < punchHits.Length; i++)
        {
            Collider2D hit = punchHits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // Enemy processing
            IntelligentDogMovement dog = hit.GetComponentInParent<IntelligentDogMovement>();
            if (dog != null)
            {
                int dogInstanceID = dog.GetInstanceID();
                if (!alreadyHitEnemies.Contains(dogInstanceID))
                {
                    alreadyHitEnemies.Add(dogInstanceID);
                    float finalDamage = punchDamage + currentAttack;
                    dog.TakeDamage(finalDamage, knockbackForce);
                }
                continue;
            }

            // GarbageCan processing
            var can = hit.GetComponentInParent<GarbageCan>();
            if (can != null)
            {
                can.OnHit();
            }
        }
    }

    private void ApplyDashDamage()
    {
        if (dashHitbox == null)
        {
            // Auto-search attempt
            Transform child = transform.Find("DashHitbox");
            if (child != null)
            {
                dashHitbox = child.GetComponent<BoxCollider2D>();
            }
            if (dashHitbox == null)
            {
                Debug.LogWarning("Dash hitbox is not assigned on PlayerController.");
                return;
            }
        }

        Vector2 center = dashHitbox.bounds.center;
        Vector2 size = dashHitbox.bounds.size;
        int mask = dashDamageLayers.value == 0 ? ~0 : dashDamageLayers.value;
        System.Array.Clear(dashHits, 0, dashHits.Length);
        int hitCount = Physics2D.OverlapBoxNonAlloc(center, size, 0f, dashHits, mask);

        // Apply knockback stat to dash
        Vector2 knockbackDir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        float dashKnockbackForce = 4f; // Base dash knockback
        float totalDashKnockback = dashKnockbackForce + currentKnockback;
        Vector2 dashKnockback = knockbackDir * totalDashKnockback;

        System.Collections.Generic.HashSet<int> alreadyHitEnemies = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < hitCount && i < dashHits.Length; i++)
        {
            Collider2D hit = dashHits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // Enemy processing
            IntelligentDogMovement dog = hit.GetComponentInParent<IntelligentDogMovement>();
            if (dog != null)
            {
                int dogInstanceID = dog.GetInstanceID();
                if (!alreadyHitEnemies.Contains(dogInstanceID))
                {
                    alreadyHitEnemies.Add(dogInstanceID);
                    float finalDamage = dashDamage + currentAttack;
                    dog.TakeDamage(finalDamage, dashKnockback);

                    // 적의 공격 취소 및 쿨타임 초기화
                    dog.CancelAttackFromDash();
                }
                continue;
            }
        }
    }

    private void InitializeInputSource()
    {
        if (inputSourceBehaviour != null)
        {
            inputSource = inputSourceBehaviour as IPlayerInputSource;
            if (inputSource == null)
            {
                Debug.LogWarning("[PlayerController] Assigned inputSourceBehaviour does not implement IPlayerInputSource.");
            }
        }

        if (inputSource == null)
        {
            inputSource = new UnityPlayerInputSource();
        }
    }

    private class UnityPlayerInputSource : IPlayerInputSource
    {
        public bool GetMoveLeft() => Input.GetKey(KeyCode.A);
        public bool GetMoveRight() => Input.GetKey(KeyCode.D);
        public bool GetJumpDown() => Input.GetKeyDown(KeyCode.W);
        public bool GetJumpHeld() => Input.GetKey(KeyCode.W);
        public bool GetJumpUp() => Input.GetKeyUp(KeyCode.W);
        public bool GetScratchDown() => Input.GetKeyDown(KeyCode.K);
        public bool GetPunchDown() => Input.GetKeyDown(KeyCode.L);
        public bool GetDashDown() => Input.GetKeyDown(KeyCode.Q);
    }
}
