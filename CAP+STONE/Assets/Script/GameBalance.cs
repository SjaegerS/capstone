using UnityEngine;

public static class GameBalance
{
    // ─────────────────────────────────────
    // 플레이어 기본 스탯
    // ─────────────────────────────────────
    public const float PlayerBaseHP = 100f;
    public const float PlayerBaseATK = 20f;
    public const float PlayerBaseDEF = 20f;

    // ─────────────────────────────────────
    // 몬스터 기본 스탯
    // ─────────────────────────────────────
    public const float EnemyBaseHP = 80f;
    public const float EnemyBaseATK = 10f;
    public const float EnemyBaseDEF = 10f;

    // ─────────────────────────────────────
    // 성장률 상수
    // ─────────────────────────────────────
    public const float PlayerStatUpgradeRate = 1.13f;
    public const float EnemyGrowthRate = 1.14f;
    public const float RewardGrowthRate = 1.14f;
    public const float ExpLevelGrowthRate = 1.16f;
    public const float UpgradeCostRate = 1.17f;
    public const float CharacterUpgradeCostRate = 1.20f;

    // ─────────────────────────────────────
    // 강화 기본값
    // ─────────────────────────────────────
    public const int StatUpgradeAmountBase = 1;
    public const int StatUpgradeGoldBase = 1000;
    public const int CharacterUpgradeGoldBase = 3000;

    // ─────────────────────────────────────
    // 보상 기본값
    // ─────────────────────────────────────
    public const int BaseStageExpReward = 60;
    public const int BaseStageGoldReward = 1000;

    public const float GoldContentMultiplier = 1.5f;

    // ─────────────────────────────────────
    // 레벨업 경험치
    // ─────────────────────────────────────
    public const int BaseRequiredExp = 1000;

    // ─────────────────────────────────────
    // 임시 보정 배율
    // ─────────────────────────────────────
    public const float CharacterTypeBonus = 1.0f;
    public const float EquipOptionBonus = 1.0f;
    public const float ConditionBonus = 1.0f;

    // ─────────────────────────────────────
    // 기본 공격속도 / 이동속도
    // ─────────────────────────────────────
    public const float DefaultAttackSpeed = 1.2f;
    public const float PlayerMoveSpeed = 3.0f;
    public const float EnemyMoveSpeed = 2.0f;

