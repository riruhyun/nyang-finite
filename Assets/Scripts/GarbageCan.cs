using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Garbage can that drops configured food after taking hits. Shakes on hit, pops lid off, spawns food pickups.
/// </summary>
public class GarbageCan : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer lidRenderer;
    [SerializeField] private Transform lidSpawnPoint;

    [Header("Hit Settings")]
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.08f;

    [Header("Drop Settings")]
    [SerializeField] private List<FoodDropEntry> drops = new List<FoodDropEntry>();
    [SerializeField] private float foodUpwardImpulse = 3f;
    [SerializeField] private float foodHorizontalJitter = 1.2f;
    [SerializeField] private Vector3 foodSpawnOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private float dropScale = 0.35f;
    [SerializeField] private bool freezeDropRotation = true;
    [SerializeField] private bool useFoodSpriteBounds = false;
    [SerializeField] private float foodColliderRadius = 0.15f;
    [SerializeField] private float foodHoverBoxPadding = 0.15f;
    [SerializeField] private bool addHoverTriggerCollider = true;
    [SerializeField] private float foodMass = 0.25f;
    [SerializeField] private float foodGravityScale = 2.0f;
    [SerializeField] private float foodLinearDrag = 0.05f;
    [SerializeField] private PhysicsMaterial2D foodPhysicsMaterial;
    [SerializeField] private Vector2 foodUIOffset = new Vector2(0f, 0.8f);
    [SerializeField] private float foodUIScale = 0.003f;
    [SerializeField] private string foodUISortingLayer = "UI";
    [SerializeField] private int foodUISortingOrder = 5000;
    [SerializeField] private Sprite foodUIBackgroundSprite;
    [SerializeField] private Sprite foodEatButtonSprite;
    [SerializeField] private Sprite foodCompleteButtonSprite;

    [Header("Lid Physics")]
    [SerializeField] private float lidUpwardImpulse = 4f;
    [SerializeField] private float lidHorizontalImpulse = 1.5f;
    [SerializeField] private float lidGravity = 3f;
    [SerializeField] private LayerMask lidGroundLayers = ~0;

    private int hitCount = 0;
    private bool lidDropped = false;
    private Coroutine shakeRoutine;
    private Vector3 basePosition;

    private void Awake()
    {
        // Auto-wire common children if not assigned
        if (bodyRenderer == null)
        {
            var bodyTf = transform.Find("GarbageCan");
            bodyRenderer = bodyTf != null ? bodyTf.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
        }
        if (lidRenderer == null)
        {
            var lidTf = transform.Find("GarbageCanLid");
            lidRenderer = lidTf != null ? lidTf.GetComponent<SpriteRenderer>() : null;
        }
        if (lidSpawnPoint == null && lidRenderer != null) lidSpawnPoint = lidRenderer.transform;
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
        basePosition = transform.localPosition;

        // Fallback load for button sprites so hover UI has visuals without manual wiring
        if (foodCompleteButtonSprite == null)
        {
            // Food 전용 리소스 우선, 없으면 기존 Toast UI 리소스 사용
            var foodUi = Resources.LoadAll<Sprite>("FoodUI/Button_complete");
            var toastUi = Resources.LoadAll<Sprite>("StatsBoxUI/Button_complete");
            if (foodUi != null && foodUi.Length > 0) foodCompleteButtonSprite = foodUi[0];
            else if (toastUi != null && toastUi.Length > 0) foodCompleteButtonSprite = toastUi[0];
        }
        if (foodEatButtonSprite == null)
        {
            var foodUi = Resources.LoadAll<Sprite>("FoodUI/Button_Eat");
            var toastUi = Resources.LoadAll<Sprite>("StatsBoxUI/Button_Eat");
            if (foodUi != null && foodUi.Length > 0) foodEatButtonSprite = foodUi[0];
            else if (toastUi != null && toastUi.Length > 0) foodEatButtonSprite = toastUi[0];
        }

        if (foodUIBackgroundSprite == null)
        {
            var foodBg = Resources.LoadAll<Sprite>("FoodUI/FoodPanel");
            var toastBg = Resources.LoadAll<Sprite>("StatsBoxUI/StatsBox");
            if (foodBg != null && foodBg.Length > 0) foodUIBackgroundSprite = foodBg[0];
            else if (toastBg != null && toastBg.Length > 0) foodUIBackgroundSprite = toastBg[0];
        }
    }

    /// <summary>
    /// Call this from player attack/damage logic.
    /// </summary>
    public void OnHit()
    {
        hitCount++;
        TriggerShake();
        TryDropFood();
    }

    private void TriggerShake()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private System.Collections.IEnumerator ShakeRoutine()
    {
        float timer = 0f;
        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            float offset = Mathf.Sin(timer * 50f) * shakeMagnitude;
            transform.localPosition = basePosition + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = basePosition;
    }

    private void TryDropFood()
    {
        foreach (var entry in drops)
        {
            if (entry == null || entry.dropped) continue;
            if (hitCount >= entry.requiredHits)
            {
                SpawnFood(entry);
                entry.dropped = true;
                if (!lidDropped)
                {
                    DropLid();
                    lidDropped = true;
                }
            }
        }
    }

    private void SpawnFood(FoodDropEntry entry)
    {
        int amount = Mathf.Clamp(entry.quantity, 1, 5);
        for (int i = 0; i < amount; i++)
        {
            var go = new GameObject($"Food_{entry.foodSprite?.name ?? "Item"}");
            go.transform.position = transform.position + foodSpawnOffset;
            go.transform.localScale = Vector3.one * dropScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = entry.foodSprite;
            if (bodyRenderer != null)
            {
                sr.sortingLayerName = bodyRenderer.sortingLayerName;
                sr.sortingOrder = bodyRenderer.sortingOrder + 2; // 음식이 몸통/뚜껑보다 앞에 보이도록
            }
            else
            {
                sr.sortingLayerName = "Default";
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = foodGravityScale;
            rb.freezeRotation = freezeDropRotation;
            rb.mass = foodMass;
            rb.linearDamping = foodLinearDrag;

            // 충돌체 설정 (물리 충돌용 - 크기를 hover 감지용으로 키움)
            if (useFoodSpriteBounds && sr.sprite != null)
            {
                var box = go.AddComponent<BoxCollider2D>();
                var b = sr.sprite.bounds;
                // OnMouseEnter/Exit을 위해 크기를 키움
                Vector2 baseSize = new Vector2(b.size.x, b.size.y) * dropScale;
                box.size = baseSize + Vector2.one * foodHoverBoxPadding;
                box.offset = new Vector2(b.center.x, b.center.y) * dropScale;
                box.isTrigger = false;
                if (foodPhysicsMaterial != null) box.sharedMaterial = foodPhysicsMaterial;
            }
            else
            {
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = false;
                // OnMouseEnter/Exit을 위해 반지름을 키움
                col.radius = foodColliderRadius + foodHoverBoxPadding;
                if (foodPhysicsMaterial != null) col.sharedMaterial = foodPhysicsMaterial;
            }

            var filter = go.AddComponent<LidCollisionFilter>();
            filter.Initialize(lidGroundLayers);

            var pickup = go.AddComponent<FoodPickup>();
            pickup.Initialize(entry.foodSprite, foodUpwardImpulse, foodHorizontalJitter);

            var hover = go.AddComponent<FoodHoverHandler>();
            var hoverData = new FoodHoverData
            {
                profileSprite = entry.profileSpriteOverride != null ? entry.profileSpriteOverride : entry.foodSprite,
                displayName = entry.foodSprite != null ? entry.foodSprite.name : "Food",
                description = entry.description,
                effectType = entry.effectType,
                effectAmount = Mathf.Clamp(entry.effectAmount, 1, 9)
            };
            hover.Initialize(hoverData, foodUIOffset, foodUIScale, foodUISortingLayer, foodUISortingOrder, foodEatButtonSprite, foodCompleteButtonSprite, foodUIBackgroundSprite);
        }
    }

    private void DropLid()
    {
        if (lidRenderer == null) return;

        var lidGO = new GameObject("GarbageCanLid_Dropped");
        lidGO.transform.position = lidSpawnPoint != null ? lidSpawnPoint.position : lidRenderer.transform.position;
        lidGO.transform.localScale = Vector3.one * dropScale;
        var sr = lidGO.AddComponent<SpriteRenderer>();
        sr.sprite = lidRenderer.sprite;
        sr.sortingLayerName = lidRenderer.sortingLayerName;
        sr.sortingOrder = (bodyRenderer != null ? bodyRenderer.sortingOrder + 1 : lidRenderer.sortingOrder - 1); // 음식보다 뒤에, 몸통보다 위 정도

        var rb = lidGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = lidGravity;
        rb.freezeRotation = freezeDropRotation;

        // 충돌체로 지면에 착지
        var lidCol = lidGO.AddComponent<BoxCollider2D>();
        lidCol.isTrigger = false;
        var filter = lidGO.AddComponent<LidCollisionFilter>();
        filter.Initialize(lidGroundLayers);

        float xImpulse = Random.Range(-lidHorizontalImpulse, lidHorizontalImpulse);
        rb.AddForce(new Vector2(xImpulse, lidUpwardImpulse), ForceMode2D.Impulse);

        // hide original lid
        lidRenderer.enabled = false;
    }

    [System.Serializable]
    public class FoodDropEntry
    {
        [Tooltip("Dropped food sprite from Assets/Sprites/Food.")]
        public Sprite foodSprite;
        [Tooltip("Optional profile sprite for hover UI (defaults to foodSprite).")]
        public Sprite profileSpriteOverride;
        [Tooltip("Hover UI description text shown for this food.")]
        [TextArea] public string description;
        [Tooltip("Effect label shown on hover UI.")]
        public FoodEffectType effectType = FoodEffectType.Heal;
        [Tooltip("효과 수치 (Heal/Damage일 때 1~9).")]
        [Range(1, 9)] public int effectAmount = 1;
        [Tooltip("Hits required to drop this food (1~5).")]
        [Range(1, 5)] public int requiredHits = 1;
        [Tooltip("How many of this food to drop.")]
        [Range(1, 5)] public int quantity = 1;
        [HideInInspector] public bool dropped = false;
    }
}
