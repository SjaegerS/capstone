using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkTest : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return TestUrl("https://www.google.com");
        yield return TestUrl("https://perennial-steadier-budding.ngrok-free.dev/docs");
        yield return TestUrl("https://perennial-steadier-budding.ngrok-free.dev/battle/status/1");
    }

    private IEnumerator TestUrl(string rawUrl)
    {
        string url = rawUrl.Trim();

        Debug.Log($"[NetworkTest] URL=[{url}]");

        try
        {
            Uri uri = new Uri(url);
            Debug.Log($"[NetworkTest] Scheme={uri.Scheme}, Host={uri.Host}, Path={uri.AbsolutePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkTest] URL 형식 오류: {e.Message}");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");

            yield return request.SendWebRequest();

            Debug.Log($"[NetworkTest] Code={request.responseCode}");
            Debug.Log($"[NetworkTest] Result={request.result}");
            Debug.Log($"[NetworkTest] Error={request.error}");
            Debug.Log($"[NetworkTest] Response={request.downloadHandler.text}");
        }
    }
}