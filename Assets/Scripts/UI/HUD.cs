using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;

    [Header("Warning Settings")]
    [SerializeField] private float yellowThreshold = 30f;  // 黄色になる残り時間
    [SerializeField] private float warningThreshold = 10f; // 点滅・拡大縮小開始の残り時間
    [SerializeField] private float maxScale = 1.03f;        // 最大拡大倍率

    private static readonly Color NormalColor = Color.white;
    private static readonly Color YellowColor = Color.yellow;
    private static readonly Color RedColor = Color.red;

    public static HUD Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.Current != EGameStateNet.Game) return;

        var timer = GameStateManager.Instance.GameRoundTimer;
        var runner = GameStateManager.Instance.Runner;

        float remaining = timer.RemainingTime(runner) ?? 120f;
        SetTimerText(remaining);
        UpdateWarningEffect(remaining);
    }

    private void UpdateWarningEffect(float remaining)
    {
        if (remaining < warningThreshold)
        {
            // ラスト10秒：赤↔黄色グラデーション点滅＋拡大縮小
            float t = (Mathf.Sin(Time.time * Mathf.PI * 2f) + 1f) / 2f;
            timeText.color = Color.Lerp(RedColor, YellowColor, t);

            float scale = 1f + t * (maxScale - 1f);
            timeText.transform.localScale = Vector3.one * scale;
        }
        else if (remaining < yellowThreshold)
        {
            // ラスト30秒：黄色固定
            timeText.color = YellowColor;
            timeText.transform.localScale = Vector3.one;
        }
        else
        {
            // 通常：白色
            timeText.color = NormalColor;
            timeText.transform.localScale = Vector3.one;
        }
    }

    public static void SetTimerText(float time)
    {
        if (Instance == null || Instance.timeText == null) return;
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        Instance.timeText.text = $"{minutes:00}:{seconds:00}";
    }
}