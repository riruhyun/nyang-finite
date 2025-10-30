using UnityEngine;

// PlayerController는 플레이어 캐릭터로서 Player 게임 오브젝트를 제어한다.
public class PlayerController : MonoBehaviour {
   public AudioClip deathClip; // 사망시 재생할 오디오 클립
   public float jumpForce = 700f; // 점프 힘
   public float moveSpeed = 5f; // 이동 속도
   public float baseMomentum = 0.5f; // 기본 관성값
   public float maxMomentum = 2.0f; // 최대 관성값 (2로 제한)
   public float momentumBuildTime = 1.0f; // 관성 증가 시간
   public float momentumDecayAmount = 2.5f; // 관성 감소량 (2.0씩 감소)

   private int jumpCount = 0; // 누적 점프 횟수
   private bool isGrounded = false; // 바닥에 닿았는지 나타냄
   private bool isDead = false; // 사망 상태
   private bool hasJumped = false; // 점프 애니메이션 트리거 발동 여부
   
   private float leftMomentum = 0f; // 왼쪽 방향 관성값
   private float rightMomentum = 0f; // 오른쪽 방향 관성값
   private float leftKeyHoldTime = 0f; // 왼쪽 키를 누른 시간
   private float rightKeyHoldTime = 0f; // 오른쪽 키를 누른 시간

   private Rigidbody2D playerRigidbody; // 사용할 리지드바디 컴포넌트
   private Animator animator; // 사용할 애니메이터 컴포넌트
   private AudioSource playerAudio; // 사용할 오디오 소스 컴포넌트

    private void Start() {
        playerRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();
        
        // 애니메이터가 없으면 경고
        if (animator == null) {
            Debug.LogWarning("Animator component not found on " + gameObject.name);
        }
    }

