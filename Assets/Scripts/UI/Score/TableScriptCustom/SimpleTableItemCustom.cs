namespace UIWidgets.Examples
{
    using System;
    using UnityEngine;

    [Serializable]
    public class SimpleTableItemCustom
    {
        [SerializeField]
        public string Field1; // RANK

        [SerializeField]
        public string Field2; // PLAYER

        [SerializeField]
        public string Field3; // RND

        [SerializeField]
        public string Field4; // TOTAL（追加）

        public override string ToString()
        {
            return string.Format("{0} | {1} | {2} | {3}", Field1, Field2, Field3, Field4);
        }
    }
}







//using UnityEngine;
//using TMPro;
//using Fusion;

//public class TableItemCustomI : NetworkBehaviour
//{
//    [Header("UI References")]
//    public TMP_Text rankText;
//    public TMP_Text playerNameText;
//    public TMP_Text roundScoreText;
//    public TMP_Text totalScoreText;

//    // NetworkData
//    [Networked] public PlayerRef OwnerRef { get; set; }
//    [Networked, Capacity(32)] public string PlayerName { get; set; }
//    [Networked] public int Rank { get; set; }
//    [Networked, Capacity(4)] public NetworkArray<int> RoundScores => default;
//    [Networked] public int TotalScore { get; set; }

//    private const int MAX_ROUNDS = 4;

//    public override void Spawned()
//    {
//        UpdateUI();
//    }

//    /// <summary>
//    /// プレイヤー情報で初期化
//    /// </summary>
//    public void Init(PlayerRef playerRef, string nickName)
//    {
//        OwnerRef = playerRef;
//        PlayerName = nickName;
//        UpdateUI();
//    }

//    /// <summary>
//    /// 特定ラウンドのスコアを設定
//    /// </summary>
//    public void SetRoundScore(int roundIndex, int score)
//    {
//        if (roundIndex < 0 || roundIndex >= MAX_ROUNDS) return;
//        RoundScores.Set(roundIndex, score);
//        CalculateTotalScore();
//        UpdateUI();
//    }

//    /// <summary>
//    /// 現在のラウンドスコアを取得
//    /// </summary>
//    public int GetRoundScore(int roundIndex)
//    {
//        if (roundIndex < 0 || roundIndex >= MAX_ROUNDS) return 0;
//        return RoundScores[roundIndex];
//    }

//    /// <summary>
//    /// 累計スコアを計算
//    /// </summary>
//    private void CalculateTotalScore()
//    {
//        int total = 0;
//        for (int i = 0; i < MAX_ROUNDS; i++)
//        {
//            total += RoundScores[i];
//        }
//        TotalScore = total;
//    }

//    /// <summary>
//    /// ランクを設定
//    /// </summary>
//    public void SetRank(int rank)
//    {
//        Rank = rank;
//        UpdateUI();
//    }

//    /// <summary>
//    /// UIを更新
//    /// </summary>
//    public void UpdateUI()
//    {
//        rankText.text = GetOrdinalString(Rank);
//        playerNameText.text = PlayerName;
//        totalScoreText.text = TotalScore.ToString();
//    }

//    /// <summary>
//    /// 特定ラウンドのスコアをUIに表示
//    /// </summary>
//    public void DisplayRoundScore(int roundIndex)
//    {
//        if (roundIndex < 0 || roundIndex >= MAX_ROUNDS)
//        {
//            roundScoreText.text = "-";
//            return;
//        }
//        roundScoreText.text = RoundScores[roundIndex].ToString();
//    }

//    /// <summary>
//    /// 順位を序数文字列に変換 (1st, 2nd, 3rd, 4th...)
//    /// </summary>
//    private string GetOrdinalString(int num)
//    {
//        if (num <= 0) return "-";

//        switch (num % 100)
//        {
//            case 11:
//            case 12:
//            case 13:
//                return num + "th";
//        }

//        switch (num % 10)
//        {
//            case 1: return num + "st";
//            case 2: return num + "nd";
//            case 3: return num + "rd";
//            default: return num + "th";
//        }
//    }
//}

//---------------------------






////using System;
//using UnityEngine;

//namespace UIWidgets.Examples
//{
//    /// <summary>
//    /// リザルトテーブルの1行分のデータ
//    /// SimpleTableItemを参考に4フィールドに拡張
//    /// </summary>
//    [Serializable]
//    public class ResultsTableItem
//    {

//        [SerializeField]
//        public string Rank;

//        [SerializeField]
//        public string PlayerName;

//        [SerializeField]
//        public string RoundScore;

//        [SerializeField]
//        public string TotalScore;

//        /// <summary>
//        /// デフォルトコンストラクタ
//        /// </summary>
//        public ResultsTableItem()
//        {
//            Rank = "";
//            PlayerName = "";
//            RoundScore = "";
//            TotalScore = "";
//        }

//        /// <summary>
//        /// RoundResultDataから生成するコンストラクタ
//        /// </summary>
//        public ResultsTableItem(RoundResultData data)
//        {
//            Rank = data.Rank.ToString();
//            PlayerName = data.PlayerName;
//            RoundScore = data.RoundScore.ToString();
//            TotalScore = data.TotalScore.ToString();
//        }

//        /// <summary>
//        /// 値を直接指定するコンストラクタ
//        /// </summary>
//        public ResultsTableItem(int rank, string playerName, int roundScore, int totalScore)
//        {
//            Rank = rank.ToString();
//            PlayerName = playerName;
//            RoundScore = roundScore.ToString();
//            TotalScore = totalScore.ToString();
//        }

//        public override string ToString()
//        {
//            return $"#{Rank} {PlayerName} | RND:{RoundScore} | TOTAL:{TotalScore}";
//        }
//    }
//}