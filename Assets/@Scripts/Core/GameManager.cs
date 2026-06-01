using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBehaviour<GameManager>
{
    public enum GameState { Idle, Playing, Tutorial, GameOver, GameClear }

    // ── 상태 ──────────────────────────────────────
    public GameState CurrentState { get; private set; } = GameState.Idle;
    private bool _isPaused;
    public bool IsPaused => _isPaused;
    public bool IsPlaying => (CurrentState == GameState.Playing || CurrentState == GameState.Tutorial) && !_isPaused;

    // ── 이벤트 ────────────────────────────────────
    public event Action<GameState> OnStateChanged;
    public event Action<bool> OnPauseChanged;

    // ── 초기화 ────────────────────────────────────
    public void Init()
    {
        Debug.Log("[GameManager] Init");
    }

    // ── 상태 전환 ─────────────────────────────────
    public void GameStart() => ChangeState(GameState.Playing);
    public void StartTutorial() => ChangeState(GameState.Tutorial);
    public void GameOver() => ChangeState(GameState.GameOver);
    public void GameClear() => ChangeState(GameState.GameClear);
    public void GoToMainMenu() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // 꼼수

    private void ChangeState(GameState state)
    {
        if (CurrentState == state) return;

        if (_isPaused)
        {
            _isPaused = false;
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }

        CurrentState = state;

        switch (state)
        {
            case GameState.Playing:
            case GameState.Tutorial:
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
            case GameState.GameClear:
                Time.timeScale = 0f;
                break;
        }

        Debug.Log($"[GameManager] State → {state}");
        OnStateChanged?.Invoke(state);
    }

    // ── 일시정지 ──────────────────────────────────
    public void Pause()
    {
        if (!IsPlaying) return;
        _isPaused = true;
        Time.timeScale = 0f;
        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        OnPauseChanged?.Invoke(false);
    }

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    // ── 정리 ──────────────────────────────────────
    protected override void Dispose()
    {
        OnStateChanged = null;
        OnPauseChanged = null;
        base.Dispose();
    }
}