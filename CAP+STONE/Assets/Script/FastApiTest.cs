using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FastApiTest : MonoBehaviour
{
    [Header("FastAPI Base URL")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    private void Start()
    {
        StartCoroutine(TestHealth());
        StartCoroutine(GetCharacters());
    }

    private IEnumerator TestHealth()
    {
        string url = $"{baseUrl}/health";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Health 성공]");
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("[Health 실패]");
                Debug.LogError(request.error);
                Debug.LogError(request.downloadHandler.text);
            }
        }
    }

    private IEnumerator GetCharacters()
    {
        string url = $"{baseUrl}/characters/";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Characters 조회 성공]");
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("[Characters 조회 실패]");
                Debug.LogError(request.error);
                Debug.LogError(request.downloadHandler.text);
            }
        }
    }
}