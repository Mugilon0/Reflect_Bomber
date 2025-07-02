using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;
using System.Linq;
// NetworkRunnerを起動、生成する

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner networkRunnerPrefab;

    NetworkRunner networkRunner;


    private void Awake()
    {
        NetworkRunner networkRunnerInScene = FindObjectOfType<NetworkRunner>();

        // If we already have a network runner in the scene then we should not create another one but rather use the existing one
        if (networkRunnerInScene != null)
            networkRunner = networkRunnerInScene;
    }


    // Start is called before the first frame update
    //void Start()
    //{
    //    if (networkRunner == null)
    //    {
    //        networkRunner = Instantiate(networkRunnerPrefab);
    //        networkRunner.name = "Network runner";

    //        if (SceneManager.GetActiveScene().name != "MainMenu")
    //        {
    //            var clientTask = InitializeNetworkRunner(networkRunner, "TestSession", GameMode.AutoHostOrClient, GameManager.instance.GetConnectionToken(), NetAddress.Any(), SceneManager.GetActiveScene().buildIndex, null);
    //        }

    //        Debug.Log($"Server NetworkRunner started");
    //    }


    //}

    private NetworkRunner GetRunner()
    {
        if (networkRunner == null)
        {
            networkRunner = FindObjectOfType<NetworkRunner>();
        }

        if (networkRunner == null)
        {
            networkRunner = Instantiate(networkRunnerPrefab);
        }
        return networkRunner;
    }




    public void StartHostMigration(HostMigrationToken hostMigrationToken)
    {
        // create a new Network runner, old one is being shut down
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.name = "Network runner -Migrated";

        var clientTask = InitializeNetworkRunnerHostMigration(networkRunner, hostMigrationToken);

        Debug.Log($"Host migration started");
    }

    INetworkSceneManager GetSceneManager(NetworkRunner runner)
    {
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneManager == null)
        {
            // Handler networked objects that already exits in the scene
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        return sceneManager;
    }


    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, string sessionName, GameMode gameMode, byte[] connectionToken, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized) // NetworkRunnerに渡す
    {
        var sceneManager = GetSceneManager(runner);

        runner.ProvideInput = true; //runnerに入力を設定する

        return runner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            Address = address,
            Scene = scene,
            SessionName = sessionName, //sessionNameから変更 4/26
            CustomLobbyName = "OurLobbyID", // 特定のゲームを行いたい場合に用いる
            Initialized = initialized,
            SceneManager = sceneManager,
            ConnectionToken = connectionToken,
            PlayerCount = 4 // added 5/2
        });

    }

    protected virtual Task InitializeNetworkRunnerHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) // NetworkRunnerに渡す
    {
        var sceneManager = GetSceneManager(runner);

        runner.ProvideInput = true; //runnerに入力を設定する

        return runner.StartGame(new StartGameArgs
        {
            //GameMode = gameMode, // トークンに含まれる
            //Address = address, // トークンに含まれる
            //Scene = scene, // トークンに含まれる
            //SessionName = sessionName,  // トークンに含まれる
            //Initialized = initialized,
            SceneManager = sceneManager, // Runnerを再開するのに必要な情報を含む
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = HostMigrationResume,
            ConnectionToken = GameManager.instance.GetConnectionToken()

            //ConnectionToken = connectionToken
        });

    }

    void HostMigrationResume(NetworkRunner runner)
    {
        Debug.Log($"HostMigrationResume started");

        // Get a reference for each Network object from the old Host
        foreach (var resumeNetworkObject in runner.GetResumeSnapshotNetworkObjects())
        {
            // Grab all the player objects, they have a NetworkCharacterControllerPrototypeCustom
            if (resumeNetworkObject.TryGetBehaviour<NetworkCharacterControllerPrototypeCustom>(out var characterController))
            {
                runner.Spawn(resumeNetworkObject, position: characterController.ReadPosition(), rotation: characterController.ReadRotation(), onBeforeSpawned: (runner, newNetworkObject) =>
                {
                    // 新しいオブジェクトに情報をコピーする
                    newNetworkObject.CopyStateFrom(resumeNetworkObject);


                    // Copy info state from old Behaviour to new behaviour
                    if (resumeNetworkObject.TryGetBehaviour<HPHandler>(out HPHandler oldHPHandler))
                    {
                        // HPも途中の状態から再開できるようにする
                        HPHandler newHPHandler = newNetworkObject.GetComponent<HPHandler>();
                        newHPHandler.CopyStateFrom(oldHPHandler);

                        newHPHandler.skipSettingStartValues = true; //HPの初期化に上書きされないようにする
                    }

                    // map the connection token with the new Network player
                    if (resumeNetworkObject.TryGetBehaviour<NetworkPlayer>(out var oldNetworkPlayer))
                    {
                        // store player token for reconnection
                        FindObjectOfType<Spawner>().SetConnectionTokenMapping(oldNetworkPlayer.token, newNetworkObject.GetComponent<NetworkPlayer>());
                    }

                });
            }
        }
        StartCoroutine(CleanUpHostMigrationCO());

        Debug.Log($"HostMigrationResume completed");
    }

    IEnumerator CleanUpHostMigrationCO()
    {
        yield return new WaitForSeconds(5.0f);

        FindObjectOfType<Spawner>().OnHostMigrationCleanUp();
    }

    public void OnJoinLobby(MainMenuUIHandler uiHandler)
    {
        NetworkRunner runner = GetRunner();
        var clientTask = JoinLobby(runner, uiHandler);
    }


    public async Task JoinLobby(NetworkRunner runner, MainMenuUIHandler uiHandler)
    {
        Debug.Log("ジョインロビー");

        string lobbyID = "OurLobbyID";

        var result = await runner.JoinSessionLobby(SessionLobby.Custom, lobbyID);

        // コールバックする
        if (!result.Ok)
        {
            Debug.LogError($"Unable to join lobby {lobbyID}");
            uiHandler?.OnLobbyJoinFailure(); // 失敗を通知
        }
        else
        {
            Debug.Log("JoinLobby ok");
            uiHandler?.OnLobbyJoinSuccess(); // 成功を通知
        }
    }


    public void CreateGame(string sessionName, string sceneName)
    {
        Debug.Log($"Create session {sessionName} scene {sceneName} build Index {SceneUtility.GetBuildIndexByScenePath($"scenes/{sceneName}")}");

        NetworkRunner runner = GetRunner();
        //Join existing game as a client
        var clientTask = InitializeNetworkRunner(networkRunner, sessionName, GameMode.Host, GameManager.instance.GetConnectionToken(), NetAddress.Any(), SceneUtility.GetBuildIndexByScenePath($"scenes/{sceneName}"), null);

    }

    public void JoinGame(SessionInfo sessionInfo)
    {
        Debug.Log($"Join session {sessionInfo.Name}");

        NetworkRunner runner = GetRunner();
        //Join existing game as a client
        var clientTask = InitializeNetworkRunner(networkRunner, sessionInfo.Name, GameMode.Client, GameManager.instance.GetConnectionToken(), NetAddress.Any(), SceneManager.GetActiveScene().buildIndex, null);

    }


}
