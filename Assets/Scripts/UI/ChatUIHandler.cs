using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ChatUIHandler : MonoBehaviour
{
    // Inspectorから、チャット1行分のプレハブを設定
    [SerializeField] private GameObject messagePrefab;
    // Inspectorから、生成したメッセージプレハブの親となるオブジェクトを設定
    [SerializeField] private Transform logParent;

    // 表示するメッセージの最大数を設定
    [SerializeField] private int maxMessages = 4;

    // 生成したメッセージオブジェクトを管理するためのリスト
    private List<GameObject> messageList = new List<GameObject>();

    /// <summary>
    /// 新しいチャットメッセージをログに追加する
    /// </summary>
    public void AddNewMessage(string playerName, string message)
    {
        Debug.Log($"--- AddNewMessage Called ---");
        Debug.Log($"Current message count (before add): {messageList.Count}");
        Debug.Log($"Max messages allowed: {maxMessages}");

        if (messagePrefab == null || logParent == null) return;

        // メッセージ数が最大を超えていたら、一番古いものから削除する
        if (messageList.Count >= maxMessages)
        {
            Destroy(messageList[0]);
            messageList.RemoveAt(0);
        }
        // メッセージプレハブを生成
        var messageInstance = Instantiate(messagePrefab, logParent);
        messageList.Add(messageInstance);

        // TextMeshProコンポーネントを取得して、テキストを設定
        var messageText = messageInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (messageText != null)
        {
            if (playerName == "System")
            {
                // システムメッセージの場合は、特別な色でメッセージだけを表示
                messageText.text = $"<i><color=yellow>{message}</color></i>";
            }
            else
            {
                messageText.text = $"<color=yellow>{playerName}</color>: {message}";
            }
        }
    }
}