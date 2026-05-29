using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class UserCreateApi : MonoBehaviour
{
    private const string UserIdKey = "TEST_USER_ID";
    private const string UserEmailKey = "TEST_USER_EMAIL";
    private const string UserNicknameKey = "TEST_USER_NICKNAME";

    [Header("Default Character")]
    [SerializeField] private long defaultCharacterId = 1;

    public long CurrentUserId { get; private set; }
    public string CurrentEmail { get; private set; }
    public string CurrentNickname { get; private set; }

    private void Start()
    {
        StartCoroutine(CreateUserIfNeeded());
    }

    public IEnumerator CreateUserIfNeeded()
    {
        if (PlayerPrefs.HasKey(UserIdKey))
        {
            CurrentUserId = long.Parse(PlayerPrefs.GetString(UserIdKey));
            CurrentEmail = PlayerPrefs.GetString(UserEmailKey);
            CurrentNickname = PlayerPrefs.GetString(UserNicknameKey);

            Debug.Log($"기존 테스트 유저 사용: user_id={CurrentUserId}, email={CurrentEmail}, nickname={CurrentNickname}");
            yield break;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        UserCreateRequest requestBody = new UserCreateRequest
        {
            email = $"test_{timestamp}@test.com",
            password_hash = "test_password_hash",
            nickname = $"TestUser_{timestamp}",
            default_character_id = defaultCharacterId
        };

        string json = JsonUtility.ToJson(requestBody);
        string url = $"{ApiConfig.BaseUrl}/users/";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("유저 생성 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        UserCreateResponse response = JsonUtility.FromJson<UserCreateResponse>(request.downloadHandler.text);

        CurrentUserId = response.user_id;
        CurrentEmail = response.email;
        CurrentNickname = response.nickname;

        PlayerPrefs.SetString(UserIdKey, CurrentUserId.ToString());
        PlayerPrefs.SetString(UserEmailKey, CurrentEmail);
        PlayerPrefs.SetString(UserNicknameKey, CurrentNickname);
        PlayerPrefs.Save();

        Debug.Log("유저 생성 완료");
        Debug.Log($"user_id={CurrentUserId}");
        Debug.Log($"email={CurrentEmail}");
        Debug.Log($"nickname={CurrentNickname}");
    }

    public void ResetLocalUser()
    {
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(UserEmailKey);
        PlayerPrefs.DeleteKey(UserNicknameKey);
        PlayerPrefs.Save();

        CurrentUserId = 0;
        CurrentEmail = "";
        CurrentNickname = "";

        Debug.Log("로컬 테스트 유저 정보 초기화 완료");
    }

    [Serializable]
    public class UserCreateRequest
    {
        public string email;
        public string password_hash;
        public string nickname;
        public long default_character_id;
    }

    [Serializable]
    public class UserCreateResponse
    {
        public long user_id;
        public string email;
        public string nickname;
        public string created_at;
        public string last_login_at;
    }
}