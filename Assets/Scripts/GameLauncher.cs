using Fusion;
using UnityEngine;
using System;
using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine.UI; // added
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;
using System.Linq; //added



public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private const int MAX_PLAYERS_IN_LOBBY = 4; // ロビーの最大人数
    private Dictionary<string, int> _lobbyPlayerCount = new Dictionary<string, int>(); // ロビーの人数管理
    private List<SessionInfo> _cachedSessionList = new List<SessionInfo>(); // 最新のセッションリスト


    public static GameLauncher Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void StartLobby()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = false; // ロビーでは入力は不要
            _runner.AddCallbacks(this);
        }

        List<SessionInfo> sessionList = new List<SessionInfo>();

        await System.Threading.Tasks.Task.Delay(1000); // 1秒待機（調整可） 強引なやりかたらしい


        string lobbyName = GetAvailableLobby();



        //Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>
        //{
        //    { "MaxPlayers", new SessionProperty(4) } //  MAX_PLAYERS_IN_LOBBY 
        //}; //added


        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient, //GameMode.Hostかも
            SessionName = lobbyName,
            // PlayerCount = MAX_PLAYERS_IN_LOBBY, // 上限を設定 PlayerCountは定義されてないしいらないかも
            //SessionProperties = sessionProperties,
            Scene = 1,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log("ロビーに接続完了！");
        //if (SceneManager.GetActiveScene().buildIndex != 1)
        //{
        //    SceneManager.LoadScene(1);
        //}　　　Fusionにまかせる方がよい
    }

    private string GetAvailableLobby()
    {
        foreach (var session in _cachedSessionList)
        {
            // すでに最大人数ならスキップ
            if (session.PlayerCount >= MAX_PLAYERS_IN_LOBBY)
            {
                Debug.Log($"ロビー {session.Name} は満員 (現在 {session.PlayerCount}/4)");
                continue;
            } //add 3/19


            if (session.PlayerCount < MAX_PLAYERS_IN_LOBBY)
            {
                Debug.Log($"既存のロビー {session.Name} に参加 (現在 {session.PlayerCount}/4)");
                return session.Name;
            }
        }

        // すべてのロビーが満員なら、新しいロビーを作る
        string newLobby = "Lobby_" + _cachedSessionList.Count;
        Debug.Log($"新しいロビー {newLobby} を作成");
        return newLobby;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined the lobby.");
        Debug.Log($"誰かがロビーに参加しました！");



        // ロビーUIの更新
        int playerCount = runner.ActivePlayers.Count();

        // もし4人を超えたらキック
        if (playerCount > MAX_PLAYERS_IN_LOBBY)
        {
            Debug.LogWarning($"プレイヤー数が {MAX_PLAYERS_IN_LOBBY} を超えたため、{player.PlayerId} をキックします");
            runner.Disconnect(player);
        } //add 3/19


        LobbyUIManager.Instance.UpdatePlayerCountUI(playerCount); // ロビーでテキストを表示するスクリプトかいたら戻す


    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left the lobby.");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("OnSessionListUpdated() が呼ばれた！");
        _cachedSessionList = sessionList;

        int playerCount = 0;

        _lobbyPlayerCount.Clear();
        Debug.Log("ロビーリスト更新！");
        if (sessionList.Count == 0)
        {
            Debug.LogWarning("セッションリストが空です！ロビーが取得できていない可能性あり。");
        }


        foreach (var session in sessionList)
        {
            _lobbyPlayerCount[session.Name] = session.PlayerCount;
            Debug.Log($"Lobby Updated: {session.Name} - {session.PlayerCount}/{MAX_PLAYERS_IN_LOBBY}");
            playerCount += session.PlayerCount;
        }



        Debug.Log($"現在のロビー参加人数: {playerCount}");
        // UI更新
        LobbyUIManager.Instance.UpdatePlayerCountUI(playerCount);// ロビーでテキストを表示するスクリプトかいたら戻す

    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to lobby server!");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("Disconnected from lobby server.");
    }







    // その他のコールバック


    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }





}









