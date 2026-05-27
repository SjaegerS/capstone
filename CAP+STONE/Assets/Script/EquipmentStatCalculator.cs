using System.Collections.Generic;
using UnityEngine;

public struct EquipmentStatBonus
{
    public int FlatStat;
    public int PercentStat;
}

public static class EquipmentStatCalculator
{
    private const float FlatStatGrowthRate = 1.15f;
    private const float PercentStatGrowthRate = 1.05f;

    public static EquipmentStatBonus GetWeaponBonus()
    {
        return GetBonus(EquipmentCategory.Weapon);
    }

    public static EquipmentStatBonus GetArmorBonus()
    {
        return GetBonus(EquipmentCategory.Armor);
    }

    public static int ApplyBonus(float upgradedBaseStat, EquipmentStatBonus bonus)
    {
        float total = (upgradedBaseStat + bonus.FlatStat) * (100f + bonus.PercentStat) / 100f;
        return Mathf.RoundToInt(total);
    }

    public static int GetBonusIncrease(float upgradedBaseStat, EquipmentStatBonus bonus)
    {
        return ApplyBonus(upgradedBaseStat, bonus) - Mathf.RoundToInt(upgradedBaseStat);
    }

    private static EquipmentStatBonus GetBonus(EquipmentCategory category)
    {
        EquipmentStatBonus totalBonus = new EquipmentStatBonus();
        IEnumerable<EquipmentInventoryRecord> records = EquipmentInventory.GetOwnedRecords();

        foreach (EquipmentInventoryRecord record in records)
        {
            if (record == null || !record.IsOwned || record.Category != category)
            {
                continue;
            }

            totalBonus.FlatStat += GetFlatStat(record.Rarity, record.Level);
            totalBonus.PercentStat += GetPercentStat(record.Rarity, record.Level);
        }

        return totalBonus;
    }

    private static int GetFlatStat(EquipmentRarityGrade rarity, int level)
    {
        int baseStat = GetBaseFlatStat(rarity);
        int safeLevel = Mathf.Max(1, level);
        return Mathf.RoundToInt(baseStat * Mathf.Pow(FlatStatGrowthRate, safeLevel - 1));
    }

    private static int GetPercentStat(EquipmentRarityGrade rarity, int level)
    {
        int basePercent = GetBasePercentStat(rarity);
        int safeLevel = Mathf.Max(1, level);
        return Mathf.RoundToInt(basePercent * Mathf.Pow(PercentStatGrowthRate, safeLevel - 1));
    }

    private static int GetBaseFlatStat(EquipmentRarityGrade rarity)
    {
        switch (rarity)
        {
            case EquipmentRarityGrade.SuperRare:
                return 15;
            case EquipmentRarityGrade.Rare:
                return 10;
            default:
                return 5;
        }
    }

    private static int GetBasePercentStat(EquipmentRarityGrade rarity)
    {
        switch (rarity)
        {
            case EquipmentRarityGrade.SuperRare:
                return 7;
            case EquipmentRarityGrade.Rare:
                return 5;
            default:
                return 3;
        }
    }
}
