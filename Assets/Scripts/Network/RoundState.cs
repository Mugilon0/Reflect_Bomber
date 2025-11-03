using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
using static Cinemachine.DocumentationSortingAttribute;
//using Helpers.Linq;

public class RoundManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    //[Networked] // (OnChanged = nameof(OnSessionConfigChanged))はいったん無視　ゲーム時間の選択肢
    //public int MaxTimeIndex { get; set; }
    public static float MaxTime => 120.0f;

    [Networked]
    public int CurrentLevel { get; set; }
    [Networked]
    public int TickStarted { get; set; }
    public static float Time => Instance?.Object?.IsValid == true // ラウンドの経過時間を計算する もしGameManagerがネットワーク上で有効なら、さらにタイマーが開始済みかチェックする。開始済みなら**経過時間（秒）**を計算して返し、まだなら0を返す。そもそもGameManagerが有効でなければ、問答無用で0を返す。
        ? (Instance.TickStarted == 0
            ? 0
            : (Instance.Runner.Simulation.Tick - Instance.TickStarted) * Instance.Runner.Simulation.DeltaTime)
        : 0;

    public static RoundManager Instance { get; private set; }

    public override void Spawned()
    {
        // シングルトンインスタンスとして自身を設定
        if (Instance == null)
        {
            Instance = this;
            // シーンをまたいで存在させる
            DontDestroyOnLoad(gameObject);

            Runner.AddCallbacks(this);
        }
        else
        {
            // 既にインスタンスが存在する場合は、重複しないように自身を破棄する
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // 破棄される際に、シングルトンインスタンスをクリアする
        if (Instance == this)
        {
            Instance = null;
            //InterfaceManager.Instance.resultsScreen.Clear(); 未実装　いるかわからない
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.SimulationUnityScene.name == "World1")
        {
            Debug.Log("レベルをスポーンします");
            Level.Load(ResourcesManager.Instance.levels[CurrentLevel]);
        }
    }

    //static void OnSessionConfigChanged(Changed<GameManager> changed)
    //{
    //    InterfaceManager.Instance.sessionScreen.UpdateSessionConfig();
    //}  // ゲーム時間をプレイヤーが決めるわけではないので省略


    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_LoadDone(RpcInfo info = default)
    {
        NetworkPlayer.ActivePlayers[info.Source].IsLoaded = true;
        ////PlayerRegistry.GetPlayer(info.Source).IsLoaded = true;
        //foreach (var playerEntry in NetworkPlayer.ActivePlayers) // ActivePlayers をイテレート
        //{
            
        //}
    }

    public void Server_ResetIsLoaded()
    {
        if (!Runner.IsServer) return;

        Debug.Log("SERVER: 全員の IsLoaded フラグを false にリセットします。");
        foreach (var entry in NetworkPlayer.ActivePlayers)
        {
            if (entry.Value != null)
            {
                // IsLoaded は NetworkPlayer.cs にある想定
                entry.Value.IsLoaded = false;
            }
        }
    }


    public bool CheckAllPlayersLoaded
    {
        get
        {
            // サーバー以外は判定不要
            if (!Runner.IsServer) return false;

            // プレイヤーがいない場合は「未完了」とする
            if (NetworkPlayer.ActivePlayers.Count == 0) //  && Runner.GameMode != GameMode.Single多分いらない条件 10/25
            {
                return false;
            }

            // --- もしくは、あなたが懸念していた foreach での書き方 ---
            foreach (var entry in NetworkPlayer.ActivePlayers)
            {
                NetworkPlayer player = entry.Value;
                if (player == null || !player.Object.IsValid || !player.IsLoaded)
                {
                    return false; // 一人でもロードが終わっていなければ false
                }
            }

            return true; // 全員がロード完了

            //LINQ を使った1行での書き方(こちらが好みかもしれません)
            //return NetworkPlayer.ActivePlayers.Values.All(p => p != null && p.Object.IsValid && p.IsLoaded);
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
