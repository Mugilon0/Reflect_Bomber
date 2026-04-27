using Fusion;
using UnityEngine;

public class LoadingState : IGameState
{
    public EGameStateNet NetState => EGameStateNet.Loading;

    private readonly NetworkRunner _runner;
    private bool _isComplete = false;
    private TickTimer minLoadTimer = TickTimer.None;
    private const float minLoadTime = 5f;



    public LoadingState(NetworkRunner runner)
    {
        _runner = runner;
    }

    public void OnEnter(IGameState prev)
    {
        _isComplete = false;

        if (!_runner.IsServer) return;

        Debug.Log($"LoadingState.OnEnter: Locking players. CountPlayers={PlayerRegistry.CountPlayers}");
        PlayerRegistry.ForEach(p =>
        {
            Debug.Log($"Locking player: {p.gameObject.name}");
            p.Lock();
            Debug.Log($"After lock: MovementHandler.IsLocked={p.MovementHandler?.IsLocked}");
        });

        // IsLoadedをリセット
        RoundManager.Instance?.Server_ResetIsLoaded();

        minLoadTimer = TickTimer.CreateFromSeconds(_runner, minLoadTime);


        var prevState = prev?.NetState ?? EGameStateNet.Ready;

        if (prevState == EGameStateNet.Ready)
        {
            // Ready→Loading：World1シーンへ遷移
            // Level.Load()はRoundManager.OnSceneLoadDone()が担当
            Debug.Log("LoadingState: SetActiveScene(World1)");
            _runner.SetActiveScene("World1");
        }
        else if (prevState == EGameStateNet.Outro)
        {
            // Outro→Loading：既にWorld1にいるのでLevel.Load()だけ呼ぶ
            Debug.Log("LoadingState: Level.Load()");
            Level.Load(ResourcesManager.Instance.levels[RoundManager.Instance.CurrentLevel]);
        }
    }

    public void OnExit(IGameState next)
    {
        // 入力ロック解除はIntroState.OnEnter()で行う
    }

    public void OnUpdate()
    {
        if (!_runner.IsServer) return;

        bool allLoaded = RoundManager.Instance != null && RoundManager.Instance.CheckAllPlayersLoaded;
        bool minTimeElapsed = minLoadTimer.Expired(_runner);

        Debug.Log($"LoadingState.OnUpdate: CountPlayers={PlayerRegistry.CountPlayers}, CheckAllPlayersLoaded={allLoaded}, MinTimeElapsed={minTimeElapsed}");

        if (allLoaded && minTimeElapsed)
        {
            Debug.Log("LoadingState: 全員ロード完了かつ最低待機時間経過 → Introへ");
            _isComplete = true;
        }
    }

    public bool IsComplete() => _isComplete;
}