using UnityEngine;

/// <summary>
/// 모든 적 캐릭터의 기본 클래스
/// </summary>
public class Enemy : MonoBehaviour
{
  [Header("Combat Stats")]
  [Tooltip("공격 속도 (초당 공격 횟수)")]
  [SerializeField] protected float attackSpeed = 1f;

  [Tooltip("적의 최대 체력")]
  [SerializeField] protected float maxHealth = 100f;

  [Tooltip("적의 현재 체력")]
  protected float currentHealth;

  [Header("Movement")]
  [Tooltip("이동 속도")]
  [SerializeField] protected float moveSpeed = 2f; // 고양이(5f)보다 느리게 설정

  [Tooltip("점프 수치 (점프력)")]
  [SerializeField] protected float jumpForce = 10f;

  [Header("Knockback")]
  [Tooltip("넉백 거리")]
  [SerializeField] protected float knockbackDistance = 2f;

  [Tooltip("넉백 지속 시간")]
  [SerializeField] protected float knockbackDuration = 0.2f;

  [Header("Detection")]
  [Tooltip("플레이어 인식 범위 (카메라 가로 길이 기준)")]
  [SerializeField] protected float detectionRange = 10f;

  [Tooltip("플레이어 Transform 참조")]
  protected Transform playerTransform;

  [Header("State")]
  [Tooltip("현재 적이 살아있는지 여부")]
  protected bool isAlive = true;

  [Tooltip("현재 넉백 상태인지 여부")]
  public bool isKnockedBack = false;

  [Tooltip("마지막 공격 시간")]
  protected float lastAttackTime = 0f;

  // Components
  protected Rigidbody2D rb;
  protected Animator animator;
  protected SpriteRenderer spriteRenderer;
  [SerializeField] private SpriteRenderer facingSpriteRenderer;
  private enum DeathColliderPreset
  {
    Auto,
    None,
    Dog,
    Cat
  }

  [Header("Death Collider")]
  [SerializeField] private BoxCollider2D deathCollider;
  [SerializeField] private DeathColliderPreset deathColliderPreset = DeathColliderPreset.Auto;
  [Tooltip("왼쪽을 바라보는 상태에서 사망했을 때 추가로 이동할 X 패딩 (양수면 더 왼쪽으로 이동).")]
  [SerializeField] private float leftFacingDeathOffsetPadding = 0f;
  [Tooltip("사망 후 바닥에 닿았는지 판정할 레이어 마스크. 기본은 모든 레이어.")]
  [SerializeField] private LayerMask deathSettleGroundLayers = ~0;
  [Tooltip("사망 후 정지로 간주할 최소 낙하 시간(초).")]
  [SerializeField] private float deathSettleMinTime = 0.05f;
  [Tooltip("사망 후 정지로 간주할 수직 속도 임계값.")]
  [SerializeField] private float deathSettleVelocityThreshold = 0.05f;

  protected virtual void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    spriteRenderer = GetComponent<SpriteRenderer>();
    if (spriteRenderer == null)
    {
      spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    if (facingSpriteRenderer == null)
    {
      facingSpriteRenderer = spriteRenderer;
    }
    if (deathCollider != null)
    {
      deathCollider.enabled = false;
    }
  }

