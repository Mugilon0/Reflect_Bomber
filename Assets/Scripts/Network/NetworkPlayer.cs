using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer Local { get; set; } // 入力権限があるプレイヤーはローカル

    public static Dictionary<PlayerRef, NetworkPlayer> ActivePlayers = new Dictionary<PlayerRef, NetworkPlayer>();

    public Transform playerModel; // レイヤーを変えるのにtransformにアクセスする必要がある

    //public CinemachineVirtualCamera virtualcamera;

    [Networked(OnChanged = nameof(OnNickNameChanged))]  // ←ちゃんと実装しなければ有効にします！！！！！！！！！
    public NetworkString<_16> nickName { get; set; } // 最大サイズ16



    // Camera mode 
    public bool is3rdPersonCamera { get; set; }


    // Start is called before the first frame update
    void Start()
    {

    }

    public override void Spawned() //プレイヤーが生成された場合に呼び出される
    {
        if (Object.HasInputAuthority) // これないと全てのクライアントで実行される1
        {
            Local = this; // ローカルプレイヤーの参照

            // Sets the layer of the local players model
            Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel")); //　レイヤーを設定　目はレンダリングされなくなる

            // Disable main camera
            Camera.main.gameObject.SetActive(false); //ローカルカメラを使用するときはメインカメラは無効にする

            RPC_SetNickName(PlayerPrefs.GetString("PlayerNickName")); // rpcを送信する

            Debug.Log("Spawned local player");
        }
        else
        {
            // Disable the camera if we are not the local player
            Camera localCamera = GetComponentInChildren<Camera>(); // ローカルカメラにアクセスし、子でカメラを取得
            localCamera.enabled = false; // 5.11 // リモートプレイヤーでは無効にする

            // Only 1 audio listener is allowed in the scene so disable remote players audio listener 
            AudioListener audioListener = GetComponentInChildren<AudioListener>();  // 音についても上と同様
            audioListener.enabled = false;


            Debug.Log("Spawned remote player"); //そうでないならリモートプレイヤー
        }

        // プレイヤーの右側を定義するため 4/17
        ActivePlayers[Object.InputAuthority] = this;

        // Make it easier to tell which player is which
        transform.name = $"P_{Object.Id}"; // 生成されるプレイヤーの名前を変更
    }

    public void PlayerLeft(PlayerRef player) // プレイヤーが離れるときの処理
    {
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
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] //入力者→サーバー rpcを受信する
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"RPC SetNickName { nickName}");
        this.nickName = nickName;
    }

}
