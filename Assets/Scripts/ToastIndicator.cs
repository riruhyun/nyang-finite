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
