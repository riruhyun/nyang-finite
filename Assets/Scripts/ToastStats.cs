using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Holds toast stat data (with optional random variance) and drives the hover UI.
/// Attach to the same GameObject as ToastIndicator.
/// </summary>
public class ToastStats : MonoBehaviour
{
    private static ToastStats activeHoverOwner;
    public static bool HasActiveHover => activeHoverOwner != null;
    public static void CloseAllPanelsExcept(ToastStats keepOpen)
    {
        var panels = GameObject.FindObjectsOfType<ToastHoverPanel>(true);
        foreach (var p in panels)
        {
            if (keepOpen != null && p == keepOpen.GetActivePanel()) continue;
            p.ForceClose();
        }
        activeHoverOwner = keepOpen;
    }
    // keepOpen이 가진 패널을 반환
    public ToastHoverPanel GetActivePanel()
    {
        return activePanel;
    }
    [Header("Profile")]
    [SerializeField] private ToastStatProfile profile;
    [SerializeField] private bool randomize = true;
    [SerializeField] private Vector2 globalVariance = Vector2.zero;
    [SerializeField] private Sprite overrideProfileSprite;
    [SerializeField] private Sprite overrideToastNameSprite;
    [SerializeField] private Font overrideFont;
    [HideInInspector]
    [TextArea]
    [SerializeField] private string overrideDescription;

    [Header("UI")]
    [SerializeField] private Vector2 uiWorldOffset = new Vector2(0f, 1.4f);
    [SerializeField] private float uiWorldScale = 0.01f;
    [SerializeField] private ToastHoverPanel hoverPanelPrefab;
    [SerializeField] private Canvas worldCanvasPrefab;
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 5000;

    [Header("Events")]
    public UnityEvent<List<RuntimeStat>> onApplyToPlayer;

    private List<RuntimeStat> runtimeStats = new List<RuntimeStat>();
    private bool hoverAllowed = false;
    private ToastHoverPanel activePanel;
    private ToastIndicator indicator;

    private void Awake()
    {
        indicator = GetComponent<ToastIndicator>();
        BuildRuntimeStats();
    }

    public void BuildRuntimeStats()
    {
        runtimeStats.Clear();
        if (profile == null) return;

        // 유효한 스탯만 필터링
        List<StatEntry> allowedStats = new List<StatEntry>();
        foreach (var entry in profile.stats)
        {
            if (IsStatAllowed(entry))
            {
                allowedStats.Add(entry);
            }
        }

        // 랜덤 선택 모드일 경우 지정된 개수만큼만 선택
        List<StatEntry> selectedStats;
        if (profile.randomSelectStats && profile.maxStatCount > 0 && allowedStats.Count > profile.maxStatCount)
        {
            // 랜덤 정렬 후 필요한 개수만 선택
            selectedStats = allowedStats
                .OrderBy(_ => Random.value)
                .Take(profile.maxStatCount)
                .ToList();
        }
        else
        {
            selectedStats = allowedStats;
        }

        foreach (var entry in selectedStats)
        {
            float value = entry.deltaValue;
            if (randomize)
            {
                value += Random.Range(entry.varianceRange.x, entry.varianceRange.y);
                value += Random.Range(globalVariance.x, globalVariance.y);
            }

            bool bonusApplied = false;
            float bonusValue = 0f;
            if (randomize && entry.bonusRolls != null)
            {
                foreach (var roll in entry.bonusRolls)
                {
                    if (roll == null) continue;
                    if (roll.chancePercent > 0f && Random.Range(0f, 100f) <= roll.chancePercent)
                    {
                        value += roll.bonusDelta;
                        bonusValue += roll.bonusDelta;
                        bonusApplied = true;
                    }
                }
            }

            runtimeStats.Add(new RuntimeStat
            {
                statType = entry.statType,
                value = value,
                unit = entry.unit,
                showSign = entry.showSign,
                bonusApplied = bonusApplied,
                bonusValue = bonusValue
            });
        }
    }

    public void MarkOwnerDead()
    {
        hoverAllowed = true;
        Debug.Log($"[ToastStats] MarkOwnerDead called. hoverAllowed={hoverAllowed}");
        TryEnsurePanel();
        if (activePanel != null)
        {
            Debug.Log($"[ToastStats] activePanel created successfully");
            activePanel.EnableHoverFromToast(this);
        }
        else
        {
            Debug.LogWarning($"[ToastStats] activePanel is null! hoverPanelPrefab={hoverPanelPrefab}");
        }
    }

