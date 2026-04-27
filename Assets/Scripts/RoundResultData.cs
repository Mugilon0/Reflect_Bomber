using Fusion;
using UnityEngine;

/// <summary>
/// 1人のプレイヤーのリザルト表示用データ
/// FusionGolfのPlayerScoreを参考に、UI表示用の一時的なデータ構造として設計
/// ネットワーク同期は不要（表示時にNetworkPlayerから生成する）
/// 
/// 使用例：
/// var results = RoundResultCalculator.GenerateResultData(currentRound);
/// foreach (var data in results)
/// {
///     Debug.Log($"#{data.Rank} {data.PlayerName}: RND={data.RoundScore}, TOTAL={data.TotalScore}");
/// }
/// </summary>
public struct RoundResultData
{
    /// <summary>順位（1〜、同スコアは同順位）</summary>
    public int Rank;

    /// <summary>プレイヤー名</summary>
    public string PlayerName;

    /// <summary>そのラウンドで取得したスコア</summary>
    public int RoundScore;

    /// <summary>累積スコア（全ラウンドの合計）</summary>
    public int TotalScore;

    /// <summary>プレイヤー識別用のPlayerRef</summary>
    public PlayerRef PlayerRef;
}