    private void Update() {
        if (isDead) {
            return;
        }

        // 좌우 이동 (A, D 키만 허용, 화살표키 차단)
        bool isPressingLeft = Input.GetKey(KeyCode.A);
        bool isPressingRight = Input.GetKey(KeyCode.D);
        
        // 왼쪽 키 처리
        if (isPressingLeft) {
            leftKeyHoldTime += Time.deltaTime;
            float momentumProgress = Mathf.Clamp01(leftKeyHoldTime / momentumBuildTime);
            leftMomentum = Mathf.Lerp(baseMomentum, maxMomentum, momentumProgress);
            // 왼쪽으로 이동할 때 스프라이트 뒤집기
            transform.localScale = new Vector3(-4f, 4f, 4f);
        } else {
            // 왼쪽 키를 놓으면 관성 감소 (1.5씩 더 빠르게 감소)
            if (leftMomentum > 0) {
                leftMomentum -= momentumDecayAmount * Time.deltaTime * 10f; // 10배 더 빠르게
                if (leftMomentum < 0) leftMomentum = 0;
            }
            leftKeyHoldTime = 0f;
        }
        
        // 오른쪽 키 처리
        if (isPressingRight) {
            rightKeyHoldTime += Time.deltaTime;
            float momentumProgress = Mathf.Clamp01(rightKeyHoldTime / momentumBuildTime);
            rightMomentum = Mathf.Lerp(baseMomentum, maxMomentum, momentumProgress);
            // 오른쪽으로 이동할 때 스프라이트 정방향
            transform.localScale = new Vector3(4f, 4f, 4f);
        } else {
            // 오른쪽 키를 놓으면 관성 감소 (1.5씩 더 빠르게 감소)
            if (rightMomentum > 0) {
                rightMomentum -= momentumDecayAmount * Time.deltaTime * 10f; // 10배 더 빠르게
                if (rightMomentum < 0) rightMomentum = 0;
            }
            rightKeyHoldTime = 0f;
        }
        
        // 실제 이동 적용 (각 방향의 관성이 독립적으로 작용)
        float finalHorizontalSpeed = 0f;
        
        // 왼쪽 관성 적용 (왼쪽 키를 누르지 않아도 관성이 남아있으면 계속 왼쪽으로 밀림)
        if (leftMomentum > 0) {
            float leftForce = moveSpeed * leftMomentum;
            finalHorizontalSpeed -= leftForce;
        }
        
        // 오른쪽 관성 적용 (오른쪽 키를 누르지 않아도 관성이 남아있으면 계속 오른쪽으로 밀림)
        if (rightMomentum > 0) {
            float rightForce = moveSpeed * rightMomentum;
            finalHorizontalSpeed += rightForce;
        }
        
        // 최종 이동 적용 (관성이 있으면 항상 이동)
        if (leftMomentum > 0 || rightMomentum > 0) {
            Vector2 movement = new Vector2(finalHorizontalSpeed, playerRigidbody.linearVelocity.y);
            playerRigidbody.linearVelocity = movement;
        }

        // 점프 (W 키)
        if (Input.GetKeyDown(KeyCode.W) && jumpCount < 1 && !hasJumped && isGrounded) {
            jumpCount++;
            hasJumped = true; // 점프 애니메이션 트리거 플래그 설정
            
            // 점프 시 즉시 Grounded를 false로 설정
            isGrounded = false;

            playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, 0);

            playerRigidbody.AddForce(new Vector2(0, jumpForce));

            playerAudio.Play();
            
            // 점프 애니메이션 트리거 및 파라미터 설정
            if (animator != null) {
                // 파라미터를 먼저 설정 (Jump 상태로 전환되도록)
                animator.SetBool("Grounded", false);
                animator.SetBool("IsMoving", false);
                
                // 그 다음 Jump 트리거 설정
                string beforeState = animator.GetCurrentAnimatorStateInfo(0).IsName("Jump") ? "Jump" : 
                                     animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") ? "Walk" : "Idle";
                animator.SetTrigger("Jump");
                Debug.Log($"Jump trigger set! Before state: {beforeState}, isGrounded: {isGrounded}");
            }
        } else if (Input.GetKeyUp(KeyCode.W) && playerRigidbody.linearVelocity.y > 0) {
            playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, playerRigidbody.linearVelocity.y * 0.5f);
        }
        
        // 애니메이션 파라미터 설정
        if (animator != null) {
            // 현재 애니메이션 상태 확인
            bool isCurrentlyInJumpState = animator.GetCurrentAnimatorStateInfo(0).IsName("Jump");
            
            // 점프 직후에는 파라미터 업데이트 건너뛰기 (Jump 상태로 전환되도록 함)
            if (hasJumped && !isGrounded && !isCurrentlyInJumpState) {
                // 점프 직후이고 아직 Jump 상태가 아니면 파라미터 업데이트 건너뛰기
                // Jump 트리거가 설정되어 Jump 상태로 전환되도록 함
            } else if (isCurrentlyInJumpState) {
                // Jump 상태일 때는 Grounded만 업데이트 (땅에 닿았는지 확인용)
                // IsMoving은 절대 업데이트하지 않음! (Jump 모션이 유지되도록)
                animator.SetBool("Grounded", isGrounded);
            } else {
                // Jump 상태가 아닐 때만 모든 파라미터 업데이트
                animator.SetBool("Grounded", isGrounded);
                
                // 이동 상태에 따른 애니메이션 설정
                if (isGrounded) {
                    // 땅에 닿았을 때 - 관성이 0.5보다 크면 Walk, 0.5 이하이거나 없으면 Idle
                    float maxMomentum = Mathf.Max(leftMomentum, rightMomentum);
                    bool isMoving = (maxMomentum > 0.5f || (isPressingLeft && leftMomentum > 0.5f) || (isPressingRight && rightMomentum > 0.5f));
                    animator.SetBool("IsMoving", isMoving);
                } else {
                    // 공중에 있을 때는 IsMoving을 false로 설정
                    animator.SetBool("IsMoving", false);
                }
                
                // Jump 트리거 리셋 (Jump 상태가 아닐 때만)
                animator.ResetTrigger("Jump");
            }
            
            // 디버그 로그 추가
            if (Time.frameCount % 60 == 0) { // 1초마다 로그 출력
                string currentState = animator.GetCurrentAnimatorStateInfo(0).IsName("Jump") ? "Jump" : 
                                      animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") ? "Walk" : "Idle";
                float maxMomentum = Mathf.Max(leftMomentum, rightMomentum);
                bool isMoving = isGrounded && (maxMomentum > 0.5f || (isPressingLeft && leftMomentum > 0.5f) || (isPressingRight && rightMomentum > 0.5f));
                bool animatorGrounded = animator.GetBool("Grounded");
                bool animatorIsMoving = animator.GetBool("IsMoving");
                Debug.Log($"State: {currentState}, isGrounded: {isGrounded}, animatorGrounded: {animatorGrounded}, animatorIsMoving: {animatorIsMoving}, IsMoving: {isMoving}, MaxMomentum: {maxMomentum:F2}, hasJumped: {hasJumped}");
            }
        }
    }

    private void Die() {
        if (animator != null) {
            animator.SetTrigger("Die");
        }

        playerAudio.clip = deathClip;

        playerAudio.Play();

        playerRigidbody.linearVelocity = Vector2.zero;

        isDead = true;

        GameManager.instance.OnPlayerDead();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Dead" && !isDead) {
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.contacts[0].normal.y > 0.7f) {
            isGrounded = true;
            jumpCount = 0;
            hasJumped = false; // 바닥에 닿으면 점프 플래그 리셋
            
            // Jump 트리거 리셋 (땅에 닿으면 확실히 리셋)
            if (animator != null) {
                animator.ResetTrigger("Jump");
                Debug.Log("Landing detected! Jump trigger reset. Grounded: true");
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision) {
        isGrounded = false;
    }
}