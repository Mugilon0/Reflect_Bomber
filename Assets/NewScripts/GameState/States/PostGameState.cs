using Fusion;

public class PostGameState : IGameState
{
    public EGameStateNet NetState => EGameStateNet.PostGame;

    private readonly NetworkRunner _runner;
    private bool _isComplete = false;
    private TickTimer _postGameTimer = TickTimer.None;
    private const float POSTGAME_DURATION = 15f;

    public PostGameState(NetworkRunner runner) { _runner = runner; }

    public void OnEnter(IGameState prev)
    {
        _isComplete = false;

        if (_runner.IsServer)
            _postGameTimer = TickTimer.CreateFromSeconds(_runner, POSTGAME_DURATION);
    }

    public void OnExit(IGameState next)
    {
        _postGameTimer = TickTimer.None;
    }

    public void OnUpdate()
    {
        if (!_runner.IsServer) return;

        if (_postGameTimer.Expired(_runner))
        {
            _isComplete = true;
            _postGameTimer = TickTimer.None;
        }
    }

    public bool IsComplete() => _isComplete;
    public void NotifyPostGameComplete() => _isComplete = true;
}