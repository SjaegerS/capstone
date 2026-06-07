using UnityEngine;

public static class GameBalance
{
    public const float PlayerBaseHP = 100f;
    public const float PlayerBaseATK = 20f;
    public const float PlayerBaseDEF = 20f;

    public const float EnemyBaseHP = 80f;
    public const float EnemyBaseATK = 10f;
    public const float EnemyBaseDEF = 10f;

    public const float PlayerStatUpgradeRate = 1.13f;
    public const float EnemyGrowthRate = 1.14f;
    public const float RewardGrowthRate = 1.14f;
    public const float ExpLevelGrowthRate = 1.16f;
    public const float UpgradeCostRate = 1.17f;

    public const float EquipmentMainEffectGrowthRate = 1.15f;
    public const float EquipmentSubEffectGrowthRate = 1.05f;
    public const float EquipmentEnhanceCostGrowthRate = 1.18f;

    public const int StatUpgradeAmountBase = 1;
    public const int StatUpgradeGoldBase = 1000;

    public const int BaseRequiredExp = 1000;

    public const int BaseStageRewardAmount = 1000;
    public const float StageExpRewardRatio = 0.6f;
    public const float StageGoldRewardRatio = 0.4f;

    public const int EquipmentEnhanceLevelMin = 1;
    public const int EquipmentEnhanceGoldBase = 500;
    public const int EquipmentEnhanceRequiredDuplicateBase = 2;

    public const int EquipmentGachaCost1 = 100;
    public const int EquipmentGachaCost11 = 1000;
    public const int EquipmentGachaCost55 = 5000;

    public const int NormalEquipmentWeight = 80;
    public const int RareEquipmentWeight = 17;
    public const int SuperRareEquipmentWeight = 3;

    public const float CharacterTypeBonus = 1.0f;
    public const float EquipOptionBonus = 1.0f;
    public const float ConditionBonus = 1.0f;
    public const float ActiveBuffBonus = 1.0f;
    public const float DefaultActiveBuffMultiplier = ActiveBuffBonus;

    public enum ActivityBuffType
    {
        ACTIVITY,     // 활동형
        RESTRAINT, // 절제형
        QUEST,     // 퀘스트형
        OFFLINE     // 종료형
    }

    public enum ConditionGrade
    {
        Normal,
        Good,
        Best
    }

    public const float DefaultAttackSpeed = 1.2f;
    public const float PlayerMoveSpeed = 3.0f;
    public const float EnemyMoveSpeed = 2.0f;

    public static int StatUpgradeAmount(int upgradeLvl)
    {
        int safeLvl = Mathf.Max(1, upgradeLvl);
        int n = safeLvl - 1;
        return Mathf.Max(1, Mathf.RoundToInt(StatUpgradeAmountBase * Mathf.Pow(PlayerStatUpgradeRate, n)));
    }

    public static int HpUpgradeAmount(int hpUpgradeLvl) => StatUpgradeAmount(hpUpgradeLvl);
    public static int AttackUpgradeAmount(int attackUpgradeLvl) => StatUpgradeAmount(attackUpgradeLvl);
    public static int DefenseUpgradeAmount(int defenseUpgradeLvl) => StatUpgradeAmount(defenseUpgradeLvl);

    public static int TotalStatUpgradeBonus(int upgradeLvl)
    {
        int safeLvl = Mathf.Max(1, upgradeLvl);
        int total = 0;

        for (int lvl = 1; lvl < safeLvl; lvl++)
            total += StatUpgradeAmount(lvl);

        return total;
    }

    public static int StatUpgradeGoldCost(int upgradeLvl)
    {
        int safeLvl = Mathf.Max(1, upgradeLvl);
        int n = safeLvl - 1;
        return Mathf.RoundToInt(StatUpgradeGoldBase * Mathf.Pow(UpgradeCostRate, n));
    }

    public static int CharacterUpgradeGoldCost(int characterUpgradeLvl) => StatUpgradeGoldCost(characterUpgradeLvl);

    public static float EnemyStatAtStage(float baseStat, int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        int n = safeStage - 1;
        return baseStat * Mathf.Pow(EnemyGrowthRate, n);
    }

    public static int EnemyHP(int stage) => Mathf.RoundToInt(EnemyStatAtStage(EnemyBaseHP, stage));
    public static int EnemyAttack(int stage) => Mathf.RoundToInt(EnemyStatAtStage(EnemyBaseATK, stage));
    public static int EnemyDefense(int stage) => Mathf.RoundToInt(EnemyStatAtStage(EnemyBaseDEF, stage));

    public static float CalculateDamage(float attack, float defense)
    {
        attack = Mathf.Max(0f, attack);
        defense = Mathf.Max(0f, defense);

        float denominator = defense + attack * 2f;

        if (denominator <= 0f)
            return Mathf.Max(1f, attack);

        float damage = attack * (1f - defense / denominator);
        return Mathf.Max(1f, damage);
    }

