using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EquipmentApi : MonoBehaviour
{
    public static EquipmentApi Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string baseUrl = "https://perennial-steadier-budding.ngrok-free.dev";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void EnhanceItem(int userItemId, Action<bool> onComplete)
    {
        StartCoroutine(PatchEnhanceItem(userItemId, onComplete));
    }

    public void EquipItem(int userItemId, Action<bool> onComplete)
    {
        StartCoroutine(PatchEquipItem(userItemId, onComplete));
    }

    private IEnumerator PatchEnhanceItem(int userItemId, Action<bool> onComplete)
    {
        string url = $"{baseUrl.Trim()}/user-items/{userItemId}/enhance/";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool success =
                request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 &&
                request.responseCode < 300;

            if (!success)
            {
                Debug.LogError(
                    "장비 강화 API 실패\n" +
                    "URL: " + url + "\n" +
                    "Code: " + request.responseCode + "\n" +
                    "Error: " + request.error + "\n" +
                    "Body: " + request.downloadHandler.text
                );
            }
            else
            {
                Debug.Log("장비 강화 API 성공: " + request.downloadHandler.text);
            }

            onComplete?.Invoke(success);
        }
    }

    private IEnumerator PatchEquipItem(int userItemId, Action<bool> onComplete)
    {
        string url = $"{baseUrl.Trim()}/user-items/{userItemId}/equip/";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool success =
                request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 &&
                request.responseCode < 300;

            if (!success)
            {
                Debug.LogError(
                    "장비 장착 API 실패\n" +
                    "URL: " + url + "\n" +
                    "Code: " + request.responseCode + "\n" +
                    "Error: " + request.error + "\n" +
                    "Body: " + request.downloadHandler.text
                );
            }
            else
            {
                Debug.Log("장비 장착 API 성공: " + request.downloadHandler.text);
            }

            onComplete?.Invoke(success);
        }
    }
}