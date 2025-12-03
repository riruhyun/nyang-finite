using UnityEngine;

/// <summary>
/// 2D Sprite 버튼에 마우스 호버 효과 적용
/// - 평소: 회색 필터로 어둡게
/// - 호버: 회색 필터 제거 + 왼쪽으로 이동
/// - 버튼이 이동해도 원래 영역 기준으로 호버 감지 (무한 루프 방지)
/// </summary>
public class SpriteButtonHoverEffect : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color hoverColor = Color.white;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private float moveDistance = -1f;

    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition;
    private Bounds originalBounds;
    private Color currentColor;
    private Vector3 currentPosition;
    private float animationProgress = 0f;
    private bool isHovering = false;
    private bool isAnimating = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError($"[SpriteButtonHoverEffect] '{gameObject.name}'에 SpriteRenderer가 없습니다!");
            enabled = false;
            return;
        }

        originalPosition = transform.position;
        currentPosition = originalPosition;
        currentColor = normalColor;
        spriteRenderer.color = normalColor;

        originalBounds = spriteRenderer.bounds;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[SpriteButtonHoverEffect] '{gameObject.name}'에 Collider2D가 없습니다!");
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.size = spriteRenderer.bounds.size;
        }
    }

    private void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        bool mouseInOriginalBounds = originalBounds.Contains(mouseWorldPos);

        if (mouseInOriginalBounds && !isHovering)
        {
            isHovering = true;
            isAnimating = true;
            animationProgress = 0f;
        }
        else if (!mouseInOriginalBounds && isHovering)
        {
            isHovering = false;
            isAnimating = true;
            animationProgress = 0f;
        }

        if (isAnimating)
        {
            animationProgress += Time.deltaTime / animationDuration;

            if (animationProgress >= 1f)
            {
                animationProgress = 1f;
                isAnimating = false;
            }

            float t = EaseInOutCubic(animationProgress);

            Color targetColor = isHovering ? hoverColor : normalColor;
            Vector3 targetPosition = isHovering ? originalPosition + new Vector3(moveDistance, 0, 0) : originalPosition;

            currentColor = Color.Lerp(currentColor, targetColor, t);
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, t);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = currentColor;
            }
            transform.position = currentPosition;

            if (animationProgress >= 1f)
            {
                currentColor = targetColor;
                currentPosition = targetPosition;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = currentColor;
                }
                transform.position = currentPosition;
            }
        }
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        transform.position = originalPosition;
    }
}