    // ─────────────────────────────────────
    // 스탯 강화 증가량
    // 공식: 1 × 1.13^n
    //
    // DB upgrade_lvl 기본값이 1이면:
    // Lv.1 -> Lv.2 강화 시 n = 0
    // Lv.2 -> Lv.3 강화 시 n = 1
    // ─────────────────────────────────────
    public static int StatUpgradeAmount(int upgradeLvl)
    {
        int safeLvl = Mathf.Max(1, upgradeLvl);
        int n = safeLvl - 1;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(StatUpgradeAmountBase * Mathf.Pow(PlayerStatUpgradeRate, n))
        );
    }

    public static int HpUpgradeAmount(int hpUpgradeLvl)
    {
        return StatUpgradeAmount(hpUpgradeLvl);
    }

    public static int AttackUpgradeAmount(int attackUpgradeLvl)
    {
        return StatUpgradeAmount(attackUpgradeLvl);
    }

    public static int DefenseUpgradeAmount(int defenseUpgradeLvl)
    {
        return StatUpgradeAmount(defenseUpgradeLvl);
    }

    // ─────────────────────────────────────
    // 스탯 강화 비용
    // 공식: 1000 × 1.17^n
    //
    // Lv.1 -> Lv.2 비용 = 1000
    // Lv.2 -> Lv.3 비용 = 1170
    // ─────────────────────────────────────
    public static int StatUpgradeGoldCost(int upgradeLvl)
    {
        int safeLvl = Mathf.Max(1, upgradeLvl);
        int n = safeLvl - 1;

        return Mathf.RoundToInt(StatUpgradeGoldBase * Mathf.Pow(UpgradeCostRate, n));
    }

    public static int CharacterUpgradeGoldCost(int characterUpgradeLvl)
    {
        int safeLvl = Mathf.Max(1, characterUpgradeLvl);
        int n = safeLvl - 1;

        return Mathf.RoundToInt(CharacterUpgradeGoldBase * Mathf.Pow(CharacterUpgradeCostRate, n));
    }

    // ─────────────────────────────────────
    // 몬스터 스탯
    // 공식: 기본 스탯 × 1.14^n
    //
    // Stage 1이면 n = 0
    // Stage 2이면 n = 1
    // ─────────────────────────────────────
    public static float EnemyStatAtStage(float baseStat, int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        int n = safeStage - 1;

        return baseStat * Mathf.Pow(EnemyGrowthRate, n);
    }

    public static int EnemyHP(int stage)
    {
        return Mathf.RoundToInt(EnemyStatAtStage(EnemyBaseHP, stage));
    }

    public static int EnemyAttack(int stage)
    {
        return Mathf.RoundToInt(EnemyStatAtStage(EnemyBaseATK, stage));
    }

    public static int EnemyDefense(int stage)
    {
        return Mathf.RoundToInt(EnemyStatAtStage(EnemyBaseDEF, stage));
    }

    // ─────────────────────────────────────
    // 전투 데미지
    // 공식: 체력 - (공격력 × (1 - 방어력 / (방어력 + 상대 공격력 × 2)))
    // 여기서는 실제 데미지만 반환
    // ─────────────────────────────────────
    public static float CalculateDamage(float attack, float defense)
    {
        float denominator = defense + attack * 2f;

        if (denominator <= 0f)
        {
            return attack;
        }

        float damage = attack * (1f - defense / denominator);

        return Mathf.Max(1f, damage);
    }

    // ─────────────────────────────────────
    // 필요 경험치
    // 공식: 1000 × 1.16^n
    //
    // Lv.1 -> Lv.2 필요 경험치 = 1000
    // ─────────────────────────────────────
    public static int RequiredExp(int playerLevel)
    {
        int safeLevel = Mathf.Max(1, playerLevel);
        int n = safeLevel - 1;

        return Mathf.RoundToInt(BaseRequiredExp * Mathf.Pow(ExpLevelGrowthRate, n));
    }

    // 기존 이름 호환용
    public static float ExpRequired(int playerLevel)
    {
        return RequiredExp(playerLevel);
    }

    // ─────────────────────────────────────
    // 스테이지 클리어 보상
    // 기본: 경험치 60, 골드 40
    // 공식: 기본값 × 1.14^n
    // ─────────────────────────────────────
    public static int RewardExp(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        int n = safeStage - 1;

        return Mathf.RoundToInt(BaseStageExpReward * Mathf.Pow(RewardGrowthRate, n));
    }

    public static int RewardGold(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        int n = safeStage - 1;

        return Mathf.RoundToInt(BaseStageGoldReward * Mathf.Pow(RewardGrowthRate, n));
    }

    // ─────────────────────────────────────
    // 골드 콘텐츠 보상
    // 공식: 골드 × 150%
    // ─────────────────────────────────────
    public static int GoldContentReward(int baseGold)
    {
        return Mathf.RoundToInt(baseGold * GoldContentMultiplier);
    }

    // ─────────────────────────────────────
    // 플레이어 스탯 계산용
    // 현재 DB에서는 max_hp, attack_power를 서버에 저장하므로
    // 이 함수는 로컬 계산/테스트용
    // ─────────────────────────────────────
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

    // Lv.1은 기본 상태로 보고, Lv.2부터 누적 보너스 계산
    public static int TotalStatUpgradeBonus(int upgradeLvl)
    {
        int safeLvl = Mathf.Max(1, upgradeLvl);
        int total = 0;

        for (int lvl = 1; lvl < safeLvl; lvl++)
        {
            total += StatUpgradeAmount(lvl);
        }

        return total;
    }
}