using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// ラウンド終了時のリザルトUI
/// SessionScore降順で順位を表示する
/// 順位ごとに異なるItemプレハブを使用する
/// </summary>
public class OutroUIHandler : MonoBehaviour
{
    [Header("Result Table")]
    [SerializeField] private Transform itemParent;

    [Tooltip("1位～4位のItemプレハブ（インデックス0=1位, 1=2位, 2=3位, 3=4位）")]
    [SerializeField] private GameObject[] rankItemPrefabs; // 4つ

    private readonly List<GameObject> _items = new List<GameObject>();

    private void OnEnable()
    {
        Debug.Log("OutroUIHandler.OnEnableを呼んだ");
        RefreshResults();
    }

    private void OnDisable()
    {
        ClearItems();
    }

    public void RefreshResults()
    {
        // OutroStateでない場合はスキップ
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.Current != EGameStateNet.Outro) return;

        ClearItems();

        if (PlayerRegistry.Instance == null) return;
        if (rankItemPrefabs == null || rankItemPrefabs.Length == 0) return;

        // SessionScore降順でソート
        var sortedPlayers = PlayerRegistry.Players
            .OrderByDescending(p => p.GetComponent<PlayerStats>()?.SessionScore ?? 0)
            .ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var player = sortedPlayers[i];
            var playerStats = player.GetComponent<PlayerStats>();
            if (playerStats == null) continue;

            // 順位に対応するプレハブを選択（範囲外ならの最後のプレハブを使用）
            int prefabIndex = Mathf.Min(i, rankItemPrefabs.Length - 1);
            var prefab = rankItemPrefabs[prefabIndex];
            if (prefab == null) continue;

            var item = Instantiate(prefab, itemParent);
            _items.Add(item);

            // ItemのTMP_Textを左から順にRank・PlayerName・Scoreに設定
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();               // Rank
                texts[1].text = player.nickName.Value;             // PlayerName
                texts[2].text = playerStats.SessionScore.ToString(); // SessionScore
            }
        }
    }

    private void ClearItems()
    {
        foreach (var item in _items)
        {
            if (item != null)
                Destroy(item);
        }
        _items.Clear();
    }
}