    public static int RequiredExp(int playerLevel)
    {
        int safeLevel = Mathf.Max(1, playerLevel);
        int n = safeLevel - 1;
        return Mathf.RoundToInt(BaseRequiredExp * Mathf.Pow(ExpLevelGrowthRate, n));
    }

    public static float ExpRequired(int playerLevel) => RequiredExp(playerLevel);

    public static int StageRewardBaseAmount(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        int n = safeStage - 1;
        return Mathf.RoundToInt(BaseStageRewardAmount * Mathf.Pow(RewardGrowthRate, n));
    }

    public static int RewardExp(int stage)
    {
        return Mathf.RoundToInt(StageRewardBaseAmount(stage) * StageExpRewardRatio);
    }

    public static int RewardGold(int stage)
    {
        return Mathf.RoundToInt(StageRewardBaseAmount(stage) * StageGoldRewardRatio);
    }

    public static int EquipmentBaseMainEffect(string grade)
    {
        switch (NormalizeGrade(grade))
        {
            case "RARE":
                return 10;
            case "SUPER_RARE":
            case "SUPERRARE":
                return 15;
            default:
                return 5;
        }
    }

    public static float EquipmentBaseSubEffectRate(string grade)
    {
        switch (NormalizeGrade(grade))
        {
            case "RARE":
                return 0.05f;
            case "SUPER_RARE":
            case "SUPERRARE":
                return 0.07f;
            default:
                return 0.03f;
        }
    }

    public static int EquipmentMainEffect(string grade, int enhanceLevel)
    {
        return EquipmentMainEffect(EquipmentBaseMainEffect(grade), enhanceLevel);
    }

    public static int EquipmentMainEffect(int baseMainEffect, int enhanceLevel)
    {
        int safeLevel = Mathf.Max(EquipmentEnhanceLevelMin, enhanceLevel);
        int n = safeLevel - 1;
        return Mathf.RoundToInt(baseMainEffect * Mathf.Pow(EquipmentMainEffectGrowthRate, n));
    }

    public static int CalculateEquipmentMainEffect(int baseMainEffect, int enhanceLevel)
    {
        return EquipmentMainEffect(baseMainEffect, enhanceLevel);
    }

    public static float EquipmentSubEffectRate(string grade, int enhanceLevel)
    {
        return EquipmentSubEffectRate(EquipmentBaseSubEffectRate(grade), enhanceLevel);
    }

    public static float EquipmentSubEffectRate(float baseSubEffectRate, int enhanceLevel)
    {
        int safeLevel = Mathf.Max(EquipmentEnhanceLevelMin, enhanceLevel);
        int n = safeLevel - 1;
        return baseSubEffectRate * Mathf.Pow(EquipmentSubEffectGrowthRate, n);
    }

    public static float CalculateEquipmentSubEffectRate(float baseSubEffectRate, int enhanceLevel)
    {
        return EquipmentSubEffectRate(baseSubEffectRate, enhanceLevel);
    }

    public static float ConvertSubEffectRateToMultiplier(float subEffectRate)
    {
        return 1f + Mathf.Max(0f, subEffectRate);
    }

    public static int EquipmentEnhanceRequiredDuplicateCount(int enhanceLevel)
    {
        int safeLevel = Mathf.Max(EquipmentEnhanceLevelMin, enhanceLevel);
        int n = safeLevel - 1;
        return EquipmentEnhanceRequiredDuplicateBase + n;
    }

    public static int EquipmentEnhanceGoldCost(int enhanceLevel)
    {
        int safeLevel = Mathf.Max(EquipmentEnhanceLevelMin, enhanceLevel);
        int n = safeLevel - 1;
        return Mathf.RoundToInt(EquipmentEnhanceGoldBase * Mathf.Pow(EquipmentEnhanceCostGrowthRate, n));
    }

    public static string RollEquipmentGrade(float randomValue01)
    {
        float value = Mathf.Clamp01(randomValue01) * 100f;

        if (value < NormalEquipmentWeight)
            return "NORMAL";

        if (value < NormalEquipmentWeight + RareEquipmentWeight)
            return "RARE";

        return "SUPER_RARE";
    }

    public static int PlayerHPFromUpgradeLvl(int hpUpgradeLvl)
    {
        return Mathf.RoundToInt(PlayerBaseHP + TotalStatUpgradeBonus(hpUpgradeLvl));
    }

    public static int PlayerAttackFromUpgradeLvl(int attackUpgradeLvl)
    {
        return Mathf.RoundToInt(PlayerBaseATK + TotalStatUpgradeBonus(attackUpgradeLvl));
    }

