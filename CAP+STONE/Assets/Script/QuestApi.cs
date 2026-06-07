using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class QuestApi : MonoBehaviour
{
    public static QuestApi Instance { get; private set; }

    // ================================
    // 기존 오늘 퀘스트 조회 응답
    // ================================
    [Serializable]
    public class TodayQuestListResponse
    {
        public bool success;
        public int user_id;
        public QuestData[] quests;
    }

    // ================================
    // 새 팝업용 퀘스트 응답
    // /users/{user_id}/quests/popup
    // ================================
    [Serializable]
    public class QuestPopupResponse
    {
        public bool success;
        public int user_id;

        public string condition_result;
        public string condition_text;

        public string quest_score;
        public string quest_score_text;

        public QuestData[] quests;
    }

    [Serializable]
    public class QuestData
    {
        public int user_quest_id;
        public int quest_id;

        public string quest_name;
        public string quest_desc;
        public string quest_type;
        public string quest_grade;
        public string quest_event;

        public int current_value;
        public int target_value;

        public int reward_gold;
        public int reward_gem;

        public bool is_completed;
        public bool is_reward_claimed;

        public string assigned_date;
        public string completed_at;
    }

    [Serializable]
    public class ClaimQuestRewardResponse
    {
        public bool success;
        public string message;

        public int user_id;
        public int user_quest_id;

        public int reward_gold;
        public int reward_gem;

        public int total_gold;
        public int total_gem;

        public bool is_reward_claimed;
    }

    [Serializable]
    public class QuestProgressRequest
    {
        public string quest_event;
        public int add_value;
    }

    [Serializable]
    public class QuestProgressResponse
    {
        public bool success;
        public int user_id;
        public string quest_event;
        public int updated_count;
        public QuestData[] quests;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =====================================================
    // 새로 추가: 퀘스트 팝업용 조회
    // AI 결과 없으면 서버에서 condition = 최상, quest_score = 상
    // user_quest 비어 있으면 서버에서 자동 생성
    // =====================================================
    public IEnumerator GetQuestPopup(
        int userId,
        Action<QuestPopupResponse> onSuccess,
        Action<string> onError
    )
    {
        if (userId <= 0)
        {
            onError?.Invoke("userId가 올바르지 않습니다.");
            yield break;
        }

        string url = $"{ApiConfig.BaseUrl}/users/{userId}/quests/popup";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"퀘스트 팝업 조회 실패: HTTP {request.responseCode} / " +
                    $"{request.error} / {request.downloadHandler.text}"
                );
                yield break;
            }

            QuestPopupResponse response = null;

            try
            {
                response = JsonUtility.FromJson<QuestPopupResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                onError?.Invoke(
                    $"퀘스트 팝업 JSON 파싱 실패: {e.Message}\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            if (response == null || response.success == false)
            {
                onError?.Invoke(
                    $"퀘스트 팝업 응답이 올바르지 않습니다.\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    // =====================================================
    // 기존 오늘 퀘스트 조회
    // 필요하면 계속 사용 가능
    // =====================================================
    public IEnumerator GetTodayQuests(
        int userId,
        Action<TodayQuestListResponse> onSuccess,
        Action<string> onError
    )
    {
        if (userId <= 0)
        {
            onError?.Invoke("userId가 올바르지 않습니다.");
            yield break;
        }

        string url = $"{ApiConfig.BaseUrl}/quests/today/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"퀘스트 조회 실패: HTTP {request.responseCode} / " +
                    $"{request.error} / {request.downloadHandler.text}"
                );
                yield break;
            }

            TodayQuestListResponse response = null;

            try
            {
                response = JsonUtility.FromJson<TodayQuestListResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                onError?.Invoke(
                    $"퀘스트 JSON 파싱 실패: {e.Message}\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            if (response == null || response.success == false)
            {
                onError?.Invoke(
                    $"퀘스트 응답이 올바르지 않습니다.\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator ClaimQuestReward(
        int userQuestId,
        Action<ClaimQuestRewardResponse> onSuccess,
        Action<string> onError
    )
    {
        if (userQuestId <= 0)
        {
            onError?.Invoke("userQuestId가 올바르지 않습니다.");
            yield break;
        }

        string url = $"{ApiConfig.BaseUrl}/quests/claim/{userQuestId}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"보상 수령 실패: HTTP {request.responseCode} / " +
                    $"{request.error} / {request.downloadHandler.text}"
                );
                yield break;
            }

            ClaimQuestRewardResponse response = null;

            try
            {
                response = JsonUtility.FromJson<ClaimQuestRewardResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                onError?.Invoke(
                    $"보상 수령 JSON 파싱 실패: {e.Message}\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            if (response == null || response.success == false)
            {
                onError?.Invoke(
                    $"보상 수령 응답이 올바르지 않습니다.\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator ReportProgress(
        int userId,
        string questEvent,
        int addValue,
        Action<QuestProgressResponse> onSuccess = null,
        Action<string> onError = null
    )
    {
        if (userId <= 0)
        {
            onError?.Invoke("userId가 올바르지 않습니다.");
            yield break;
        }

        if (string.IsNullOrEmpty(questEvent))
        {
            onError?.Invoke("questEvent가 비어 있습니다.");
            yield break;
        }

        string url = $"{ApiConfig.BaseUrl}/quests/progress/{userId}";

        QuestProgressRequest body = new QuestProgressRequest
        {
            quest_event = questEvent,
            add_value = Mathf.Max(1, addValue)
        };

        string json = JsonUtility.ToJson(body);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"퀘스트 진행도 갱신 실패: HTTP {request.responseCode} / " +
                    $"{request.error} / {request.downloadHandler.text}"
                );
                yield break;
            }

            QuestProgressResponse response = null;

            try
            {
                response = JsonUtility.FromJson<QuestProgressResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                onError?.Invoke(
                    $"퀘스트 진행도 JSON 파싱 실패: {e.Message}\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            if (response == null || response.success == false)
            {
                onError?.Invoke(
                    $"퀘스트 진행도 응답이 올바르지 않습니다.\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }
}