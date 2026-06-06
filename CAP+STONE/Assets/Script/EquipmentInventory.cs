using System.Collections.Generic;
using UnityEngine;

public enum EquipmentCategory
{
    Weapon,
    Armor
}

public enum EquipmentRarityGrade
{
    Normal,
    Rare,
    SuperRare
}

public struct EquipmentStatSummary
{
    public int hpMainEffectSum;
    public int attackMainEffectSum;
    public int defenseMainEffectSum;

    public float hpSubEffectMultiplier;
    public float attackSubEffectMultiplier;
    public float defenseSubEffectMultiplier;

    public int HpMainEffectSum => hpMainEffectSum;
    public int AttackMainEffectSum => attackMainEffectSum;
    public int DefenseMainEffectSum => defenseMainEffectSum;

    public float HpSubEffectMultiplier => hpSubEffectMultiplier;
    public float AttackSubEffectMultiplier => attackSubEffectMultiplier;
    public float DefenseSubEffectMultiplier => defenseSubEffectMultiplier;

    public static EquipmentStatSummary Empty
    {
        get
        {
            EquipmentStatSummary summary = new EquipmentStatSummary();

            summary.hpMainEffectSum = 0;
            summary.attackMainEffectSum = 0;
            summary.defenseMainEffectSum = 0;

            summary.hpSubEffectMultiplier = 1f;
            summary.attackSubEffectMultiplier = 1f;
            summary.defenseSubEffectMultiplier = 1f;

            return summary;
        }
    }
}

public sealed class EquipmentInventoryRecord
{
    public string SpriteName { get; private set; }

    public int UserItemId { get; private set; }
    public int ItemId { get; private set; }

    public int TotalCount { get; private set; }
    public int Level { get; private set; }
    public int RequiredCount { get; private set; }

    public string ItemKey { get; private set; }
    public string ImageKey { get; private set; }
    public string ItemName { get; private set; }
    public string ItemType { get; private set; }
    public string ItemGrade { get; private set; }

    public int EnhanceGoldCost { get; private set; }
    public bool IsEquipped { get; private set; }

    public int FinalAttack { get; private set; }
    public int FinalDefense { get; private set; }

    public EquipmentCategory Category { get; private set; }
    public EquipmentRarityGrade Rarity { get; private set; }

    public bool IsOwned => UserItemId > 0 || TotalCount > 0 || Level > 0;
    public bool CanUpgrade { get; private set; }

    public EquipmentInventoryRecord(
        string spriteName,
        int totalCount,
        int level,
        EquipmentCategory category,
        EquipmentRarityGrade rarity,
        int userItemId = 0,
        int itemId = 0
    )
    {
        SpriteName = string.IsNullOrEmpty(spriteName) ? string.Empty : spriteName;

        UserItemId = Mathf.Max(0, userItemId);
        ItemId = Mathf.Max(0, itemId);

        TotalCount = Mathf.Max(0, totalCount);
        Level = Mathf.Max(0, level);

        Category = category;
        Rarity = rarity;

        ItemKey = SpriteName;
        ImageKey = SpriteName;
        ItemName = SpriteName;
        ItemType = ConvertCategoryToItemType(category);
        ItemGrade = ConvertRarityToGrade(rarity);

        EnhanceGoldCost = 0;
        IsEquipped = false;

        FinalAttack = 0;
        FinalDefense = 0;

        CanUpgrade = false;

        Recalculate();
    }

    public void SetMetadata(EquipmentCategory category, EquipmentRarityGrade rarity)
    {
        Category = category;
        Rarity = rarity;

        ItemType = ConvertCategoryToItemType(category);
        ItemGrade = ConvertRarityToGrade(rarity);

        Recalculate();
    }

    public void SetServerData(int userItemId, int itemId, int quantity, int enhanceLevel)
    {
        UserItemId = Mathf.Max(0, userItemId);
        ItemId = Mathf.Max(0, itemId);
        TotalCount = Mathf.Max(0, quantity);
        Level = Mathf.Max(0, enhanceLevel);

        Recalculate();
    }

