using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LevelData
{
    [Tooltip("Уникальный неизменный ID, например level_1. Без символа ';'.")]
    public string levelId;

    [Tooltip("Название в UI, например «1. Казематы».")]
    public string displayName;

    [Tooltip("Точное имя файла сцены (сцена должна быть в Build Settings).")]
    public string sceneName;

    [Tooltip("Превью для карточки уровня (опционально).")]
    public Sprite previewIcon;
}

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Tooltip("Уровни по порядку прохождения.")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();

    public IReadOnlyList<LevelData> Levels => levels;
    public int Count => levels.Count;

    public bool TryGetLevel(int index, out LevelData data)
    {
        if (index >= 0 && index < levels.Count)
        {
            data = levels[index];
            return true;
        }

        data = default;
        return false;
    }

    public int IndexOfScene(string sceneName)
    {
        for (int i = 0; i < levels.Count; i++)
            if (levels[i].sceneName == sceneName)
                return i;

        return -1;
    }

    public int IndexOfId(string levelId)
    {
        for (int i = 0; i < levels.Count; i++)
            if (levels[i].levelId == levelId)
                return i;

        return -1;
    }

    private void OnValidate()
    {
        HashSet<string> ids = new HashSet<string>();

        for (int i = 0; i < levels.Count; i++)
        {
            LevelData level = levels[i];

            if (string.IsNullOrWhiteSpace(level.levelId))
                Debug.LogWarning($"{name}: у уровня #{i} пустой levelId.", this);
            else if (level.levelId.Contains(";"))
                Debug.LogWarning($"{name}: levelId «{level.levelId}» содержит ';', это зарезервированный разделитель.", this);
            else if (!ids.Add(level.levelId))
                Debug.LogWarning($"{name}: levelId «{level.levelId}» повторяется.", this);

            if (string.IsNullOrWhiteSpace(level.sceneName))
                Debug.LogWarning($"{name}: у уровня #{i} пустой sceneName.", this);
        }
    }
}