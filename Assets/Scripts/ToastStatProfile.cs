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
    [Tooltip("true면 Stats 리스트에서 랜덤하게 선택합니다. false면 모든 Stats가 적용됩니다.")]
    public bool randomSelectStats = false;

    [Tooltip("랜덤 선택 시 적용할 최대 스탯 개수 (0이면 제한 없음)")]
    [Min(0)]
    public int maxStatCount = 0;

    public List<StatEntry> stats = new List<StatEntry>();
}

[System.Serializable]
public class StatEntry
{
    [Tooltip("능력치 타입 (드롭다운에서 선택)")]
    public StatType statType;

    [FormerlySerializedAs("baseValue")]
    [Tooltip("주요 변화량(증감). 감소는 음수로 입력.")]
    public float deltaValue;

    [Tooltip("무작위 가감 범위(스폰마다 적용).")]
    public Vector2 varianceRange;

    public List<BonusRoll> bonusRolls = new List<BonusRoll>();

    [Tooltip("이 스탯이 등장할 최소 등급")]
    public ToastStatProfile.Rarity minRarity = ToastStatProfile.Rarity.Common;

    [Tooltip("이 스탯이 등장할 최대 등급")]
    public ToastStatProfile.Rarity maxRarity = ToastStatProfile.Rarity.Epic;

    [Tooltip("단위 표시 (예: m/s, %, 등)")]
    public string unit;

    [Tooltip("true면 +/− 기호를 붙여 증감량처럼 표기합니다.")]
    public bool showSign = true;

    // 이전 버전과의 호환성을 위한 필드 (기존 데이터 마이그레이션용)
    [HideInInspector]
    [FormerlySerializedAs("name")]
    public string oldName;
}

[System.Serializable]
public class BonusRoll
{
    [Tooltip("보너스 증감값(감소는 음수).")]
    public float bonusDelta;
    [Tooltip("이 보너스를 적용할 확률 (%).")]
    public float chancePercent;
}
