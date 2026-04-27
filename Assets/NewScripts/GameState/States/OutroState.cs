using Fusion;
using UnityEngine;

public class OutroState : IGameState
{
    public EGameStateNet NetState => EGameStateNet.Outro;

    private readonly NetworkRunner _runner;
    private bool _isComplete = false;
    private TickTimer _outroTimer = TickTimer.None;
    private const float OUTRO_DURATION = 10f;

    public OutroState(NetworkRunner runner) { _runner = runner; }

    public void OnEnter(IGameState prev)
    {
        _isComplete = false;

        if (_runner.IsServer)
        {
            // PlayerStatsにラウンド結果を累積してPlayerScoreをリセット
            PlayerRegistry.ForEach(p =>
            {
                var playerStats = p.GetComponent<PlayerStats>();
                var playerScore = p.GetComponent<PlayerScore>();

                if (playerStats != null && playerScore != null)
                    playerStats.AddRoundResult(playerScore);

                playerScore?.ResetRoundData();
            });

            _outroTimer = TickTimer.CreateFromSeconds(_runner, OUTRO_DURATION);
        }
    }

    public void OnExit(IGameState next)
    {
        _outroTimer = TickTimer.None;
    }

    public void OnUpdate()
    {
        if (!_runner.IsServer) return;

        if (_outroTimer.Expired(_runner))
        {
            _isComplete = true;
            _outroTimer = TickTimer.None;
        }
    }

    public bool IsComplete() => _isComplete;
    public void NotifyOutroComplete() => _isComplete = true;
}