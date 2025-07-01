using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;
using System;
using UnityEngine.SocialPlatforms;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer Local { get; set; } // 入力権限があるプレイヤーはローカル

    public static Dictionary<PlayerRef, NetworkPlayer> ActivePlayers = new Dictionary<PlayerRef, NetworkPlayer>();

    public Transform playerModel; // レイヤーを変えるのにtransformにアクセスする必要がある

    //public CinemachineVirtualCamera virtualcamera;

    [Networked(OnChanged = nameof(OnNickNameChanged))]  // ←ちゃんと実装しなければ有効にします！！！！！！！！！
    public NetworkString<_16> nickName { get; set; } // 最大サイズ16

    [Networked(OnChanged = nameof(OnScoreChanged))]
    public int score { get; set; } = 0;

    private InGameScoreUIHandler scoreUIHandler;

    // Remote Client Token Hash
    [Networked] public int token { get; set; }

    public LocalCameraHandler localCameraHandler;

    // Camera mode 
    public bool is3rdPersonCamera { get; set; }


    // Start is called before the first frame update
    void Start()
    {

    }
    private void Awake()
    {
        // このオブジェクトがシーン遷移で破棄されないようにする
        DontDestroyOnLoad(this.gameObject);
    }

    public override void Spawned() //プレイヤーが生成された場合に呼び出される
    {
        bool isReadyScene = SceneManager.GetActiveScene().name == "Ready";
        if (Object.HasInputAuthority) // これないと全てのクライアントで実行される1
        {
            Local = this; // ローカルプレイヤーの参照

            if (isReadyScene)
            {
                Camera.main.transform.position = new Vector3(transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);

                //Disable local camera
                localCameraHandler.gameObject.SetActive(false);

                //Disable UI for local player
                //localUI.SetActive(false);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Sets the layer of the local players model
                Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel")); //　レイヤーを設定　目はレンダリングされなくなる

                // Disable main camera
                if (Camera.main != null)
                    Camera.main.gameObject.SetActive(false); //ローカルカメラを使用するときはメインカメラは無効にする

                //Enabled 1 audio listner
                AudioListener audioListner = GetComponentInChildren<AudioListener>(true);
                audioListner.enabled = true;


                // Enabled the local camera
                localCameraHandler.localCamera.enabled = true;
                localCameraHandler.gameObject.SetActive(true);

                // Detach camera if enabled
                localCameraHandler.transform.parent = null;

                //localUI.SetActive(true);  // 4/26 ep5 45:37

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }


            RPC_SetNickName(GameManager.instance.playerNickName); // rpcを送信する

            // tps視点で自分の名前が見えないようにする
            playerNickNameTM.gameObject.SetActive(false);

            Debug.Log("Spawned local player");
        }
        else
        {

            // Enabled the local camera
            localCameraHandler.localCamera.enabled = false;

            //localUI.SetActive(false); //UIもけす

            // Only 1 audio listener is allowed in the scene so disable remote players audio listener 
            AudioListener audioListener = GetComponentInChildren<AudioListener>();  // 音についても上と同様
            audioListener.enabled = false;


            Debug.Log("Spawned remote player"); //そうでないならリモートプレイヤー
        }



        // プレイヤーの右側を定義するため 4/17
        ActivePlayers[Object.InputAuthority] = this;

        // Make it easier to tell which player is which
        transform.name = $"P_{Object.Id}"; // 生成されるプレイヤーの名前を変更

        scoreUIHandler = FindObjectOfType<InGameScoreUIHandler>(true);
        //if (scoreUIHandler != null) // OnNickNameChanged()内で呼び出すので不要になった
        //    scoreUIHandler.UpdateAllPlayerScores(); // 初期スコアの表示もここでできる
    }

    public void PlayerLeft(PlayerRef player) // プレイヤーが離れるときの処理
    {
        //if (Object.HasStateAuthority)
        //{
        //    if (Runner.TryGetPlayerObject(player, out NetworkObject playerLeftNetworkObject))
        //        Local.GetComponent<NetworkInGameMessages>().SendInGameRPCMessage(playerLeftNetworkObject.GetComponent<NetworkPlayer>().nickName.ToString(), "left");
        //}  //  メッセージUIはないので省略


        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }

    static void OnNickNameChanged(Changed<NetworkPlayer> changed) // 呼び出されるのはこっち
    {
        Debug.Log($"{Time.time} OnHPChanged value {changed.Behaviour.nickName}");

        changed.Behaviour.OnNickNameChanged();
    }


    private void OnNickNameChanged()
    {
        Debug.Log($"Nickname changed for player to {nickName} for player {gameObject.name}");

        playerNickNameTM.text = nickName.ToString();

        if (scoreUIHandler != null)
            scoreUIHandler.UpdateAllPlayerScores();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] //入力者→サーバー rpcを受信する
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"RPC SetNickName {nickName}");
        this.nickName = nickName;
    }

    void OnDestroy()
    {
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);

        // ActivePlayers から自身を削除
        if (Object != null && Object.InputAuthority.IsValid && ActivePlayers.ContainsKey(Object.InputAuthority))
        {
            if (ActivePlayers[Object.InputAuthority] == this)
            {
                ActivePlayers.Remove(Object.InputAuthority);
            }
        }
    }

    // ★★★ 修正点 3: OnEnable/OnDisable/OnSceneLoaded を修正 ★★★
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoadedを呼びます");
        if (Object == null || !Object.IsValid) return;

        Debug.Log($"NetworkPlayer {Object.Id}: OnSceneLoaded for scene '{scene.name}'.");

        // 新しいシーン用の初期化処理 (Spawnedのロジックを参考に再整理)
        bool isReadyScene = scene.name == "Ready";

        if (Object.HasInputAuthority) // ローカルプレイヤーの処理
        {
            if (isReadyScene)
            {
                // Readyシーンに戻ってきた場合などの設定
                localCameraHandler.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else // "World1" などのゲームシーンでの設定
            {
                Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));
                if (Camera.main != null) Camera.main.gameObject.SetActive(false);

                var audioListener = GetComponentInChildren<AudioListener>(true);
                if (audioListener != null) audioListener.enabled = true;

                localCameraHandler.localCamera.enabled = true;
                localCameraHandler.gameObject.SetActive(true);
                if (localCameraHandler.transform.parent != null) localCameraHandler.transform.parent = null;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else // リモートプレイヤーの処理
        {
            localCameraHandler.localCamera.enabled = false;
            var audioListener = GetComponentInChildren<AudioListener>(true);
            if (audioListener != null) audioListener.enabled = false;
        }

        // ゲームシーンに入ったらリスポーンを要求
        if (!isReadyScene && Object.HasStateAuthority)
        {
            var ch = GetComponent<CharacterMovementHandler>();
            if (ch != null)
            {
                ch.RequestRespawn();
            }
            else
            {
                Debug.LogError($"CharacterMovementHandler not found on player {Object.Id}");
            }
        }
    }

    static void OnScoreChanged(Changed<NetworkPlayer> changed)
    {
        changed.Behaviour.OnScoreChanged();
    }

    private void OnScoreChanged()
    {
        //Debug.Log($"スコアが変更されました: {score}");

        //if (Object.HasInputAuthority)
        //{
        //    // ローカルのみがUI更新するようにする
        //    GameManager.instance.inGameScoreUIHandler.UpdateScoreboard();
        //}
        //if (Object.HasInputAuthority)
        //{

        if (scoreUIHandler != null)
            scoreUIHandler.UpdateAllPlayerScores();
        //}

    }

    // NetworkPlayer.cs
    //public void InitializeForNewScene()
    //{
    //    Debug.Log($"NetworkPlayer {Object.Id}: InitializeForNewScene called in scene {SceneManager.GetActiveScene().name}.");

    //    if (Object == null || !Object.IsValid) return;

    //    if (SceneManager.GetActiveScene().name != "Ready")
    //    {
    //        if (Object.HasStateAuthority && Object.HasInputAuthority)
    //        {
    //            Spawned(); // カメラやUIなどローカル設定の再適用
    //        }
    //        if (Object.HasStateAuthority)
    //        {
    //            GetComponent<CharacterMovementHandler>()?.RequestRespawn(); // リスポーン要求
    //        }
    //    }
    //}





}