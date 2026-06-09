using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LevelPanelApi : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl = "https://perennial-steadier-budding.ngrok-free.dev";

    [Serializable]
    public class UserLevelPanelResponse
    {
        public bool success;
        public int user_id;
        public string user_name;

        public int base_attack;
        public int base_defense;

        public int weapon_base_attack;
        public int armor_base_defense;

        public int weapon_enhance_level;
        public int armor_enhance_level;

        public int condition_score;
        public string condition_grade;

        public string buff_type;
        public float current_effect_value;

        public string latest_feedback_content;
    }

    public IEnumerator GetUserLevelPanel(
        int userId,
        Action<UserLevelPanelResponse> onSuccess,
        Action<string> onError
    )
    {
        if (userId <= 0)
        {
            onError?.Invoke("userId가 올바르지 않습니다.");
            yield break;
        }

        string url = $"{baseUrl.Trim()}/users/{userId}/level-panel";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    $"HTTP {request.responseCode} / {request.error} / {request.downloadHandler.text}"
                );
                yield break;
            }

            string json = request.downloadHandler.text;

            UserLevelPanelResponse response;

            try
            {
                response = JsonUtility.FromJson<UserLevelPanelResponse>(json);
            }
            catch (Exception e)
            {
                onError?.Invoke($"JSON 파싱 실패: {e.Message}\n{json}");
                yield break;
            }

            if (response == null)
            {
                onError?.Invoke($"레벨 패널 응답이 비어 있습니다.\n{json}");
                yield break;
            }

            if (response.success == false)
            {
                onError?.Invoke($"레벨 패널 조회 실패\n{json}");
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }
}