using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    public string Id { get; private set; }
    public string DisplayName { get; private set; }

    public float MaxHP { get; private set; }

    // UnitController에서 현재 체력을 직접 읽고 수정하므로 public set 유지
    public float CurrentHP { get; set; }

    public float AttackDamage { get; private set; }
    public float Defense { get; private set; }
    public float AttackSpeed { get; private set; }
    public float MoveSpeed { get; private set; }

    public CharacterStats(
        string id,
        string displayName,
        float maxHP,
        float attackDamage,
        float defense,
        float attackSpeed,
        float moveSpeed
    )
    {
        Id = id;
        DisplayName = displayName;

        MaxHP = maxHP;
        CurrentHP = maxHP;

        AttackDamage = attackDamage;
        Defense = defense;
        AttackSpeed = attackSpeed;
        MoveSpeed = moveSpeed;
    }

    public static CharacterStats CreatePlayer(
        int hpUpgradeLvl,
        int attackUpgradeLvl
    )
    {
        int safeHpLvl = Mathf.Max(1, hpUpgradeLvl);
        int safeAttackLvl = Mathf.Max(1, attackUpgradeLvl);

        float maxHP = GameBalance.PlayerBaseHP + GameBalance.TotalStatUpgradeBonus(safeHpLvl);
        float attackDamage = GameBalance.PlayerBaseATK + GameBalance.TotalStatUpgradeBonus(safeAttackLvl);
        float defense = GameBalance.PlayerBaseDEF;

        maxHP *= GameBalance.CharacterTypeBonus;
        maxHP *= GameBalance.EquipOptionBonus;
        maxHP *= GameBalance.ConditionBonus;

        attackDamage *= GameBalance.CharacterTypeBonus;
        attackDamage *= GameBalance.EquipOptionBonus;
        attackDamage *= GameBalance.ConditionBonus;

        defense *= GameBalance.CharacterTypeBonus;
        defense *= GameBalance.EquipOptionBonus;
        defense *= GameBalance.ConditionBonus;

        return new CharacterStats(
            "player",
            "Player",
            maxHP,
            attackDamage,
            defense,
            GameBalance.DefaultAttackSpeed,
            GameBalance.PlayerMoveSpeed
        );
    }

    public static CharacterStats CreatePlayerFromDb(
        float maxHP,
        float attackPower,
        float defensePower
    )
    {
        return new CharacterStats(
            "player",
            "Player",
            maxHP,
            attackPower,
            defensePower,
            GameBalance.DefaultAttackSpeed,
            GameBalance.PlayerMoveSpeed
        );
    }

    public static CharacterStats CreateEnemy(int stage)
    {
        int safeStage = Mathf.Max(1, stage);

        float maxHP = GameBalance.EnemyHP(safeStage);
        float attackDamage = GameBalance.EnemyAttack(safeStage);
        float defense = GameBalance.EnemyDefense(safeStage);

        return new CharacterStats(
            $"enemy_stage_{safeStage}",
            $"Enemy Stage {safeStage}",
            maxHP,
            attackDamage,
            defense,
            GameBalance.DefaultAttackSpeed,
            GameBalance.EnemyMoveSpeed
        );
    }

    public void ResetCurrentHP()
    {
        CurrentHP = MaxHP;
    }

    public void TakeDamage(float damage)
    {
        CurrentHP = Mathf.Max(0f, CurrentHP - damage);
    }

    public bool IsDead()
    {
        return CurrentHP <= 0f;
    }
}