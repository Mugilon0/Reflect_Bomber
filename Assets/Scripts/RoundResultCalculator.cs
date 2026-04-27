using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ラウンド終了時のスコア計算・順位決定を行う静的クラス
/// FusionGolfのGameManager.CalculateScores()を参考に設計
/// 
/// 責務：
/// - サーバー側：各プレイヤーの現在スコアをRoundScoresに保存
/// - 全クライアント：表示用のソート済みリザルトデータを生成
/// 
/// 使用例（GameStateManager.Outro.onEnterで呼び出す）：
/// if (Runner.IsServer)
/// {
///     RoundResultCalculator.Server_FinalizeRoundScores(currentRound);
/// }
/// var results = RoundResultCalculator.GenerateResultData(currentRound);
/// </summary>
public static class RoundResultCalculator
{
    /// <summary>
    /// ラウンド終了時にサーバーが呼び出す
    /// 各プレイヤーの現在のscoreをRoundScores[roundIndex]に保存する
    /// NetworkArrayなので自動的に全クライアントに同期される
    /// 
    /// FusionGolfでの対応箇所：
    /// kvp.Key.Scores.Set(Instance.CurrentHole, kvp.Value);
    /// </summary>
    /// <param name="roundIndex">現在のラウンドインデックス（0始まり）</param>
    /// 



    // いったんオフにする 3/25
    //public static void Server_FinalizeRoundScores(int roundIndex)
    //{
    //    Debug.Log($"[RoundResultCalculator] Server_FinalizeRoundScores called for round {roundIndex}");

    //    PlayerRegistry.ForEach(player =>
    //    {
    //        // 現在のラウンドスコアをNetworkArrayに保存
    //        player.RoundScores.Set(roundIndex, player.score);
    //        Debug.Log($"  {player.nickName}: score={player.score} -> RoundScores[{roundIndex}]");
    //    });
    //}

    /// <summary>
    /// UI表示用のリザルトデータを生成
    /// TotalScore降順でソートし、順位を付ける
    /// 同スコアのプレイヤーは同順位になる（FusionGolfのstrokeRanks方式）
    /// 
    /// FusionGolfでの対応箇所：
    /// PlayerRegistry.OrderDesc(p => p.TotalScore)
    /// </summary>
    /// <param name="currentRound">現在のラウンドインデックス（0始まり）</param>
    /// <returns>順位順にソートされたリザルトデータのリスト</returns>
    //public static List<RoundResultData> GenerateResultData(int currentRound) // いったんオフにしています 2026.3.14
    //{
    //    var results = new List<RoundResultData>();

    //    // 全プレイヤーのデータを収集
    //    foreach (var player in PlayerRegistry.AllPlayers)
    //    {
    //        int roundScore = 0;

    //        // RoundScoresが有効な場合のみ取得
    //        if (currentRound >= 0 && currentRound < player.RoundScores.Length)
    //        {
    //            roundScore = player.RoundScores[currentRound];
    //        }

    //        results.Add(new RoundResultData
    //        {
    //            PlayerRef = player.Object.InputAuthority,
    //            PlayerName = player.nickName.ToString(),
    //            RoundScore = roundScore,
    //            TotalScore = player.TotalScore,
    //            Rank = 0  // 後で設定
    //        });
    //    }

    //    // TotalScore降順でソート（高スコアが上位）
    //    results = results.OrderByDescending(r => r.TotalScore).ToList();

    //    // 順位を設定（同スコアは同順位 - FusionGolfのstrokeRanks方式）
    //    AssignRanks(results);

    //    return results;
    //}

    /// <summary>
    /// 同スコアは同順位になるよう順位を設定
    /// FusionGolfのstrokeRanks計算ロジックを参考
    /// 
    /// 例：スコア [100, 80, 80, 50] → 順位 [1, 2, 2, 4]
    /// </summary>
    private static void AssignRanks(List<RoundResultData> results)
    {
        if (results.Count == 0) return;

        int currentRank = 1;
        int previousScore = int.MaxValue;

        for (int i = 0; i < results.Count; i++)
        {
            var data = results[i];

            // スコアが前のプレイヤーより低い場合のみ順位を更新
            if (data.TotalScore < previousScore)
            {
                currentRank = i + 1;
            }

            data.Rank = currentRank;
            previousScore = data.TotalScore;
            results[i] = data;  // structなので再代入が必要
        }
    }

    /// <summary>
    /// デバッグ用：リザルトデータをログ出力
    /// </summary>
    public static void DebugLogResults(List<RoundResultData> results, int roundIndex)
    {
        Debug.Log($"=== Round {roundIndex + 1} Results ===");
        foreach (var r in results)
        {
            Debug.Log($"  #{r.Rank} {r.PlayerName}: RND={r.RoundScore}, TOTAL={r.TotalScore}");
        }
        Debug.Log("=============================");
    }
}