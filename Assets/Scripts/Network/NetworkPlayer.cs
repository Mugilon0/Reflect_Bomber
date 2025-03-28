using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{

    public static NetworkPlayer Local { get; set; } // 入力権限があるプレイヤーはローカル

    public Transform playerModel; // レイヤーを変えるのにtransformにアクセスする必要がある

    //public CinemachineVirtualCamera virtualcamera;





    // Camera mode 



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
    }

    public void PlayerLeft(PlayerRef player) // プレイヤーが離れるときの処理
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}
