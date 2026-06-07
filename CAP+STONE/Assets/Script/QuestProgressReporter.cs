using UnityEngine;

public class QuestProgressReporter : MonoBehaviour
{
    public static QuestProgressReporter Instance { get; private set; }

    [SerializeField] private QuestApi questApi;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (questApi == null)
            questApi = QuestApi.Instance;

        if (questApi == null)
            questApi = FindFirstObjectByType<QuestApi>();
    }

    public void ReportProgress(string questEvent, int addValue = 1)
    {
        int userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogWarning("[QuestProgressReporter] USER_ID가 없습니다.");
            return;
        }

        if (questApi == null)
            questApi = FindFirstObjectByType<QuestApi>();

        if (questApi == null)
        {
            Debug.LogWarning("[QuestProgressReporter] QuestApi가 없습니다.");
            return;
        }

        StartCoroutine(questApi.ReportProgress(
            userId,
            questEvent,
            addValue,
            response =>
            {
                Debug.Log(
                    $"[QuestProgressReporter] 진행도 반영 완료 | " +
                    $"event={questEvent}, updated={response.updated_count}"
                );

                CurrencyUIManager.Instance?.RefreshFromDb();

                QuestPopupController popup =
                    FindFirstObjectByType<QuestPopupController>(FindObjectsInactive.Include);

                if (popup != null)
                    popup.Refresh();
            },
            error =>
            {
                Debug.LogWarning(error);
            }
        ));
    }

    public void RefreshQuestBonus()
    {
        ReportProgress(QuestEvent.Quest, 1);
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
}