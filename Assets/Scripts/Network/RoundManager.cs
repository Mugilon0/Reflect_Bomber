using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
//using Helpers.Linq;

public class RoundManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Networked]
    public int CurrentLevel { get; set; }
    //[Networked] // (OnChanged = nameof(OnSessionConfigChanged))
    public static RoundManager Instance { get; private set; }

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            Runner.AddCallbacks(this);
        }
        else
        {

            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
            //InterfaceManager.Instance.resultsScreen.Clear();
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.SimulationUnityScene.name == "World1")
        {
            Debug.Log("���x�����X�|�[�����܂�");
            Level.Load(ResourcesManager.Instance.levels[CurrentLevel]);
        }
    }

    //static void OnSessionConfigChanged(Changed<GameManager> changed)
    //{
    //    InterfaceManager.Instance.sessionScreen.UpdateSessionConfig();
    //} 


    /// <summary>
    /// ホストがレベルを選択する（一時的な実装）
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SelectLevel(int levelIndex, RpcInfo info = default)
    {
        if (!PlayerRegistry.IsHost(info.Source)) return;
        CurrentLevel = levelIndex;
        Debug.Log($"Level selected: {levelIndex}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_LoadDone(RpcInfo info = default)
    {
        var player = PlayerRegistry.GetPlayer(info.Source);
        if (player != null) player.IsLoaded = true;

    }

    public void Server_ResetIsLoaded()
    {
        if (!Runner.IsServer) return;

        Debug.Log("SERVER: �S���� IsLoaded �t���O�� false �Ƀ��Z�b�g���܂��B");
        PlayerRegistry.ForEach(p => p.IsLoaded = false);
    }


    public bool CheckAllPlayersLoaded
    {
        get
        {
            if (!Runner.IsServer) return false;
            if (PlayerRegistry.CountPlayers == 0) return false;
            return PlayerRegistry.All(p => p != null && p.Object.IsValid && p.IsLoaded);
        }
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    //void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

}