    public void SetServerEquipmentData(
        int userItemId,
        int itemId,
        string itemKey,
        string imageKey,
        string itemName,
        string itemType,
        string itemGrade,
        int enhanceLevel,
        int quantity,
        bool isEquipped
    )
    {
        UserItemId = Mathf.Max(0, userItemId);
        ItemId = Mathf.Max(0, itemId);

        ItemKey = string.IsNullOrEmpty(itemKey) ? SpriteName : itemKey;
        ImageKey = string.IsNullOrEmpty(imageKey) ? ItemKey : imageKey;
        ItemName = string.IsNullOrEmpty(itemName) ? ItemKey : itemName;

        ItemType = NormalizeItemType(itemType);
        ItemGrade = NormalizeGrade(itemGrade);

        Category = ConvertItemTypeToCategory(ItemType);
        Rarity = ConvertGradeToRarity(ItemGrade);

        Level = Mathf.Max(1, enhanceLevel);
        TotalCount = Mathf.Max(0, quantity);

        IsEquipped = isEquipped;

        Recalculate();
    }

    public void SetServerEquipmentData(
        int userItemId,
        int itemId,
        string itemKey,
        string imageKey,
        string itemName,
        string itemType,
        string itemGrade,
        int enhanceLevel,
        int quantity,
        int requiredCount,
        int enhanceGoldCost,
        bool isEquipped,
        bool canUpgrade,
        int finalAttack,
        int finalDefense
    )
    {
        UserItemId = Mathf.Max(0, userItemId);
        ItemId = Mathf.Max(0, itemId);

        ItemKey = string.IsNullOrEmpty(itemKey) ? SpriteName : itemKey;
        ImageKey = string.IsNullOrEmpty(imageKey) ? ItemKey : imageKey;
        ItemName = string.IsNullOrEmpty(itemName) ? ItemKey : itemName;

        ItemType = NormalizeItemType(itemType);
        ItemGrade = NormalizeGrade(itemGrade);

        Category = ConvertItemTypeToCategory(ItemType);
        Rarity = ConvertGradeToRarity(ItemGrade);

        Level = Mathf.Max(1, enhanceLevel);
        TotalCount = Mathf.Max(0, quantity);

        IsEquipped = isEquipped;

        Recalculate();
    }

    public void SetEquipped(bool isEquipped)
    {
        IsEquipped = isEquipped;
        Recalculate();
    }

    public void Add(int amount)
    {
        TotalCount = Mathf.Max(0, TotalCount + amount);

        if (TotalCount > 0 && Level <= 0)
            Level = 1;

        Recalculate();
    }

    public bool TryUpgradeLocalOnly()
    {
        Recalculate();

        bool localCanUpgrade =
            IsOwned &&
            UserItemId > 0 &&
            TotalCount >= RequiredCount;

        if (!localCanUpgrade)
            return false;

        TotalCount -= RequiredCount;
        Level++;

        Recalculate();
        return true;
    }

    private void Recalculate()
    {
        if (!IsOwned)
        {
            Level = 0;
            RequiredCount = GameBalance.EquipmentEnhanceRequiredDuplicateCount(1);
            EnhanceGoldCost = GameBalance.EquipmentEnhanceGoldCost(1);
            CanUpgrade = false;
            FinalAttack = 0;
            FinalDefense = 0;
            return;
        }

        Level = Mathf.Max(1, Level);

        RequiredCount = GameBalance.EquipmentEnhanceRequiredDuplicateCount(Level);
        EnhanceGoldCost = GameBalance.EquipmentEnhanceGoldCost(Level);

        CanUpgrade = TotalCount >= RequiredCount;

        int mainEffect = GameBalance.EquipmentMainEffect(ItemGrade, Level);

        string normalizedItemType = NormalizeItemType(ItemType);

        if (normalizedItemType == "ARMOR")
        {
            Category = EquipmentCategory.Armor;
            FinalAttack = 0;
            FinalDefense = mainEffect;
        }
        else
        {
            Category = EquipmentCategory.Weapon;
            FinalAttack = mainEffect;
            FinalDefense = 0;
        }

        Rarity = ConvertGradeToRarity(ItemGrade);
    }

