using UnityEngine;

public class PlayTimeQuestTracker : MonoBehaviour
{
    [SerializeField] private float limitSeconds = 3600f;

    private float startTime;
    private bool reported;

    private void Start()
    {
        startTime = Time.realtimeSinceStartup;
        reported = false;
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            TryReportPlayTimeQuest();
    }

    private void OnApplicationQuit()
    {
        TryReportPlayTimeQuest();
    }

    private void TryReportPlayTimeQuest()
    {
        if (reported)
            return;

        float playSeconds = Time.realtimeSinceStartup - startTime;

        if (playSeconds <= limitSeconds)
        {
            QuestProgressReporter reporter =
                FindFirstObjectByType<QuestProgressReporter>();

            if (reporter != null)
            {
                reporter.ReportProgress(QuestEvent.PlayTime, 1);
                reported = true;
            }
        }
    }
}