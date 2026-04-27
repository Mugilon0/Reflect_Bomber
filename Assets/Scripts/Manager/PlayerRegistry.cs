using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// プレイヤーコレクション管理クラス（汎用）
/// NetworkPlayer.ActivePlayersをラップし、汎用的なユーティリティメソッドを提供
/// 
/// 設計方針：
/// - ゲーム固有の処理は含めない
/// - 汎用的なイテレーション・検索・条件チェックのみ
/// - 他のプロジェクトでも再利用可能
/// 
/// 使用例：
/// - PlayerRegistry.ForEach(p => p.LockAllActions(999.0f));
/// - bool allReady = PlayerRegistry.All(p => p.IsReady);
/// - int readyCount = PlayerRegistry.CountWhere(p => p.IsReady);
/// </summary>
public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance { get; private set; }

    /// <summary>
    /// NetworkPlayer.ActivePlayersへの参照
    /// データはコピーせず、参照のみ（メモリ効率的、GC発生なし）
    /// </summary>
    private static Dictionary<PlayerRef, NetworkPlayer> Players => NetworkPlayer.ActivePlayers;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("PlayerRegistry initialized");
        }
        else
        {
            Debug.LogWarning("Duplicate PlayerRegistry detected. Destroying...");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Player Count

    /// <summary>
    /// 有効なプレイヤーの総数を取得
    /// nullチェックとIsValidチェックを自動で行う
    /// </summary>
    public static int CountValid
    {
        get
        {
            int count = 0;
            foreach (var kvp in Players)
            {
                if (IsValidPlayer(kvp.Value))
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// プレイヤーが存在するかチェック（0人ならfalse）
    /// </summary>
    public static bool HasPlayers => CountValid > 0;

    #endregion

    #region Core Iteration Methods

    /// <summary>
    /// 有効な全プレイヤーに対してアクションを実行
    /// nullチェックとIsValidチェックを自動で行う
    /// 
    /// 使用例：
    /// PlayerRegistry.ForEach(p => p.LockAllActions(999.0f));
    /// </summary>
    /// <param name="action">各プレイヤーに対して実行するアクション</param>
    public static void ForEach(System.Action<NetworkPlayer> action)
    {
        if (action == null)
        {
            Debug.LogWarning("PlayerRegistry.ForEach: action is null");
            return;
        }

        foreach (var kvp in Players)
        {
            if (IsValidPlayer(kvp.Value))
            {
                action(kvp.Value);
            }
        }
    }

    /// <summary>
    /// 条件に一致する全プレイヤーに対してアクションを実行
    /// 
    /// 使用例：
    /// PlayerRegistry.ForEachWhere(
    ///     match: p => !p.IsDead,
    ///     action: p => p.ApplyKnockback(Vector3.up * 5)
    /// );
    /// </summary>
    /// <param name="match">条件判定</param>
    /// <param name="action">実行するアクション</param>
    public static void ForEachWhere(System.Predicate<NetworkPlayer> match, System.Action<NetworkPlayer> action)
    {
        if (match == null || action == null)
        {
            Debug.LogWarning("PlayerRegistry.ForEachWhere: match or action is null");
            return;
        }

        foreach (var kvp in Players)
        {
            if (!IsValidPlayer(kvp.Value))
                continue;

            if (match(kvp.Value))
            {
                action(kvp.Value);
            }
        }
    }

    #endregion

    #region Condition Check Methods

    /// <summary>
    /// 全プレイヤーが条件を満たすかチェック
    /// プレイヤーが0人の場合はfalseを返す
    /// 
    /// 使用例：
    /// if (PlayerRegistry.All(p => p.IsReady))
    /// {
    ///     StartGame();
    /// }
    /// </summary>
    /// <param name="match">条件判定</param>
    /// <returns>全員が条件を満たす場合true</returns>
    public static bool All(System.Predicate<NetworkPlayer> match)
    {
        if (match == null)
        {
            Debug.LogWarning("PlayerRegistry.All: match is null");
            return false;
        }

        int validPlayerCount = 0;

        foreach (var kvp in Players)
        {
            if (!IsValidPlayer(kvp.Value))
                continue;

            validPlayerCount++;

            if (!match(kvp.Value))
                return false;
        }

        // 有効なプレイヤーが0人の場合もfalse
        return validPlayerCount > 0;
    }

    /// <summary>
    /// 条件を満たすプレイヤーが1人でも存在するかチェック
    /// 
    /// 使用例：
    /// if (PlayerRegistry.Any(p => p.score >= 20))
    /// {
    ///     Debug.Log("Someone has 20+ points!");
    /// }
    /// </summary>
    /// <param name="match">条件判定</param>
    /// <returns>条件を満たすプレイヤーが存在する場合true</returns>
    public static bool Any(System.Predicate<NetworkPlayer> match)
    {
        if (match == null)
        {
            Debug.LogWarning("PlayerRegistry.Any: match is null");
            return false;
        }

        foreach (var kvp in Players)
        {
            if (!IsValidPlayer(kvp.Value))
                continue;

            if (match(kvp.Value))
                return true;
        }

        return false;
    }

    #endregion

    #region Count Methods

    /// <summary>
    /// 条件を満たすプレイヤーの数をカウント
    /// 
    /// 使用例：
    /// int readyCount = PlayerRegistry.CountWhere(p => p.IsReady);
    /// Debug.Log($"{readyCount}/{PlayerRegistry.CountValid} players are ready");
    /// </summary>
    /// <param name="match">条件判定</param>
    /// <returns>条件を満たすプレイヤーの数</returns>
    public static int CountWhere(System.Predicate<NetworkPlayer> match)
    {
        if (match == null)
        {
            Debug.LogWarning("PlayerRegistry.CountWhere: match is null");
            return 0;
        }

        int count = 0;

        foreach (var kvp in Players)
        {
            if (!IsValidPlayer(kvp.Value))
                continue;

            if (match(kvp.Value))
                count++;
        }

        return count;
    }

    #endregion

    #region Player Access Methods

    /// <summary>
    /// PlayerRefからNetworkPlayerを安全に取得
    /// 無効なプレイヤーの場合はnullを返す
    /// 
    /// 使用例：
    /// NetworkPlayer player = PlayerRegistry.GetPlayer(playerRef);
    /// if (player != null)
    /// {
    ///     player.score += 10;
    /// }
    /// </summary>
    /// <param name="pRef">PlayerRef</param>
    /// <returns>有効なNetworkPlayer、または null</returns>
    public static NetworkPlayer GetPlayer(PlayerRef pRef)
    {
        if (!pRef.IsValid)
            return null;

        if (Players.TryGetValue(pRef, out var player))
        {
            if (IsValidPlayer(player))
                return player;
        }

        return null;
    }

    /// <summary>
    /// 条件に一致する最初のプレイヤーを取得
    /// 
    /// 使用例：
    /// NetworkPlayer topScorer = PlayerRegistry.First(p => p.score >= 10);
    /// if (topScorer != null)
    /// {
    ///     Debug.Log($"{topScorer.nickName} reached 10 points!");
    /// }
    /// </summary>
    /// <param name="match">条件判定</param>
    /// <returns>条件に一致するプレイヤー、または null</returns>
    public static NetworkPlayer First(System.Predicate<NetworkPlayer> match)
    {
        if (match == null)
        {
            Debug.LogWarning("PlayerRegistry.First: match is null");
            return null;
        }

        foreach (var kvp in Players)
        {
            if (!IsValidPlayer(kvp.Value))
                continue;

            if (match(kvp.Value))
                return kvp.Value;
        }

        return null;
    }

    #endregion

    #region Collection Methods

    /// <summary>
    /// 条件に一致する全プレイヤーのコレクションを取得
    /// foreach で使用可能
    /// 
    /// 使用例：
    /// foreach (var player in PlayerRegistry.Where(p => p.score > 5))
    /// {
    ///     Debug.Log($"{player.nickName}: {player.score}");
    /// }
    /// </summary>
    /// <param name="match">条件判定（nullの場合は全プレイヤー）</param>
    /// <returns>プレイヤーのコレクション</returns>
    public static IEnumerable<NetworkPlayer> Where(System.Predicate<NetworkPlayer> match = null)
    {
        foreach (var kvp in Players)
        {
            if (!IsValidPlayer(kvp.Value))
                continue;

            if (match == null || match(kvp.Value))
                yield return kvp.Value;
        }
    }

    /// <summary>
    /// 有効な全プレイヤーのコレクションを取得
    /// foreach で使用可能
    /// 
    /// 使用例：
    /// foreach (var player in PlayerRegistry.AllPlayers)
    /// {
    ///     Debug.Log(player.nickName);
    /// }
    /// </summary>
    public static IEnumerable<NetworkPlayer> AllPlayers => Where(null);

    #endregion

    #region Helper Methods

    /// <summary>
    /// プレイヤーが有効かチェック（内部用）
    /// nullチェック、Object存在チェック、IsValidチェックを行う
    /// </summary>
    private static bool IsValidPlayer(NetworkPlayer player)
    {
        return player != null && player.Object != null && player.Object.IsValid;
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// デバッグ用：現在のプレイヤー情報をログ出力
    /// 
    /// 使用例：
    /// PlayerRegistry.DebugLogPlayers();
    /// </summary>
    public static void DebugLogPlayers()
    {
        Debug.Log($"=== PlayerRegistry Debug ===");
        Debug.Log($"Total entries: {Players.Count}");
        Debug.Log($"Valid players: {CountValid}");

        int index = 0;
        foreach (var kvp in Players)
        {
            if (IsValidPlayer(kvp.Value))
            {
                Debug.Log($"Player {index}: {kvp.Value.nickName} (PlayerRef: {kvp.Key}, Score: {kvp.Value.score})");
                index++;
            }
        }

        Debug.Log($"=========================");
    }

    #endregion
}