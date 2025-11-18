using UnityEngine;

/// <summary>
/// '개' 타입 적 캐릭터
/// Enemy 클래스를 상속받아 구체적인 행동 구현
/// </summary>
public class DogEnemy : Enemy
{
  [Header("Dog Specific Settings")]
  [Tooltip("추격 속도 배율")]
  [SerializeField] private float chaseSpeedMultiplier = 1.2f;

  [Tooltip("공격 범위")]
  [SerializeField] private float attackRange = 1.5f;

  [Tooltip("순찰 범위")]
  [SerializeField] private float patrolRange = 5f;

  [Tooltip("공격 데미지 (하트 1칸 = 1)")]
  [SerializeField] private int attackDamage = 1; // 하트 9칸 기준, 9번 맞으면 사망

  [Tooltip("공격 애니메이션 지속 시간")]
  [SerializeField] private float attackAnimationDuration = 0.5f;

  private bool isAttacking = false;
  private float attackAnimationTimer = 0f;

  [Header("Patrol Settings")]
  [SerializeField] private float patrolWaitTime = 2f;

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

  protected override void Start()
  {
    base.Start();
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

    // 플레이어 감지 및 상태 전환
    CheckAndUpdateState();
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

    Move(new Vector2(patrolDirection, 0));
  }

  /// <summary>
  /// 추격 행동
  /// </summary>
  private void ChaseBehavior()
  {
    if (playerTransform == null) return;

    Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
    Move(directionToPlayer * chaseSpeedMultiplier);
  }

  /// <summary>
  /// 공격 행동
  /// </summary>
  private void AttackBehavior()
  {
    // 공격 중에는 이동 정지
    Move(Vector2.zero);

    // 공격 실행
    Attack();
  }

  /// <summary>
  /// 대기 행동
  /// </summary>
  private void IdleBehavior()
  {
    Move(Vector2.zero);
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
        Debug.Log($"Dog attacks player for {attackDamage} damage!");
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
  /// 디버그용 범위 시각화
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
  }
}
