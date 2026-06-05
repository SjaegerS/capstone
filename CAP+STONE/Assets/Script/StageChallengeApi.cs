using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class StageChallengeApi : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    [Serializable]
    public class StageChallengeResponse
    {
        public bool success;
        public int user_id;
        public int current_stage;
        public int max_cleared_stage;
        public string message;
    }

    public IEnumerator ChallengeStage(
        int userId,
        Action<StageChallengeResponse> onSuccess,
        Action<string> onError
    )
    {
        if (userId <= 0)
        {
            onError?.Invoke("userId가 올바르지 않습니다.");
            yield break;
        }

        string url = $"{baseUrl}/battle/challenge-stage/{userId}";

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, ""))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"HTTP {request.responseCode} / {request.error} / {request.downloadHandler.text}"
                );
                yield break;
            }

            StageChallengeResponse response =
                JsonUtility.FromJson<StageChallengeResponse>(request.downloadHandler.text);

            onSuccess?.Invoke(response);
        }
    }
}