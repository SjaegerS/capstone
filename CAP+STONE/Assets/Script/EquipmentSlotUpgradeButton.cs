using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUpgradeButton : MonoBehaviour
{
    private Button button;
    private Sprite equipmentSprite;

    public void Configure(Sprite sprite)
    {
        equipmentSprite = sprite;
        EnsureButton();
        RefreshInteractable();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Upgrade);
        }
    }

    private void EnsureButton()
    {
        if (button != null)
        {
            return;
        }

        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        button.onClick.RemoveListener(Upgrade);
        button.onClick.AddListener(Upgrade);
    }

    private void Upgrade()
    {
        if (EquipmentInventory.TryUpgrade(equipmentSprite))
        {
            EquipmentInventoryView.RefreshAll();
            BattleManager battleManager = FindFirstObjectByType<BattleManager>();
            if (battleManager != null)
            {
                battleManager.RefreshPlayerStats();
            }
        }
    }

    private void RefreshInteractable()
    {
        if (button == null)
        {
            return;
        }

        EquipmentInventoryRecord record = EquipmentInventory.GetRecord(equipmentSprite);
        button.interactable = record.CanUpgrade;
    }
}
