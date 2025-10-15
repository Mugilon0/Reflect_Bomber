using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;
using System;
using UnityEngine.SocialPlatforms;
using AAMAP;

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
    //public bool is3rdPersonCamera { get; set; }


    // Start is called before the first frame update
    //void Start()
    //{

    //}
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
                //Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel")); //　レイヤーを設定　目はレンダリングされなくなる

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

        if (Object.HasInputAuthority)
        {
            // InterfaceManagerが管理する送信ボタンのonPressedイベントに、自分のOnSendメソッドを登録（購読）
            if (InterfaceManager.Instance != null && InterfaceManager.Instance.readyMessageButtonSend != null)
            {
                InterfaceManager.Instance.readyMessageButtonSend.onPressed += OnSend;
            }
        }





        //var mapIcon = GetComponentInChildren<MapIcon>();

        //// もしMapIconが見つかったら
        //if (mapIcon != null)
        //{
        //    // シーン内から「Minimap Camera」という名前のゲームオブジェクトを探す
        //    var minimapCameraObject = GameObject.Find("Minimap Camera");

        //    // もしMinimap Cameraが見つかったら
        //    if (minimapCameraObject != null)
        //    {
        //        // MapIconのMinimap Camera欄に、見つけてきたカメラを自動で設定する
        //        mapIcon.SetMinimapCamera(minimapCameraObject);
        //    }
        //    else
        //    {
        //        // 見つからなかった場合は、エラーメッセージをコンソールに表示する
        //        Debug.LogError("Minimap Cameraがシーンに見つかりませんでした。");
        //    }

        //    // (もし全画面マップも使うなら、Map Cameraも同様に探して設定します)
        //    // var mapCameraObject = GameObject.Find("Map Camera");
        //    // if (mapCameraObject != null)
        //    // {
        //    //     mapIcon.SetMapCamera(mapCameraObject);
        //    // }
        //}
        //else
        //{
        //    Debug.LogError("プレイヤープレハブの子にMapIconが見つかりませんでした。");
        //}


        //// ★★★ ミニマップに自分を追跡ターゲットとして設定する ★★★
        //// シーン内にあるMinimapManagerコンポーネントを探す
        //var minimapManager = FindObjectOfType<MinimapManager>();

        //// もしMinimapManagerが見つかったら
        //if (minimapManager != null)
        //{
        //    // 追跡ターゲット(Target Object)に自分自身(このプレイヤーオブジェクト)を設定する
        //    minimapManager.SetTargetObject(this.gameObject);
        //}
        //else
        //{
        //    Debug.LogError("Minimapがシーンに見つかりませんでした。");
        //}


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


    // Ready内におけるメッセージ送受信機能
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        // メッセージは16文字まで
        if (message.Length > 16)
            message = message.Substring(0, 16);

        if (GameStateManager.Instance != null)
        {
            // "this.nickName" を使って、送信者の名前を直接取得します。
            Debug.Log($"Player {this.nickName} sent message: {message}");

            // GameStateManagerの中継RPCを呼び出します。
            GameStateManager.Instance.RPC_RelayChatMessage(this.nickName.ToString(), message);
        }
        else
        {
            Debug.LogError("GameStateManager.Instance is null. Cannot relay chat message.");
        }
    }

    private void OnSend()
    {
        // 自分（入力権限を持つプレイヤー）からの呼び出しでなければ、何もしない
        // ※このチェックは、Spawnedでローカルプレイヤーのみがイベント登録するため、厳密には不要ですが、安全のために入れておくと良いです。
        if (!Object.HasInputAuthority)
        {
            return;
        }

        // InterfaceManager経由で入力欄のテキストを取得
        string message = InterfaceManager.Instance.readyMessageInputField.text;

        // 入力が空でなければRPCを呼び出す
        if (!string.IsNullOrWhiteSpace(message))
        {
            RPC_SendMessage(message);

            // 送信後、入力欄をクリアする
            InterfaceManager.Instance.readyMessageInputField.text = "";
        }
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

        if (Object != null && Object.HasInputAuthority)
        {
            if (InterfaceManager.Instance != null && InterfaceManager.Instance.readyMessageButtonSend != null)
            {
                InterfaceManager.Instance.readyMessageButtonSend.onPressed -= OnSend;
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
        bool isWorld1Scene = scene.name == "World1";

        if (Object.HasInputAuthority) // ローカルプレイヤーの処理
        {
            if (isReadyScene)
            {
                // Readyシーンに戻ってきた場合などの設定
                localCameraHandler.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if(isWorld1Scene) // "World1" などのゲームシーンでの設定
            {
                //Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));
                if (Camera.main != null) Camera.main.gameObject.SetActive(false);

                var audioListener = GetComponentInChildren<AudioListener>(true);
                if (audioListener != null) audioListener.enabled = true;

                localCameraHandler.localCamera.enabled = true;
                localCameraHandler.gameObject.SetActive(true);
                if (localCameraHandler.transform.parent != null) localCameraHandler.transform.parent = null;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;



                // 以下Miinimapの参照に関する処理
                // DontDestroyOnLoadで持ち越されたInterfaceManagerを探す
                InterfaceManager interfaceManager = FindObjectOfType<InterfaceManager>();
                if (interfaceManager == null)
                {
                    Debug.LogError("InterfaceManagerが見つかりません！");
                    return;
                }

                // InterfaceManagerの中から、非アクティブなMinimapManagerを探す
                MinimapManager minimapManager = interfaceManager.GetComponentInChildren<MinimapManager>(true);
                if (minimapManager == null)
                {
                    Debug.LogError("InterfaceManager内にMinimapが見つかりません！");
                    return;
                }

                // 自分の子からMapIconを探す
                MapIcon mapIcon = GetComponentInChildren<MapIcon>();
                if (mapIcon == null)
                {
                    Debug.LogError("プレイヤープレハブの子にMapIconが見つかりません！");
                    return;
                }

                // World1シーンから「Minimap Camera」を探す
                GameObject minimapCameraObject = GameObject.Find("Minimap Camera");
                if (minimapCameraObject == null)
                {
                    Debug.LogError("World1シーンにMinimap Cameraが見つかりません！");
                    return;
                } else
                {
                    Debug.Log("Minimap cameraをみつけました");
                }

                    // --- すべての部品が見つかったので、接続を開始 ---

                    // 1. MapIconにカメラを教える
                    mapIcon.SetMinimapCamera(minimapCameraObject);

                // 2. ミニマップに追跡ターゲット（自分自身）を教える
                minimapManager.SetTargetObject(this.gameObject);

                minimapManager.SetCamera(minimapCameraObject);


                Debug.Log("ミニマップの接続が完了しました！");


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