    private static string ConvertCategoryToItemType(EquipmentCategory category)
    {
        return category == EquipmentCategory.Armor ? "ARMOR" : "WEAPON";
    }

    private static EquipmentCategory ConvertItemTypeToCategory(string itemType)
    {
        string normalized = NormalizeItemType(itemType);

        if (normalized == "ARMOR")
            return EquipmentCategory.Armor;

        return EquipmentCategory.Weapon;
    }

    private static string ConvertRarityToGrade(EquipmentRarityGrade rarity)
    {
        switch (rarity)
        {
            case EquipmentRarityGrade.Rare:
                return "RARE";

            case EquipmentRarityGrade.SuperRare:
                return "SUPER_RARE";

            default:
                return "NORMAL";
        }
    }

    private static EquipmentRarityGrade ConvertGradeToRarity(string grade)
    {
        string normalized = NormalizeGrade(grade);

        switch (normalized)
        {
            case "RARE":
                return EquipmentRarityGrade.Rare;

            case "SUPER_RARE":
            case "SUPERRARE":
                return EquipmentRarityGrade.SuperRare;

            default:
                return EquipmentRarityGrade.Normal;
        }
    }

    private static string NormalizeItemType(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return "WEAPON";

        return itemType
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");
    }

    private static string NormalizeGrade(string grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
            return "NORMAL";

        return grade
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");
    }
}

public static class EquipmentInventory
{
    private const string SavePrefix = "EquipmentInventory.Count.";
    private const string LevelSavePrefix = "EquipmentInventory.Level.";
    private const string CategorySavePrefix = "EquipmentInventory.Category.";
    private const string RaritySavePrefix = "EquipmentInventory.Rarity.";
    private const string UserItemIdSavePrefix = "EquipmentInventory.UserItemId.";
    private const string ItemIdSavePrefix = "EquipmentInventory.ItemId.";
    private const string EquippedSavePrefix = "EquipmentInventory.Equipped.";
    private const string IndexSaveKey = "EquipmentInventory.Index";
    private const string ItemKeySavePrefix = "EquipmentInventory.ItemKey.";
    private const string ImageKeySavePrefix = "EquipmentInventory.ImageKey.";
    private const string ItemNameSavePrefix = "EquipmentInventory.ItemName.";
    private const string ItemTypeSavePrefix = "EquipmentInventory.ItemType.";
    private const string ItemGradeSavePrefix = "EquipmentInventory.ItemGrade.";

    private static readonly Dictionary<string, EquipmentInventoryRecord> records =
        new Dictionary<string, EquipmentInventoryRecord>();

    public static void Add(Sprite sprite)
    {
        Add(sprite, GetCategory(sprite), EquipmentRarityGrade.Normal);
    }

    public static void Add(Sprite sprite, EquipmentRarityGrade rarity)
    {
        Add(sprite, GetCategory(sprite), rarity);
    }

    public static void Add(Sprite sprite, EquipmentCategory category, EquipmentRarityGrade rarity)
    {
        if (sprite == null)
            return;

        EquipmentInventoryRecord record = GetRecord(sprite);
        record.SetMetadata(category, rarity);
        record.Add(1);

        SaveRecord(sprite.name, record);
        PlayerPrefs.Save();
    }

    public static void AddRange(IEnumerable<Sprite> sprites)
    {
        bool changed = false;

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
                continue;

            EquipmentInventoryRecord record = GetRecord(sprite);
            record.Add(1);
            SaveRecord(sprite.name, record);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();
    }

    public static EquipmentInventoryRecord GetRecord(Sprite sprite)
    {
        if (sprite == null)
        {
            return new EquipmentInventoryRecord(
                string.Empty,
                0,
                0,
                EquipmentCategory.Weapon,
                EquipmentRarityGrade.Normal
            );
        }

        return GetRecord(sprite.name);
    }

    public static EquipmentInventoryRecord GetRecord(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return new EquipmentInventoryRecord(
                string.Empty,
                0,
                0,
                EquipmentCategory.Weapon,
                EquipmentRarityGrade.Normal
            );
        }

