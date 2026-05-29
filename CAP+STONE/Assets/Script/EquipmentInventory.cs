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

public sealed class EquipmentInventoryRecord
{
    public string SpriteName { get; private set; }

    public int UserItemId { get; private set; }
    public int ItemId { get; private set; }

    public int TotalCount { get; private set; }
    public int Level { get; private set; }
    public int RequiredCount { get; private set; }
    public EquipmentCategory Category { get; private set; }
    public EquipmentRarityGrade Rarity { get; private set; }

    public bool IsOwned => Level > 0 || TotalCount > 0;
    public bool CanUpgrade => IsOwned && TotalCount >= RequiredCount && UserItemId > 0;

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
        SpriteName = spriteName;
        UserItemId = Mathf.Max(0, userItemId);
        ItemId = Mathf.Max(0, itemId);
        TotalCount = Mathf.Max(0, totalCount);
        Level = Mathf.Max(0, level);
        Category = category;
        Rarity = rarity;
        Recalculate();
    }

    public void SetMetadata(EquipmentCategory category, EquipmentRarityGrade rarity)
    {
        Category = category;
        Rarity = rarity;
    }

    public void SetServerData(int userItemId, int itemId, int quantity, int enhanceLevel)
    {
        UserItemId = Mathf.Max(0, userItemId);
        ItemId = Mathf.Max(0, itemId);
        TotalCount = Mathf.Max(0, quantity);
        Level = Mathf.Max(0, enhanceLevel);
        Recalculate();
    }

    public void Add(int amount)
    {
        TotalCount = Mathf.Max(0, TotalCount + amount);

        if (TotalCount > 0 && Level <= 0)
        {
            Level = 1;
        }

        Recalculate();
    }

    public bool TryUpgradeLocalOnly()
    {
        if (!CanUpgrade)
        {
            return false;
        }

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
            RequiredCount = 2;
            return;
        }

        Level = Mathf.Max(1, Level);
        RequiredCount = Level + 1;
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
    private const string IndexSaveKey = "EquipmentInventory.Index";

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
        {
            return;
        }

        EquipmentInventoryRecord record = GetRecord(sprite);
        record.SetMetadata(category, rarity);
        record.Add(1);

        SaveRecord(sprite, record);
        PlayerPrefs.Save();
    }

    public static void AddRange(IEnumerable<Sprite> sprites)
    {
        bool changed = false;

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                continue;
            }

            EquipmentInventoryRecord record = GetRecord(sprite);
            record.Add(1);
            SaveRecord(sprite, record);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }
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

        string key = GetSaveKey(sprite);

        if (!records.TryGetValue(key, out EquipmentInventoryRecord record))
        {
            int count = PlayerPrefs.GetInt(key, 0);
            int level = PlayerPrefs.GetInt(GetLevelSaveKey(sprite), count > 0 ? 1 : 0);
            int userItemId = PlayerPrefs.GetInt(GetUserItemIdSaveKey(sprite.name), 0);
            int itemId = PlayerPrefs.GetInt(GetItemIdSaveKey(sprite.name), 0);

            EquipmentCategory category =
                (EquipmentCategory)PlayerPrefs.GetInt(
                    GetCategorySaveKey(sprite.name),
                    (int)GetCategory(sprite)
                );

            EquipmentRarityGrade rarity =
                (EquipmentRarityGrade)PlayerPrefs.GetInt(
                    GetRaritySaveKey(sprite.name),
                    (int)EquipmentRarityGrade.Normal
                );

            record = new EquipmentInventoryRecord(
                sprite.name,
                count,
                level,
                category,
                rarity,
                userItemId,
                itemId
            );

            records[key] = record;
        }

        return record;
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

        SaveRecord(sprite, record);
        PlayerPrefs.Save();
    }

    public static IEnumerable<EquipmentInventoryRecord> GetOwnedRecords()
    {
        LoadIndexedRecords();

        foreach (EquipmentInventoryRecord record in records.Values)
        {
            if (record != null && record.IsOwned)
            {
                yield return record;
            }
        }
    }

    public static bool TryUpgrade(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        EquipmentInventoryRecord record = GetRecord(sprite);

        if (!record.TryUpgradeLocalOnly())
        {
            return false;
        }

        SaveRecord(sprite, record);
        PlayerPrefs.Save();
        return true;
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
                {
                    continue;
                }

                PlayerPrefs.DeleteKey(GetSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetLevelSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetCategorySaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetRaritySaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetUserItemIdSaveKey(spriteName));
                PlayerPrefs.DeleteKey(GetItemIdSaveKey(spriteName));
            }
        }

        PlayerPrefs.DeleteKey(IndexSaveKey);

        records.Clear();
        PlayerPrefs.Save();
    }

    private static void SaveRecord(Sprite sprite, EquipmentInventoryRecord record)
    {
        if (sprite == null || record == null)
        {
            return;
        }

        PlayerPrefs.SetInt(GetSaveKey(sprite), record.TotalCount);
        PlayerPrefs.SetInt(GetLevelSaveKey(sprite), record.Level);
        PlayerPrefs.SetInt(GetCategorySaveKey(sprite.name), (int)record.Category);
        PlayerPrefs.SetInt(GetRaritySaveKey(sprite.name), (int)record.Rarity);
        PlayerPrefs.SetInt(GetUserItemIdSaveKey(sprite.name), record.UserItemId);
        PlayerPrefs.SetInt(GetItemIdSaveKey(sprite.name), record.ItemId);

        AddToIndex(sprite.name);
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

    private static void AddToIndex(string spriteName)
    {
        string index = PlayerPrefs.GetString(IndexSaveKey, string.Empty);
        string token = "|" + spriteName + "|";

        if (("|" + index + "|").Contains(token))
        {
            return;
        }

        PlayerPrefs.SetString(
            IndexSaveKey,
            string.IsNullOrEmpty(index) ? spriteName : index + "|" + spriteName
        );
    }

    private static void LoadIndexedRecords()
    {
        string index = PlayerPrefs.GetString(IndexSaveKey, string.Empty);

        if (string.IsNullOrEmpty(index))
        {
            return;
        }

        string[] spriteNames = index.Split('|');

        foreach (string spriteName in spriteNames)
        {
            if (!string.IsNullOrEmpty(spriteName))
            {
                GetRecord(spriteName);
            }
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