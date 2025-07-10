using UnityEngine;
using Fusion; // NetworkObjectを使うために追加

public class MinimapCamera : MonoBehaviour
{
    private Transform target;

    [SerializeField] 
    public Vector3 offset = new Vector3(0, 100, 0);

    void Start()
    {
        // まず、自分がどのプレイヤーに所属しているかを確認する
        NetworkObject nwo = GetComponentInParent<NetworkObject>();

        // もし、自分に操作権限がある（＝ローカルプレイヤーの）カメラなら、処理を続行
        if (nwo != null && nwo.HasInputAuthority)
        {
            // ターゲットとして自分自身の親（プレイヤー）を設定
            target = transform.parent;
            // カメラ自身も親から切り離し、独立して動けるようにする
            transform.SetParent(null);

            Debug.Log("Local MinimapCamera has been activated and detached.");
        }
        else
        {
            // 自分のものでない（＝リモートプレイヤーの）カメラなら、
            // 何もせずに自身を非表示にして、処理を完全に停止する
            gameObject.SetActive(false);
            Debug.Log("Remote MinimapCamera has been deactivated.");
        }
    }

    void LateUpdate()
    {
        // ターゲットが設定されている（＝ローカルカメラである）場合のみ、追従処理を行う
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}