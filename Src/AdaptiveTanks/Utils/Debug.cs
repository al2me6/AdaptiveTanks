namespace AdaptiveTanks;

public static class Debug
{
    public static void Log(string str, bool always = false)
    {
#if DEBUG
        var isBetaVersion = true;
#else
        bool isBetaVersion = always;
#endif
        if (isBetaVersion) UnityEngine.Debug.Log($"[AdaptiveTanks] {str}");
    }

    public static void LogWarning(string str)
    {
        UnityEngine.Debug.LogWarning($"[AdaptiveTanks] {str}");
    }

    public static void LogError(string str)
    {
        UnityEngine.Debug.LogError($"[AdaptiveTanks] {str}");
    }
}