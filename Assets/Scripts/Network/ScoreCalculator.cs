using UnityEngine;
using Fusion;

public class ScoreCalculator : NetworkBehaviour
{
    // 参照するコンポーネント
    private NetworkPlayer networkPlayer;

    private void Awake()
    {
        // 必要なコンポーネントをあらかじめ取得しておく
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    /// <summary>
    /// 他のプレイヤーを倒した時にスコアを加算する
    /// </summary>
    public void OnKill()
    {
        // サーバー以外では何もしない
        //if (!Object.HasStateAuthority) return;

        // 自爆でなければスコアを加算
        // (このメソッドを呼び出す側で自爆チェックをするので、ここでは不要)
        networkPlayer.score++;
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
            // 新しいルール：スコアを3で割った商に1を足すと、ペナルティ点数になる
            // (例: スコア6 -> 6/3 + 1 = 3点減点)
            // (例: スコア9 -> 9/3 + 1 = 4点減点)
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
}