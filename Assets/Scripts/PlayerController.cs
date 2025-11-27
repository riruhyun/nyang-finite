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
        Scratch
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
    public float jumpForce = 700f;
    public float moveSpeed = 5f;
    [Header("Slope Handling")]
    [SerializeField] private float slopeNormalMin = 0.4f; // 경사�?55???�함) ?�상?�면 바닥 취급
    [SerializeField] private float slopeSpeedMultiplier = 1.2f;
    [SerializeField] private float flatFriction = 0.3f;
    [SerializeField] private float slopeFriction = 0f;
    [SerializeField] private float flatNormalThreshold = 0.95f;

    [Header("Momentum Settings")]
    public float baseMomentum = 0.5f;
    public float maxMomentum = 2.0f;
    public float momentumBuildTime = 1.0f;
    public float momentumDecayAmount = 2.5f;

    [Header("Animation Settings")]
    public float walkMomentumThreshold = 0.5f;
    public float dashDuration = 0.5f; // Dash ?�니메이??지???�간

    [SerializeField] private LayerMask groundLayer;

    [Header("Scratch Settings")]
    [SerializeField] private BoxCollider2D scratchHitbox;
    [SerializeField] private LayerMask scratchDamageLayers;
    [SerializeField] private float scratchDamage = 1.5f;
    [SerializeField] private float scratchDuration = 0.25f; // Target ~0.25s total
    [SerializeField] private string scratchAnimationName = "Scratch";

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
    private float lastJumpTime = -1f;

    // ?�니메이???�태 관�?
    private AnimationState currentAnimState = AnimationState.Idle;
    private AnimationState previousAnimState = AnimationState.Idle;
    private float animationTransitionDelay = 0.1f;
    private float lastAnimationChangeTime = 0f;
    private float pendingJumpResumeNormalizedTime = -1f;

    // Dash ?�태 관�?
    private bool isDashing = false;
    private float dashStartTime = 0f;

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
    private readonly Collider2D[] scratchHits = new Collider2D[8];
    private bool wasAirborneBeforeScratch = false;
    private float savedJumpNormalizedTime = 0f;
    private float savedJumpLength = 0.85f;
    private float scratchEndTime = -1f;
    private float scratchStartTime = -1f;
    private bool scratchStoppedAnimator = false;
    private AnimationClip scratchClip;
    private float scratchPlaySpeed = 1f;
    private PhysicsMaterial2D runtimeMaterial;
    private bool wasOnSlope = false; // ?�전 ?�레?�에 경사�??��??��? 추적

    // ?�니메이???�라미터 ?�시
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int DashHash = Animator.StringToHash("Dash");

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
        CacheScratchClip();

        // ü�� �ʱ�ȭ
        currentHealth = maxHealth;
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHealth(currentHealth);
        }

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

        // ★ Scratch 상태 먼저 처리 (최고 우선순위)
        UpdateScratchState();
        
        // Scratch 중이면 다른 입력 처리 안함
        if (!isScratching)
        {
            HandleScratchInput();
            HandleMovementInput();
            HandleJumpInput();
            HandleDashInput();
        }
        
        UpdateAnimationState();
    }

    private void HandleMovementInput()
    {
        bool isPressingLeft = Input.GetKey(KeyCode.A);
        bool isPressingRight = Input.GetKey(KeyCode.D);

        // ?�어지???�태?��? ?�인 (공중 + ?�강)
        bool isFalling = !isGrounded && playerRigidbody.linearVelocity.y < -0.1f;

        // ?�쪽 ??처리 (?�쪽 벽에 붙어?�거???�어지??중이�?무시)
        if (isPressingLeft && !isCollidingLeftWall && !isFalling)
        {
            leftKeyHoldTime += Time.deltaTime;
            float momentumProgress = Mathf.Clamp01(leftKeyHoldTime / momentumBuildTime);
            leftMomentum = Mathf.Lerp(baseMomentum, maxMomentum, momentumProgress);
            transform.localScale = new Vector3(-5f, 5f, 5f);
        }
        else
        {
            if (leftMomentum > 0)
            {
                leftMomentum -= momentumDecayAmount * Time.deltaTime * 10f;
                leftMomentum = Mathf.Max(leftMomentum, 0f);
            }
            leftKeyHoldTime = 0f;
        }

        // ?�른�???처리 (?�른�?벽에 붙어?�거???�어지??중이�?무시)
        if (isPressingRight && !isCollidingRightWall && !isFalling)
        {
            rightKeyHoldTime += Time.deltaTime;
            float momentumProgress = Mathf.Clamp01(rightKeyHoldTime / momentumBuildTime);
            rightMomentum = Mathf.Lerp(baseMomentum, maxMomentum, momentumProgress);
            transform.localScale = new Vector3(5f, 5f, 5f);
        }
        else
        {
            if (rightMomentum > 0)
            {
                rightMomentum -= momentumDecayAmount * Time.deltaTime * 10f;
                rightMomentum = Mathf.Max(rightMomentum, 0f);
            }
            rightKeyHoldTime = 0f;
        }

        ApplyMovement();
    }

    private void ApplyMovement()
    {
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

        if (Input.GetKeyDown(KeyCode.W))
        {
            bool slopeGround = IsOnJumpableSlope();

            // 경사면에??y가 ?�수�?찍�????�프 ?�용?�고 ?�으�??�거 ?�예 빼도 ??
            bool isFallingFromCliff = playerRigidbody.linearVelocity.y < -0.1f && IsOnFlatGround();

            bool canJump = (isGrounded || slopeGround) && !isFallingFromCliff;

            Debug.Log($"[JUMP INPUT] W! grounded={isGrounded}, onSlope={slopeGround}, velY={playerRigidbody.linearVelocity.y:F2}, canJump={canJump}");

            if (canJump)
            {
                PerformJump();
            }
        }
        else if (Input.GetKeyUp(KeyCode.W) && playerRigidbody.linearVelocity.y > 0)
        {
            playerRigidbody.linearVelocity = new Vector2(
                playerRigidbody.linearVelocity.x,
                playerRigidbody.linearVelocity.y * 0.5f
            );
        }
    }

    private void HandleDashInput()
    {
        if (isScratching)
        {
            return;
        }
        // R ?��? ?�르�??�재 Dash 중이 ?�닐 ?�만 ?�행
        if (Input.GetKeyDown(KeyCode.R) && !isDashing)
        {
            isDashing = true;
            dashStartTime = Time.time;
            SetAnimationState(AnimationState.Dash);
            Debug.Log("[DASH] ?�진 ?�니메이???�행!");
        }

        // Dash 지???�간???�나�?Dash ?�태 ?�제
        if (isDashing && Time.time - dashStartTime >= dashDuration)
        {
            isDashing = false;
            Debug.Log("[DASH] ?�진 ?�니메이??종료!");
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


    private void PerformJump()
    {
        isGrounded = false;
        lastJumpTime = Time.time;

        playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, 0);
        playerRigidbody.AddForce(new Vector2(0, jumpForce));

        if (playerAudio != null)
        {
            playerAudio.Play();
        }

        SetAnimationState(AnimationState.Jump);
        Debug.Log($"[JUMP] ?�프 ?�행! Time={Time.time:F3}");
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

    public void TakeDamage(float damage)
    {
        // 무적 ?�간 체크
        if (Time.time - lastDamageTime < invincibilityTime)
        {
            Debug.Log("무적 ?�간 �? ?��?지 무시");
            return;
        }

        if (isDead)
        {
            return;
        }

        // ?��?지 ?�용
        currentHealth -= damage;
        lastDamageTime = Time.time;
        StartDamageFlash();

        Debug.Log($"?�레?�어 ?��?지 받음! ?�재 체력: {currentHealth}/{maxHealth}");

        // ?��?지 ?�운???�생
        if (playerAudio != null && damageClip != null)
        {
            playerAudio.PlayOneShot(damageClip);
        }

        // GameManager??체력 ?�데?�트 ?�림
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Dead") && !isDead)
        {
            Die();
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
        body.AddForce(new Vector2(0, jumpForce));
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

    private void HandleScratchInput()
    {
        if (isScratching || isDead)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.K))
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
        
        // ?��? 공격???�들??추적?�기 ?�한 HashSet
        System.Collections.Generic.HashSet<int> alreadyHitEnemies = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < hitCount && i < scratchHits.Length; i++)
        {
            Collider2D hit = scratchHits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            IntelligentDogMovement dog = hit.GetComponentInParent<IntelligentDogMovement>();
            if (dog != null)
            {
                // ?��? ?�격한 ?�인지 ?�인
                int dogInstanceID = dog.GetInstanceID();
                if (!alreadyHitEnemies.Contains(dogInstanceID))
                {
                    alreadyHitEnemies.Add(dogInstanceID);
                    dog.TakeDamage(scratchDamage, knockbackDir);
                    Debug.Log($"[Player] Scratch �������� {dog.name}���� {scratchDamage} ������ ����");
                }
            }
        }
    }
}
