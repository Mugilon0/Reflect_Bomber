using UnityEngine;

public class LoadingUIHandler : MonoBehaviour
{
    // 現状は静的なUIを表示するだけ
    // 将来的には参加者情報表示・複数枚のローディング画像などを追加予定

    private void OnEnable()
    {
        Debug.Log("LoadingUI表示");
    }

    private void OnDisable()
    {
        Debug.Log("LoadingUI非表示");
    }
}
