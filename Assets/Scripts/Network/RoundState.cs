using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
//using Helpers.Linq;

public class RoundManager : NetworkBehaviour
{
    //[Networked] // (OnChanged = nameof(OnSessionConfigChanged))はいったん無視　ゲーム時間の選択肢
    //public int MaxTimeIndex { get; set; }
    public static float MaxTime => 120.0f;


    [Networked]
    public int TickStarted { get; set; }
    public static float Time => Instance?.Object?.IsValid == true // ラウンドの経過時間を計算する もしGameManagerがネットワーク上で有効なら、さらにタイマーが開始済みかチェックする。開始済みなら**経過時間（秒）**を計算して返し、まだなら0を返す。そもそもGameManagerが有効でなければ、問答無用で0を返す。
        ? (Instance.TickStarted == 0
            ? 0
            : (Instance.Runner.Simulation.Tick - Instance.TickStarted) * Instance.Runner.Simulation.DeltaTime)
        : 0;

    public static RoundManager Instance { get; private set; }

    public override void Spawned()
    {
        // シングルトンインスタンスとして自身を設定
        if (Instance == null)
        {
            Instance = this;
            // シーンをまたいで存在させる
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 既にインスタンスが存在する場合は、重複しないように自身を破棄する
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // 破棄される際に、シングルトンインスタンスをクリアする
        if (Instance == this)
        {
            Instance = null;
            //InterfaceManager.Instance.resultsScreen.Clear(); 未実装　いるかわからない
        }
    }


    //static void OnSessionConfigChanged(Changed<GameManager> changed)
    //{
    //    InterfaceManager.Instance.sessionScreen.UpdateSessionConfig();
    //}  // ゲーム時間をプレイヤーが決めるわけではないので省略







}
