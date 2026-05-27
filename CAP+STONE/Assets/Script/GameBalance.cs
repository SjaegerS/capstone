using UnityEngine;

public static class GameBalance
{
    // ── 플레이어 기본 스탯 ────────────────────────────────────────
    public const float PlayerBaseHP  = 100f;
    public const float PlayerBaseATK = 20f;
    public const float PlayerBaseDEF = 20f;

    // ── 몬스터 기본 스탯 ─────────────────────────────────────────
    public const float EnemyBaseHP  = 80f;
    public const float EnemyBaseATK = 10f;
    public const float EnemyBaseDEF = 10f;

    // ── 공식 계수 ─────────────────────────────────────────────────
    private const float PlayerGrowthRate   = 1.13f;
    private const float EnemyGrowthRate    = 1.15f;
    private const float RewardGrowthRate   = 1.14f;
    private const float ExpLevelGrowthRate = 1.16f;
    private const float UpgradeCostRate    = 1.17f;

    // ── 미확정 보정 배율 (기획서 수치 미정 → 1.0) ─────────────────
    public const float CharacterTypeBonus = 1.0f;
    public const float EquipOptionBonus   = 1.0f;
    public const float ConditionBonus     = 1.0f;

    // ── 기본 공격속도/이동속도 ────────────────────────────────────
    public const float DefaultAttackSpeed = 1.2f;
    public const float PlayerMoveSpeed    = 3.0f;
    public const float EnemyMoveSpeed     = 2.0f;

    // ── 보상 비율 ─────────────────────────────────────────────────
    private const float BaseReward              = 1000f;
    private const float ExpRatio                = 0.6f;
    private const float GoldRatio               = 0.4f;
    public  const float GoldContentMultiplier   = 1.5f;

    /// <summary>플레이어 스탯 강화 후 수치 — 해석 A: base + 1×1.13^n</summary>
    /// <remarks>
    /// ⚠ 기획자 확인 필요: 해석 B(base + n×1.13^n) 또는 C(base × 1.13^n)일 경우
    /// 이 한 줄만 수정하면 됩니다.
    /// </remarks>
    public static float PlayerStatAfterUpgrade(float baseStat, int upgradeLevel)
    {
        int n = Mathf.Max(0, upgradeLevel);
        return baseStat * Mathf.Pow(PlayerGrowthRate, n);
    }

    /// <summary>스테이지 n 몬스터 스탯 — base × 1.15^(n-1)</summary>
    public static float EnemyStatAtStage(float baseStat, int stage)
    {
        int n = Mathf.Max(0, stage - 1);
        return baseStat * Mathf.Pow(EnemyGrowthRate, n);
    }

    /// <summary>데미지 공식 — atk × (1 - def / (def + atk×2))</summary>
    public static float CalculateDamage(float atk, float def)
    {
        float denom = def + atk * 2f;
        if (denom <= 0f) return atk;
        return atk * (1f - def / denom);
    }

    /// <summary>레벨 n → 다음 레벨 필요 경험치 — 1000 × 1.16^n</summary>
    public static float ExpRequired(int level)
    {
        return 1000f * Mathf.Pow(ExpLevelGrowthRate, level);
    }

    /// <summary>강화 n회차 골드 비용 — 1000 × 1.17^n</summary>
    public static float StatUpgradeGoldCost(int upgradeCount)
    {
        return 1000f * Mathf.Pow(UpgradeCostRate, upgradeCount);
    }

    /// <summary>Character upgrade gold cost: 1000 * 1.17^n.</summary>
    public static int CharacterUpgradeGoldCost(int upgradeCount)
    {
        return Mathf.RoundToInt(StatUpgradeGoldCost(upgradeCount));
    }

    /// <summary>스테이지 n 총 보상 — 1000 × 1.14^(n-1)</summary>
    public static float TotalRewardAtStage(int stage)
    {
        int n = Mathf.Max(0, stage - 1);
        return BaseReward * Mathf.Pow(RewardGrowthRate, n);
    }

    /// <summary>스테이지 n 경험치 보상 (총 보상의 60%)</summary>
    public static int RewardExp(int stage)
    {
        return Mathf.RoundToInt(TotalRewardAtStage(stage) * ExpRatio);
    }

    /// <summary>스테이지 n 골드 보상 (총 보상의 40%)</summary>
    public static int RewardGold(int stage)
    {
        return Mathf.RoundToInt(TotalRewardAtStage(stage) * GoldRatio);
    }

    /// <summary>골드 콘텐츠 보상 — baseGold × 1.5</summary>
    public static float GoldContentReward(float baseGold)
    {
        return baseGold * GoldContentMultiplier;
    }
}
