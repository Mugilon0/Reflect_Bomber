using UnityEngine;

/// <summary>
/// Tabキーでスコアボードのトグル表示/非表示を切り替える
/// 初期状態は表示、Game中のみ有効
/// </summary>
public class ScoreboardController : MonoBehaviour
{
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private InGameScoreUIHandler scoreUIHandler;

    private void Start()
    {
        // 最初は表示
        ShowScoreboard();
    }

    private void Update()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.Current != EGameStateNet.Game) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (scoreboardPanel.activeSelf)
                HideScoreboard();
            else
                ShowScoreboard();
        }
    }

    private void ShowScoreboard()
    {
        scoreboardPanel.SetActive(true);
        scoreUIHandler.RefreshAllScores();
    }

    private void HideScoreboard()
    {
        scoreboardPanel.SetActive(false);
    }
}