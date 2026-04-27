using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyAppState : IAppState
{
    public EAppState AppState => EAppState.Lobby;

    private readonly NetworkRunnerHandler _networkRunnerHandler;
    private bool _isComplete = false;

    public LobbyAppState(NetworkRunnerHandler networkRunnerHandler)
    {
        _networkRunnerHandler = networkRunnerHandler;
    }

    public void OnEnter(IAppState prev)
    {
        _isComplete = false;
        _networkRunnerHandler.OnGameCreated += NotifySessionJoined;
        _networkRunnerHandler.OnGameJoined += NotifySessionJoined;
        _networkRunnerHandler.OnJoinLobby();
    }

    public void OnExit(IAppState next)
    {
        _networkRunnerHandler.OnGameCreated -= NotifySessionJoined;
        _networkRunnerHandler.OnGameJoined -= NotifySessionJoined;
    }

    public void OnUpdate() { }
    public bool IsComplete() => _isComplete;
    private void NotifySessionJoined() => _isComplete = true;
}