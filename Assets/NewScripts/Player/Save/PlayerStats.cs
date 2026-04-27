using Fusion;

/// <summary>
/// セッション全体の累計スコアを管理するクラス
/// ラウンド終了ごとにPlayerScoreのデータを累積する
/// PostGame時にSaveDataManagerへ渡す
/// </summary>
public class PlayerStats : NetworkBehaviour
{
    [Networked] public int SessionScore { get; private set; }
    [Networked] public int SessionKills { get; private set; }
    [Networked] public int SessionDeaths { get; private set; }

    [Networked, Capacity(8)]
    public NetworkArray<int> RoundScores { get; }

    private int _currentRound = 0;

    /// <summary>
    /// ラウンド終了時にPlayerScoreのデータを累積する
    /// OutroStateから呼ぶ
    /// </summary>
    public void AddRoundResult(PlayerScore playerScore)
    {
        if (!Object.HasStateAuthority) return;

        SessionScore += playerScore.RoundScore;
        SessionKills += playerScore.RoundKills;
        SessionDeaths += playerScore.RoundDeaths;

        if (_currentRound < RoundScores.Length)
            RoundScores.Set(_currentRound, playerScore.RoundScore);

        _currentRound++;
    }

    /// <summary>
    /// 全ラウンド終了後にリセット
    /// PostGameStateから呼ぶ
    /// </summary>
    public void ClearAllData()
    {
        if (!Object.HasStateAuthority) return;

        SessionScore = 0;
        SessionKills = 0;
        SessionDeaths = 0;
        _currentRound = 0;

        for (int i = 0; i < RoundScores.Length; i++)
            RoundScores.Set(i, 0);
    }
}