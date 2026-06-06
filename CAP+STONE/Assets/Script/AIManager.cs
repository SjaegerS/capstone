using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

// ============================================================
//  AI 응답/입력 데이터 모델
// ============================================================

// AI가 돌려줄 JSON (검산 전 원본)
[System.Serializable]
public class AIFeedbackResponse
{
    public string pattern_summary;     // "상"/"중"/"하"  (코드가 검산 후 덮어씀)
    public float usage_score;          // 0~100          (코드가 검산 후 덮어씀)
    public string condition_result;    // BEST/GOOD/NORMAL(코드가 확정 후 덮어씀)
    public string feedback_content;    // 멘트            (등급 불일치 시 고정멘트로 교체)
    public int[] assigned_quest_ids;   // 퀘스트 3개      (available 목록 내로 필터)
}

// 퀘스트 정보(입력용)
[System.Serializable]
public class AvailableQuest
{
    public int quest_id;
    public string quest_type;
}

// 검산이 끝난 최종 결과(게임/DB에 넘길 신뢰 가능한 값)
public class ValidatedResult
{
    public string Grade;            // 상/중/하
    public float Score;             // 0~100
    public string Condition;        // BEST/GOOD/NORMAL
    public string Feedback;         // 최종 멘트
    public List<int> QuestIds;      // 검증된 퀘스트 id
    public bool AiHadError;         // AI 원본과 검산값이 달랐는지(로깅/디버그용)
}

public class AIManager : MonoBehaviour
{
    // ── API 설정 ──────────────────────────────────────────────
    // 보안: 키를 코드에 직접 박지 말 것. 별도 파일/환경에서 주입.
    //  - 테스트 단계: 인스펙터에 입력하거나 ApiConfig 같은 별도 클래스에서 로드
    //  - A 방식(직접 연결)이라 클라 노출은 불가피하나, 최소한 소스/깃에는 안 올림
    private string apiKey;

    void Awake()
    {
        // Resources 폴더의 키 파일을 런타임에 로드 (씬·코드에 키 안 박힘)
        TextAsset keyAsset = Resources.Load<TextAsset>("gemini_key");
        if (keyAsset != null)
            apiKey = keyAsset.text.Trim();
        else
            Debug.LogError("[AIManager] gemini_key.txt를 Assets/Resources/에 두세요.");
    }

    // 스크린샷에서 확인된 실제 사용 모델 (AI Studio: gemini-2.5-flash)
    // 단일 응답 엔드포인트(generateContent) 사용. 스트리밍 아님.
    private const string MODEL = "gemini-2.5-flash";
    private string ApiUrl => $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent";

    private static readonly Dictionary<string, string> FALLBACK_MSG = new Dictionary<string, string>
    {
        { "상", "사용시간이 평균에 비해 감소 했습니다. 훌륭합니다. 앞으로도 계속 줄여봅시다." },
        { "중", "평균 사용시간을 유지했습니다. 이는 한발짝만 더 내밀면 성장할 수 있다는 좋은 신호입니다." },
        { "하", "오늘은 핸드폰을 평소보다 많이 켰었습니다. 그만큼 바쁜 생활을 하셨군요. 내일은 잠시 일을 내려놓고 핸드폰을 쉬어주는게 어떨까요?" }
    };

    // ── 고정 폴백 멘트 (기획서 5차 원문) ─────────────────────────
    private readonly string systemInstructionText = @"역할: 당신은 방치형 키우기 게임의 플레이어 컨디션을 관리하는 AI 에이전트입니다.
목표: 플레이어의 7일간 평균과 어제 사용 시간을 분석하여 등급을 판정하고, 고정 피드백 멘트 및 맞춤형 퀘스트 ID 3개를 JSON으로 반환합니다.

[상태 판정 규칙]
1. 평균 산출: 'recent_7days_minutes' 배열의 평균값을 구합니다.
2. 감소율 계산: ((평균 - 어제 사용 시간) / 평균) * 100 (양수면 감소, 음수면 증가)
3. 점수 환산: 50 + (감소율 * 4) (0점 미만은 0, 100점 초과는 100으로 고정)
4. 사용시간 등급(pattern_summary): 70점 이상 ""상"", 30~70점 미만 ""중"", 30점 미만 ""하""
5. 컨디션(condition_result): 'yesterday_quest_completed' 2개 이상 ""BEST"", 1개 ""GOOD"", 0개 ""NORMAL""

[피드백 및 퀘스트 할당 로직]
1. feedback_content: 등급별 문구를 임의 수정 없이 출력
   - 상: ""사용시간이 평균에 비해 감소 했습니다. 훌륭합니다. 앞으로도 계속 줄여봅시다.""
   - 중: ""평균 사용시간을 유지했습니다. 이는 한발짝만 더 내밀면 성장할 수 있다는 좋은 신호입니다.""
   - 하: ""오늘은 핸드폰을 평소보다 많이 켰었습니다. 그만큼 바쁜 생활을 하셨군요. 내일은 잠시 일을 내려놓고 핸드폰을 쉬어주는게 어떨까요?""
2. assigned_quest_ids: 'available_quests' 목록에서 아래 조건에 맞는 퀘스트의 ID를 골라 배열로 출력. (없는 ID 생성 금지)
   - 등급이 '하'인 경우: quest_type이 '하' 또는 '공통'인 퀘스트
   - 등급이 '중'인 경우: quest_type이 '중' 또는 '공통'인 퀘스트
   - 등급이 '상'인 경우: quest_type이 '상', '중', '공통'인 퀘스트

[출력 형식] 부연 설명 없이 순수 JSON 객체만 반환.";

