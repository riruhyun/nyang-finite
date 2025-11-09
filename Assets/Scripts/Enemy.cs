using UnityEngine;

// 적 캐릭터(개) 스크립트
// 플레이어와 충돌 시 데미지를 입힙니다.
public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int damageAmount = 1; // 플레이어에게 입히는 데미지
    public float moveSpeed = 2f; // 이동 속도
    public bool moveLeft = true; // 왼쪽으로 이동할지 여부

    [Header("Patrol Settings")]
    public bool enablePatrol = false; // 순찰 기능 활성화
    public float patrolDistance = 5f; // 순찰 거리

    private Rigidbody2D enemyRigidbody;
    private Vector3 startPosition;
    private bool movingRight = false;

    private void Start()
    {
        // Rigidbody2D 컴포넌트 가져오기
        enemyRigidbody = GetComponent<Rigidbody2D>();

        if (enemyRigidbody == null)
        {
            Debug.LogError("Enemy에 Rigidbody2D 컴포넌트가 없습니다!");
        }

        startPosition = transform.position;

        // 초기 방향 설정
        if (!moveLeft)
        {
            movingRight = true;
            Flip();
        }
    }

    private void Update()
    {
        if (enablePatrol)
        {
            Patrol();
        }
        else
        {
            SimpleMove();
        }
    }

    // 단순 이동
    private void SimpleMove()
    {
        if (enemyRigidbody != null)
        {
            float direction = moveLeft ? -1f : 1f;
            enemyRigidbody.linearVelocity = new Vector2(direction * moveSpeed, enemyRigidbody.linearVelocity.y);
        }
    }

    // 순찰 이동
    private void Patrol()
    {
        if (enemyRigidbody != null)
        {
            float direction = movingRight ? 1f : -1f;
            enemyRigidbody.linearVelocity = new Vector2(direction * moveSpeed, enemyRigidbody.linearVelocity.y);

            // 순찰 거리 체크
            float distanceFromStart = transform.position.x - startPosition.x;

            if (movingRight && distanceFromStart >= patrolDistance)
            {
                movingRight = false;
                Flip();
            }
            else if (!movingRight && distanceFromStart <= -patrolDistance)
            {
                movingRight = true;
                Flip();
            }
        }
    }

    // 스프라이트 방향 전환
    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // 플레이어와 충돌 시
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어의 PlayerController 컴포넌트 가져오기
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null)
            {
                // 플레이어에게 데미지 입히기
                player.TakeDamage(damageAmount);
                Debug.Log($"플레이어가 적과 충돌! 데미지: {damageAmount}");
            }
        }
    }
}
