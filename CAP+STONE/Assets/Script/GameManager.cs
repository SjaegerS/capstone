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
    public int usage_log_id;

    public string feedback_content;
    public string pattern_summary;
    public int previous_condition_quest_completed;
    public string condition_result;
    public string created_at;
}

public class GameManager : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendUrl = "http://127.0.0.1:8000";

    [Header("Test")]
    [SerializeField] private bool triggerWithSpaceKey = true;

    private int currentUserId;
    private bool isRequesting;

    private void Start()
    {
        currentUserId = ResolveUserId();

        Debug.Log(
            $"[GameManager] 준비 완료 | USER_ID={currentUserId}, " +
            $"Space 테스트={triggerWithSpaceKey}"
        );
    }

    private void Update()
    {
        if (!triggerWithSpaceKey)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            !isRequesting)
        {
            StartCoroutine(RoutineDailyAIAnalysis());
        }
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

        Debug.Log("[GameManager] 2단계: FastAPI AI 피드백 생성 요청");

        AIFeedbackGenerateResponse feedbackResponse = null;

        yield return StartCoroutine(GenerateAIFeedbackOnBackend(
            totalScreenMinutes,
            response =>
            {
                feedbackResponse = response;
            }
        ));

        if (feedbackResponse != null)
        {
            Debug.Log(
                $"[GameManager] AI 피드백 완료 | " +
                $"condition={feedbackResponse.condition_result}, " +
                $"summary={feedbackResponse.pattern_summary}, " +
                $"feedback={feedbackResponse.feedback_content}"
            );
        }

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

    private IEnumerator GenerateAIFeedbackOnBackend(
        int totalScreenMinutes,
        Action<AIFeedbackGenerateResponse> onComplete
    )
    {
        string endpoint = $"{backendUrl}/users/{currentUserId}/ai-feedbacks/generate/";

        AIFeedbackGenerateRequest body = new AIFeedbackGenerateRequest
        {
            total_screen_minutes = Mathf.Max(0, totalScreenMinutes)
        };

        string jsonBody = JsonConvert.SerializeObject(body);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[GameManager] AI 피드백 생성 실패\n" +
                    $"HTTP {request.responseCode}\n" +
                    $"{request.error}\n" +
                    $"{request.downloadHandler.text}"
                );

                onComplete?.Invoke(null);
                yield break;
            }

            try
            {
                AIFeedbackGenerateResponse response =
                    JsonConvert.DeserializeObject<AIFeedbackGenerateResponse>(
                        request.downloadHandler.text
                    );

                onComplete?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GameManager] AI 피드백 JSON 파싱 실패: {e.Message}\n" +
                    request.downloadHandler.text
                );

                onComplete?.Invoke(null);
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