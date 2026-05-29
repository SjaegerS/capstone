using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterStatusApi : MonoBehaviour
{
    [SerializeField] private UserCreateApi userCreateApi;

    public void OnClickGetCharacterStatuses()
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

        StartCoroutine(GetCharacterStatuses(userCreateApi.CurrentUserId));
    }

    public IEnumerator GetCharacterStatuses(long userId)
    {
        string url = $"{ApiConfig.BaseUrl}/users/{userId}/character-statuses/";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("캐릭터 상태 조회 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        string wrappedJson = "{\"characters\":" + request.downloadHandler.text + "}";
        CharacterStatusListResponse response =
            JsonUtility.FromJson<CharacterStatusListResponse>(wrappedJson);

        Debug.Log($"캐릭터 상태 조회 성공. 개수={response.characters.Length}");

        foreach (CharacterStatusDto character in response.characters)
        {
            Debug.Log(
                $"character_id={character.character_id}, " +
                $"level={character.character_level}, " +
                $"hp={character.current_hp}/{character.max_hp}, " +
                $"atk={character.attack_power}, def={character.defense_power}"
            );
        }
    }

    [Serializable]
    public class CharacterStatusListResponse
    {
        public CharacterStatusDto[] characters;
    }

    [Serializable]
    public class CharacterStatusDto
    {
        public long user_id;
        public long character_id;
        public int character_level;
        public int max_hp;
        public int current_hp;
        public int attack_power;
        public int defense_power;
    }
}