    public static int PlayerDefenseFromUpgradeLvl(int defenseUpgradeLvl)
    {
        return Mathf.RoundToInt(PlayerBaseDEF + TotalStatUpgradeBonus(defenseUpgradeLvl));
    }

    public static float CalculateTotalBonusMultiplier(
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return Mathf.Max(0f, characterTypeBonus)
            * Mathf.Max(0f, equipOptionBonus)
            * Mathf.Max(0f, conditionBonus)
            * Mathf.Max(0f, activeBuffBonus);
    }

    public static int CalculateFinalStat(
        float baseStat,
        int statUpgradeLvl,
        int equipmentMainEffectSum,
        float equipmentSubEffectMultiplier,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        float safeBaseStat = Mathf.Max(0f, baseStat);
        int safeMainEffect = Mathf.Max(0, equipmentMainEffectSum);
        float safeSubMultiplier = Mathf.Max(1f, equipmentSubEffectMultiplier);
        int statUpgradeBonus = TotalStatUpgradeBonus(statUpgradeLvl);

        float totalBonusMultiplier = CalculateTotalBonusMultiplier(
            characterTypeBonus,
            equipOptionBonus,
            conditionBonus,
            activeBuffBonus
        );

        return Mathf.RoundToInt((safeBaseStat + statUpgradeBonus + safeMainEffect) * safeSubMultiplier * totalBonusMultiplier);
    }

    public static int CalculateFinalHP(
        int hpUpgradeLvl,
        int equipmentMainEffectSum = 0,
        float equipmentSubEffectMultiplier = 1f,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return Mathf.Max(1, CalculateFinalStat(PlayerBaseHP, hpUpgradeLvl, equipmentMainEffectSum, equipmentSubEffectMultiplier, characterTypeBonus, equipOptionBonus, conditionBonus, activeBuffBonus));
    }

    public static int CalculateFinalAttackPower(
        int attackUpgradeLvl,
        int equipmentMainEffectSum = 0,
        float equipmentSubEffectMultiplier = 1f,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return CalculateFinalStat(PlayerBaseATK, attackUpgradeLvl, equipmentMainEffectSum, equipmentSubEffectMultiplier, characterTypeBonus, equipOptionBonus, conditionBonus, activeBuffBonus);
    }

    public static int CalculateFinalDefensePower(
        int defenseUpgradeLvl,
        int equipmentMainEffectSum = 0,
        float equipmentSubEffectMultiplier = 1f,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return CalculateFinalStat(PlayerBaseDEF, defenseUpgradeLvl, equipmentMainEffectSum, equipmentSubEffectMultiplier, characterTypeBonus, equipOptionBonus, conditionBonus, activeBuffBonus);
    }

    public static int CalculateFinalStatFromDbValue(
        int dbStat,
        int equipmentMainEffectSum,
        float equipmentSubEffectMultiplier,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        int safeDbStat = Mathf.Max(0, dbStat);
        int safeMainEffect = Mathf.Max(0, equipmentMainEffectSum);
        float safeSubMultiplier = Mathf.Max(1f, equipmentSubEffectMultiplier);

        float totalBonusMultiplier = CalculateTotalBonusMultiplier(
            characterTypeBonus,
            equipOptionBonus,
            conditionBonus,
            activeBuffBonus
        );

        return Mathf.RoundToInt((safeDbStat + safeMainEffect) * safeSubMultiplier * totalBonusMultiplier);
    }

    public static int CalculateFinalHPFromDbValue(
        int dbMaxHp,
        int equipmentMainEffectSum = 0,
        float equipmentSubEffectMultiplier = 1f,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return Mathf.Max(1, CalculateFinalStatFromDbValue(dbMaxHp, equipmentMainEffectSum, equipmentSubEffectMultiplier, characterTypeBonus, equipOptionBonus, conditionBonus, activeBuffBonus));
    }

    public static int CalculateFinalAttackPowerFromDbValue(
        int dbAttackPower,
        int equipmentMainEffectSum = 0,
        float equipmentSubEffectMultiplier = 1f,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return CalculateFinalStatFromDbValue(dbAttackPower, equipmentMainEffectSum, equipmentSubEffectMultiplier, characterTypeBonus, equipOptionBonus, conditionBonus, activeBuffBonus);
    }

    public static int CalculateFinalDefensePowerFromDbValue(
        int dbDefensePower,
        int equipmentMainEffectSum = 0,
        float equipmentSubEffectMultiplier = 1f,
        float characterTypeBonus = CharacterTypeBonus,
        float equipOptionBonus = EquipOptionBonus,
        float conditionBonus = ConditionBonus,
        float activeBuffBonus = ActiveBuffBonus
    )
    {
        return CalculateFinalStatFromDbValue(dbDefensePower, equipmentMainEffectSum, equipmentSubEffectMultiplier, characterTypeBonus, equipOptionBonus, conditionBonus, activeBuffBonus);
    }