        string key = GetSaveKey(spriteName);

        if (!records.TryGetValue(key, out EquipmentInventoryRecord record))
        {
            int count = PlayerPrefs.GetInt(key, 0);
            int level = PlayerPrefs.GetInt(GetLevelSaveKey(spriteName), count > 0 ? 1 : 0);
            int userItemId = PlayerPrefs.GetInt(GetUserItemIdSaveKey(spriteName), 0);
            int itemId = PlayerPrefs.GetInt(GetItemIdSaveKey(spriteName), 0);
            bool isEquipped = PlayerPrefs.GetInt(GetEquippedSaveKey(spriteName), 0) == 1;

            EquipmentCategory category =
                (EquipmentCategory)PlayerPrefs.GetInt(
                    GetCategorySaveKey(spriteName),
                    (int)GetCategory(spriteName)
                );

            EquipmentRarityGrade rarity =
                (EquipmentRarityGrade)PlayerPrefs.GetInt(
                    GetRaritySaveKey(spriteName),
                    (int)EquipmentRarityGrade.Normal
                );

            record = new EquipmentInventoryRecord(
            spriteName,
            count,
            level,
            category,
            rarity,
            userItemId,
            itemId
        );

        bool hasSavedMetadata =
            PlayerPrefs.HasKey(GetItemKeySaveKey(spriteName)) ||
            PlayerPrefs.HasKey(GetImageKeySaveKey(spriteName)) ||
            PlayerPrefs.HasKey(GetItemNameSaveKey(spriteName)) ||
            PlayerPrefs.HasKey(GetItemTypeSaveKey(spriteName)) ||
            PlayerPrefs.HasKey(GetItemGradeSaveKey(spriteName));

        if (hasSavedMetadata)
        {
            string savedItemKey = PlayerPrefs.GetString(GetItemKeySaveKey(spriteName), spriteName);
            string savedImageKey = PlayerPrefs.GetString(GetImageKeySaveKey(spriteName), savedItemKey);
            string savedItemName = PlayerPrefs.GetString(GetItemNameSaveKey(spriteName), string.Empty);
            string savedItemType = PlayerPrefs.GetString(GetItemTypeSaveKey(spriteName), record.ItemType);
            string savedItemGrade = PlayerPrefs.GetString(GetItemGradeSaveKey(spriteName), record.ItemGrade);

            if (string.IsNullOrEmpty(savedItemName))
                savedItemName = savedItemKey;

            record.SetServerEquipmentData(
                userItemId,
                itemId,
                savedItemKey,
                savedImageKey,
                savedItemName,
                savedItemType,
                savedItemGrade,
                level,
                count,
                isEquipped
            );

            Debug.Log(
                "[EquipmentInventory] 저장된 메타데이터 복원 | " +
                $"spriteName={spriteName}, " +
                $"itemKey={savedItemKey}, " +
                $"imageKey={savedImageKey}, " +
                $"itemName={savedItemName}, " +
                $"itemType={savedItemType}, " +
                $"itemGrade={savedItemGrade}, " +
                $"quantity={count}, " +
                $"level={level}"
            );
        }
        else
        {
            record.SetEquipped(isEquipped);

            Debug.LogWarning(
                "[EquipmentInventory] 저장된 메타데이터 없음. 기본 spriteName 사용 | " +
                $"spriteName={spriteName}, quantity={count}, level={level}"
            );
        }

        records[key] = record;
        }
        
