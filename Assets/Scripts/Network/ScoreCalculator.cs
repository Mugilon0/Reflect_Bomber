using UnityEngine;
using Fusion;

public class ScoreCalculator : NetworkBehaviour
{
    // 参照するコンポーネント
    private NetworkPlayer networkPlayer;

    [Header("Score Popup Settings")]
    [SerializeField] private GameObject scorePopupPrefab;
    [SerializeField] private Vector3 popupOffset = new Vector3(1.5f, 2.0f, 0); // キャラの右上側にオフセット
    [SerializeField] private float popupDuration = 1.5f;
    private void Awake()
    {
        networkPlayer = GetComponent<NetworkPlayer>();
    }


    /// 他のプレイヤーを倒した時にスコアを加算する
    public void OnKill()
    {
        // サーバー以外では何もしない
        //if (!Object.HasStateAuthority) return;

        // 自爆でなければスコアを加算
        // (このメソッドを呼び出す側で自爆チェックをするので、ここでは不要)
        networkPlayer.score++;

        RPC_PlayPointGainSound();
        //RPC_ShowScorePopup(); --11/13
    }

    /// <summary>
    /// 死亡してリスポーンする時にスコアを減算する
    /// </summary>
    public void OnDeathPenalty()
    {
        // サーバー以外では何もしない
        if (!Object.HasStateAuthority) return;

        int currentScore = networkPlayer.score;
        int penalty = 0;

        // ルールに基づいて減点数を計算
        if (currentScore > 0)
        {
            penalty = (currentScore / 3) + 1;
        }

        //if-else文はこうも書けるらしい
        //if (currentScore > 0)
        //{
        //    penalty = 1 + (currentScore - 1) / 3;
        //    penalty = Mathf.Min(penalty, 3);
        //}

        // スコアを減算し、0未満にならないようにする
        networkPlayer.score = Mathf.Max(0, currentScore - penalty);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_PlayPointGainSound(RpcInfo info = default)
    {
        // このRPCは InputAuthority (操作権限を持つプレイヤー) にだけ届く
        // ここでAudioManagerを呼び出すことで、攻撃した本人のPCでのみ音が再生される
        AudioManager.Play("PointGainSFX", AudioManager.MixerTarget.SFX);
    }


    //[Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    //private void RPC_ShowScorePopup(RpcInfo info = default)
    //{
    //    ScorePopupManager.ShowPopup(transform.position, "+1", popupOffset);
    //}




}