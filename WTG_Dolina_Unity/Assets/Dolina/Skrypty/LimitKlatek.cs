using UnityEngine;

// Ogranicza liczbę klatek do 30 na sekundę we wszystkich scenach — mniejsze obciążenie karty graficznej.
public static class LimitKlatek
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Ustaw()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 200;
    }
}
