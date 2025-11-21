using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private enum AnimationState
    {
        Idle,
        Walk,
        Jump,
        Die,
        Dash
    }

    [Header("Audio Settings")]
    public AudioClip deathClip;
    public AudioClip damageClip; // 데미지 받을 때 사운드

    [Header("Health Settings")]
    public int maxHealth = 9; // 최대 체력
    private int currentHealth; // 현재 체력
    public float invincibilityTime = 1.5f; // 무적 시간
    private float lastDamageTime = -10f; // 마지막 데미지를 받은 시간

    [Header("Movement Settings")]
    public float jumpForce = 700f;
    public float moveSpeed = 5f;

    [Header("Momentum Settings")]
    public float baseMomentum = 0.5f;
    public float maxMomentum = 2.0f;
    public float momentumBuildTime = 1.0f;
    public float momentumDecayAmount = 2.5f;

    [Header("Animation Settings")]
    public float walkMomentumThreshold = 0.5f;
    public float dashDuration = 0.5f; // Dash 애니메이션 지속 시간

    [SerializeField] private LayerMask groundLayer;

    // 물리 상태
    private bool isGrounded = false;
    private bool isDead = false;

    // 관성 시스템
    private float leftMomentum = 0f;
    private float rightMomentum = 0f;
    private float leftKeyHoldTime = 0f;
    private float rightKeyHoldTime = 0f;

    // 벽 충돌 상태
    private bool isCollidingLeftWall = false;
    private bool isCollidingRightWall = false;

    // 점프 grace time (점프 직후 착지 감지 무시)
    private float jumpGraceTime = 0.1f;
    private float lastJumpTime = -1f;

    // 애니메이션 상태 관리
    private AnimationState currentAnimState = AnimationState.Idle;
    private AnimationState previousAnimState = AnimationState.Idle;
    private float animationTransitionDelay = 0.1f;
    private float lastAnimationChangeTime = 0f;

    // Dash 상태 관리
    private bool isDashing = false;
    private float dashStartTime = 0f;

    // 컴포넌트
    private Rigidbody2D playerRigidbody;
    private Animator animator;
    private AudioSource playerAudio;

    // 애니메이터 파라미터 해시
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

        // 체력 초기화
        currentHealth = maxHealth;

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

            // Physics Material 생성 (경사면에서 적당히 미끄러지도록)
            if (capsuleCollider.sharedMaterial == null)
            {
                PhysicsMaterial2D physicsMat = new PhysicsMaterial2D("PlayerPhysics");
                physicsMat.friction = 0.3f;  // 0.4 -> 0.3으로 감소 (경사면에서 미끄러지도록)
                physicsMat.bounciness = 0f;
                capsuleCollider.sharedMaterial = physicsMat;
            }
        }

        Debug.Log("플레이어 충돌체 최적화 완료 (friction=0.3, 55도 경사면 지원)");
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        HandleMovementInput();
        HandleJumpInput();
        HandleDashInput();
        UpdateAnimationState();
    }

    private void HandleMovementInput()
    {
        bool isPressingLeft = Input.GetKey(KeyCode.A);
        bool isPressingRight = Input.GetKey(KeyCode.D);

        // 떨어지는 상태인지 확인 (공중 + 하강)
        bool isFalling = !isGrounded && playerRigidbody.linearVelocity.y < -0.1f;

        // 왼쪽 키 처리 (왼쪽 벽에 붙어있거나 떨어지는 중이면 무시)
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

        // 오른쪽 키 처리 (오른쪽 벽에 붙어있거나 떨어지는 중이면 무시)
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

        if (leftMomentum > 0 || rightMomentum > 0)
        {
            Vector2 movement = new Vector2(finalHorizontalSpeed, playerRigidbody.linearVelocity.y);
            playerRigidbody.linearVelocity = movement;
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            bool onSlope = IsOnJumpableSlope();

            // 경사면에선 y가 음수로 찍혀도 점프 허용하고 싶으면 이거 아예 빼도 됨
            bool isFallingFromCliff = playerRigidbody.linearVelocity.y < -0.1f && IsOnFlatGround();

            bool canJump = (isGrounded || onSlope) && !isFallingFromCliff;

            Debug.Log($"[JUMP INPUT] W! grounded={isGrounded}, onSlope={onSlope}, velY={playerRigidbody.linearVelocity.y:F2}, canJump={canJump}");

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
        // R 키를 누르고 현재 Dash 중이 아닐 때만 실행
        if (Input.GetKeyDown(KeyCode.R) && !isDashing)
        {
            isDashing = true;
            dashStartTime = Time.time;
            SetAnimationState(AnimationState.Dash);
            Debug.Log("[DASH] 돌진 애니메이션 실행!");
        }

        // Dash 지속 시간이 끝나면 Dash 상태 해제
        if (isDashing && Time.time - dashStartTime >= dashDuration)
        {
            isDashing = false;
            Debug.Log("[DASH] 돌진 애니메이션 종료!");
        }
    }

    private Vector2 GetFeetPos()
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            return (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y + 0.02f);
        }
        // 혹시 콜라이더 없으면 대충
        return (Vector2)transform.position + Vector2.down * 0.15f;
    }

    /// <summary>
    /// 55도 이하 경사면 위에 있으면 true
    /// </summary>
    private bool IsOnJumpableSlope()
    {
        Vector2 origin = GetFeetPos();
        float dist = 0.6f; // 살짝 여유

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            dist,
            groundLayer
        );

        // 디버그 찍어보면 바로 느낌 옴
        if (hit.collider != null)
        {
            float ny = hit.normal.y;
            // 55도 이하 → normal.y > cos(55°) ≈ 0.57
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
        Debug.Log($"[JUMP] 점프 실행! Time={Time.time:F3}");
    }

    private void UpdateAnimationState()
    {
        if (animator == null || isDead) return;

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
                animator.SetTrigger(JumpHash);
                break;

            case AnimationState.Die:
                animator.SetBool(GroundedHash, false);
                animator.SetBool(IsMovingHash, false);
                animator.SetTrigger(DieHash);
                break;

            case AnimationState.Dash:
                animator.SetTrigger(DashHash);
                break;
        }
    }

    // 데미지를 받는 메서드
    public void TakeDamage(int damage)
    {
        // 무적 시간 체크
        if (Time.time - lastDamageTime < invincibilityTime)
        {
            Debug.Log("무적 시간 중! 데미지 무시");
            return;
        }

        if (isDead)
        {
            return;
        }

        // 데미지 적용
        currentHealth -= damage;
        lastDamageTime = Time.time;

        Debug.Log($"플레이어 데미지 받음! 현재 체력: {currentHealth}/{maxHealth}");

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

        // 체력이 0 이하면 사망
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
        }

        CheckWallCollisionExit(collision);
    }

    private void CheckGroundContact(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 위쪽으로 향한 충돌면만 바닥으로 인식 (normal.y > 0.57 = 약 55도 이상)
            if (contact.normal.y > 0.4f)
            {  // 0.7 -> 0.57로 변경 (55도)
                // 평평한 바닥인지 경사면인지 판단
                bool isFlat = contact.normal.y > 0.95f;

                if (isFlat)
                {
                    // 평평한 바닥: grace time 체크
                    float timeSinceJump = Time.time - lastJumpTime;
                    if (timeSinceJump < jumpGraceTime)
                    {
                        Debug.Log($"[GRACE] Grace time 중 ({timeSinceJump:F3}s < {jumpGraceTime}s)");
                        return;
                    }
                }
                // 경사면은 grace time 무시하고 즉시 착지!

                // 착지!
                if (!isGrounded)
                {
                    Debug.Log($"[GROUND] 착지! (normal.y={contact.normal.y:F2}, flat={isFlat}, angle={(Mathf.Acos(contact.normal.y) * Mathf.Rad2Deg):F1}°)");
                }
                isGrounded = true;
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
        return hit.collider != null && hit.normal.y > 0.3f; // 너무 빡빡하게 안 함
    }

    private void CheckWallCollision(Collision2D collision)
    {
        bool foundLeftWall = false;
        bool foundRightWall = false;

        foreach (ContactPoint2D contact in collision.contacts)
        {
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
}
