using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "MainScene";

    [Header("Demo User")]
    [SerializeField] private int defaultCharacterId = 1;

    private const string USER_ID_KEY = "USER_ID";

    public void OnClickGameStart()
    {
        Debug.Log("[TitleManager] 게임 시작 버튼 클릭됨");

        int savedUserId = PlayerPrefs.GetInt(USER_ID_KEY, -1);

        Debug.Log($"[TitleManager] 저장된 USER_ID = {savedUserId}");

        if (savedUserId > 0)
        {
            Debug.Log($"[TitleManager] 기존 유저로 시작: user_id = {savedUserId}");
            StartGame(savedUserId);
        }
        else
        {
            Debug.Log("[TitleManager] 저장된 user_id 없음. 새 유저 생성 요청.");
            StartCoroutine(CreateUserAndStart());
        }
    }

    private IEnumerator CreateUserAndStart()
    {
        string url = $"{baseUrl}/users";

        Debug.Log($"[TitleManager] 유저 생성 API 호출: {url}");

        string jsonBody = JsonUtility.ToJson(new CreateUserRequest
        {
            email = $"demo_{System.DateTime.Now.Ticks}@test.com",
            password_hash = "1234",
            nickname = "DemoUser",
            default_character_id = defaultCharacterId
        });

        Debug.Log($"[TitleManager] 요청 JSON: {jsonBody}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 5;

        yield return request.SendWebRequest();

        Debug.Log($"[TitleManager] API 응답 코드: {request.responseCode}");
        Debug.Log($"[TitleManager] API 응답 내용: {request.downloadHandler.text}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[TitleManager] 유저 생성 실패: {request.responseCode} / {request.error}");
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        string responseText = request.downloadHandler.text;

        CreateUserResponse response = JsonUtility.FromJson<CreateUserResponse>(responseText);

        if (response == null || response.user_id <= 0)
        {
            Debug.LogError("[TitleManager] 유저 생성 응답에서 user_id를 읽지 못했습니다.");
            yield break;
        }

        PlayerPrefs.SetInt(USER_ID_KEY, response.user_id);
        PlayerPrefs.Save();

        Debug.Log($"[TitleManager] 새 유저 저장 완료: user_id = {response.user_id}");

        StartGame(response.user_id);
    }

    private void StartGame(int userId)
    {
        Debug.Log($"[TitleManager] StartGame 실행. user_id = {userId}, scene = {gameSceneName}");

        CurrentUser.UserId = userId;

        SceneManager.LoadScene(gameSceneName);
    }

    public void ResetDemoUser()
    {
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.Save();

        CurrentUser.UserId = -1;

        Debug.Log("[TitleManager] USER_ID 초기화 완료");
    }
}

[System.Serializable]
public class CreateUserRequest
{
    public string email;
    public string password_hash;
    public string nickname;
    public int default_character_id;
}

[System.Serializable]
public class CreateUserResponse
{
    public int user_id;
    public string email;
    public string nickname;
}