    // ============================================================
    //  검산 함수들 (핵심 안전장치) — AI 출력을 신뢰하지 않고 재계산
    // ============================================================

    /// <summary>점수 검산: 50 + 감소율×4, 0~100 clamp. (확정 k=4)</summary>
    private float CalcScore(int[] recent7Days, int yesterday)
    {
        if (recent7Days == null || recent7Days.Length == 0) return 50f;
        float avg = (float)recent7Days.Average();
        if (avg <= 0f) return 50f;
        float decreaseRate = ((avg - yesterday) / avg) * 100f;
        return Mathf.Clamp(50f + decreaseRate * 4f, 0f, 100f);
    }

    /// <summary>점수 → 등급. 70+ 상, 30~70 중, 30- 하.</summary>
    private string ScoreToGrade(float score)
    {
        if (score >= 70f) return "상";
        if (score >= 30f) return "중";
        return "하";
    }

    /// <summary>퀘스트 완료 개수 → 컨디션 ENUM. (코드가 확정)</summary>
    private string CompletedToCondition(int completed)
    {
        if (completed >= 2) return "BEST";
        if (completed == 1) return "GOOD";
        return "NORMAL";
    }

    /// <summary>
    /// AI 응답을 받아 4중 검산. 점수·등급·컨디션·멘트·퀘스트를 코드가 확정.
    /// </summary>
    private ValidatedResult Validate(AIFeedbackResponse ai, int[] recent7Days,
                                     int yesterday, int completed, AvailableQuest[] available)
    {
        var result = new ValidatedResult();
        bool hadError = false;

        // (1) 점수 — 코드 계산값으로 확정
        result.Score = CalcScore(recent7Days, yesterday);
        if (ai != null && Mathf.Abs(ai.usage_score - result.Score) > 0.5f) hadError = true;

        // (2) 등급 — 검산 점수에서 재도출 (AI의 pattern_summary 무시)
        result.Grade = ScoreToGrade(result.Score);
        if (ai != null && ai.pattern_summary != result.Grade) hadError = true;

        // (3) 컨디션 — 퀘스트 개수로 코드가 확정
        result.Condition = CompletedToCondition(completed);
        if (ai != null && ai.condition_result != result.Condition) hadError = true;

        // (4) 멘트 — AI 멘트가 확정 등급의 고정멘트와 정확히 같을 때만 채택, 아니면 교체
        string expected = FALLBACK_MSG[result.Grade];
        if (ai != null && ai.feedback_content == expected)
            result.Feedback = ai.feedback_content;
        else
        {
            result.Feedback = expected;   // 불일치/빈값/변형 → 고정멘트로 교체
            if (ai == null || ai.feedback_content != expected) hadError = true;
        }

        // (5) 퀘스트 id — available 목록 내 존재하는 것만, 부족하면 목록서 채움
        // (5) 퀘스트 id — available 목록 내 존재하는 것만
        var picked = new List<int>();
        var allowedTypes = new List<string>();

        // 요청하신 등급별 타입 허용 규칙
        if (result.Grade == "하") allowedTypes = new List<string> { "하", "공통" };
        else if (result.Grade == "중") allowedTypes = new List<string> { "중", "공통" };
        else if (result.Grade == "상") allowedTypes = new List<string> { "중", "상", "공통" };

        // 현재 들어온 퀘스트 풀에서 허용된 타입의 ID만 골라냄
        var validQuestIds = (available ?? new AvailableQuest[0])
            .Where(q => allowedTypes.Contains(q.quest_type))
            .Select(q => q.quest_id)
            .ToList();

        // AI가 정상 응답했고, 고른 ID가 허용된 타입 안에 있다면 수용
        if (ai != null && ai.assigned_quest_ids != null)
        {
            foreach (int id in ai.assigned_quest_ids)
                if (validQuestIds.Contains(id) && !picked.Contains(id))
                    picked.Add(id);
        }

        // [동적 강제 배정] AI가 실패했거나 3개를 못 채웠을 경우, 허용된 타입 목록에서 무조건 3개를 긁어옴
        if (picked.Count == 0)
        {
            picked.AddRange(validQuestIds);
            hadError = true;
        }

        result.QuestIds = picked;
        result.AiHadError = hadError;
        return result;
    }


