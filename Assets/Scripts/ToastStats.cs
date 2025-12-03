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

        switch (profile.selectionMode)
        {
            case ToastStatProfile.StatSelectionMode.GroupBased:
                BuildFromGroups();
                break;
            case ToastStatProfile.StatSelectionMode.RandomCount:
                BuildFromRandomCount();
                break;
            case ToastStatProfile.StatSelectionMode.All:
            default:
                BuildFromAllStats();
                break;
        }
    }

    private void BuildFromGroups()
    {
        foreach (var group in profile.statGroups)
        {
            if (group.entries == null || group.entries.Count == 0) continue;

            // 가중치 기반 랜덤 선택
            var selected = SelectWeightedRandom(group.entries);
            if (selected == null) continue;

            AddRuntimeStat(selected.statType, selected.deltaValue, selected.varianceRange,
                          selected.bonusRolls, selected.unit, selected.showSign);
        }
    }

    private void BuildFromRandomCount()
    {
        // 모든 그룹의 모든 항목을 평탄화
        List<StatGroupEntry> allEntries = profile.statGroups
            .Where(g => g.entries != null)
            .SelectMany(g => g.entries)
            .ToList();

        List<StatGroupEntry> selectedStats;
        if (profile.maxStatCount > 0 && allEntries.Count > profile.maxStatCount)
        {
            selectedStats = allEntries
                .OrderBy(_ => Random.value)
                .Take(profile.maxStatCount)
                .ToList();
        }
        else
        {
            selectedStats = allEntries;
        }

        foreach (var entry in selectedStats)
        {
            AddRuntimeStat(entry.statType, entry.deltaValue, entry.varianceRange,
                          entry.bonusRolls, entry.unit, entry.showSign);
        }
    }

    private void BuildFromAllStats()
    {
        // 모든 그룹의 모든 항목 적용
        foreach (var group in profile.statGroups)
        {
            if (group.entries == null) continue;
            foreach (var entry in group.entries)
            {
                AddRuntimeStat(entry.statType, entry.deltaValue, entry.varianceRange,
                              entry.bonusRolls, entry.unit, entry.showSign);
            }
        }
    }

    private void AddRuntimeStat(StatType statType, float deltaValue, Vector2 varianceRange,
                                List<BonusRoll> bonusRolls, string unit, bool showSign)
    {
        float value = deltaValue;
        if (randomize)
        {
            value += Random.Range(varianceRange.x, varianceRange.y);
            value += Random.Range(globalVariance.x, globalVariance.y);
        }

        bool bonusApplied = false;
        float bonusValue = 0f;
        if (randomize && bonusRolls != null)
        {
            foreach (var roll in bonusRolls)
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
            statType = statType,
            value = value,
            unit = unit,
            showSign = showSign,
            bonusApplied = bonusApplied,
            bonusValue = bonusValue
        });
    }

    private StatGroupEntry SelectWeightedRandom(List<StatGroupEntry> entries)
    {
        float totalWeight = entries.Sum(e => e.weight);
        if (totalWeight <= 0) return entries.FirstOrDefault();

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in entries)
        {
            cumulative += entry.weight;
            if (random <= cumulative)
                return entry;
        }

        return entries.LastOrDefault();
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

    public void SetProfile(ToastStatProfile newProfile, bool preserveVisualOverrides = false)
    {
        if (newProfile == null) return;
        profile = newProfile;
        if (!preserveVisualOverrides)
        {
            overrideProfileSprite = null;
            overrideToastNameSprite = null;
            overrideFont = null;
            overrideDescription = string.Empty;
        }
        BuildRuntimeStats();
    }

    public ToastStatProfile.Rarity GetRarity()
    {
        return profile != null ? profile.rarity : ToastStatProfile.Rarity.Common;
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
