//using Fusion;
//using UnityEngine;

//// 不要なステートのため削除
//public class LobbyState : IGameState
//{
//    public EGameStateNet NetState => EGameStateNet.Lobby;

//    private readonly NetworkRunner _runner;
//    private readonly NetworkRunnerHandler _networkRunnerHandler;
//    private bool _isComplete = false;

//    public LobbyState(NetworkRunner runner, NetworkRunnerHandler networkRunnerHandler)
//    {
//        _runner = runner;
//        _networkRunnerHandler = networkRunnerHandler;
//    }

//    public void OnEnter(IGameState prev)
//    {
//        Debug.Log("LobbyState.OnEnter()が呼ばれた!!");
//        _isComplete = false;

//        // イベント購読
//        _networkRunnerHandler.OnGameCreated += NotifyRoomJoined;
//        _networkRunnerHandler.OnGameJoined += NotifyRoomJoined;

//        // Offからでも、PostGameからでも同じ
//        // GetRunner()が「あれば使い回す、なければ生成する」を自動でやる
//        _networkRunnerHandler.OnJoinLobby();

//        // TODO: LobbyUI表示
//    }

//    public void OnExit(IGameState next)
//    {
//        // イベント解除（メモリリーク防止）
//        _networkRunnerHandler.OnGameCreated -= NotifyRoomJoined;
//        _networkRunnerHandler.OnGameJoined -= NotifyRoomJoined;

//        // TODO: LobbyUI非表示
//    }

//    public void OnUpdate() { }

//    public bool IsComplete() => _isComplete;

//    private void NotifyRoomJoined() => _isComplete = true;
//}