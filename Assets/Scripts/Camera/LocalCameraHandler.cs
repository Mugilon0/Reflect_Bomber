using UnityEngine;
using Cinemachine;

public class LocalCameraHandler : MonoBehaviour
{
    public Camera localCamera;

    private CameraObstructionHandler cameraObstructionHandler;
    private NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    private CinemachineVirtualCamera cinemachineVirtualCamera;

    // 観戦対象
    private NetworkPlayer _spectatingTarget = null;

    private void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();

        cameraObstructionHandler = GetComponent<CameraObstructionHandler>();
        if (cameraObstructionHandler == null)
            cameraObstructionHandler = gameObject.AddComponent<CameraObstructionHandler>();
    }

    void LateUpdate()
    {
        if (!localCamera.enabled) return;

        if (cinemachineVirtualCamera == null)
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (cinemachineVirtualCamera == null) return;

        var localPlayerState = NetworkPlayer.Local?.GetComponent<PlayerState>();
        bool isSpectating = localPlayerState != null && localPlayerState.IsSpectator;

        if (isSpectating)
        {
            HandleSpectatorCamera();
        }
        else
        {
            // 通常カメラ：自分のplayerModelをFollow
            cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel;
            cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
            cinemachineVirtualCamera.enabled = true;
            transform.parent = null;
            HandleWallTransparency();
        }
    }

    private void HandleSpectatorCamera()
    {
        // 観戦対象が未設定または無効なら最初の非Spectatorプレイヤーを選ぶ
        if (_spectatingTarget == null || !_spectatingTarget.Object.IsValid)
        {
            _spectatingTarget = GetNextSpectatingTarget(null);
        }

        if (_spectatingTarget == null) return;

        // クリックで次のプレイヤーに切り替え
        if (Input.GetMouseButtonDown(0))
        {
            _spectatingTarget = GetNextSpectatingTarget(_spectatingTarget);
        }

        // 観戦対象のplayerModelをFollow
        cinemachineVirtualCamera.Follow = _spectatingTarget.playerModel;
        cinemachineVirtualCamera.LookAt = _spectatingTarget.playerModel;
        cinemachineVirtualCamera.enabled = true;
        transform.parent = null;
    }

    private NetworkPlayer GetNextSpectatingTarget(NetworkPlayer current)
    {
        // 非Spectatorのプレイヤー一覧から次を取得
        if (current == null)
            return PlayerRegistry.First(p => !p.GetComponent<PlayerState>().IsSpectator
                && p != NetworkPlayer.Local);

        return PlayerRegistry.NextWhere(
            current,
            p => !p.GetComponent<PlayerState>().IsSpectator && p != NetworkPlayer.Local);
    }

    private void HandleWallTransparency()
    {
        if (NetworkPlayer.Local == null || NetworkPlayer.Local.playerModel == null)
            return;

        if (cameraObstructionHandler != null)
        {
            Vector3 cameraPos = transform.position;
            Vector3 playerPos = NetworkPlayer.Local.playerModel.position;
            cameraObstructionHandler.HandleObstructions(cameraPos, playerPos);
        }
    }
}