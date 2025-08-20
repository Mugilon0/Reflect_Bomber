using UnityEngine;
using Fusion; // NetworkPlayer.Local を使うために追加
using UnityEngine.SceneManagement;

// クラス名を FaceLocalPlayerCamera など、より分かりやすい名前に変更するのをお勧めします
public class FaceCamera : MonoBehaviour
{
    private Camera localPlayerCamera;

    void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name == "Ready")
            return;  // readyシーンではなぜかテキストが反転してしまうので無し

        // まだローカルプレイヤーのカメラを見つけていなければ、探す試みをする
        if (localPlayerCamera == null)
        {
            // NetworkPlayer.Local は、このクライアントで操作しているプレイヤーを指す
            if (NetworkPlayer.Local != null && NetworkPlayer.Local.localCameraHandler != null)
            {
                localPlayerCamera = NetworkPlayer.Local.localCameraHandler.localCamera;
            }

            // まだ見つからなければ、何もせず処理を終える
            if (localPlayerCamera == null)
            {
                return;
            }
        }

        // カメラの方向を向く処理
        this.transform.LookAt(
            this.transform.position + localPlayerCamera.transform.rotation * Vector3.forward,
            localPlayerCamera.transform.rotation * Vector3.up
        );
    }
}