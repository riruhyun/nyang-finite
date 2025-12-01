using UnityEngine;

/// <summary>
/// 가시(Spike) 부모 오브젝트에 붙이는 스크립트
/// 자식 오브젝트들의 충돌을 감지하여 플레이어에게 데미지를 줍니다.
/// 부모 오브젝트(Spikes)에 하나만 붙이면 됩니다.
/// </summary>
public class Spike : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("가시에 닿았을 때 플레이어가 받는 데미지")]
    [SerializeField] private float damage = 1f;

    [Header("Player Detection")]
    [Tooltip("플레이어 태그 (기본값: Player)")]
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        // 자식 오브젝트들에 SpikeChild 컴포넌트 추가
        AddSpikeChildToChildren(transform);
    }

    private void AddSpikeChildToChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // 자식에 Collider2D가 있으면 SpikeChild 추가
            Collider2D childCollider = child.GetComponent<Collider2D>();
            if (childCollider != null)
            {
                SpikeChild spikeChild = child.GetComponent<SpikeChild>();
                if (spikeChild == null)
                {
                    spikeChild = child.gameObject.AddComponent<SpikeChild>();
                }
                spikeChild.Initialize(this);
            }

            // 재귀적으로 자식의 자식도 처리
            if (child.childCount > 0)
            {
                AddSpikeChildToChildren(child);
            }
        }
    }

    /// <summary>
    /// 플레이어에게 데미지를 주는 메서드 (SpikeChild에서 호출)
    /// </summary>
    public void DamagePlayer(Collider2D other)
    {
        if (other == null) return;

        // 플레이어 태그 확인
        if (!other.CompareTag(playerTag)) return;

        // PlayerController 컴포넌트 가져오기
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    // 부모 오브젝트 자체에도 Collider가 있을 경우를 위한 처리
    private void OnTriggerEnter2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DamagePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DamagePlayer(collision.collider);
    }
}

/// <summary>
/// 자식 가시 오브젝트에 자동으로 추가되는 헬퍼 컴포넌트
/// 충돌 감지 시 부모 Spike 컴포넌트에 알림
/// </summary>
public class SpikeChild : MonoBehaviour
{
    private Spike parentSpike;

    public void Initialize(Spike parent)
    {
        parentSpike = parent;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (parentSpike != null)
        {
            parentSpike.DamagePlayer(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (parentSpike != null)
        {
            parentSpike.DamagePlayer(other);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (parentSpike != null)
        {
            parentSpike.DamagePlayer(collision.collider);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (parentSpike != null)
        {
            parentSpike.DamagePlayer(collision.collider);
        }
    }
}