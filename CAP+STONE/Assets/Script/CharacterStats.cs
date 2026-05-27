using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    public string ID;
    public string Name;
    public float MaxHP;
    public float CurrentHP;
    public float AttackDamage;
    public float AttackSpeed;
    public float MoveSpeed;
    public float Defense;

    public CharacterStats(string id, string name, float hp, float dmg, float spd, float move, float def)
    {
        ID = id;
        Name = name;
        MaxHP = hp;
        CurrentHP = hp;
        AttackDamage = dmg;
        AttackSpeed = spd;
        MoveSpeed = move;
        Defense = def;
    }

    public static CharacterStats CreatePlayer(int upgradeLevel)
    {
        return CreatePlayer(upgradeLevel, upgradeLevel);
    }

    public static CharacterStats CreatePlayer(int hpUpgradeLevel, int attackUpgradeLevel)
    {
        float upgradedHp = Mathf.Round(GameBalance.PlayerStatAfterUpgrade(GameBalance.PlayerBaseHP, hpUpgradeLevel));
        float upgradedAtk = Mathf.Round(GameBalance.PlayerStatAfterUpgrade(GameBalance.PlayerBaseATK, attackUpgradeLevel));
        float hp = EquipmentStatCalculator.ApplyBonus(upgradedHp, EquipmentStatCalculator.GetArmorBonus());
        float atk = EquipmentStatCalculator.ApplyBonus(upgradedAtk, EquipmentStatCalculator.GetWeaponBonus());
        float def = Mathf.Round(GameBalance.PlayerBaseDEF);
        return new CharacterStats("P1", "Hero", hp, atk, GameBalance.DefaultAttackSpeed, GameBalance.PlayerMoveSpeed, def);
    }

    public static CharacterStats CreateEnemy(int stage)
    {
        float hp  = Mathf.Round(GameBalance.EnemyStatAtStage(GameBalance.EnemyBaseHP,  stage));
        float atk = Mathf.Round(GameBalance.EnemyStatAtStage(GameBalance.EnemyBaseATK, stage));
        float def = Mathf.Round(GameBalance.EnemyStatAtStage(GameBalance.EnemyBaseDEF, stage));
        return new CharacterStats("E1", "Monster", hp, atk, 1.0f, GameBalance.EnemyMoveSpeed, def);
    }
}
