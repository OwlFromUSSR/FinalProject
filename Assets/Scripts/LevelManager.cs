using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum LevelState
{
    Playing,
    Dead,
    Completed
}

public class LevelManager : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Если пусто, будет найден автоматически.")]
    [SerializeField] private PlayerDeath playerDeath;
    [Tooltip("Нужен, чтобы отключить управление при завершении уровня и паузе. Если пусто, будет найден автоматически.")]
    [SerializeField] private PlayerController player;

    [Header("Рестарт")]
    [Tooltip("Пауза (в реальных секундах) между смертью и перезагрузкой сцены.")]
    [SerializeField, Min(0f)] private float restartDelay = 0.8f;

    [Header("Навигация")]
    [Tooltip("Точное имя сцены выбора уровня (должна быть в Build Settings).")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    [Header("Пауза (Esc)")]
    [Tooltip("Панель мини-меню паузы. Если пусто, Esc сразу возвращает на экран выбора уровня.")]
    [SerializeField] private GameObject pausePanel;

    private InputAction pauseAction;
    private Coroutine restartRoutine;
    private bool isLoading;

    public LevelState State { get; private set; } = LevelState.Playing;
    public bool IsPaused { get; private set; }

    public event Action<LevelState> OnStateChanged;
    public event Action OnLevelRestarting;
    public event Action OnLevelCompleted;
    public event Action<bool> OnPauseChanged;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (playerDeath == null) playerDeath = FindFirstObjectByType<PlayerDeath>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
    }

    private void OnEnable()
    {
        if (playerDeath != null)
            playerDeath.OnPlayerDied += HandlePlayerDied;

        pauseAction.performed += OnPausePerformed;
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        if (playerDeath != null)
            playerDeath.OnPlayerDied -= HandlePlayerDied;

        pauseAction.performed -= OnPausePerformed;
        pauseAction.Disable();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        pauseAction?.Dispose();
    }

    public bool CompleteLevel()
    {
        if (State != LevelState.Playing) return false;

        SetState(LevelState.Completed);

        if (playerDeath != null)
            playerDeath.IsInvulnerable = true;

        if (player != null)
            player.SetControlEnabled(false);

        OnLevelCompleted?.Invoke();
        return true;
    }

    public void RestartLevel()
    {
        if (isLoading) return;
        isLoading = true;

        if (restartRoutine != null)
        {
            StopCoroutine(restartRoutine);
            restartRoutine = null;
        }

        Time.timeScale = 1f;
        OnLevelRestarting?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadLevelSelect() => LoadSceneByName(levelSelectSceneName);

    public void LoadSceneByName(string sceneName)
    {
        if (isLoading) return;
        isLoading = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void ResumeGame() => SetPaused(false);


    private void HandlePlayerDied()
    {
        if (State != LevelState.Playing) return;


        SetState(LevelState.Dead);
        if (IsPaused) SetPaused(false);
        restartRoutine = StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(restartDelay);
        restartRoutine = null;
        RestartLevel();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (isLoading) return;

        if (pausePanel == null)
        {
            LoadLevelSelect();
            return;
        }

        if (State != LevelState.Playing) return;

        SetPaused(!IsPaused);
    }

    private void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;

        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(paused);

        if (player != null && State == LevelState.Playing)
            player.SetControlEnabled(!paused);

        OnPauseChanged?.Invoke(paused);
    }

    private void SetState(LevelState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}