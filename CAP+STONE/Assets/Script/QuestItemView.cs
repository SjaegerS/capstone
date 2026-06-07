using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private TextMeshProUGUI gemRewardText;

    [Header("Reward Icons")]
    [SerializeField] private Image goldRewardIcon;
    [SerializeField] private Image gemRewardIcon;

    [Header("Check")]
    [SerializeField] private Image checkImage;

    [Header("State Roots")]
    [SerializeField] private GameObject normalRoot;
    [SerializeField] private GameObject focusRoot;
    [SerializeField] private GameObject disableRoot;

    private QuestApi.QuestData currentData;

    public void Bind(QuestApi.QuestData data, QuestPopupController popup)
    {
        currentData = data;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (currentData == null)
            return;

        int target = Mathf.Max(1, currentData.target_value);
        int current = Mathf.Clamp(currentData.current_value, 0, target);

        bool completed = currentData.is_completed;
        bool claimed = currentData.is_reward_claimed;
        bool inProgress = !completed && !claimed;

        if (questNameText != null)
            questNameText.text = currentData.quest_name;

        if (progressText != null)
            progressText.text = $"({current}/{target})";

        bool hasGold = currentData.reward_gold >= 0;
        bool hasGem = currentData.reward_gem >= 0;

        if (goldRewardIcon != null)
            goldRewardIcon.gameObject.SetActive(hasGold);

        if (goldRewardText != null)
        {
            goldRewardText.gameObject.SetActive(hasGold);
            goldRewardText.text = currentData.reward_gold.ToString("N0");
        }

        if (gemRewardIcon != null)
            gemRewardIcon.gameObject.SetActive(hasGem);

        if (gemRewardText != null)
        {
            gemRewardText.gameObject.SetActive(hasGem);
            gemRewardText.text = currentData.reward_gem.ToString("N0");
        }

        if (checkImage != null)
            checkImage.gameObject.SetActive(claimed);

        ApplyStateVisual(inProgress, completed, claimed);
    }

    private void ApplyStateVisual(bool inProgress, bool completed, bool claimed)
    {
        if (normalRoot != null)
            normalRoot.SetActive(inProgress);

        if (focusRoot != null)
            focusRoot.SetActive(completed && !claimed);

        if (disableRoot != null)
            disableRoot.SetActive(claimed);
    }
}