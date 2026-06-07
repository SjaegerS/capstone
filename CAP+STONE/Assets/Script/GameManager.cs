using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.InputSystem;

// 백엔드에서 받아올 데이터 모델 (FastAPI의 RecentUsageResponse와 완벽 일치)
[System.Serializable]
public class UserUsageSummary
{
    public int[] recent_7days_minutes;
    public int yesterday_minutes;
    public int yesterday_quest_completed;
}

public class GameManager : MonoBehaviour
{
    [Header("매니저 연결")]
    public AIManager aiManager; // 하이어라키의 AIManager 연결

    [Header("백엔드 설정")]
    private string backendUrl = "http://127.0.0.1:8000"; // 로컬 FastAPI 주소
    private int currentUserId; // public을 private으로 변경하고 고정 숫자(= 9) 삭제

    // 중복 호출 방지용 락(Lock) 변수
    private bool isRequesting = false;

    void Start()
    {
        // "CurrentUserId"를 "USER_ID"로 바꿉니다! (대문자, 스펠링 정확히 일치해야 함)
        currentUserId = PlayerPrefs.GetInt("USER_ID", 1);

        Debug.Log($"[GameManager] 준비 완료 (접속 ID: {currentUserId}). 스페이스바(Space)를 누르면 AI 분석이 1회 시작됩니다.");
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !isRequesting)
        {
            StartCoroutine(RoutineDailyAIAnalysis());
        }
    }

    // ==========================================================
    // 0. FastAPI에서 유저의 실제 사용량 데이터를 가져오는 함수
    // ==========================================================
    private IEnumerator FetchUserDataFromBackend(System.Action<UserUsageSummary> onComplete)
    {
        // main.py에 이미 구현된 그 API 주소입니다.
        string endpoint = $"{backendUrl}/usage-logs/recent/{currentUserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                UserUsageSummary summary = JsonConvert.DeserializeObject<UserUsageSummary>(jsonResponse);
                Debug.Log("[GameManager] 1단계 성공: 백엔드에서 실제 데이터를 로드했습니다!");
                onComplete?.Invoke(summary);
            }
            else
            {
                Debug.LogError($"[GameManager] 데이터 로드 실패: {request.error}\n{request.downloadHandler.text}");
                onComplete?.Invoke(null); // 통신 실패 시 null 반환
            }
        }
    }

    private IEnumerator RoutineDailyAIAnalysis()
    {
        isRequesting = true;

        // ==========================================================
        // 1. 데이터 가져오기 (GET)
        // ==========================================================
        Debug.Log("[GameManager] 1단계: 백엔드에서 7일치 유저 데이터를 조회합니다...");

        UserUsageSummary userData = null;
        yield return StartCoroutine(FetchUserDataFromBackend((result) => {
            userData = result;
        }));

        // 통신이 실패했을 경우의 안전장치 (임시 0값)
        if (userData == null)
        {
            Debug.LogWarning("[GameManager] DB 데이터를 가져오지 못해 임시 0값으로 진행합니다.");
            userData = new UserUsageSummary
            {
                recent_7days_minutes = new int[] { 0, 0, 0, 0, 0, 0, 0 },
                yesterday_minutes = 0,
                yesterday_quest_completed = 0
            };
        }

        // ==========================================================
        // 2. AI 매니저 분석 및 검산 (Google Gemini API 호출)
        // ==========================================================
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

        // 더미(가짜) 변수를 지우고, GET으로 받아온 실제 userData를 AI 매니저에 넘겨줍니다.
        StartCoroutine(aiManager.RequestAIFeedback(
            userData.recent_7days_minutes,
            userData.yesterday_minutes,
            userData.yesterday_quest_completed,
            availableQuests,
            (result) => {
                finalResult = result;
                isAiFinished = true;
            }
        ));

        // AI 응답이 올 때까지 대기
        yield return new WaitUntil(() => isAiFinished);
        Debug.Log($"[GameManager] AI 분석 완료! 도출된 등급: {finalResult.Grade}");

        // ==========================================================
        // 3. 분석 결과를 백엔드 DB에 저장 (POST)
        // ==========================================================
        Debug.Log("[GameManager] 3단계: 분석 결과를 FastAPI 백엔드에 저장합니다...");
        yield return StartCoroutine(SaveAIFeedbackToBackend(finalResult));

        isRequesting = false;
        Debug.Log("[GameManager] 통신 사이클 종료. 다시 스페이스바를 눌러 테스트할 수 있습니다.");
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

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[GameManager] 백엔드 저장 성공! DB 테이블이 업데이트 되었습니다.");
            }
            else
            {
                Debug.LogError($"[GameManager] 백엔드 전송 실패: {request.error}\n응답 본문: {request.downloadHandler.text}");
            }
        }
    }
}