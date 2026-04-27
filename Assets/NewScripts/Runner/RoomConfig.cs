using System.Collections.Generic;
using UnityEngine;

public enum RoomMode
{
    Public,   // 公開マッチ
    Private,　  // パスワード制プライベートマッチ
    Solo      // 一人練習
}

public class RoomConfig
{
    // 基本項目
    public string SessionName { get; set; } = "MyRoom";
    public int MaxPlayers { get; set; } = 4;
    public string Password { get; set; } = "";
    public string HostComment { get; set; } = "";
    public int MaxRounds { get; set; } = 4;
    public RoomMode RoomMode { get; set; } = RoomMode.Public;

    // 将来の拡張用（リージョン、カスタムルールなど）
    public Dictionary<string, string> ExtraOptions { get; set; } = new Dictionary<string, string>();

    [SerializeField] private int roundDuration = 120; // 秒

}