using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InGameScoreUIHandler : MonoBehaviour
{
    [SerializeField] private Transform scoreBoardParent;
    [SerializeField] private GameObject scoreEntryPrefab;

    Dictionary<PlayerRef, TextMeshProUGUI> scoreEntries = new Dictionary<PlayerRef, TextMeshProUGUI>();

    public void UpdateAllPlayerScores()
    {
        foreach (var kvp in NetworkPlayer.ActivePlayers)
        {
            PlayerRef playerRef = kvp.Key;
            NetworkPlayer player = kvp.Value;

            if (!scoreEntries.ContainsKey(playerRef))
            {
                var entry = Instantiate(scoreEntryPrefab, scoreBoardParent);
                scoreEntries[playerRef] = entry.GetComponent<TextMeshProUGUI>();
            }

            scoreEntries[playerRef].text = $"{player.nickName}: {player.score}";
        }
    }
}
