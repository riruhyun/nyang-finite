using UnityEngine;

/// <summary>
/// 비둘기 적 - 카메라 위에서 소환되어 포물선을 그리며 플레이어에게 돌진합니다.
/// 플레이어가 공격하지 않으면 토스트를 빼앗고 체력 1칸을 감소시킵니다.
/// 한 대 맞으면 즉사합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PigeonController : Enemy
{
    public enum PigeonState
    {
        Spawning,       // 소환 대기 (카메라 위)
        Diving,         // 포물선 돌진 중
        Attacking,      // 플레이어에게 도달하여 공격 중
        Hurt,           // 피격 중
        Dead            // 사망
    }

    [Header("Pigeon Settings")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float toastStealDamage = 1f;     // 토스트 탈취 시 데미지 (체력 1칸)
    [SerializeField] private float spawnHeightAboveCamera = 3f; // 카메라 위 소환 높이
    [SerializeField] private float attackRadius = 0.5f;       // 공격 판정 반경

    [Header("Parabola Settings")]
    [SerializeField] private float parabolicHeight = 4f;      // 포물선 최대 높이
    [SerializeField] private float diveDuration = 1.2f;       // 돌진 지속 시간

    [Header("Trigger")]
    [SerializeField] private float triggerDistance = 5f;      // 플레이어가 비둘기보다 이 거리만큼 앞에 있으면 돌진

    // Animation
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");

    [Header("Hurt Animation")]
    [SerializeField] private float hurtAnimationDuration = 0.5f; // Hurt 애니메이션 지속 시간

    // State
    private PigeonState currentState = PigeonState.Spawning;
    private Vector3 diveStartPosition;
    private Vector3 diveTargetPosition;
    private float diveProgress;
    private bool hasTriedToSteal = false;
    private Camera mainCamera;
    private PlayerController playerController;

    protected override void Start()
    {
        base.Start();

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        // PlayerController 참조 가져오기
        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<PlayerController>();
        }

        // Rigidbody2D 설정 - 물리 무시 (직접 위치 제어)
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 초기 상태 설정
        currentState = PigeonState.Spawning;

        Debug.Log($"[Pigeon] 생성됨! 위치: {transform.position}");
    }

    protected override void Update()
    {
        // Hurt, Dead 상태에서는 Update 로직 중지
        if (!isAlive || currentState == PigeonState.Hurt || currentState == PigeonState.Dead) return;

        switch (currentState)
        {
            case PigeonState.Spawning:
                UpdateSpawning();
                break;
            case PigeonState.Diving:
                UpdateDiving();
                break;
            case PigeonState.Attacking:
                UpdateAttacking();
                break;
        }

        UpdateSpriteDirection();
    }

    /// <summary>
    /// 소환 위치 설정 - 카메라 위 + 플레이어 근처 X 좌표에서 랜덤 오프셋
    /// </summary>
    public void SetupSpawnPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindAnyObjectByType<Camera>();
        }

        if (mainCamera != null && playerTransform != null)
        {
            // 카메라 상단 Y 좌표 계산
            float cameraTopY = mainCamera.transform.position.y + mainCamera.orthographicSize;

            // 플레이어 X 좌표 + 랜덤 오프셋
            float randomOffsetX = Random.Range(-3f, 3f);
            float spawnX = playerTransform.position.x + randomOffsetX;

            // 소환 위치
            transform.position = new Vector3(spawnX, cameraTopY + spawnHeightAboveCamera, 0f);

            Debug.Log($"[Pigeon] 소환 위치 설정: {transform.position}, 카메라 상단 Y: {cameraTopY}");
        }
    }

    /// <summary>
    /// 소환 대기 상태 - 플레이어가 비둘기보다 앞에 있으면 돌진 시작
    /// </summary>
    private void UpdateSpawning()
    {
        if (playerTransform == null) return;

        // 플레이어가 비둘기의 X 좌표보다 triggerDistance 이상 앞에 있으면 돌진
        float playerAheadDistance = playerTransform.position.x - transform.position.x;

        if (playerAheadDistance >= triggerDistance)
        {
            StartDive();
        }
    }

    /// <summary>
    /// 돌진 시작 - 포물선 궤적 계산
    /// </summary>
    private void StartDive()
    {
        if (playerTransform == null) return;

        currentState = PigeonState.Diving;
        diveStartPosition = transform.position;
        diveTargetPosition = playerTransform.position;
        diveProgress = 0f;
        hasTriedToSteal = false;

        // Walk 애니메이션 재생 (날아오는 동작)
        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, true);
        }

        Debug.Log($"[Pigeon] 돌진 시작! {diveStartPosition} → {diveTargetPosition}");
    }

    /// <summary>
    /// 돌진 중 - 포물선 궤적 이동
    /// </summary>
    private void UpdateDiving()
    {
        if (playerTransform == null)
        {
            currentState = PigeonState.Attacking; // 플레이어 없으면 화면 밖으로 이동 후 제거
            return;
        }

        // 진행률 계산
        diveProgress += Time.deltaTime / diveDuration;
        diveProgress = Mathf.Clamp01(diveProgress);

        // 타겟 위치 실시간 업데이트 (플레이어 추적)
        Vector3 currentTarget = playerTransform.position;

        // 포물선 궤적 계산
        Vector3 newPos = CalculateParabolicPosition(diveStartPosition, currentTarget, diveProgress);
        transform.position = newPos;

        // 플레이어와의 거리 체크
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer <= attackRadius)
        {
            // 플레이어에게 도달!
            TryStealToast();
            currentState = PigeonState.Attacking;
            return;
        }

        // 돌진이 끝났지만 플레이어에게 못 닿았을 경우 - 다시 올라가서 제거
        if (diveProgress >= 1f)
        {
            currentState = PigeonState.Attacking; // Attacking 상태에서 위로 올라가며 제거됨
            Debug.Log("[Pigeon] 돌진 실패, 화면 밖으로 이동");
        }
    }

    /// <summary>
    /// 포물선 위치 계산
    /// </summary>
    private Vector3 CalculateParabolicPosition(Vector3 start, Vector3 end, float t)
    {
        // 선형 보간 기본 위치
        Vector3 linearPos = Vector3.Lerp(start, end, t);

        // 포물선 높이 계산 (0에서 시작, 중간에서 최대, 끝에서 0)
        // sin 함수로 부드러운 아크 생성
        float parabolicOffset = Mathf.Sin(t * Mathf.PI) * parabolicHeight;

        // 시작점이 이미 높으면 포물선 높이를 줄임
        float heightDiff = start.y - end.y;
        if (heightDiff > 0)
        {
            // 이미 위에서 내려오므로 포물선 높이 감소
            parabolicOffset *= Mathf.Clamp01(1f - heightDiff / 10f);
        }

        linearPos.y += parabolicOffset;
        return linearPos;
    }

    /// <summary>
    /// 공격 중 - 토스트 탈취 시도 후 화면 밖으로 이동
    /// </summary>
    private void UpdateAttacking()
    {
        // 공격 후 화면 밖으로 이동 후 제거
        if (mainCamera == null) return;

        // 위쪽으로 이동
        transform.position += Vector3.up * 6f * Time.deltaTime;

        // 카메라 위로 완전히 벗어나면 제거
        float cameraTopY = mainCamera.transform.position.y + mainCamera.orthographicSize;
        if (transform.position.y > cameraTopY + 5f)
        {
            Debug.Log("[Pigeon] 화면 밖으로 도망, 제거됨");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 토스트 탈취 시도
    /// </summary>
    private void TryStealToast()
    {
        if (hasTriedToSteal || playerController == null) return;
        hasTriedToSteal = true;

        // 플레이어가 공격 중인지 확인 (Scratch, Punch, Dash)
        bool isPlayerAttacking = playerController.IsAttacking();

        if (isPlayerAttacking)
        {
            // 플레이어가 공격 중이면 비둘기가 데미지를 받음
            Debug.Log("[Pigeon] 플레이어 공격에 격퇴됨!");
            TakeDamage(attackDamage, (transform.position - playerTransform.position).normalized);
            return;
        }

        // 플레이어가 토스트를 갖고 있는지 확인
        string currentToastId = playerController.CurrentToastId;
        if (!string.IsNullOrEmpty(currentToastId))
        {
            // 토스트 탈취!
            Debug.Log($"[Pigeon] 토스트 탈취 성공! ToastId: {currentToastId}");
            playerController.RemoveToast();

            // 체력 데미지
            playerController.TakeDamage(toastStealDamage, gameObject);
        }
        else
        {
            // 토스트가 없어도 데미지만 줌
            Debug.Log("[Pigeon] 토스트 없음, 공격만 실행");
            playerController.TakeDamage(attackDamage, gameObject);
        }
    }
    /// <summary>
    /// 스프라이트 방향 업데이트
    /// </summary>
    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null) return;

        Vector3 moveDir = Vector3.zero;

        switch (currentState)
        {
            case PigeonState.Diving:
                if (playerTransform != null)
                    moveDir = playerTransform.position - transform.position;
                break;
            case PigeonState.Attacking:
                moveDir = Vector3.up;
                break;
        }

        if (moveDir.x != 0)
        {
            spriteRenderer.flipX = moveDir.x < 0;
        }
    }

    /// <summary>
    /// 데미지 처리 오버라이드 - 한 대 맞으면 즉사
    /// </summary>
    public override void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (!isAlive || currentState == PigeonState.Hurt || currentState == PigeonState.Dead) return;

        Debug.Log($"[Pigeon] 데미지 받음: {damage}, 즉사 처리!");

        // Hurt 상태로 전환
        currentState = PigeonState.Hurt;

        // 한 대 맞으면 바로 사망 처리
        StartCoroutine(PlayHurtThenDeath());
    }

    /// <summary>
    /// Hurt 애니메이션 재생 후 Death 처리
    /// </summary>
    private System.Collections.IEnumerator PlayHurtThenDeath()
    {
        // 이미 Hurt 상태로 전환됨 (TakeDamage에서)

        // Hurt 애니메이션 재생
        if (animator != null)
        {
            // 모든 파라미터 초기화
            animator.SetBool(IsWalkingHash, false);
            animator.ResetTrigger(DieHash);

            // CrossFade로 즉시 Hurt 애니메이션 재생
            animator.CrossFade("Hurt", 0f, 0);
            Debug.Log($"[Pigeon] Hurt 애니메이션 CrossFade 실행! Animator enabled: {animator.enabled}, GameObject active: {gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[Pigeon] Animator가 null입니다!");
        }

        Debug.Log("[Pigeon] Hurt 애니메이션 시작 (사망 예정)");

        // Hurt 애니메이션 대기
        yield return new WaitForSeconds(hurtAnimationDuration);

        // Death 상태로 전환 및 처리
        currentState = PigeonState.Dead;
        Die();
    }

    /// <summary>
    /// 사망 처리 오버라이드
    /// </summary>
    protected override void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        // 이미 Dead 상태로 전환됨 (PlayHurtThenDeath에서)

        Debug.Log("[Pigeon] 사망!");

        // Death 애니메이션 재생 - CrossFade 사용
        if (animator != null)
        {
            animator.CrossFade("Death", 0f, 0);
            Debug.Log("[Pigeon] Death 애니메이션 CrossFade 실행!");
        }

        // Rigidbody 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Destroy(gameObject, 1f);
    }

    /// <summary>
    /// 현재 상태 반환
    /// </summary>
    public PigeonState GetCurrentState() => currentState;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // 공격 반경 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // 포물선 경로 미리보기 (에디터에서)
        if (playerTransform != null && currentState == PigeonState.Spawning)
        {
            Gizmos.color = Color.yellow;
            Vector3 start = transform.position;
            Vector3 end = playerTransform.position;

            Vector3 prev = start;
            for (float t = 0.1f; t <= 1f; t += 0.1f)
            {
                Vector3 pos = CalculateParabolicPosition(start, end, t);
                Gizmos.DrawLine(prev, pos);
                prev = pos;
            }
        }
    }
}