  protected virtual void Start()
  {
    currentHealth = maxHealth;

    // 플레이어 찾기
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
      playerTransform = player.transform;
    }
  }

  protected virtual void Update()
  {
    if (!isAlive || isKnockedBack) return;

    CheckPlayerInRange();
  }

  protected virtual void FixedUpdate()
  {
    if (awaitingDeathAlignment && !deathAlignmentComplete && ShouldFinalizeDeathPose())
    {
      AlignCapsuleCollidersForDeath();
    }
  }

  /// <summary>
  /// 플레이어가 인식 범위 내에 있는지 확인
  /// </summary>
  protected virtual void CheckPlayerInRange()
  {
    if (playerTransform == null) return;

    float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

    if (distanceToPlayer <= detectionRange)
    {
      OnPlayerDetected();
    }
  }

  /// <summary>
  /// 플레이어가 감지되었을 때 호출
  /// </summary>
  protected virtual void OnPlayerDetected()
  {
    // 자식 클래스에서 구현
  }

  /// <summary>
  /// 적이 데미지를 받을 때 호출
  /// </summary>
  public virtual void TakeDamage(float damage, Vector2 knockbackDirection)
  {
    if (!isAlive) return;

    currentHealth -= damage;

    if (currentHealth <= 0)
    {
      Die();
    }
    else
    {
      ApplyKnockback(knockbackDirection);
    }
  }

  /// <summary>
  /// 넉백 적용
  /// </summary>
  protected virtual void ApplyKnockback(Vector2 direction)
  {
    if (rb == null) return;

    isKnockedBack = true;
    float magnitude = direction.magnitude;
    // Respect provided impulse magnitude when available; otherwise fall back to distance-based knockback.
    Vector2 knockbackForce = magnitude > 1.01f
        ? direction
        : direction.normalized * knockbackDistance;
    rb.AddForce(knockbackForce, ForceMode2D.Impulse);

    Invoke(nameof(ResetKnockback), knockbackDuration);
  }

  /// <summary>
  /// 넉백 상태 해제
  /// </summary>
  protected virtual void ResetKnockback()
  {
    isKnockedBack = false;
  }

  /// <summary>
  /// 적이 죽었을 때 호출
  /// </summary>
  protected virtual void Die()
  {
    isAlive = false;

    // 사망 애니메이션 재생 (있다면)
    if (animator != null)
    {
      animator.SetTrigger("Die");
    }

    awaitingDeathAlignment = rb != null;
    deathAlignmentComplete = false;
    deathFallStartTime = Time.time;
    EnsureDeathColliderDisabled();

    if (rb != null)
    {
      rb.bodyType = RigidbodyType2D.Dynamic;
    }

    if (!awaitingDeathAlignment)
    {
      AlignCapsuleCollidersForDeath();
    }

    // 일정 시간 후 오브젝트 제거
    Destroy(gameObject, 2f);
  }

  protected void AlignCapsuleCollidersForDeath()
  {
    awaitingDeathAlignment = false;
    deathAlignmentComplete = true;

    var capsules = GetComponentsInChildren<CapsuleCollider2D>();
    if (capsules != null)
    {
      foreach (var capsule in capsules)
      {
        if (capsule != null) Destroy(capsule);
      }
    }

    var box = deathCollider != null ? deathCollider : GetComponent<BoxCollider2D>();
    if (box == null)
    {
      box = gameObject.AddComponent<BoxCollider2D>();
    }
    ApplyDeathColliderPreset(box);
    box.gameObject.SetActive(true);
    box.enabled = true;
    box.isTrigger = false;

    int layer = LayerMask.NameToLayer("Obstacles");
    if (layer >= 0)
    {
      gameObject.layer = layer;
      foreach (Transform child in transform)
      {
        child.gameObject.layer = layer;
      }
    }

    if (rb != null)
    {
      rb.linearVelocity = Vector2.zero;
      rb.angularVelocity = 0f;
      rb.bodyType = RigidbodyType2D.Static;
    }
  }

  private void ApplyDeathColliderPreset(BoxCollider2D box)
  {
    if (box == null) return;

    DeathColliderPreset presetToApply = deathColliderPreset;
    if (presetToApply == DeathColliderPreset.Auto)
    {
      presetToApply = TryDetectPresetFromContext();
    }

    float facingSign = GetFacingSign();

    Vector2 size;
    Vector2 offset;
    bool hasOverride = false;

    switch (presetToApply)
    {
      case DeathColliderPreset.Dog:
        size = new Vector2(0.395f, 0.175f);
        offset = new Vector2(0.05305527f, -0.075f);
        hasOverride = true;
        break;
      case DeathColliderPreset.Cat:
        size = new Vector2(0.32f, 0.105f);
        offset = new Vector2(0.115f, 0.06f);
        hasOverride = true;
        break;
      case DeathColliderPreset.None:
      case DeathColliderPreset.Auto:
      default:
        hasOverride = false;
        size = box.size;
        offset = box.offset;
        break;
    }

    if (hasOverride)
    {
      box.size = size;
      offset.x *= facingSign;
      if (facingSign < 0f)
      {
        offset.x -= Mathf.Abs(leftFacingDeathOffsetPadding);
      }
      box.offset = offset;
    }
    else if (facingSign < 0f && Mathf.Abs(leftFacingDeathOffsetPadding) > 0f)
    {
      var current = box.offset;
      current.x -= Mathf.Abs(leftFacingDeathOffsetPadding);
      box.offset = current;
    }
  }

  private DeathColliderPreset TryDetectPresetFromContext()
  {
    var sr = facingSpriteRenderer != null ? facingSpriteRenderer : spriteRenderer;
    if (sr != null && sr.sprite != null)
    {
      string spriteName = sr.sprite.name.ToLowerInvariant();
      if (spriteName.Contains("cat")) return DeathColliderPreset.Cat;
      if (spriteName.Contains("dog")) return DeathColliderPreset.Dog;
    }

    string goName = gameObject.name.ToLowerInvariant();
    if (goName.Contains("cat")) return DeathColliderPreset.Cat;
    if (goName.Contains("dog")) return DeathColliderPreset.Dog;

    return DeathColliderPreset.None;
  }

  private float GetFacingSign()
  {
    float sign = 1f;
    var sr = facingSpriteRenderer != null ? facingSpriteRenderer : spriteRenderer;
    if (sr != null)
    {
      sign = sr.flipX ? -1f : 1f;
    }
    else if (transform.localScale.x < 0f)
    {
      sign = -1f;
    }

    if (transform.lossyScale.x < 0f)
    {
      sign *= -1f;
    }

    return sign;
  }

  private bool ShouldFinalizeDeathPose()
  {
    if (!awaitingDeathAlignment || deathAlignmentComplete) return false;
    if (rb == null) return true;

    if (Time.time - deathFallStartTime < deathSettleMinTime)
    {
      return false;
    }

    if (!IsTouchingSettleGround())
    {
      return false;
    }

    return Mathf.Abs(rb.linearVelocity.y) <= deathSettleVelocityThreshold;
  }

  private bool IsTouchingSettleGround()
  {
    var colliders = GetComponentsInChildren<Collider2D>();
    if (colliders == null || colliders.Length == 0) return false;

    foreach (var col in colliders)
    {
      if (col == null || !col.enabled) continue;
      if (deathCollider != null && col == deathCollider) continue;
      if (col.IsTouchingLayers(deathSettleGroundLayers))
      {
        return true;
      }
    }
    return false;
  }

  private void EnsureDeathColliderDisabled()
  {
    if (deathCollider == null) return;
    deathCollider.enabled = false;
  }

  private bool awaitingDeathAlignment;
  private bool deathAlignmentComplete;
  private float deathFallStartTime;

  /// <summary>
  /// 공격 실행
  /// </summary>
  protected virtual void Attack()
  {
    if (Time.time - lastAttackTime < 1f / attackSpeed) return;

    lastAttackTime = Time.time;

    // 자식 클래스에서 구체적인 공격 로직 구현
    PerformAttack();
  }

  /// <summary>
  /// 실제 공격 수행 (자식 클래스에서 오버라이드)
  /// </summary>
  protected virtual void PerformAttack()
  {
    // 자식 클래스에서 구현
  }

  /// <summary>
  /// 점프 실행
  /// </summary>
  protected virtual void Jump()
  {
    if (rb == null) return;

    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
  }

  /// <summary>
  /// 이동 처리
  /// </summary>
  protected virtual void Move(Vector2 direction)
  {
    if (rb == null || isKnockedBack) return;

    rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

    // 애니메이터 파라미터 업데이트 (이동 중인지)
    if (animator != null)
    {
      bool isMoving = direction.magnitude > 0.01f;
      animator.SetBool("IsWalking", isMoving);
    }

    // 이동 방향에 따라 스프라이트 뒤집기
    if (direction.x != 0 && spriteRenderer != null)
    {
      spriteRenderer.flipX = direction.x < 0;
    }
  }

  /// <summary>
  /// 디버그용 인식 범위 시각화
  /// </summary>
  protected virtual void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRange);
  }

  // Getters
  public float GetCurrentHealth() => currentHealth;
  public float GetMaxHealth() => maxHealth;
  public bool IsAlive() => isAlive;
  public float GetDetectionRange() => detectionRange;

  /// <summary>
  /// Apply basic stats overrides (used by spawn helper/config).
  /// </summary>
  public virtual void ApplyBaseStats(float moveSpeedOverride, float maxHealthOverride, float attackSpeedOverride)
  {
    if (moveSpeedOverride > 0f) moveSpeed = moveSpeedOverride;
    if (maxHealthOverride > 0f)
    {
      maxHealth = maxHealthOverride;
      currentHealth = maxHealthOverride;
    }
    if (attackSpeedOverride > 0f) attackSpeed = attackSpeedOverride;
  }
}