        return record;
    }

    public static void ApplyServerUserItem(
        Sprite sprite,
        int userItemId,
        int itemId,
        int quantity,
        int enhanceLevel
    )
    {
        if (sprite == null)
        {
            Debug.LogWarning("ApplyServerUserItem 실패: sprite가 null입니다.");
            return;
        }

        EquipmentInventoryRecord record = GetRecord(sprite);
        record.SetServerData(userItemId, itemId, quantity, enhanceLevel);

        SaveRecord(sprite.name, record);
        PlayerPrefs.Save();
    }

    public static void ApplyServerUserItem(
        Sprite sprite,
        int userItemId,
        int itemId,
        string itemKey,
        string imageKey,
        string itemName,
        string itemType,
        string itemGrade,
        int quantity,
        int enhanceLevel,
        bool isEquipped
    )
    {
        if (sprite == null)
        {
            Debug.LogWarning("ApplyServerUserItem 실패: sprite가 null입니다.");
            return;
        }

        EquipmentInventoryRecord record = GetRecord(sprite);

        record.SetServerEquipmentData(
            userItemId,
            itemId,
            itemKey,
            imageKey,
            itemName,
            itemType,
            itemGrade,
            enhanceLevel,
            quantity,
            isEquipped
        );

        SaveRecord(sprite.name, record);
        PlayerPrefs.Save();
    }

    public static IEnumerable<EquipmentInventoryRecord> GetOwnedRecords()
    {
        LoadIndexedRecords();

        foreach (EquipmentInventoryRecord record in records.Values)
        {
            if (record != null && record.IsOwned)
                yield return record;
        }
    }

    public static EquipmentStatSummary CalculateEquippedStatSummary()
    {
        LoadIndexedRecords();

        EquipmentStatSummary summary = EquipmentStatSummary.Empty;

        foreach (EquipmentInventoryRecord record in records.Values)
        {
            if (record == null || !record.IsOwned || !record.IsEquipped)
                continue;

            int mainEffect = GameBalance.EquipmentMainEffect(record.ItemGrade, record.Level);
            float subRate = GameBalance.EquipmentSubEffectRate(record.ItemGrade, record.Level);
            float subMultiplier = GameBalance.ConvertSubEffectRateToMultiplier(subRate);

            if (record.Category == EquipmentCategory.Weapon)
            {
                summary.attackMainEffectSum += mainEffect;
                summary.attackSubEffectMultiplier *= subMultiplier;
            }
            else if (record.Category == EquipmentCategory.Armor)
            {
                summary.defenseMainEffectSum += mainEffect;
                summary.defenseSubEffectMultiplier *= subMultiplier;
            }
        }

        return summary;
    }

    public static void EquipOnlyThis(EquipmentInventoryRecord targetRecord)
    {
        if (targetRecord == null)
            return;

        LoadIndexedRecords();

        foreach (EquipmentInventoryRecord record in records.Values)
        {
            if (record == null || !record.IsOwned)
                continue;

            bool sameCategory = record.Category == targetRecord.Category;

            if (sameCategory)
            {
                record.SetEquipped(record.UserItemId == targetRecord.UserItemId);
                SaveRecord(record.SpriteName, record);
            }
        }

        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        LoadIndexedRecords();

        string index = PlayerPrefs.GetString(IndexSaveKey, string.Empty);

        if (!string.IsNullOrEmpty(index))
        {
            string[] spriteNames = index.Split('|');

            foreach (string spriteName in spriteNames)
            {
                if (string.IsNullOrEmpty(spriteName))
                    continue;

                PlayerPrefs.DeleteKey(GetSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetLevelSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetCategorySaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetRaritySaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetUserItemIdSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetItemIdSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetEquippedSaveKey(spriteName));
            }
        }

        PlayerPrefs.DeleteKey(IndexSaveKey);

        records.Clear();
        PlayerPrefs.Save();
    }

    private static void SaveRecord(string spriteName, EquipmentInventoryRecord record)
    {
        if (string.IsNullOrEmpty(spriteName) || record == null)
            return;

        PlayerPrefs.SetInt(GetSaveKey(spriteName), record.TotalCount);
        PlayerPrefs.SetInt(GetLevelSaveKey(spriteName), record.Level);
        PlayerPrefs.SetInt(GetCategorySaveKey(spriteName), (int)record.Category);
        PlayerPrefs.SetInt(GetRaritySaveKey(spriteName), (int)record.Rarity);
        PlayerPrefs.SetInt(GetUserItemIdSaveKey(spriteName), record.UserItemId);
        PlayerPrefs.SetInt(GetItemIdSaveKey(spriteName), record.ItemId);
        PlayerPrefs.SetInt(GetEquippedSaveKey(spriteName), record.IsEquipped ? 1 : 0);


        PlayerPrefs.SetString(GetItemKeySaveKey(spriteName), record.ItemKey);
        PlayerPrefs.SetString(GetImageKeySaveKey(spriteName), record.ImageKey);
        PlayerPrefs.SetString(GetItemNameSaveKey(spriteName), record.ItemName);
        PlayerPrefs.SetString(GetItemTypeSaveKey(spriteName), record.ItemType);
        PlayerPrefs.SetString(GetItemGradeSaveKey(spriteName), record.ItemGrade);

        AddToIndex(spriteName);
    }

    public static void SaveRecordToPrefs(EquipmentInventoryRecord record)
    {
        if (record == null)
            return;

        SaveRecord(record.SpriteName, record);
        PlayerPrefs.Save();
    }

    private static string GetSaveKey(Sprite sprite)
    {
        return GetSaveKey(sprite.name);
    }

    private static string GetSaveKey(string spriteName)
    {
        return SavePrefix + spriteName;
    }

    private static string GetLevelSaveKey(Sprite sprite)
    {
        return GetLevelSaveKey(sprite.name);
    }

    private static string GetLevelSaveKey(string spriteName)
    {
        return LevelSavePrefix + spriteName;
    }

    private static string GetCategorySaveKey(string spriteName)
    {
        return CategorySavePrefix + spriteName;
    }

    private static string GetRaritySaveKey(string spriteName)
    {
        return RaritySavePrefix + spriteName;
    }

    private static string GetUserItemIdSaveKey(string spriteName)
    {
        return UserItemIdSavePrefix + spriteName;
    }

    private static string GetItemIdSaveKey(string spriteName)
    {
        return ItemIdSavePrefix + spriteName;
    }

    private static string GetItemKeySaveKey(string spriteName)
    {
        return ItemKeySavePrefix + spriteName;
    }

    private static string GetImageKeySaveKey(string spriteName)
    {
        return ImageKeySavePrefix + spriteName;
    }

    private static string GetItemNameSaveKey(string spriteName)
    {
        return ItemNameSavePrefix + spriteName;
    }

    private static string GetItemTypeSaveKey(string spriteName)
    {
        return ItemTypeSavePrefix + spriteName;
    }

    private static string GetItemGradeSaveKey(string spriteName)
    {
        return ItemGradeSavePrefix + spriteName;
    }

    private static string GetEquippedSaveKey(string spriteName)
    {
        return EquippedSavePrefix + spriteName;
    }

    private static void AddToIndex(string spriteName)
    {
        string index = PlayerPrefs.GetString(IndexSaveKey, string.Empty);
        string token = "|" + spriteName + "|";

        if (("|" + index + "|").Contains(token))
            return;

        PlayerPrefs.SetString(
            IndexSaveKey,
            string.IsNullOrEmpty(index) ? spriteName : index + "|" + spriteName
        );
    }

    private static void LoadIndexedRecords()
    {
        string index = PlayerPrefs.GetString(IndexSaveKey, string.Empty);

        if (string.IsNullOrEmpty(index))
            return;

        string[] spriteNames = index.Split('|');

        foreach (string spriteName in spriteNames)
        {
            if (!string.IsNullOrEmpty(spriteName))
                GetRecord(spriteName);
        }
    }

    private static EquipmentCategory GetCategory(Sprite sprite)
    {
        return sprite == null ? EquipmentCategory.Weapon : GetCategory(sprite.name);
    }

    private static EquipmentCategory GetCategory(string spriteName)
    {
        string lowerName = spriteName.ToLowerInvariant();

        if (
            lowerName.Contains("wp") ||
            lowerName.Contains("weapon") ||
            lowerName.Contains("sword") ||
            lowerName.Contains("axe") ||
            lowerName.Contains("bow") ||
            lowerName.Contains("spear") ||
            lowerName.Contains("staff") ||
            lowerName.Contains("blunt") ||
            lowerName.Contains("fist") ||
            lowerName.Contains("sickle")
        )
        {
            return EquipmentCategory.Weapon;
        }

        return EquipmentCategory.Armor;
    }
}