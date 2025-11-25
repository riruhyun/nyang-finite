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
    public AudioClip damageClip; // ?°ë?ì§€ ë°›ì„ ???¬ìš´??

    [Header("Health Settings")]
    public float maxHealth = 9f; // ìµœë? ì²´ë ¥
    private float currentHealth; // ?„ì¬ ì²´ë ¥
    public float invincibilityTime = 1.5f; // ë¬´ì  ?œê°„
    private float lastDamageTime = -10f; // ë§ˆì?ë§??°ë?ì§€ë¥?ë°›ì? ?œê°„

    [Header("Movement Settings")]
    public float jumpForce = 700f;
    public float moveSpeed = 5f;
    [Header("Slope Handling")]
    [SerializeField] private float slopeNormalMin = 0.4f; // ê²½ì‚¬ë©?55???¬í•¨) ?´ìƒ?´ë©´ ë°”ë‹¥ ì·¨ê¸‰
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
    public float dashDuration = 0.5f; // Dash ? ë‹ˆë©”ì´??ì§€???œê°„

    [SerializeField] private LayerMask groundLayer;

    [Header("Scratch Settings")]
    [SerializeField] private BoxCollider2D scratchHitbox;
    [SerializeField] private LayerMask scratchDamageLayers;
    [SerializeField] private float scratchDamage = 1.5f;
    [SerializeField] private float scratchDuration = 0.78f; // Scratch.anim length
    [SerializeField] private string scratchAnimationName = "Scratch";

    // ë¬¼ë¦¬ ?íƒœ
    private bool isGrounded = false;
    private bool isDead = false;
    private bool onSlope = false;
    private bool isScratching = false;

    // ê´€???œìŠ¤??
    private float leftMomentum = 0f;
    private float rightMomentum = 0f;
    private float leftKeyHoldTime = 0f;
    private float rightKeyHoldTime = 0f;

    // ë²?ì¶©ëŒ ?íƒœ
    private bool isCollidingLeftWall = false;
    private bool isCollidingRightWall = false;

    // ?í”„ grace time (?í”„ ì§í›„ ì°©ì? ê°ì? ë¬´ì‹œ)
    private float jumpGraceTime = 0.1f;
    private float lastJumpTime = -1f;

    // ? ë‹ˆë©”ì´???íƒœ ê´€ë¦?
    private AnimationState currentAnimState = AnimationState.Idle;
    private AnimationState previousAnimState = AnimationState.Idle;
    private float animationTransitionDelay = 0.1f;
    private float lastAnimationChangeTime = 0f;
    private float pendingJumpResumeNormalizedTime = -1f;

    // Dash ?íƒœ ê´€ë¦?
    private bool isDashing = false;
    private float dashStartTime = 0f;

    // ì»´í¬?ŒíŠ¸
    private Rigidbody2D playerRigidbody;
    private Animator animator;
    private AudioSource playerAudio;
    private readonly Collider2D[] scratchHits = new Collider2D[8];
    private bool wasAirborneBeforeScratch = false;
    private float savedJumpNormalizedTime = 0f;
    private float savedJumpLength = 0.85f;
    private float scratchEndTime = -1f;
    private float scratchStartTime = -1f;
    private bool scratchStoppedAnimator = false;
    private AnimationClip scratchClip;
    private PhysicsMaterial2D runtimeMaterial;
    private bool wasOnSlope = false; // ?´ì „ ?„ë ˆ?„ì— ê²½ì‚¬ë©??„ì??”ì? ì¶”ì 

    // ? ë‹ˆë©”ì´???Œë¼ë¯¸í„° ?´ì‹œ
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
        CacheScratchClip();

        // Ã¼·Â ÃÊ±âÈ­
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

            // Physics Material ?ì„± (ê²½ì‚¬ë©´ì—???ë‹¹??ë¯¸ë„?¬ì??„ë¡)
            if (capsuleCollider.sharedMaterial == null)
            {
                PhysicsMaterial2D physicsMat = new PhysicsMaterial2D("PlayerPhysics");
                physicsMat.friction = flatFriction;  // ê¸°ë³¸ ?‰ì? ë§ˆì°°
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

        Debug.Log("?Œë ˆ?´ì–´ ì¶©ëŒì²?ìµœì ???„ë£Œ (friction=0.3, 55??ê²½ì‚¬ë©?ì§€??");
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        HandleScratchInput();
        UpdateScratchState();
        bool allowMovement = !isScratching;
        if (allowMovement)
        {
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

        // ?¨ì–´ì§€???íƒœ?¸ì? ?•ì¸ (ê³µì¤‘ + ?˜ê°•)
        bool isFalling = !isGrounded && playerRigidbody.linearVelocity.y < -0.1f;

        // ?¼ìª½ ??ì²˜ë¦¬ (?¼ìª½ ë²½ì— ë¶™ì–´?ˆê±°???¨ì–´ì§€??ì¤‘ì´ë©?ë¬´ì‹œ)
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

        // ?¤ë¥¸ìª???ì²˜ë¦¬ (?¤ë¥¸ìª?ë²½ì— ë¶™ì–´?ˆê±°???¨ì–´ì§€??ì¤‘ì´ë©?ë¬´ì‹œ)
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

            // ê²½ì‚¬ë©´ì—??yê°€ ?Œìˆ˜ë¡?ì°í????í”„ ?ˆìš©?˜ê³  ?¶ìœ¼ë©??´ê±° ?„ì˜ˆ ë¹¼ë„ ??
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
        // R ?¤ë? ?„ë¥´ê³??„ì¬ Dash ì¤‘ì´ ?„ë‹ ?Œë§Œ ?¤í–‰
        if (Input.GetKeyDown(KeyCode.R) && !isDashing)
        {
            isDashing = true;
            dashStartTime = Time.time;
            SetAnimationState(AnimationState.Dash);
            Debug.Log("[DASH] ?Œì§„ ? ë‹ˆë©”ì´???¤í–‰!");
        }

        // Dash ì§€???œê°„???ë‚˜ë©?Dash ?íƒœ ?´ì œ
        if (isDashing && Time.time - dashStartTime >= dashDuration)
        {
            isDashing = false;
            Debug.Log("[DASH] ?Œì§„ ? ë‹ˆë©”ì´??ì¢…ë£Œ!");
        }
    }

    private Vector2 GetFeetPos()
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            return (Vector2)col.bounds.center + Vector2.down * (col.bounds.extents.y + 0.02f);
        }
        // ?¹ì‹œ ì½œë¼?´ë” ?†ìœ¼ë©??€ì¶?
        return (Vector2)transform.position + Vector2.down * 0.15f;
    }

    /// <summary>
    /// 55???´í•˜ ê²½ì‚¬ë©??„ì— ?ˆìœ¼ë©?true
    /// </summary>
    private bool IsOnJumpableSlope()
    {
        Vector2 origin = GetFeetPos();
        float dist = 0.6f; // ?´ì§ ?¬ìœ 

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            dist,
            groundLayer
        );

        // ?”ë²„ê·?ì°ì–´ë³´ë©´ ë°”ë¡œ ?ë‚Œ ??
        if (hit.collider != null)
        {
            float ny = hit.normal.y;
            // 55???´í•˜ ??normal.y > cos(55Â°) ??0.57
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
        Debug.Log($"[JUMP] ?í”„ ?¤í–‰! Time={Time.time:F3}");
    }

    private void UpdateAnimationState()
    {
        if (animator == null || isDead) return;

        if (isScratching)
        {
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

        // Dash ì¤‘ì¼ ?ŒëŠ” Dash ?íƒœ ? ì?
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
                animator.Play(scratchAnimationName, 0, 0f);
                scratchStoppedAnimator = false;
                break;
        }
    }

    // ?°ë?ì§€ë¥?ë°›ëŠ” ë©”ì„œ??
    public void TakeDamage(float damage)
    {
        // ë¬´ì  ?œê°„ ì²´í¬
        if (Time.time - lastDamageTime < invincibilityTime)
        {
            Debug.Log("ë¬´ì  ?œê°„ ì¤? ?°ë?ì§€ ë¬´ì‹œ");
            return;
        }

        if (isDead)
        {
            return;
        }

        // ?°ë?ì§€ ?ìš©
        currentHealth -= damage;
        lastDamageTime = Time.time;

        Debug.Log($"?Œë ˆ?´ì–´ ?°ë?ì§€ ë°›ìŒ! ?„ì¬ ì²´ë ¥: {currentHealth}/{maxHealth}");

        // ?°ë?ì§€ ?¬ìš´???¬ìƒ
        if (playerAudio != null && damageClip != null)
        {
            playerAudio.PlayOneShot(damageClip);
        }

        // GameManager??ì²´ë ¥ ?…ë°?´íŠ¸ ?Œë¦¼
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateHealth(currentHealth);
        }

        // ì²´ë ¥??0 ?´í•˜ë©??¬ë§
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
            Debug.Log("[GROUND] ê³µì¤‘ ?íƒœ");

            // ê²½ì‚¬ë©´ì„ ?€ê³??¬ë¼ê°€??ì§€ë©´ì„ ?ƒì? ê²½ìš°?ë§Œ ëª¨ë©˜?€/?˜í‰?ë„ ?œê±°
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
            // ?„ìª½?¼ë¡œ ?¥í•œ ì¶©ëŒë©´ë§Œ ë°”ë‹¥?¼ë¡œ ?¸ì‹ (normal.y > slopeNormalMin)
            if (contact.normal.y > slopeNormalMin)
            {
                // ?‰í‰??ë°”ë‹¥?¸ì? ê²½ì‚¬ë©´ì¸ì§€ ?ë‹¨
                bool isFlat = contact.normal.y > 0.95f;

                if (isFlat)
                {
                    // ?‰í‰??ë°”ë‹¥: grace time ì²´í¬
                    float timeSinceJump = Time.time - lastJumpTime;
                    if (timeSinceJump < jumpGraceTime)
                    {
                        Debug.Log($"[GRACE] Grace time ì¤?({timeSinceJump:F3}s < {jumpGraceTime}s)");
                        return;
                    }
                }
                // ê²½ì‚¬ë©´ì? grace time ë¬´ì‹œ?˜ê³  ì¦‰ì‹œ ì°©ì?!

                // ì°©ì?!
                if (!isGrounded)
                {
                    Debug.Log($"[GROUND] ì°©ì?! (normal.y={contact.normal.y:F2}, flat={isFlat}, angle={(Mathf.Acos(contact.normal.y) * Mathf.Rad2Deg):F1}Â°)");
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
                // ë£¨í”„ ?¤ì •?´ë¼??1???¬ìƒ?˜ë„ë¡?WrapMode ë³€ê²??œë„
                clip.wrapMode = WrapMode.Once;
                if (scratchDuration < clip.length)
                {
                    scratchDuration = clip.length;
                }
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
        // ?´ë™/?í”„/?€???…ë ¥ ë¬´ì‹œ ?íƒœë¡??„í™˜
        leftMomentum = 0f;
        rightMomentum = 0f;
        leftKeyHoldTime = 0f;
        rightKeyHoldTime = 0f;

        if (animator != null)
        {
            animator.speed = 1f; // ensure normal speed at start
        }
        isScratching = true;
        scratchStartTime = Time.time;
        float clipLen = scratchClip != null ? scratchClip.length : scratchDuration;
        scratchEndTime = Time.time + Mathf.Max(scratchDuration, clipLen);
        wasAirborneBeforeScratch = !isGrounded;
        scratchStoppedAnimator = false;

        // Animatorê°€ ?¤ë¥¸ ?íƒœë¡?ì¦‰ì‹œ ??–´?°ì? ?Šë„ë¡??„ì¬ ?íƒœë¥?ë¦¬ì…‹
        if (animator != null)
        {
            animator.ResetTrigger(JumpHash);
            animator.ResetTrigger(DieHash);
            animator.ResetTrigger(DashHash);
        }

        if (wasAirborneBeforeScratch && animator != null)
        {
            AnimatorStateInfo jumpState = animator.GetCurrentAnimatorStateInfo(0);
            savedJumpNormalizedTime = jumpState.normalizedTime;
            savedJumpLength = Mathf.Max(0.01f, jumpState.length);
        }

        SetAnimationState(AnimationState.Scratch);
        ApplyScratchDamage();
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
            // ? ë‹ˆë©”ì´??1???¬ìƒ ??ë°”ë¡œ ì¢…ë£Œ
            if (state.IsName(scratchAnimationName) && state.normalizedTime >= 1f)
            {
                EndScratch();
                return;
            }
        }

        if (Time.time >= scratchEndTime)
        {
            EndScratch();
        }
    }

    private void EndScratch()
    {
        if (!isScratching) return;

        isScratching = false;
        if (scratchStoppedAnimator && animator != null)
        {
            animator.speed = 1f;
        }
        scratchStoppedAnimator = false;
        scratchEndTime = -1f;

        if (wasAirborneBeforeScratch && !isGrounded)
        {
            float scratchElapsed = Time.time - scratchStartTime;
            float extraNormalized = savedJumpLength > 0.001f
                ? scratchElapsed / savedJumpLength
                : 0f;
            pendingJumpResumeNormalizedTime = savedJumpNormalizedTime + extraNormalized;
        }
        else
        {
            pendingJumpResumeNormalizedTime = -1f;
        }

        wasAirborneBeforeScratch = false;
        SetAnimationState(DetermineAnimationState());
    }

private void ApplyScratchDamage()
    {
        if (scratchHitbox == null)
        {
            // ?ë™ ?ìƒ‰ ?œë„
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
        
        // ?´ë? ê³µê²©???ë“¤??ì¶”ì ?˜ê¸° ?„í•œ HashSet
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
                // ?´ë? ?€ê²©í•œ ?ì¸ì§€ ?•ì¸
                int dogInstanceID = dog.GetInstanceID();
                if (!alreadyHitEnemies.Contains(dogInstanceID))
                {
                    alreadyHitEnemies.Add(dogInstanceID);
                    dog.TakeDamage(scratchDamage, knockbackDir);
                    Debug.Log($"[Player] Scratch °ø°İÀ¸·Î {dog.name}¿¡°Ô {scratchDamage} µ¥¹ÌÁö °¡ÇÔ");
                }
            }
        }
    }
}




