using System.Collections;
using UnityEngine;

public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
    [Header("Battle Transition")]
    [SerializeField] private UIForwardMotionEffect _forwardMotionEffect;
    [SerializeField, Min(0.1f)] private float _forwardMotionFallbackTimeout = 5f;

    private Coroutine _rewardCloseRoutine;
    private bool _missingForwardMotionLogged;

    public void Init()
    {
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Debug.Log("[GameFlowManager] Init");
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Playing:
                CardManager.Instance.ClearCombatPersistentStates();
                CardManager.Instance.DiscardHand();
                CardManager.Instance.DiscardGrid();
                CardManager.Instance.ResetDeck();
                GridManager.Instance.BuildGrid();
                CardManager.Instance.StartBattleDraw();
                BattleManager.Instance.StartBattle();
                break;

            case GameManager.GameState.FirstRunTutorial:
                // StartSceneTutorialDirector가 직접 처리
                break;

            case GameManager.GameState.GameOver:
            case GameManager.GameState.GameClear:
                // TODO
                break;
        }
    }

    // UI_RewardPopup이 OnBattleEnded 구독 후 선택 완료 시 호출
    public void OnRewardClosed()
    {
        if (_rewardCloseRoutine != null)
            return;

        _rewardCloseRoutine = StartCoroutine(RewardClosedRoutine());
    }

    private IEnumerator RewardClosedRoutine()
    {
        CardManager.Instance.ClearCombatPersistentStates();
        CardManager.Instance.DiscardHand();
        CardManager.Instance.DiscardGrid();
        GridManager.Instance.BuildGrid();
        CardManager.Instance.StartBattleDraw();

        yield return PlayForwardMotionEffectRoutine();

        BattleManager.Instance.NextBattle();
        _rewardCloseRoutine = null;
    }

    private IEnumerator PlayForwardMotionEffectRoutine()
    {
        UIForwardMotionEffect effect = ResolveForwardMotionEffect();
        if (effect == null)
        {
            if (!_missingForwardMotionLogged)
            {
                Debug.LogWarning("[GameFlowManager] UIForwardMotionEffect를 찾지 못해 다음 전투를 바로 시작합니다.");
                _missingForwardMotionLogged = true;
            }
            yield break;
        }

        GameObject effectObject = effect.gameObject;
        if (effectObject.activeSelf)
            effect.PlayForwardMotion();
        else
            effectObject.SetActive(true);

        float elapsed = 0f;
        while (effectObject.activeSelf && elapsed < _forwardMotionFallbackTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (effectObject.activeSelf)
        {
            Debug.LogWarning("[GameFlowManager] UIForwardMotionEffect 완료 대기 시간이 초과되어 다음 전투를 진행합니다.");
            effect.StopForwardMotion();
            effectObject.SetActive(false);
        }
    }

    private UIForwardMotionEffect ResolveForwardMotionEffect()
    {
        if (_forwardMotionEffect != null)
            return _forwardMotionEffect;

        UIForwardMotionEffect[] effects = Resources.FindObjectsOfTypeAll<UIForwardMotionEffect>();
        foreach (UIForwardMotionEffect effect in effects)
        {
            if (effect == null || !effect.gameObject.scene.IsValid() || !effect.gameObject.scene.isLoaded)
                continue;

            _forwardMotionEffect = effect;
            return _forwardMotionEffect;
        }

        return null;
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        _rewardCloseRoutine = null;
        base.Dispose();
    }
}
