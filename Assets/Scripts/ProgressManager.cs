using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ProgressManager
{
    private const string UnlockedIndexKey = "progress.unlockedLevelIndex";
    private const string CompletedLevelsKey = "progress.completedLevels";
    private const char Separator = ';';

    public static event Action OnProgressChanged;

    public static int UnlockedLevelIndex => Mathf.Max(0, PlayerPrefs.GetInt(UnlockedIndexKey, 0));

    public static bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex <= UnlockedLevelIndex;
    }

    public static bool IsLevelCompleted(string levelId)
    {
        return !string.IsNullOrEmpty(levelId) && ReadCompleted().Contains(levelId);
    }

    public static void UnlockNextLevel(int currentLevelIndex)
    {
        if (!TryUnlock(currentLevelIndex)) return;

        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    public static void MarkLevelCompleted(string levelId)
    {
        if (!TryMarkCompleted(levelId)) return;

        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    public static void CompleteLevel(int levelIndex, string levelId)
    {
        bool unlocked = TryUnlock(levelIndex);
        bool marked = TryMarkCompleted(levelId);

        if (!unlocked && !marked) return;

        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockedIndexKey);
        PlayerPrefs.DeleteKey(CompletedLevelsKey);
        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }


    private static bool TryUnlock(int currentLevelIndex)
    {
        if (currentLevelIndex < 0) return false;

        int current = UnlockedLevelIndex;
        int target = Mathf.Max(current, currentLevelIndex + 1);
        if (target == current) return false;

        PlayerPrefs.SetInt(UnlockedIndexKey, target);
        return true;
    }

    private static bool TryMarkCompleted(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return false;

        HashSet<string> completed = ReadCompleted();
        if (!completed.Add(levelId)) return false;

        PlayerPrefs.SetString(CompletedLevelsKey, string.Join(Separator.ToString(), completed));
        return true;
    }

    private static HashSet<string> ReadCompleted()
    {
        string raw = PlayerPrefs.GetString(CompletedLevelsKey, string.Empty);
        string[] parts = raw.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
        return new HashSet<string>(parts);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Progress/Reset Level Progress")]
    private static void ResetFromMenu()
    {
        ResetProgress();
        Debug.Log("ProgressManager: прогресс уровней сброшен.");
    }
#endif
}