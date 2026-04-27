using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class SessionInfoListUIItem : MonoBehaviour
{
    [Header("必須項目")]
    [SerializeField] private TextMeshProUGUI sessionNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    [Header("任意項目（未設定でもエラーにならない）")]
    [SerializeField] private GameObject lockIcon;         // Privateなら表示
    [SerializeField] private TextMeshProUGUI roundsText;  // ラウンド数
    [SerializeField] private TextMeshProUGUI commentText; // ホストコメント

    private SessionInfo _sessionInfo;

    public event Action<SessionInfo> OnJoinSession;

    public void SetInfomation(SessionInfo sessionInfo)
    {
        _sessionInfo = sessionInfo;

        // セッション名
        if (sessionNameText != null)
            sessionNameText.text = sessionInfo.Name;

        // 参加人数
        if (playerCountText != null)
            playerCountText.text = $"{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}";

        // 満員ならJoinボタンを非表示
        if (joinButton != null)
            joinButton.gameObject.SetActive(sessionInfo.PlayerCount < sessionInfo.MaxPlayers);

        // パスワードの有無（鍵マーク）
        if (lockIcon != null)
        {
            bool isPrivate = sessionInfo.Properties.TryGetValue("roomMode", out var roomMode)
                && (int)roomMode == (int)RoomMode.Private;
            lockIcon.SetActive(isPrivate);
        }

        // ラウンド数
        if (roundsText != null)
        {
            if (sessionInfo.Properties.TryGetValue("maxRounds", out var maxRounds))
                roundsText.text = $"{maxRounds} Rounds";
            else
                roundsText.gameObject.SetActive(false);
        }

        // ホストコメント
        if (commentText != null)
        {
            if (sessionInfo.Properties.TryGetValue("hostComment", out var comment)
                && !string.IsNullOrEmpty(comment.ToString()))
                commentText.text = comment.ToString();
            else
                commentText.gameObject.SetActive(false);
        }
    }

    public void OnClick()
    {
        OnJoinSession?.Invoke(_sessionInfo);
    }
}