    // ============================================================
    //  메인 API 호출 코루틴
    // ============================================================
    public IEnumerator RequestAIFeedback(int[] recent7Days, int yesterday, int completedQuests,
                                         AvailableQuest[] availableQuests,
                                         System.Action<ValidatedResult> onComplete = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[AIManager] apiKey 미설정. 인스펙터 또는 외부 주입 필요.");
            // 키가 없어도 게임이 멈추지 않게 검산만으로 폴백 결과 생성
            var fb = Validate(null, recent7Days, yesterday, completedQuests, availableQuests);
            onComplete?.Invoke(fb);
            yield break;
        }

        // 1. 입력 JSON 구성
        var userInputData = new
        {
            recent_7days_minutes = recent7Days,
            yesterday_minutes = yesterday,
            yesterday_quest_completed = completedQuests,
            available_quests = availableQuests
        };
        string userInputJson = JsonConvert.SerializeObject(userInputData);

        // 2. 페이로드 (thinkingConfig 제거 — 무료 티어 호환/불필요)
        var payload = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userInputJson } } }
            },
            systemInstruction = new { parts = new[] { new { text = systemInstructionText } } },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        pattern_summary = new { type = "string" },
                        usage_score = new { type = "number" },
                        condition_result = new { type = "string" },
                        feedback_content = new { type = "string" },
                        assigned_quest_ids = new { type = "array", items = new { type = "integer" } }
                    },
                    required = new[] { "pattern_summary", "usage_score", "condition_result", "feedback_content", "assigned_quest_ids" }
                }
            }
        };
        string jsonPayload = JsonConvert.SerializeObject(payload);

        // 3. POST
        using (UnityWebRequest request = new UnityWebRequest($"{ApiUrl}?key={apiKey}", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            AIFeedbackResponse aiData = null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    var gemini = JsonConvert.DeserializeObject<GeminiResponseRoot>(responseText);
                    string actualJson = gemini?.candidates?[0]?.content?.parts?[0]?.text;
                    if (!string.IsNullOrEmpty(actualJson))
                        aiData = JsonConvert.DeserializeObject<AIFeedbackResponse>(actualJson);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AIManager] 파싱 실패, 검산 폴백으로 진행: {e.Message}");
                    aiData = null;
                }
            }
            else
            {
                Debug.LogError($"[AIManager] 통신 실패: {request.error}\n{request.downloadHandler.text}");
                // 통신 실패해도 검산만으로 결과 생성(게임 진행 보장)
            }

            // 4. 4중 검산 — AI 성공/실패 무관하게 항상 코드가 최종값 확정
            ValidatedResult result = Validate(aiData, recent7Days, yesterday, completedQuests, availableQuests);

            if (result.AiHadError)
                Debug.LogWarning($"[AIManager] AI 출력과 검산값 불일치 → 코드값으로 정정함. " +
                                 $"(등급 {result.Grade}/점수 {result.Score}/컨디션 {result.Condition})");

            Debug.Log($"[AIManager] 최종 확정 — 등급:{result.Grade} 점수:{result.Score} " +
                      $"컨디션:{result.Condition} 퀘스트:[{string.Join(",", result.QuestIds)}]");

            // TODO: DB 저장(ai_feedback_log, user_quest, character_condition) + UI 멘트 + 버프 적용
            onComplete?.Invoke(result);
        }
    }
}

// ============================================================
//  Gemini API 응답 래퍼
// ============================================================
[System.Serializable] public class GeminiResponseRoot { public Candidate[] candidates; }
[System.Serializable] public class Candidate { public Content content; }
[System.Serializable] public class Content { public Part[] parts; }
[System.Serializable] public class Part { public string text; }