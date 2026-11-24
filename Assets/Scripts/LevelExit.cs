using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Конфиг уровней: по нему определяются номер текущего уровня и следующая сцена.")]
    [SerializeField] private LevelDatabase database;
    [Tooltip("Если пусто, будет найден в сцене автоматически.")]
    [SerializeField] private LevelManager levelManager;

    [Header("После прохождения")]
    [Tooltip("Вкл: загрузить следующий уровень из базы (на последнем уровне откроется выбор уровня). Выкл: вернуться на экран выбора.")]
    [SerializeField] private bool loadNextLevel = true;
    [Tooltip("Задержка (реальные секунды) перед переходом: время для эффекта или звука.")]
    [SerializeField, Min(0f)] private float loadDelay = 0.6f;

    [Header("События")]
    [Tooltip("Вызывается в момент входа игрока (эффекты, звук).")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private int currentIndex = -1;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager == null)
            Debug.LogError($"{nameof(LevelExit)}: в сцене нет LevelManager.", this);

        if (database == null)
        {
            Debug.LogWarning($"{nameof(LevelExit)}: не назначен LevelDatabase: прогресс не сохранится, переход будет на экран выбора.", this);
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        currentIndex = database.IndexOfScene(sceneName);

        if (currentIndex < 0)
            Debug.LogWarning($"{nameof(LevelExit)}: сцена «{sceneName}» не найдена в LevelDatabase: прогресс не сохранится.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();
        if (death == null || death.IsDead) return;
        if (levelManager == null) return;

        if (!levelManager.CompleteLevel()) return;

        SaveProgress();
        onLevelCompleted?.Invoke();
        StartCoroutine(LoadAfterDelay());
    }

    private void SaveProgress()
    {
        if (database == null || !database.TryGetLevel(currentIndex, out LevelData data)) return;

        ProgressManager.CompleteLevel(currentIndex, data.levelId);
    }

    private IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(loadDelay);

        if (loadNextLevel && database != null && currentIndex >= 0
            && database.TryGetLevel(currentIndex + 1, out LevelData next))
        {
            if (Application.CanStreamedLevelBeLoaded(next.sceneName))
            {
                levelManager.LoadSceneByName(next.sceneName);
                yield break;
            }

            Debug.LogError($"{nameof(LevelExit)}: сцена «{next.sceneName}» не найдена в Build Settings. Возврат на экран выбора.", this);
        }

        levelManager.LoadLevelSelect();
    }
}