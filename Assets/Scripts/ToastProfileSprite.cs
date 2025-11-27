using UnityEngine;

/// <summary>
/// SpriteRenderer 기반 Toast 프로필 스위처.
/// 하나의 렌더러로 Jam/Butter/Crispy/Hub/Raven/Admin 스프라이트를 교체합니다.
/// </summary>
public class ToastProfileSprite : MonoBehaviour
{
    public enum ToastProfileType
    {
        Jam,
        Butter,
        Crispy,
        Hub,
        Raven,
        Admin
    }

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ToastProfileType initialProfile = ToastProfileType.Jam;

    [Header("Sprites")]
    [SerializeField] private Sprite jamToast;
    [SerializeField] private Sprite butterToast;
    [SerializeField] private Sprite crispyToast;
    [SerializeField] private Sprite hubToast;
    [SerializeField] private Sprite ravenToast;
    [SerializeField] private Sprite adminToast;

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

        ApplyProfile(initialProfile);
    }

    public void SetProfile(ToastProfileType type)
    {
        ApplyProfile(type);
    }

    private void ApplyProfile(ToastProfileType type)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = type switch
        {
            ToastProfileType.Jam => jamToast,
            ToastProfileType.Butter => butterToast,
            ToastProfileType.Crispy => crispyToast,
            ToastProfileType.Hub => hubToast,
            ToastProfileType.Raven => ravenToast,
            ToastProfileType.Admin => adminToast,
            _ => jamToast
        };
    }
}
