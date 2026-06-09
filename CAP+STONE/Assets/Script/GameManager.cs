using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.InputSystem;

[Serializable]
public class UserUsageSummary
{
    public int[] recent_7days_minutes;
    public int yesterday_minutes;
    public int yesterday_quest_completed;
}

[Serializable]
public class AIFeedbackGenerateRequest
{
    public int total_screen_minutes;
}

[Serializable]
public class AIFeedbackGenerateResponse
{
    public int feedback_id;
    public int user_id;
    public int? usage_log_id;

    public string feedback_content;
    public string pattern_summary;
    public int previous_condition_quest_completed;
    public string condition_result;
    public string created_at;
}

public class GameManager : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendUrl = "https://perennial-steadier-budding.ngrok-free.dev";

    [Header("매니저 연결")]
    public AIManager aiManager;
    private int currentUserId;
    private bool isRequesting;

    private void Start()
    {
        currentUserId = ResolveUserId();

        Debug.Log(
            $"[GameManager] 준비 완료 | USER_ID={currentUserId}, " 
        );

        // 스페이스바 입력 대기 없이 게임 시작과 동시에 코루틴 실행
        StartCoroutine(RoutineDailyAIAnalysis());
    }


    public void RunDailyAIAnalysis()
    {
        if (isRequesting)
            return;

        StartCoroutine(RoutineDailyAIAnalysis());
    }

    private IEnumerator RoutineDailyAIAnalysis()
    {
        isRequesting = true;

        currentUserId = ResolveUserId();

        if (currentUserId <= 0)
        {
            Debug.LogError("[GameManager] USER_ID가 없습니다.");
            isRequesting = false;
            yield break;
        }

        Debug.Log("[GameManager] 1단계: 사용시간 요약 조회");

        UserUsageSummary usageSummary = null;

        yield return StartCoroutine(FetchUserDataFromBackend(result =>
        {
            usageSummary = result;
        }));

        int totalScreenMinutes = 0;

        if (usageSummary != null)
        {
            totalScreenMinutes = Mathf.Max(0, usageSummary.yesterday_minutes);
        }
        else
        {
            Debug.LogWarning("[GameManager] 사용시간 요약 조회 실패. total_screen_minutes=0으로 진행");
        }

        Debug.Log("[GameManager] 2단계: AI 분석 및 4중 검산 요청 시작...");

        AvailableQuest[] availableQuests = new AvailableQuest[]
        {
            new AvailableQuest { quest_id = 1, quest_type = "하" },
            new AvailableQuest { quest_id = 2, quest_type = "하" },
            new AvailableQuest { quest_id = 3, quest_type = "중" },
            new AvailableQuest { quest_id = 4, quest_type = "중" },
            new AvailableQuest { quest_id = 5, quest_type = "상" },
            new AvailableQuest { quest_id = 6, quest_type = "공통" }
        };

        bool isAiFinished = false;
        ValidatedResult finalResult = null;

        yield return StartCoroutine(aiManager.RequestAIFeedback(
            usageSummary != null ? usageSummary.recent_7days_minutes : new int[7],
            totalScreenMinutes,
            usageSummary != null ? usageSummary.yesterday_quest_completed : 0,
            availableQuests,
            (result) => {
                finalResult = result;
                isAiFinished = true;
            }
        ));

        yield return new WaitUntil(() => isAiFinished);
        Debug.Log($"[GameManager] AI 분석 완료! 도출된 등급: {finalResult.Grade}");

        // 3단계: 분석 결과를 백엔드 DB에 저장 (POST)
        yield return StartCoroutine(SaveAIFeedbackToBackend(finalResult));

        QuestProgressReporter questReporter =
            FindFirstObjectByType<QuestProgressReporter>();

        if (questReporter != null)
        {
            questReporter.RefreshQuestBonus();
        }

        isRequesting = false;
    }

    private IEnumerator FetchUserDataFromBackend(Action<UserUsageSummary> onComplete)
    {
        string endpoint = $"{backendUrl}/usage-logs/recent/{currentUserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[GameManager] 사용시간 요약 조회 실패\n" +
                    $"HTTP {request.responseCode}\n" +
                    $"{request.error}\n" +
                    $"{request.downloadHandler.text}"
                );

                onComplete?.Invoke(null);
                yield break;
            }

            try
            {
                UserUsageSummary summary =
                    JsonConvert.DeserializeObject<UserUsageSummary>(
                        request.downloadHandler.text
                    );

                onComplete?.Invoke(summary);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GameManager] 사용시간 요약 JSON 파싱 실패: {e.Message}\n" +
                    request.downloadHandler.text
                );

                onComplete?.Invoke(null);
            }
        }
    }

    private IEnumerator SaveAIFeedbackToBackend(ValidatedResult result)
    {
        string endpoint = $"{backendUrl}/ai-feedbacks/";

        var payload = new
        {
            user_id = currentUserId,
            pattern_summary = result.Grade,
            usage_score = result.Score,
            condition_result = result.Condition,
            feedback_content = result.Feedback,
            assigned_quest_ids = result.QuestIds
        };

        string jsonBody = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GameManager] 백엔드 전송 실패: {request.error}");
            }
            else
            {
                Debug.Log("[GameManager] 백엔드 저장 성공! DB 테이블이 업데이트 되었습니다.");
            }
        }
    }

    private int ResolveUserId()
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