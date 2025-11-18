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
  protected bool isKnockedBack = false;

  [Tooltip("마지막 공격 시간")]
  protected float lastAttackTime = 0f;

  // Components
  protected Rigidbody2D rb;
  protected Animator animator;
  protected SpriteRenderer spriteRenderer;

  protected virtual void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    spriteRenderer = GetComponent<SpriteRenderer>();
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
    Vector2 knockbackForce = direction.normalized * knockbackDistance;
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

    // 일정 시간 후 오브젝트 제거
    Destroy(gameObject, 2f);
  }

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
}
