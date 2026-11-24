using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Данные")]
    [SerializeField] private LevelDatabase database;

    [Header("UI")]
    [Tooltip("Префаб карточки уровня.")]
    [SerializeField] private LevelButtonItem buttonPrefab;
    [Tooltip("Контейнер для карточек (Content с GridLayoutGroup внутри ScrollRect).")]
    [SerializeField] private RectTransform contentRoot;
    [Tooltip("Кнопка сброса прогресса (только для тестирования). Можно оставить пустой.")]
    [SerializeField] private Button resetProgressButton;

    private readonly List<LevelButtonItem> items = new List<LevelButtonItem>();
    private bool isLoading;

    private void Start()
    {
        Time.timeScale = 1f;

        if (database == null || buttonPrefab == null || contentRoot == null)
        {
            Debug.LogError($"{nameof(LevelSelectUI)}: назначь Database, Button Prefab и Content Root.", this);
            return;
        }

        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(OnResetClicked);

        Rebuild();
    }

    private void OnDestroy()
    {
        if (resetProgressButton != null)
            resetProgressButton.onClick.RemoveListener(OnResetClicked);
    }

    public void Rebuild()
    {
        foreach (LevelButtonItem old in items)
            if (old != null) Destroy(old.gameObject);

        items.Clear();

        if (database.Count == 0)
        {
            Debug.LogWarning($"{nameof(LevelSelectUI)}: LevelDatabase пуст.", this);
            return;
        }

        int unlockedIndex = ProgressManager.UnlockedLevelIndex;

        for (int i = 0; i < database.Count; i++)
        {
            LevelData data = database.Levels[i];
            bool unlocked = i <= unlockedIndex;
            bool completed = ProgressManager.IsLevelCompleted(data.levelId);

            LevelButtonItem item = Instantiate(buttonPrefab, contentRoot);
            item.Setup(data, i, unlocked, completed, OnLevelChosen);
            items.Add(item);
        }

        SelectDefault(unlockedIndex);
    }

    private void SelectDefault(int unlockedIndex)
    {
        int index = Mathf.Min(unlockedIndex, items.Count - 1);

        if (index >= 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(items[index].gameObject);
    }

    private void OnLevelChosen(int index)
    {
        if (isLoading) return;
        if (!ProgressManager.IsLevelUnlocked(index)) return;
        if (!database.TryGetLevel(index, out LevelData data)) return;

        if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
        {
            Debug.LogError($"{nameof(LevelSelectUI)}: сцена «{data.sceneName}» не найдена в Build Settings или имя указано неверно.", this);
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(data.sceneName);
    }

    private void OnResetClicked()
    {
        ProgressManager.ResetProgress();
        Rebuild();
    }
}