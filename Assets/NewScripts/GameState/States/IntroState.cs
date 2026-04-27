using Fusion;
using UnityEngine;

public class IntroState : IGameState
{
    public EGameStateNet NetState => EGameStateNet.Intro;

    private readonly NetworkRunner _runner;
    private bool _isComplete = false;
    private TickTimer _timer = TickTimer.None;
    private const float INTRO_DURATION = 3f;
    private IntroUIHandler _introUIHandler;

    public IntroState(NetworkRunner runner) { _runner = runner; }

    public void OnEnter(IGameState prev)
    {
        _isComplete = false;

        // 入力ロック解除
        // カウントダウン再生
        if (_introUIHandler == null)
            _introUIHandler = GameObject.FindObjectOfType<IntroUIHandler>();
        _introUIHandler?.PlayCountdown();

        if (_runner.IsServer)
            _timer = TickTimer.CreateFromSeconds(_runner, INTRO_DURATION);
    }

    public void OnExit(IGameState next) { }

    public void OnUpdate()
    {
        if (!_runner.IsServer) return;
        if (_timer.Expired(_runner))
        {
            _timer = TickTimer.None;
            _isComplete = true;
        }
    }

    public bool IsComplete() => _isComplete;
}