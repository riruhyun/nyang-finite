using UnityEngine;

/// <summary>
/// Simple pigeon enemy with lightweight chase/attack behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PigeonMovement : Enemy
{
    [Header("Pigeon Settings")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackRange = 1.5f;

    private bool isAttacking = false;

    protected override void OnPlayerDetected()
    {
        if (playerTransform == null) return;

        // Simple horizontal chase
        Vector2 dir = (playerTransform.position - transform.position).normalized;
        Move(new Vector2(dir.x, 0f));

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= attackRange)
        {
            Attack();
        }
    }

    protected override void PerformAttack()
    {
        if (playerTransform == null || isAttacking) return;
        isAttacking = true;

        // Dummy damage application (extend to real player hitbox later)
        // Here we just reset attack flag after cooldown
        Invoke(nameof(ResetAttack), 1f / Mathf.Max(0.01f, attackSpeed));
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    public void ApplyConfig(IntelligentDogMovement.Config config)
    {
        // Reuse same config struct for convenience
        if (config == null) return;
        ApplyBaseStats(config.moveSpeed, config.maxHealth, config.attackSpeed);
        if (config.attackDamage > 0f) attackDamage = config.attackDamage;
    }
}
