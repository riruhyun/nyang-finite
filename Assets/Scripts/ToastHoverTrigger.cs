using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

/// <summary>
/// Simple hover trigger for toasts using manual raycast detection.
/// Works reliably even with EventSystem present.
/// </summary>
public class ToastHoverTrigger : MonoBehaviour
{
    [SerializeField] private ToastStats toastStats;
    [SerializeField] private SpriteRenderer toastRenderer;
    [SerializeField] private bool showToastFromStart = false; // show/hide renderer on start
    private bool ownerDead = false;
    private bool wasHovering = false;
    private Camera mainCam;

    private void Reset()
    {
        toastStats = GetComponent<ToastStats>();
        toastRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (toastStats == null) toastStats = GetComponent<ToastStats>();
        if (toastRenderer == null) toastRenderer = GetComponent<SpriteRenderer>();

        // toastRenderer가 아직도 null이면 ToastIndicator의 spriteRenderer를 가져옴
        if (toastRenderer == null)
        {
            var indicator = GetComponent<ToastIndicator>();
            if (indicator != null)
            {
                // ToastIndicator의 private spriteRenderer 필드에 접근하기 위해 reflection 사용
                var field = typeof(ToastIndicator).GetField("spriteRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    toastRenderer = field.GetValue(indicator) as SpriteRenderer;
                }
            }
        }

        if (toastRenderer != null)
        {
            toastRenderer.enabled = showToastFromStart;
            Debug.Log($"[ToastHoverTrigger] {gameObject.name} toastRenderer set, enabled={showToastFromStart}");
        }
        else
        {
            Debug.LogWarning($"[ToastHoverTrigger] {gameObject.name} toastRenderer is still null!");
        }

        mainCam = Camera.main;
    }

    public void SetOwnerDead()
    {
        ownerDead = true;

        // EnemyToast 스프라이트 표시
        if (toastRenderer != null)
        {
            toastRenderer.enabled = true;
            Debug.Log($"[ToastHoverTrigger] {gameObject.name} sprite enabled after owner death");
        }

        if (toastStats != null)
        {
            toastStats.MarkOwnerDead();
        }
    }

    private void Update()
    {
        if (!ownerDead) return;
        if (mainCam == null)
        {
            Debug.LogWarning($"[ToastHoverTrigger] {gameObject.name} mainCam is null!");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            bool clickedOnThis = false;

            // 1) OverlapPoint
            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);
            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                clickedOnThis = true;
            }

            // 2) Raycast
            if (!clickedOnThis)
            {
                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0.1f);
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    clickedOnThis = true;
                }
            }

            // 3) Circle overlap (shrunk to reduce accidental clicks)
            if (!clickedOnThis)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPos, 0.05f);
                foreach (var col in hits)
                {
                    if (col.gameObject == gameObject)
                    {
                        clickedOnThis = true;
                        break;
                    }
                }
            }


            if (clickedOnThis)
            {
                // 다른 패널 열려 있으면 닫고 이 패널을 연다
                if (ToastStats.HasActiveHover)
                {
                    ToastStats.CloseAllPanelsExcept(toastStats);

                }
                wasHovering = true;
                toastStats?.ShowHover();
            }
            else if (wasHovering)
            {
                bool clickedOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!clickedOnUI)
                {
                    wasHovering = false;
                    toastStats?.HideHover();
                }
            }
        }
    }

    private void TogglePanel()
    {
        if (toastStats == null) return;

        // 패널이 열려있으면 닫고, 닫혀있으면 열기
        if (wasHovering)
        {
            wasHovering = false;
            toastStats.HideHover();
            Debug.Log($"[ToastHoverTrigger] Panel closed for {gameObject.name}");
        }
        else
        {
            wasHovering = true;
            toastStats.ShowHover();
            Debug.Log($"[ToastHoverTrigger] Panel opened for {gameObject.name}");
        }
    }
}
