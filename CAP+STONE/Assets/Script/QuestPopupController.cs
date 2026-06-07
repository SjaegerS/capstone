using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPopupController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private QuestApi questApi;

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("List")]
    [SerializeField] private Transform questContentParent;
    [SerializeField] private QuestItemView questItemTemplate;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Condition / Score Texts")]
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private TextMeshProUGUI questScoreText;

    private bool isLoading;
    private bool isOpen;
    private Coroutine loadCoroutine;

    private QuestApi.QuestPopupResponse cachedResponse;

    private void Awake()
    {
        ResolveQuestApi();

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(TogglePopup);
            openButton.onClick.AddListener(TogglePopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (questItemTemplate != null)
            questItemTemplate.gameObject.SetActive(false);

        isOpen = false;
        isLoading = false;
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(TogglePopup);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void TogglePopup()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (popupRoot == null)
        {
            Debug.LogError("popupRoot가 연결되지 않았습니다.");
            return;
        }

        popupRoot.SetActive(true);
        isOpen = true;

        if (cachedResponse != null)
            RenderPopup(cachedResponse);

        LoadQuestPopup();
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        isOpen = false;
    }

    public void LoadQuestPopup()
    {
        if (isLoading)
            return;

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("QuestPopupController가 붙은 오브젝트가 비활성화되어 있습니다. 이 오브젝트는 항상 활성화해야 합니다.");
            return;
        }

        int userId = GetUserId();

        if (userId <= 0)
        {
            SetMessage("USER_ID가 없습니다.");
            return;
        }

        ResolveQuestApi();

        if (questApi == null)
        {
            SetMessage("QuestApi가 씬에 없습니다.");
            return;
        }

        if (loadCoroutine != null)
            StopCoroutine(loadCoroutine);

        loadCoroutine = StartCoroutine(LoadQuestPopupRoutine(userId));
    }

    private IEnumerator LoadQuestPopupRoutine(int userId)
    {
        isLoading = true;

        if (isOpen)
            SetMessage("퀘스트 불러오는 중...");

        yield return questApi.GetQuestPopup(
            userId,
            response =>
            {
                cachedResponse = response;

                if (!isOpen)
                    return;

                RenderPopup(response);
            },
            error =>
            {
                Debug.LogError(error);

                if (isOpen)
                    SetMessage("퀘스트 조회 실패");
            }
        );

        isLoading = false;
        loadCoroutine = null;
    }

    public void Refresh()
    {
        /*
         * 중요:
         * current_value는 팝업이 닫혀 있어도 갱신되어야 함.
         * 따라서 isOpen == false 라고 return하면 안 됨.
         *
         * 대신 팝업이 닫혀 있으면 서버 데이터만 cachedResponse에 저장하고,
         * UI 렌더링은 Open() 할 때 처리함.
         */
        LoadQuestPopup();
    }

    private void RenderPopup(QuestApi.QuestPopupResponse response)
    {
        ClearGeneratedItems();

        ApplyConditionAndScore(response);

        if (response == null || response.quests == null || response.quests.Length == 0)
        {
            SetMessage("오늘 표시할 퀘스트가 없습니다.");
            return;
        }

        if (questItemTemplate == null)
        {
            SetMessage("QuestItemTemplate이 연결되지 않았습니다.");
            return;
        }

        if (questContentParent == null)
        {
            SetMessage("QuestContentParent가 연결되지 않았습니다.");
            return;
        }

        foreach (QuestApi.QuestData quest in response.quests)
        {
            QuestItemView item = Instantiate(questItemTemplate, questContentParent);
            item.gameObject.SetActive(true);
            item.Bind(quest, this);
        }

        SetMessage("");
    }

    private void ApplyConditionAndScore(QuestApi.QuestPopupResponse response)
    {
        if (conditionText != null)
        {
            string condition = "최상";

            if (response != null && !string.IsNullOrEmpty(response.condition_text))
                condition = response.condition_text;

            conditionText.text = $"컨디션: {condition}";
        }

        if (questScoreText != null)
        {
            string score = "상";

            if (response != null && !string.IsNullOrEmpty(response.quest_score_text))
                score = response.quest_score_text;

            questScoreText.text = $"퀘스트 스코어: {score}";
        }
    }

    private void ClearGeneratedItems()
    {
        if (questContentParent == null)
            return;

        for (int i = questContentParent.childCount - 1; i >= 0; i--)
        {
            Transform child = questContentParent.GetChild(i);

            if (questItemTemplate != null && child == questItemTemplate.transform)
                continue;

            Destroy(child.gameObject);
        }
    }

    private int GetUserId()
    {
        if (CurrentUser.UserId > 0)
            return CurrentUser.UserId;

        int savedUserId = PlayerPrefs.GetInt(BattleRewardApi.USER_ID_KEY, -1);

        if (savedUserId > 0)
        {
            CurrentUser.UserId = savedUserId;
            return savedUserId;
        }

        return -1;
    }

    private void ResolveQuestApi()
    {
        if (questApi != null)
            return;

        questApi = QuestApi.Instance;

        if (questApi == null)
            questApi = FindFirstObjectByType<QuestApi>();
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}