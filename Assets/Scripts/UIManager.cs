//using UnityEngine;
//using Fusion;
//using System.Collections;
//using System.Collections.Generic;

//public class UIManager : MonoBehaviour
//{
//    private bool isLobbySearchComplete = false;

//    public void OnRandomMatchButtonClicked()
//    {
//        if (GameLauncher.Instance == null) return;

//        // ロビー検索を開始
//        Debug.Log("ロビー検索を開始...");
//        isLobbySearchComplete = false;

//        // 一定時間ロビーリストの更新を待つ
//        StartCoroutine(WaitForLobbyUpdate());
//    }

//    private IEnumerator WaitForLobbyUpdate()
//    {
//        float timeout = 10.0f; // 最大10秒待つ
//        float elapsedTime = 0f;

//        while (!isLobbySearchComplete && elapsedTime < timeout)
//        {
//            yield return new WaitForSeconds(0.5f); // 0.5秒ごとにチェック
//            elapsedTime += 0.5f;
//        }



//        // 更新されなかった場合の処理
//        if (!isLobbySearchComplete)
//        {
//            Debug.LogWarning("ロビー情報が更新されませんでした。新しいロビーを作成します"); // 再試行してください。
//            //UIにメッセージを表示　LobbyUIManager.Instance.ShowErrorMessage("ロビーが見つかりませんでした。もう一度試してください。");
//            GameLauncher.Instance.StartLobby(); // ← 新規ロビーを作成
//            yield break; // ここで処理を終了（ロビーに入らない）
//        }



//        // 最新のロビー情報が取得できたらロビーに参加
//        Debug.Log("ロビー検索完了！ロビーに接続します...");
//        GameLauncher.Instance.StartLobby();
//    }


//    public void OnExitButtonClicked()
//    {
//        Application.Quit();
//    }

//    // GameLauncher から呼ばれるコールバックを作成
//    public void OnLobbyUpdated()
//    {
//        Debug.Log("ロビー情報が更新されました！");
//        isLobbySearchComplete = true;
//    }
//}
