using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer Local { get; set; } // 入力権限があるプレイヤーはローカル



    // Start is called before the first frame update
    void Start()
    {

    }

    public override void Spawned() //プレイヤーが生成された場合に呼び出される
    {
        if (Object.HasInputAuthority) // これないと全てのクライアントで実行される1
        {
            Local = this; // ローカルプレイヤーの参照
            Debug.Log("Spawned local player");
        }
        else
        {
            Debug.Log("Spawned remote player"); //そうでないならリモートプレイヤー
        }
    }

    public void PlayerLeft(PlayerRef player) // プレイヤーが離れるときの処理
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}
