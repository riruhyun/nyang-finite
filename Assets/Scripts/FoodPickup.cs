using UnityEngine;

/// <summary>
/// Simple food pickup that pops upward once then falls with gravity.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class FoodPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private float gravityScale = 3f;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();
        rb2d.gravityScale = gravityScale;
    }

    public void Initialize(Sprite sprite, float upwardForce, float horizontalJitter)
    {
        if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            float xImpulse = Random.Range(-horizontalJitter, horizontalJitter);
            rb2d.AddForce(new Vector2(xImpulse, upwardForce), ForceMode2D.Impulse);
        }
    }
}
