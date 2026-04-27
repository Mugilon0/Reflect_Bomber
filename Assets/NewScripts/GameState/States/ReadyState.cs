using Fusion;
using UnityEngine;

public class ReadyState : IGameState
{
    public EGameStateNet NetState => EGameStateNet.Ready;

    private readonly NetworkRunner _runner;
    private ReadyUIHandler _readyUIHandler;
    private bool _isComplete = false;
    private bool _wasAllReady = false;
    private const float COUNTDOWN_SECONDS = 10f;

    // GameStateManager.ReadyCountdownTimer‚Ö‚Ì’ZkƒAƒNƒZƒX
    private TickTimer CountdownTimer
    {
        get => GameStateManager.Instance.ReadyCountdownTimer;
        set => GameStateManager.Instance.ReadyCountdownTimer = value;
    }

    public ReadyState(NetworkRunner runner)
    {
        _runner = runner;
    }

    public void OnEnter(IGameState prev)
    {
        _isComplete = false;
        _wasAllReady = false;

        _readyUIHandler = GameObject.FindObjectOfType<ReadyUIHandler>();

        if (_runner.IsServer)
        {
            CountdownTimer = TickTimer.None;
            PlayerRegistry.ForEach(p =>
                p.GetComponent<PlayerState>()?.RPC_SetReady(false));
        }

        _readyUIHandler?.ResetCountdown();
    }

    public void OnExit(IGameState next)
    {
        if (_runner.IsServer)
            CountdownTimer = TickTimer.None;
    }

    public void OnUpdate()
    {
        if (!_runner.IsServer) return;
        if (PlayerRegistry.CountPlayers == 0) return;

        bool allReady = CheckAllReady();

        if (allReady)
        {
            if (!_wasAllReady)
            {
                CountdownTimer = TickTimer.CreateFromSeconds(_runner, COUNTDOWN_SECONDS);
                _wasAllReady = true;
            }

            if (CountdownTimer.Expired(_runner))
            {
                _isComplete = true;
                CountdownTimer = TickTimer.None;
            }
        }
        else
        {
            if (_wasAllReady)
            {
                CountdownTimer = TickTimer.None;
                _wasAllReady = false;
            }
        }
    }

    private bool CheckAllReady()
    {
        bool hasPlayers = PlayerRegistry.Any(p =>
            !(p.GetComponent<PlayerState>()?.IsSpectator == true));

        bool allReady = PlayerRegistry.All(p =>
        {
            var state = p.GetComponent<PlayerState>();
            return state != null && (state.IsSpectator || state.IsReady);
        });

        return hasPlayers && allReady;
    }

    public bool IsComplete() => _isComplete;
}