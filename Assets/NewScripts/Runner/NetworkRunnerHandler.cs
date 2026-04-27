using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField] private NetworkRunner _networkRunnerPrefab;
    //[SerializeField] private GameStateManager _gameStateManagerPrefab;

    private NetworkRunner _networkRunner;
    private IConnectionTokenProvider _tokenProvider;

    // ロビー
    public event Action OnLobbyJoinSuccess;
    public event Action OnLobbyJoinFailure;

    // ルーム作成
    public event Action OnGameCreated;
    public event Action OnGameCreateFailed;

    // ルーム参加
    public event Action OnGameJoined;
    public event Action OnGameJoinFailed;

    // ランダムマッチング
    public event Action OnRandomMatchSuccess;
    public event Action OnRandomMatchFailed;

    private void Start()
    {
        OnJoinLobby();
    }
    public void Initialize(IConnectionTokenProvider tokenProvider)
    {
        Debug.Log("Initializeを呼んだ！！");
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); //シーン遷移しても壊れないようにする

        var existing = FindObjectOfType<NetworkRunner>();
        if (existing != null) _networkRunner = existing;

        // GameManagerからtokenProviderを取得
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            Initialize(gameManager);
        else
            Debug.LogError("GameManagerが見つかりません");
    }
    private NetworkRunner GetRunner()
    {
        if (_networkRunner == null)
            _networkRunner = FindObjectOfType<NetworkRunner>();
        if (_networkRunner == null)
            _networkRunner = Instantiate(_networkRunnerPrefab);
        return _networkRunner;
    }

    private Fusion.GameMode ToFusionGameMode(RoomMode roomMode)
    {
        switch (roomMode)
        {
            case RoomMode.Public: return Fusion.GameMode.Host;
            case RoomMode.Private: return Fusion.GameMode.Host;
            case RoomMode.Solo: return Fusion.GameMode.Single;
            default: return Fusion.GameMode.AutoHostOrClient;
        }
    }

    // ロビー参加
    public void OnJoinLobby()
    {
        Debug.Log("OnJoinLobbyを呼んだ！！");
        _ = JoinLobbyAsync(GetRunner());
    }

    private async Task JoinLobbyAsync(NetworkRunner runner)
    {
        var result = await runner.JoinSessionLobby(SessionLobby.Custom, "OurLobbyID");
        if (!result.Ok)
        {
            Debug.LogError($"ロビーに参加できませんでした: {result.ShutdownReason}");
            OnLobbyJoinFailure?.Invoke();
        }
        else
        {
            OnLobbyJoinSuccess?.Invoke();
        }
    }

    // ルーム作成
    public async void CreateGame(RoomConfig config)
    {
        Debug.Log($"CreateGame _tokenProvider is null: {_tokenProvider == null}"); // ログ用 3/10
        var result = await InitializeNetworkRunner(
            GetRunner(),
            config.SessionName,
            ToFusionGameMode(config.RoomMode),
            _tokenProvider.GetConnectionToken(),
            NetAddress.Any(),
            SceneUtility.GetBuildIndexByScenePath("scenes/Ready"),
            config
        );

        if (result.Ok)
            OnGameCreated?.Invoke();
        else
        {
            Debug.LogError($"ルーム作成失敗: {result.ShutdownReason}");
            OnGameCreateFailed?.Invoke();
        }
    }

    // ルーム参加
    public async void JoinGame(SessionInfo sessionInfo, string password = "")
    {
        if (!string.IsNullOrEmpty(password))
        {
            if (sessionInfo.Properties.TryGetValue("password", out var stored))
            {
                if (stored.ToString() != password)
                {
                    Debug.LogError("パスワードが違います");
                    OnGameJoinFailed?.Invoke();
                    return;
                }
            }
        }

        var result = await InitializeNetworkRunner(
            GetRunner(),
            sessionInfo.Name,
            Fusion.GameMode.Client,
            _tokenProvider.GetConnectionToken(),
            NetAddress.Any(),
            SceneManager.GetActiveScene().buildIndex,
            null
        );

        if (result.Ok)
            OnGameJoined?.Invoke();
        else
        {
            Debug.LogError($"ルーム参加失敗: {result.ShutdownReason}");
            OnGameJoinFailed?.Invoke();
        }
    }

    // ランダムマッチング
    public async void JoinRandomGame()
    {
        var result = await InitializeNetworkRunner(
            GetRunner(),
            Guid.NewGuid().ToString(),
            Fusion.GameMode.AutoHostOrClient,
            _tokenProvider.GetConnectionToken(),
            NetAddress.Any(),
            SceneManager.GetActiveScene().buildIndex,
            new RoomConfig { RoomMode = RoomMode.Public }
        );

        if (result.Ok)
            OnRandomMatchSuccess?.Invoke();
        else
        {
            Debug.LogError($"ランダムマッチング失敗: {result.ShutdownReason}");
            OnRandomMatchFailed?.Invoke();
        }
    }

    protected virtual Task<StartGameResult> InitializeNetworkRunner(
        NetworkRunner runner,
        string sessionName,
        Fusion.GameMode gameMode,
        byte[] connectionToken,
        NetAddress address,
        int sceneIndex,
        RoomConfig config)
    {
        var sceneManager = GetSceneManager(runner);
        runner.ProvideInput = true;

        var sessionProperties = new Dictionary<string, SessionProperty>();
        if (config != null)
        {
            sessionProperties["password"] = config.Password;
            sessionProperties["hostComment"] = config.HostComment;
            sessionProperties["maxRounds"] = config.MaxRounds;
            sessionProperties["roomMode"] = (int)config.RoomMode;
        }

        return runner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            Address = address,
            Scene = sceneIndex,
            SessionName = sessionName,
            CustomLobbyName = "OurLobbyID",
            SceneManager = sceneManager,
            ConnectionToken = connectionToken,
            PlayerCount = config?.MaxPlayers ?? 4,
            SessionProperties = sessionProperties
        });
    }

    private INetworkSceneManager GetSceneManager(NetworkRunner runner)
    {
        var sceneManager = runner
            .GetComponents<MonoBehaviour>()
            .OfType<INetworkSceneManager>()
            .FirstOrDefault();

        return sceneManager ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
    }
}