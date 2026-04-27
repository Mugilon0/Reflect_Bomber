using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Fusion;

/// <summary>
/// ゲーム中のスコアボードUI管理
/// PlayerScore.RoundScoreのOnChangedで自動更新される
/// </summary>
public class InGameScoreUIHandler : MonoBehaviour
{
    [SerializeField] private Transform scoreBoardParent;
    [SerializeField] private GameObject scoreEntryPrefab;

    private Dictionary<PlayerRef, TextMeshProUGUI> _scoreEntries = new Dictionary<PlayerRef, TextMeshProUGUI>();

    private void OnEnable()
    {
        RefreshAllScores();
    }

    private void OnDisable()
    {
        ClearEntries();
    }

    public void RefreshAllScores()
    {
        if (PlayerRegistry.Instance == null) return;

        PlayerRegistry.ForEach(p =>
        {
            var playerScore = p.GetComponent<PlayerScore>();
            if (playerScore == null) return;

            PlayerRef pRef = p.GetComponent<NetworkObject>().InputAuthority;

            if (!_scoreEntries.ContainsKey(pRef))
            {
                var entry = Instantiate(scoreEntryPrefab, scoreBoardParent);
                _scoreEntries[pRef] = entry.GetComponent<TextMeshProUGUI>();
            }

            string nickName = p.nickName.Value;
            int score = playerScore.RoundScore;
            _scoreEntries[pRef].text = $"{nickName}: {score}";
        });
    }

    private void ClearEntries()
    {
        foreach (var entry in _scoreEntries.Values)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }
        _scoreEntries.Clear();
    }
}