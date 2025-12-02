using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 사용 가능한 모든 Toast/Food 능력치 타입
/// </summary>
public enum StatType
{
    Speed,          // 이동 속도 증가
    Jump,           // 점프력 증가
    StaminaRegen,   // 스태미나 회복 속도 증가
    Attack,         // 공격력 증가
    Defense,        // 받는 피해 감소
    Invincibility,  // 무적 시간 증가
    Haste,          // 쿨다운 감소
    Agility,        // 가속도 향상 (momentum build time 감소)
    Sprint,         // 최대/기본 속도 증가
    Poise,          // 넉백 저항 (받는 넉백 감소)
    Thorns,         // 반격 데미지
    Nutrition,      // 음식 회복 보너스
    Friction,       // 감속 증가
    Knockback,      // 넉백력 증가 (주는 넉백 증가)
    DashForce       // Dash 힘 증가
}

[CreateAssetMenu(menuName = "Toast/Stat Profile", fileName = "ToastStatProfile")]
public class ToastStatProfile : ScriptableObject
{
    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2
    }

    public enum StatSelectionMode
    {
        All,            // 모든 스탯 적용
        RandomCount,    // 전체에서 랜덤하게 N개 선택
        GroupBased      // 그룹별로 하나씩 선택
    }

    [Header("Identity")]
    public ToastIndicator.ToastType toastType = ToastIndicator.ToastType.Jam;
    public Rarity rarity = Rarity.Common;
    public Sprite profileSprite;
    public Sprite toastNameSprite;
    public Font font;
    [HideInInspector]
    [TextArea]
    public string description;

    [Header("Stat Selection")]
    [Tooltip("스탯 선택 방식")]
    public StatSelectionMode selectionMode = StatSelectionMode.All;

    [Tooltip("RandomCount 모드: 적용할 최대 스탯 개수 (0이면 제한 없음)")]
    [Min(0)]
    public int maxStatCount = 0;

    [Tooltip("스탯 그룹 리스트. All/RandomCount 모드에서는 각 그룹의 모든 항목이 후보가 됩니다. GroupBased 모드에서는 각 그룹에서 하나씩 랜덤 선택됩니다.")]
    public List<StatGroup> statGroups = new List<StatGroup>();
}

/// <summary>
/// 스탯 그룹 - 그룹 내에서 하나의 스탯이 랜덤 선택됨
/// </summary>
[System.Serializable]
public class StatGroup
{
    [Tooltip("그룹 이름 (에디터 표시용)")]
    public string groupName;

    [Tooltip("이 그룹의 스탯들 (하나가 랜덤 선택됨)")]
    public List<StatGroupEntry> entries = new List<StatGroupEntry>();
}

/// <summary>
/// 그룹 내 개별 스탯 항목 (선택 확률 포함)
/// </summary>
[System.Serializable]
public class StatGroupEntry
{
    [Tooltip("능력치 타입")]
    public StatType statType;

    [Tooltip("선택 확률 가중치 (같은 그룹 내 다른 항목과 비교)")]
    public float weight = 1f;

    [Tooltip("주요 변화량(증감)")]
    public float deltaValue;

    [Tooltip("무작위 가감 범위")]
    public Vector2 varianceRange;

    public List<BonusRoll> bonusRolls = new List<BonusRoll>();

    [Tooltip("단위 표시")]
    public string unit;

    [Tooltip("true면 +/− 기호 표시")]
    public bool showSign = true;
}

[System.Serializable]
public class BonusRoll
{
    [Tooltip("보너스 증감값(감소는 음수).")]
    public float bonusDelta;
    [Tooltip("이 보너스를 적용할 확률 (%).")]
    public float chancePercent;
}
