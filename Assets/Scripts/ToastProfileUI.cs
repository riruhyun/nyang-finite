using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI toast profile switcher for Canvas. Assign sprites (Jam/Butter/Crispy/Hub/Raven/Admin)
/// and call SetProfile to swap. Keeps its anchored position offset relative to the parent.
/// </summary>
public class ToastProfileUI : MonoBehaviour
{
    public enum ToastType
    {
        Jam,
        Butter,
        Crispy,
        Hub,
        Raven,
        Admin
    }

    [Header("UI")]
    [SerializeField] private Image targetImage;
    [SerializeField] private ToastType initialToast = ToastType.Jam;
    [SerializeField] private bool setNativeSizeOnApply = false;

    [Header("Sprites")]
    [SerializeField] private Sprite jamToast;
    [SerializeField] private Sprite butterToast;
    [SerializeField] private Sprite crispyToast;
    [SerializeField] private Sprite hubToast;
    [SerializeField] private Sprite ravenToast;
    [SerializeField] private Sprite adminToast;

    [Header("Placement")]
    [SerializeField] private Vector2 anchoredOffset = Vector2.zero;
    [SerializeField] private bool freezeRotation = true;

    private RectTransform rectTransform;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        ApplyAnchoredOffset();
        ApplySprite(initialToast);
    }

    private void LateUpdate()
    {
        ApplyAnchoredOffset();
        if (freezeRotation && rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Switch the displayed toast sprite.
    /// </summary>
    public void SetProfile(ToastType type)
    {
        ApplySprite(type);
    }

    private void ApplyAnchoredOffset()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredOffset;
        }
    }

    private void ApplySprite(ToastType type)
    {
        if (targetImage == null) return;

        targetImage.sprite = type switch
        {
            ToastType.Jam => jamToast,
            ToastType.Butter => butterToast,
            ToastType.Crispy => crispyToast,
            ToastType.Hub => hubToast,
            ToastType.Raven => ravenToast,
            ToastType.Admin => adminToast,
            _ => jamToast
        };

        if (setNativeSizeOnApply && targetImage.sprite != null)
        {
            targetImage.SetNativeSize();
        }
    }
}
