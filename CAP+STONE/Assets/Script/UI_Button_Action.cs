using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Button_Action : MonoBehaviour
{
    [Header("Middle View")]
    [SerializeField] public GameObject CharcterScroll;
    [SerializeField] public GameObject DungeonScroll;
    [SerializeField] public GameObject GachaScroll;
    [SerializeField] public GameObject Inventory;

    [Header("Inventory View")]
    [SerializeField] public GameObject WeaponScroll;
    [SerializeField] public GameObject ArmorScroll;
    [SerializeField] public GameObject CharcterUpgradeScroll;
    [SerializeField] public GameObject WeaponButtonUI;
    [SerializeField] public GameObject SelectButtonUI;
    [SerializeField] public GameObject CharcterSelectButton;

    [Header("Bottom Buttons")]
    [SerializeField] public Button CharcterButton;
    [SerializeField] public Button DungeonButton;
    [SerializeField] public Button ShopButton;
    [SerializeField] public Button ItemButton;

    [Header("Select Buttons")]
    [SerializeField] public Button WeaponUpgradeButton;
    [SerializeField] public Button ArmorUpgradeButton;
    [SerializeField] public Button CharcterUpgradeButton;

    private void Awake()
    {
        RegisterButtonEvents();
    }

    private void Start()
    {
        ShowCharcter();
    }

    public void ShowCharcter()
    {
        SetMiddleView(CharcterScroll);
        SelectBottomButton(CharcterButton);
    }

    public void ShowDungeon()
    {
        SetMiddleView(DungeonScroll);
        SelectBottomButton(DungeonButton);
    }

    public void ShowShop()
    {
        SetMiddleView(GachaScroll);
        SelectBottomButton(ShopButton);
    }

    public void ShowItem()
    {
        SetMiddleView(Inventory);
        ShowWeaponUpgrade();
        SelectBottomButton(ItemButton);
    }

    public void ShowWeaponUpgrade()
    {
        SetInventoryView(WeaponScroll, showWeaponButtonUI: true, showSelectButtonUI: true, showCharcterSelectButton: false);
        SelectUpgradeButton(WeaponUpgradeButton);
    }

    public void ShowArmorUpgrade()
    {
        SetInventoryView(ArmorScroll, showWeaponButtonUI: true, showSelectButtonUI: true, showCharcterSelectButton: false);
        SelectUpgradeButton(ArmorUpgradeButton);
    }

    public void ShowCharcterUpgrade()
    {
        SetInventoryView(CharcterUpgradeScroll, showWeaponButtonUI: false, showSelectButtonUI: true, showCharcterSelectButton: true);
        SelectUpgradeButton(CharcterUpgradeButton);
    }

    private void SetMiddleView(GameObject activeView)
    {
        SetActiveOnly(activeView, CharcterScroll, DungeonScroll, GachaScroll, Inventory);

        if (activeView != Inventory)
        {
            SetActiveSafe(WeaponScroll, false);
            SetActiveSafe(ArmorScroll, false);
            SetActiveSafe(CharcterUpgradeScroll, false);
            SetActiveSafe(WeaponButtonUI, false);
            SetActiveSafe(SelectButtonUI, false);
            SetActiveSafe(CharcterSelectButton, false);
        }
    }

    private void SetInventoryView(GameObject activeScroll, bool showWeaponButtonUI, bool showSelectButtonUI, bool showCharcterSelectButton)
    {
        SetActiveSafe(Inventory, true);
        SetActiveOnly(activeScroll, WeaponScroll, ArmorScroll, CharcterUpgradeScroll);
        SetActiveSafe(WeaponButtonUI, showWeaponButtonUI);
        SetActiveSafe(SelectButtonUI, showSelectButtonUI);
        SetActiveSafe(CharcterSelectButton, showCharcterSelectButton);
    }

    private void RegisterButtonEvents()
    {
        Register(CharcterButton, ShowCharcter);
        Register(DungeonButton, ShowDungeon);
        Register(ShopButton, ShowShop);
        Register(ItemButton, ShowItem);
        Register(WeaponUpgradeButton, ShowWeaponUpgrade);
        Register(ArmorUpgradeButton, ShowArmorUpgrade);
        Register(CharcterUpgradeButton, ShowCharcterUpgrade);
    }

    private static void Register(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void SelectBottomButton(Button SelectedButton)
    {
        SelectButton(SelectedButton);
        ApplyButtonGroupVisual(SelectedButton, CharcterButton, DungeonButton, ShopButton, ItemButton);
    }

    private void SelectUpgradeButton(Button SelectedButton)
    {
        ApplyButtonGroupVisual(SelectedButton, WeaponUpgradeButton, ArmorUpgradeButton, CharcterUpgradeButton);
    }

    private static void ApplyButtonGroupVisual(Button SelectedButton, params Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            if (button == null || button.targetGraphic == null)
            {
                continue;
            }

            ColorBlock colors = button.colors;
            button.targetGraphic.color = button == SelectedButton ? colors.selectedColor : colors.normalColor;
        }
    }

    private static void SetActiveOnly(GameObject activeObject, params GameObject[] objects)
    {
        foreach (GameObject target in objects)
        {
            SetActiveSafe(target, target == activeObject);
        }
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
    private static Button FindSceneButton(params string[] names)
    {
        GameObject target = FindSceneObject(names);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static GameObject FindSceneObject(params string[] names)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (string name in names)
        {
            foreach (Transform target in transforms)
            {
                if (target == null || !target.gameObject.scene.IsValid() || !target.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (target.name.Trim() == name)
                {
                    return target.gameObject;
                }
            }
        }

        return null;
    }
}
