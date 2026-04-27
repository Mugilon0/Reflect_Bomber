using UnityEngine;
using UIWidgets.Examples;

public class ResultUIScreen : UIScreen
{
    [Header("Scoreboard")]
    public SimpleTable table; // SimpleTableをアサイン

    /// <summary>
    /// リザルト画面を初期化
    /// </summary>
    public void Initialize()
    {
        table.DataSource.Clear();
    }

    /// <summary>
    /// プレイヤーのリザルトを追加
    /// </summary>
    public void AddPlayerResult(int rank, string nickName, int roundScore, int totalScore)
    {
        var item = new SimpleTableItem
        {
            Field1 = GetOrdinalString(rank),
            Field2 = nickName,
            Field3 = roundScore.ToString(),
            Field4 = totalScore.ToString()
        };
        table.DataSource.Add(item);
    }

    private string GetOrdinalString(int num)
    {
        if (num <= 0) return "-";

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch (num % 10)
        {
            case 1: return num + "st";
            case 2: return num + "nd";
            case 3: return num + "rd";
            default: return num + "th";
        }
    }
}