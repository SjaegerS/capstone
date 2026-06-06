using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.InputSystem;
public class GameManager : MonoBehaviour
{
    [Header("매니저 연결")]
    public AIManager aiManager; // 하이어라키의 AIManager 연결

    [Header("백엔드 설정")]
    private string backendUrl = "http://127.0.0.1:8000"; // 로컬 FastAPI 주소
    private int currentUserId = 1; // 테스트용 유저 ID

    // 중복 호출 방지용 락(Lock) 변수
    private bool isRequesting = false;

    void Start()
    {
        // 429 에러 방지를 위해 자동 실행을 제거하고 수동 트리거 대기 상태로 전환
        Debug.Log("[GameManager] 준비 완료. 스페이스바(Space)를 누르면 AI 분석이 1회 시작됩니다.");
    }

    void Update()
    {
        // New Input System 방식으로 스페이스바 입력 감지 및 중복 실행 차단
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !isRequesting)
        {
            StartCoroutine(RoutineDailyAIAnalysis());
        }
    }

    private IEnumerator RoutineDailyAIAnalysis()
    {
        isRequesting = true; // 통신 시작 시 락(Lock) 잠금

        // ==========================================================
        // 1. 데이터 가져오기 (GET)
        // ==========================================================
        Debug.Log("[GameManager] 1단계: 백엔드에서 7일치 유저 데이터를 조회합니다...");
        yield return new WaitForSeconds(0.5f);

        int[] recent7Days = { 240, 240, 240, 240, 240, 240, 240 };
        int yesterday = 200;
        int completedQuests = 2;

        // 기획서 5차 기준 잠금해제 퀘스트가 제거된 최신 퀘스트 목록 반영
        // DB 테이블(quest)과 완벽하게 일치하는 타입 셋팅 (7번 메타 퀘스트 제외)
        AvailableQuest[] availableQuests = new AvailableQuest[]
        {
            new AvailableQuest { quest_id = 1, quest_type = "하" },
            new AvailableQuest { quest_id = 2, quest_type = "하" },
            new AvailableQuest { quest_id = 3, quest_type = "중" },
            new AvailableQuest { quest_id = 4, quest_type = "중" },
            new AvailableQuest { quest_id = 5, quest_type = "상" },
            new AvailableQuest { quest_id = 6, quest_type = "공통" }
        };

        // ==========================================================
        // 2. AI 매니저 분석 및 검산 (Google Gemini API 호출)
        // ==========================================================
        Debug.Log("[GameManager] 2단계: AI 분석 및 4중 검산 요청 시작...");
        bool isAiFinished = false;
        ValidatedResult finalResult = null;

        StartCoroutine(aiManager.RequestAIFeedback(recent7Days, yesterday, completedQuests, availableQuests, (result) => {
            finalResult = result;
            isAiFinished = true;
        }));

        // AI 응답이 올 때까지 대기
        yield return new WaitUntil(() => isAiFinished);
        Debug.Log($"[GameManager] AI 분석 완료! 도출된 등급: {finalResult.Grade}");

        // ==========================================================
        // 3. 분석 결과를 백엔드 DB에 저장 (POST)
        // ==========================================================
        Debug.Log("[GameManager] 3단계: 분석 결과를 FastAPI 백엔드에 저장합니다...");
        yield return StartCoroutine(SaveAIFeedbackToBackend(finalResult));

        isRequesting = false; // 모든 통신 사이클 완전 종료 시 락(Lock) 해제
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