    public static void AddEquipmentEffect(
        string grade,
        int enhanceLevel,
        ref int mainEffectSum,
        ref float subEffectMultiplier
    )
    {
        int mainEffect = EquipmentMainEffect(grade, enhanceLevel);
        float subEffectRate = EquipmentSubEffectRate(grade, enhanceLevel);
        float multiplier = ConvertSubEffectRateToMultiplier(subEffectRate);

        mainEffectSum += mainEffect;
        subEffectMultiplier *= multiplier;
    }

    public static ConditionGrade GetConditionGradeByScore(int conditionScore)
    {
        if (conditionScore >= 70)
            return ConditionGrade.Best;

        if (conditionScore >= 40)
            return ConditionGrade.Good;

        return ConditionGrade.Normal;
    }

    public static float GetActivityBuffPercent(ActivityBuffType buffType, ConditionGrade grade)
    {
        switch (buffType)
        {
            case ActivityBuffType.ACTIVITY:
                return GetActiveBuffPercent(grade);

            case ActivityBuffType.RESTRAINT:
                return GetRestraintBuffPercent(grade);

            case ActivityBuffType.QUEST:
                return GetQuestBuffPercent(grade);

            case ActivityBuffType.OFFLINE:
                return GetEndingBuffPercent(grade);

            default:
                return 0f;
        }
    }

    public static float GetActivityBuffMultiplier(ActivityBuffType buffType, ConditionGrade grade)
    {
        float percent = GetActivityBuffPercent(buffType, grade);
        return 1f + percent / 100f;
    }

    public static float GetActivityBuffMultiplier(ActivityBuffType buffType, int conditionScore)
    {
        ConditionGrade grade = GetConditionGradeByScore(conditionScore);
        return GetActivityBuffMultiplier(buffType, grade);
    }

    // 활동형: 보통 2%, 좋음 4%, 최상 6%
    private static float GetActiveBuffPercent(ConditionGrade grade)
    {
        switch (grade)
        {
            case ConditionGrade.Normal:
                return 2f;

            case ConditionGrade.Good:
                return 4f;

            case ConditionGrade.Best:
                return 6f;

            default:
                return 0f;
        }
    }

    // 절제형: 보통 0%, 좋음 5%, 최상 10%
    private static float GetRestraintBuffPercent(ConditionGrade grade)
    {
        switch (grade)
        {
            case ConditionGrade.Normal:
                return 0f;

            case ConditionGrade.Good:
                return 5f;

            case ConditionGrade.Best:
                return 10f;

            default:
                return 0f;
        }
    }

    // 퀘스트형: 보통 골드 +1%, 좋음 5%, 최상 10%
    // 보석 증가는 normal 단계에서는 없음. 보석 보상 계산은 별도 메서드에서 처리.
    private static float GetQuestBuffPercent(ConditionGrade grade)
    {
        switch (grade)
        {
            case ConditionGrade.Normal:
                return 1f;

            case ConditionGrade.Good:
                return 5f;

            case ConditionGrade.Best:
                return 10f;

            default:
                return 0f;
        }
    }

    // 종료형: 게임 종료 중 자동사냥 보상 증가
    // 구체 수치가 아직 없으므로 임시값. 필요하면 여기만 수정.
    private static float GetEndingBuffPercent(ConditionGrade grade)
    {
        switch (grade)
        {
            case ConditionGrade.Normal:
                return 2f;

            case ConditionGrade.Good:
                return 5f;

            case ConditionGrade.Best:
                return 10f;

            default:
                return 0f;
        }
    }

    public static int ApplyQuestGoldBuff(int baseGold, int conditionScore)
    {
        float multiplier = GetActivityBuffMultiplier(ActivityBuffType.QUEST, conditionScore);
        return Mathf.RoundToInt(baseGold * multiplier);
    }

    public static int ApplyQuestGemBuff(int baseGem, int conditionScore)
    {
        ConditionGrade grade = GetConditionGradeByScore(conditionScore);

        // 보통: 보석 증가 없음
        if (grade == ConditionGrade.Normal)
            return baseGem;

        float multiplier = GetActivityBuffMultiplier(ActivityBuffType.QUEST, grade);
        return Mathf.RoundToInt(baseGem * multiplier);
    }

    public static int ApplyEndingRewardBuff(int baseReward, int conditionScore)
    {
        float multiplier = GetActivityBuffMultiplier(ActivityBuffType.OFFLINE, conditionScore);
        return Mathf.RoundToInt(baseReward * multiplier);
    }

    private static string NormalizeGrade(string grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
            return "NORMAL";

        return grade.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
    }



}
