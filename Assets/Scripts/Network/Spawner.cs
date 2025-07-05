using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Diagnostics;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{

    public NetworkPlayer playerPrefab;

    public GameStateManager gameStateManagerPrefab;
    // Mapping between Token ID and Re-created Players
    Dictionary<int, NetworkPlayer> mapTokenIDWithNetworkPlayer;

    // other components
    CharacterInputHandler characterInputHandler;
    SessionListUIHandler sessionListUIHandler;

    void Awake()
    {
        // create a new Dictionary
        mapTokenIDWithNetworkPlayer = new Dictionary<int, NetworkPlayer>();

        sessionListUIHandler = FindObjectOfType<SessionListUIHandler>(true);
    }

    void Start()
    {

    }

    int GetPlayerToken(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer == player)
        {
            // Just use the local Player Connection Token
            return ConnectionTokenUtils.HashToken(GameManager.instance.GetConnectionToken());
        }
        else
        {
            // Get the Connection Token stored when the Client connects to this Host
            var token = runner.GetPlayerConnectionToken(player);

            if (token != null)
                return ConnectionTokenUtils.HashToken(token);

            Debug.LogError($"GetPlayerToken returned invalid token");

            return 0; // invalid
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer");
    }


    public void SetConnectionTokenMapping(int token, NetworkPlayer networkPlayer)
    {
        mapTokenIDWithNetworkPlayer.Add(token, networkPlayer);
    }



    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (GameStateManager.Instance == null)
            {
                Debug.Log("Spawning GameStateManager");
                runner.Spawn(gameStateManagerPrefab); // プレハブを生成
            }

            // Get the token for the player
            int playerToken = GetPlayerToken(runner, player); //プレイヤーのトークンを取得

            Debug.Log("OnPlayerJoined we are server. Connection token {playerToken}");

            // check if the token is already recorded by the server
            if (mapTokenIDWithNetworkPlayer.TryGetValue(playerToken, out NetworkPlayer networkPlayer))
            {
                Debug.Log($"Found old connection token for token {playerToken}. Assigning controlls to that player");

                networkPlayer.GetComponent<NetworkObject>().AssignInputAuthority(player); // このプレイヤーを制御できるようになる
                
                // 再接続するプレイヤーにもSetPlayerObjectを実行
                runner.SetPlayerObject(player, networkPlayer.GetComponent<NetworkObject>());

                networkPlayer.Spawned();
            }
            else
            {
                Debug.Log($"Spawning new player for connection token {playerToken}");

                bool isReadyScene = SceneManager.GetActiveScene().name == "Ready";

                Vector3 spawnPosition = Utils.GetRandomSpawnPoint();

                if (isReadyScene)
                {
                    //Check if we are the host
                    if (runner.SessionInfo.MaxPlayers - player.PlayerId == 1)
                        spawnPosition = new Vector3(-1 * 3, 1, 0); // host用
                    else
                        spawnPosition = new Vector3(player.PlayerId * 3, 1, 0);
                }

                NetworkPlayer spawnedNetworkPlayer = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player); // 後でリスポーン地点変える
                spawnedNetworkPlayer.transform.position = spawnPosition; // 位置のバグ？を修正する

                // PlayerRefに登録(?)
                runner.SetPlayerObject(player, spawnedNetworkPlayer.Object);

                // Store the token for the player
                spawnedNetworkPlayer.token = playerToken;

                // store the mapping between playerToken and the spawned network player
                mapTokenIDWithNetworkPlayer[playerToken] = spawnedNetworkPlayer;

            }
        }
        else Debug.Log("OnPlayerJoined");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {  // 入力された値を受け取り、実際の動作を実装する
        if (characterInputHandler == null && NetworkPlayer.Local != null) // ローカルの入力にアクセスできるようにする
            characterInputHandler = NetworkPlayer.Local.GetComponent<CharacterInputHandler>();

        if (characterInputHandler != null)
        {
            input.Set(characterInputHandler.GetNetworkInput()); // 入力を設定、動くようになる
        }
    }



    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    // 他プレイヤーのロビー情報を受け取る
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // SessionListUIHandlerが有効な場合のみ
        if (sessionListUIHandler == null)
            return;
        if (sessionList.Count == 0)
        {
            Debug.Log("Joined lobby no sessions found");

            sessionListUIHandler.OnNoSessionFound();
        }
        else
        {
            sessionListUIHandler.ClearList(); //古い情報をクリア

            foreach (SessionInfo sessionInfo in sessionList)
            {
                sessionListUIHandler.AddToList(sessionInfo);

                Debug.Log($"Found session {sessionInfo.Name} playerCount {sessionInfo.PlayerCount}");
            }
        }


    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("OnHostMigration");

        // shut down the current runner
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        // Find the network runner handler and start the host migration
        FindObjectOfType<NetworkRunnerHandler>().StartHostMigration(hostMigrationToken);

    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnHostMigrationCleanUp() //　消えたplayerをけす
    {
        Debug.Log("Spawner OnHostMigrationCleanUp started");

        foreach (KeyValuePair<int, NetworkPlayer> entry in mapTokenIDWithNetworkPlayer)
        {
            NetworkObject networkObjectInDictionary = entry.Value.GetComponent<NetworkObject>();

            if (networkObjectInDictionary.InputAuthority.IsNone)
            {
                Debug.Log($"{Time.time} Found player that has not reconnected. Despawning {entry.Value.nickName}");

                networkObjectInDictionary.Runner.Despawn(networkObjectInDictionary);
            }
        }

        Debug.Log("Spawner OnHostMigrationCleanUp completed");
    }


}
