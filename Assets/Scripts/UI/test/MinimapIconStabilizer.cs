using UnityEngine;

public class MinimapIconStabilizer : MonoBehaviour
{
    // アイコンの固定角度（真上から見るなら90, 0, 0）
    private Quaternion fixedRotation = Quaternion.Euler(0f, 0f, 0f);

    void LateUpdate()
    {
        // 毎フレーム、親（ボム）の回転を無視して、角度を強制上書きする
        transform.rotation = fixedRotation;
    }
}