    public void ShowHover()
    {
        Debug.Log($"[ToastStats] ★★★ ShowHover called on {gameObject.name}. hoverAllowed={hoverAllowed}");
        if (!hoverAllowed)
        {
            Debug.LogWarning($"[ToastStats] ShowHover blocked - hoverAllowed is FALSE!");
            return;
        }

        // 이미 다른 토스트가 활성화되어 있으면 그것을 먼저 숨김
        if (activeHoverOwner != null && activeHoverOwner != this)
        {
            Debug.Log($"[ToastStats] Another toast is active, hiding it first");
            CloseAllPanelsExcept(this);
        }

        activeHoverOwner = this;
        Debug.Log($"[ToastStats] Calling TryEnsurePanel...");
        TryEnsurePanel();

        if (activePanel != null)
        {
            Debug.Log($"[ToastStats] activePanel EXISTS! Calling Show with offset={uiWorldOffset}, layer={sortingLayerName}, order={sortingOrder}");
            Debug.Log($"[ToastStats] activePanel GameObject: {activePanel.gameObject.name}, active={activePanel.gameObject.activeInHierarchy}");
            activePanel.Show(this, uiWorldOffset, sortingLayerName, sortingOrder);
        }
        else
        {
            Debug.LogError($"[ToastStats] ★★★ activePanel is NULL in ShowHover! ★★★");
        }
    }

    public void HideHover()
    {
        if (activePanel != null)
        {
            activePanel.Hide();
        }
        if (activeHoverOwner == this)
        {
            activeHoverOwner = null;
        }
    }

    public void ApplyToPlayer()
    {
        onApplyToPlayer?.Invoke(runtimeStats);
    }

    public List<RuntimeStat> GetRuntimeStatsList()
    {
        return runtimeStats;
    }

    public Sprite GetProfileSprite()
    {
        return overrideProfileSprite != null ? overrideProfileSprite : profile != null ? profile.profileSprite : null;
    }

    public Font GetFont()
    {
        return overrideFont != null ? overrideFont : profile != null ? profile.font : null;
    }

    public Sprite GetToastNameSprite()
    {
        if (overrideToastNameSprite != null) return overrideToastNameSprite;
        return profile != null ? profile.toastNameSprite : null;
    }

    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(overrideDescription)) return overrideDescription;
        return profile != null ? profile.description : "";
    }

    public List<RuntimeStat> GetRuntimeStats() => runtimeStats;

    public ToastIndicator.ToastType GetToastType()
    {
        return profile != null ? profile.toastType : ToastIndicator.ToastType.Jam;
    }

    public void SetProfile(ToastStatProfile newProfile)
    {
        if (newProfile == null) return;
        profile = newProfile;
        BuildRuntimeStats();
    }

    public ToastStatProfile.Rarity GetRarity()
    {
        return profile != null ? profile.rarity : ToastStatProfile.Rarity.Common;
    }

    private bool IsStatAllowed(StatEntry entry)
    {
        if (profile == null) return true;
        var r = profile.rarity;
        return r >= entry.minRarity && r <= entry.maxRarity;
    }

    private void TryEnsurePanel()
    {
        if (activePanel != null)
        {
            Debug.Log("[ToastStats] Panel already exists, skipping creation");
            return;
        }
        if (hoverPanelPrefab == null)
        {
            Debug.LogError("[ToastStats] hoverPanelPrefab is null! Please assign it in the Inspector.");
            return;
        }

        Debug.Log($"[ToastStats] Creating new panel. worldCanvasPrefab={(worldCanvasPrefab != null ? "exists" : "null")}");

        if (worldCanvasPrefab != null)
        {
            var canvas = Instantiate(worldCanvasPrefab);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingLayerName = sortingLayerName;
            canvas.sortingOrder = sortingOrder;
            canvas.transform.localScale = Vector3.one * uiWorldScale;

            // Disable CanvasScaler for WorldSpace - it messes up layout
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null) scaler.enabled = false;

            activePanel = Instantiate(hoverPanelPrefab, canvas.transform);
            Debug.Log($"[ToastStats] Created panel with worldCanvas at {canvas.transform.position}, scale={canvas.transform.localScale}");
        }
        else
        {
            activePanel = Instantiate(hoverPanelPrefab);
            var panelCanvas = activePanel.GetComponent<Canvas>();
            if (panelCanvas != null)
            {
                panelCanvas.renderMode = RenderMode.WorldSpace;
                panelCanvas.worldCamera = Camera.main;
                panelCanvas.sortingLayerName = sortingLayerName;
                panelCanvas.sortingOrder = sortingOrder;
                panelCanvas.transform.localScale = Vector3.one * uiWorldScale;

                // Disable CanvasScaler for WorldSpace - it messes up layout
                var scaler = panelCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null) scaler.enabled = false;

                Debug.Log($"[ToastStats] Created panel without worldCanvas, canvas found on panel. Position={panelCanvas.transform.position}, Scale={panelCanvas.transform.localScale}");
            }
            else
            {
                Debug.LogWarning($"[ToastStats] Created panel but no Canvas component found on it!");
            }
        }
    }
}

[System.Serializable]
public struct RuntimeStat
{
    public StatType statType;
    public float value;
    public string unit;
    public bool showSign;
    public bool bonusApplied;
    public float bonusValue;

    // UI 표시용 이름 가져오기
    public string GetDisplayName()
    {
        return statType.ToString();
    }
}
