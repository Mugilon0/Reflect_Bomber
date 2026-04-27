using Fusion;
using UnityEngine;

/// <summary>
/// ラウンド中のスコアを管理するクラス
/// OnChangedでInGameScoreUIHandlerを自動更新する
/// </summary>
public class PlayerScore : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnRoundScoreChanged))]
    public int RoundScore { get; set; }

    [Networked]
    public int RoundKills { get; set; }

    [Networked]
    public int RoundDeaths { get; set; }

    public void ResetRoundData()
    {
        if (!Object.HasStateAuthority) return;
        RoundScore = 0;
        RoundKills = 0;
        RoundDeaths = 0;
    }

    static void OnRoundScoreChanged(Changed<PlayerScore> changed)
    {
        FindObjectOfType<InGameScoreUIHandler>()?.RefreshAllScores();
    }
}