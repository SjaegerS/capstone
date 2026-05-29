using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class UserCurrencyApi : MonoBehaviour
{
    [SerializeField] private UserCreateApi userCreateApi;

    public void OnClickGetCurrency()
    {
        if (userCreateApi == null)
        {
            Debug.LogError("UserCreateApi가 연결되지 않았습니다.");
            return;
        }

        if (userCreateApi.CurrentUserId <= 0)
        {
            Debug.LogError("생성된 유저 ID가 없습니다.");
            return;
        }

        StartCoroutine(GetCurrency(userCreateApi.CurrentUserId));
    }

    public IEnumerator GetCurrency(long userId)
    {
        string url = $"{ApiConfig.BaseUrl}/users/{userId}/currency/";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("재화 조회 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        UserCurrencyResponse response = JsonUtility.FromJson<UserCurrencyResponse>(request.downloadHandler.text);

        Debug.Log("재화 조회 성공");
        Debug.Log($"user_id={response.user_id}");
        Debug.Log($"gold={response.gold}");
        Debug.Log($"gem={response.gem}");
    }

    [Serializable]
    public class UserCurrencyResponse
    {
        public long user_id;
        public long gold;
        public long gem;
        public string updated_at;
    }
}
