using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkPlayer playerPrefab;
    public GameStateManager gameStateManagerPrefab;
    public PlayerRegistry playerRegistry;
    public RoundManager roundManagerPrefab; //仮実装 3/17

    private Dictionary<int, NetworkPlayer> mapTokenIDWithNetworkPlayer;
    private CharacterInputHandler characterInputHandler;
    private IConnectionTokenProvider _tokenProvider;

    // UIの代わりにイベントで通知
    public event Action<List<SessionInfo>> OnSessionListReceived;
    public event Action OnNoSessionFound;

    public void Initialize(IConnectionTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    void Awake()
    {
        mapTokenIDWithNetworkPlayer = new Dictionary<int, NetworkPlayer>();

        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            Initialize(gameManager);
        else
            Debug.LogError("GameManagerが見つかりません（Spawner.Awake）");
    }

    int GetPlayerToken(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer == player)
        {
            return ConnectionTokenUtils.HashToken(_tokenProvider.GetConnectionToken());
        }
        else
        {
            var token = runner.GetPlayerConnectionToken(player);
            if (token != null)
                return ConnectionTokenUtils.HashToken(token);

            Debug.LogError("GetPlayerToken returned invalid token");
            return 0;
        }
    }

    public void SetConnectionTokenMapping(int token, NetworkPlayer networkPlayer)
    {
        mapTokenIDWithNetworkPlayer.Add(token, networkPlayer);
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (GameStateManager.Instance == null)
            {
                Debug.Log("Spawning GameStateManager");
                runner.Spawn(gameStateManagerPrefab);
            }

            if (PlayerRegistry.Instance == null)
            {
                Debug.Log("Spawning PlayerRegistry");
                runner.Spawn(playerRegistry);
            }

            // いつか消す 3/17
            if (RoundManager.Instance == null)
            {
                Debug.Log("Spawning RoundManager");
                runner.Spawn(roundManagerPrefab);
            } 


            int playerToken = GetPlayerToken(runner, player);
            Debug.Log($"OnPlayerJoined we are server. Connection token {playerToken}");

            if (mapTokenIDWithNetworkPlayer.TryGetValue(playerToken, out NetworkPlayer networkPlayer))
            {
                Debug.Log($"Found old connection token for token {playerToken}. Assigning controls to that player");
                networkPlayer.GetComponent<NetworkObject>().AssignInputAuthority(player);
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
                    if (runner.SessionInfo.MaxPlayers - player.PlayerId == 1)
                        spawnPosition = new Vector3(-1 * 3, 1, 0);
                    else
                        spawnPosition = new Vector3(player.PlayerId * 3, 1, 0);
                }

                NetworkPlayer spawnedNetworkPlayer = runner.Spawn(
                    playerPrefab, spawnPosition, Quaternion.identity, player);
                spawnedNetworkPlayer.transform.position = spawnPosition;

                //CharacterOutfitHandler outfitHandler = spawnedNetworkPlayer.GetComponent<CharacterOutfitHandler>();
                //if (outfitHandler != null)
                //{
                //    int playerCount = runner.ActivePlayers.Count();
                //    if (playerCount == 1)
                //    {
                //        Debug.Log($"Host (Player {player.PlayerId}) joined. Using default material.");
                //    }
                //    else
                //    {
                //        int materialIndex = playerCount - 2;
                //        if (materialIndex >= 0 && materialIndex < outfitHandler.bodyMaterials.Count)
                //        {
                //            outfitHandler.SetInitialMaterialIndex(materialIndex);
                //            Debug.Log($"Client (Player {player.PlayerId}) joined. Assigning material index {materialIndex}.");
                //        }
                //        else
                //        {
                //            Debug.LogWarning($"Could not assign material to player {player.PlayerId}. Index {materialIndex} is out of bounds.");
                //        }
                //    }
                //}

                runner.SetPlayerObject(player, spawnedNetworkPlayer.Object);
                spawnedNetworkPlayer.token = playerToken;
                mapTokenIDWithNetworkPlayer[playerToken] = spawnedNetworkPlayer;


                if (PlayerRegistry.Instance != null)
                    PlayerRegistry.Server_Add(runner, player, spawnedNetworkPlayer);

                spawnedNetworkPlayer.GetComponent<CharacterOutfitHandler>()?.SetColorIndex(); //3/28
                spawnedNetworkPlayer.GetComponentInChildren<MapIconColor>()?.SetColorIndex();

                StartCoroutine(NotifyPlayerJoined(spawnedNetworkPlayer));
            }
        }
        else Debug.Log("OnPlayerJoined");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (characterInputHandler == null && NetworkPlayer.Local != null)
            characterInputHandler = NetworkPlayer.Local.GetComponent<CharacterInputHandler>();

        if (characterInputHandler != null)
            input.Set(characterInputHandler.GetNetworkInput());
    }

    // UIを直接操作せずイベントで通知するだけ
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (sessionList.Count == 0)
        {
            Debug.Log("Joined lobby no sessions found");
            OnNoSessionFound?.Invoke();
        }
        else
        {
            OnSessionListReceived?.Invoke(sessionList);
        }
    }

    private IEnumerator NotifyPlayerJoined(NetworkPlayer newPlayer)
    {
        yield return new WaitForSeconds(0.5f);
        // TODO: チャット機能は後で別クラスに切り出す
    }

    // 未使用コールバック
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}