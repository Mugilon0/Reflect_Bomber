using Fusion;
using UnityEngine;

public class GameState : IGameState
{
    public EGameStateNet NetState => EGameStateNet.Game;

    private readonly NetworkRunner _runner;
    private readonly int _roundDuration;
    private bool _isComplete = false;

    private TickTimer RoundTimer
    {
        get => GameStateManager.Instance.GameRoundTimer;
        set => GameStateManager.Instance.GameRoundTimer = value;
    }

    public GameState(NetworkRunner runner, int roundDuration = 120)
    {
        _runner = runner;
        _roundDuration = roundDuration;
    }

    public void OnEnter(IGameState prev)
    {
        _isComplete = false;

        if (_runner.IsServer)
        {
            PlayerRegistry.ForEach(p =>
            {
                p.Unlock();
                // ラウンド開始時にHPをリセット
                p.GetComponent<HPHandler>()?.OnRespawned();  // ゲームの開始時でなくともIntro等でもいい気がする
            });
            RoundTimer = TickTimer.CreateFromSeconds(_runner, _roundDuration);
        }
    }

    public void OnExit(IGameState next)
    {
        if (_runner.IsServer)
            RoundTimer = TickTimer.None;
    }

    public void OnUpdate()
    {
        if (!_runner.IsServer) return;

        if (RoundTimer.Expired(_runner))
        {
            _isComplete = true;
            RoundTimer = TickTimer.None;
        }
    }

    public bool IsComplete() => _isComplete;
    public void NotifyGameEnd() => _isComplete = true;
}