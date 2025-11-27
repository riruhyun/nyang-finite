using UnityEngine;

/// <summary>
/// Keeps a SpriteRenderer-based toast indicator anchored to a world target with offsets.
/// Useful when you want a "UI-like" badge that follows a moving object while staying upright.
/// </summary>
public class ToastProfileFollower : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ToastIndicator toastIndicator;
    [SerializeField] private ToastProfileSprite toastProfileSprite;
    [SerializeField] private Transform worldTarget;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private bool freezeRotation = true;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        toastIndicator = GetComponent<ToastIndicator>();
        toastProfileSprite = GetComponent<ToastProfileSprite>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (toastIndicator == null)
        {
            toastIndicator = GetComponent<ToastIndicator>();
        }
        if (toastProfileSprite == null)
        {
            toastProfileSprite = GetComponent<ToastProfileSprite>();
        }
    }

    private void LateUpdate()
    {
        if (worldTarget != null)
        {
            transform.position = worldTarget.position + localOffset;
        }

        if (freezeRotation)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Switch toast appearance.
    /// </summary>
    public void SetToast(ToastIndicator.ToastType type)
    {
        toastIndicator?.SetToast(type);
    }

    /// <summary>
    /// Switch toast profile (SpriteRenderer-based, independent of ToastIndicator).
    /// </summary>
    public void SetProfile(ToastProfileSprite.ToastProfileType type)
    {
        toastProfileSprite?.SetProfile(type);
    }
}
