using System;

/// <summary>
/// プレイヤーの長期保存データ
/// ロジックなし・データのみ
/// SaveDataManager経由でJSON保存・読み込みを行う
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public string NickName = "Player";
    public int TotalScore = 0;
    public int TotalKills = 0;
    public int TotalDeaths = 0;

    // 将来追加予定
    // public int TotalWins = 0;
    // public int GamesPlayed = 0;
}
