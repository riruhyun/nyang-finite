using UnityEngine;

/// <summary>
/// Keeps the attached collider colliding only with allowed ground layers; ignores everything else.
/// Useful for dropped lid/food so they fall through anything that is not ground.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class LidCollisionFilter : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers = ~0; // set to Obstacles in inspector or via Initialize
    private Collider2D col;

    public void Initialize(LayerMask groundMask)
    {
        groundLayers = groundMask;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (col == null) return;
        if (((1 << collision.collider.gameObject.layer) & groundLayers.value) == 0)
        {
            Physics2D.IgnoreCollision(col, collision.collider, true);
        }
    }
}
