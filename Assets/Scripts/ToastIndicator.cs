using UnityEngine;

/// <summary>
/// Shows a toast sprite at a fixed offset from its parent (e.g., player) and
/// allows switching between multiple toast appearances with a single renderer.
/// </summary>
public class ToastIndicator : MonoBehaviour
{
    public enum ToastType
    {
        Jam,
        Butter,
        Crispy,
        Herb,
        Raven,
        Admin
    }

    [Header("Render")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ToastType initialToast = ToastType.Jam;
    [SerializeField] private Sprite jamToast;
    [SerializeField] private Sprite butterToast;
    [SerializeField] private Sprite crispyToast;
    [SerializeField] private Sprite herbToast;
    [SerializeField] private Sprite ravenToast;
    [SerializeField] private Sprite adminToast;

    [Header("Placement")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private bool freezeUpright = true;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        transform.localPosition = localOffset;
        ApplyToast(initialToast);
    }

    private void LateUpdate()
    {
        // Keep the indicator at the desired offset each frame (in case parent moves)
        transform.localPosition = localOffset;

        if (freezeUpright)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Switch the toast sprite.
    /// </summary>
    public void SetToast(ToastType type)
    {
        ApplyToast(type);
    }

    /// <summary>
    /// Override local offset at runtime (e.g., from spawner).
    /// </summary>
    public void SetLocalOffset(Vector3 offset)
    {
        localOffset = offset;
        transform.localPosition = offset;
    }

    /// <summary>
    /// Spawn a fading ghost of the current toast at the same world position.
    /// </summary>
    public void SpawnGhost(float alpha = 0.4f, float lifetime = 0.35f)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        var ghost = new GameObject("ToastGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = freezeUpright ? Quaternion.identity : transform.rotation;
        ghost.transform.localScale = transform.lossyScale;
        ghost.transform.localEulerAngles = freezeUpright ? Vector3.zero : transform.eulerAngles;
        ghost.transform.parent = null;

        var sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
        var c = spriteRenderer.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;

        StartCoroutine(FadeAndDestroy(sr, lifetime));
    }

    private System.Collections.IEnumerator FadeAndDestroy(SpriteRenderer sr, float duration)
    {
        // Keep the ghost fully visible for its lifetime, then clean up.
        yield return new WaitForSeconds(duration);
        Destroy(sr.gameObject);
    }

    private void ApplyToast(ToastType type)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = type switch
        {
            ToastType.Jam => jamToast,
            ToastType.Butter => butterToast,
            ToastType.Crispy => crispyToast,
            ToastType.Herb => herbToast,
            ToastType.Raven => ravenToast,
            ToastType.Admin => adminToast,
            _ => jamToast
        };
    }
}
