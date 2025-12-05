using UnityEngine;
using Fusion;

public class FaceCameraTPS : MonoBehaviour
{
    private Camera localCamera;

    void LateUpdate()
    {
        if (localCamera == null)
        {
            if (NetworkPlayer.Local != null &&
                NetworkPlayer.Local.localCameraHandler != null)
            {
                localCamera = NetworkPlayer.Local.localCameraHandler.localCamera;
            }
            if (localCamera == null) return;
        }

        // ----- TPS用：Y軸だけカメラに向ける -----
        Vector3 target = localCamera.transform.position;
        target.y = transform.position.y; // Yだけを合わせる

        // カメラ方向へ向ける
        transform.LookAt(target);

        // ここで180°補正（Y軸反転を修正）
        transform.Rotate(0f, 180f, 0f);
    }
}
