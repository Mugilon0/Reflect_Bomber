using System.IO;
using UnityEngine;

/// <summary>
/// PlayerSaveDataのJSON保存・読み込みを管理する静的クラス
/// 保存先: Application.persistentDataPath/savedata.json
/// セーブタイミング: ゲーム終了時のみ
/// </summary>
public static class SaveDataManager
{
    private static readonly string SaveFileName = "savedata.json";
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private static PlayerSaveData _cache = null;

    /// <summary>
    /// 現在のセーブデータを取得（キャッシュがあればそれを返す）
    /// </summary>
    public static PlayerSaveData Current
    {
        get
        {
            if (_cache == null)
                _cache = Load();
            return _cache;
        }
    }

    /// <summary>
    /// セーブデータをJSONファイルに保存する
    /// ゲーム終了時に呼ぶ
    /// </summary>
    public static void Save()
    {
        if (_cache == null)
        {
            Debug.LogWarning("SaveDataManager: セーブデータがありません");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(_cache, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"SaveDataManager: セーブ完了 → {SaveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveDataManager: セーブ失敗 → {e.Message}");
        }
    }

    /// <summary>
    /// JSONファイルからセーブデータを読み込む
    /// ファイルが存在しない場合はデフォルト値で新規作成する
    /// </summary>
    public static PlayerSaveData Load()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                _cache = JsonUtility.FromJson<PlayerSaveData>(json);
                Debug.Log($"SaveDataManager: ロード完了 → {SaveFilePath}");
            }
            else
            {
                Debug.Log("SaveDataManager: 初回起動。デフォルト値で新規作成します");
                _cache = new PlayerSaveData
                {
                    // デフォルトニックネームはPCのユーザー名を使用
                    // ゲーム内で変更した場合はUpdateNickName()で上書きされる
                    NickName = SystemInfo.deviceName
                };
                // 初回はまだ保存しない（ゲーム終了時に保存）
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveDataManager: ロード失敗 → {e.Message}。デフォルト値を使用します");
            _cache = new PlayerSaveData { NickName = SystemInfo.deviceName };
        }

        return _cache;
    }

    /// <summary>
    /// ゲーム終了時に呼ぶ
    /// セッション内データを長期データに累積してセーブする
    /// </summary>
    public static void CommitGameResult(int totalScore, int totalKills, int totalDeaths)
    {
        Current.TotalScore += totalScore;
        Current.TotalKills += totalKills;
        Current.TotalDeaths += totalDeaths;
        Save();
    }

    /// <summary>
    /// ニックネームを更新する（次回セーブ時に反映される）
    /// </summary>
    public static void UpdateNickName(string nickName)
    {
        Current.NickName = nickName;
    }

    /// <summary>
    /// キャッシュをクリアする（テスト用）
    /// </summary>
    public static void ClearCache()
    {
        _cache = null;
    }
}