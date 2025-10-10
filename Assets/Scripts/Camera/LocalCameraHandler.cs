using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class LocalCameraHandler : MonoBehaviour
{
    //public CinemachineVirtualCamera cinemachineVirtualCamera;
    //public Transform cameraAnchorPoint;
    public Camera localCamera;


    // Input
    //Vector2 viewInput;

    ////Rotation
    //float cameraRotationX = 0;
    //float cameraRotationY = 0;

    // other componets
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    CinemachineVirtualCamera cinemachineVirtualCamera;

    private void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        //cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
    }
    // Start is called before the first frame update
    //void Start()
    //{
    //    cameraRotationX = GameManager.instance.cameraViewRotation.x;
    //    cameraRotationY = GameManager.instance.cameraViewRotation.y;

    //    ////Detach camera if enabled ローカルのカメラのみが有効
    //    //if (localCamera.enabled)
    //    //    localCamera.transform.parent = null; // カメラがcharactermovementhandlerから切り離される キャラの動きによって更新されないようにする
    //}

    // Update is called once per frame
    void LateUpdate()
    {
        //if (cameraAnchorPoint == null) // クライアントが削除されるとcameraAnchorPointがnullになる可能性がある
        //    return;
        if (!localCamera.enabled) // ローカルで有効でないなら更新する意味ない
            return;

        //　Cinemachineがあるか確認
        if (cinemachineVirtualCamera == null)
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (cinemachineVirtualCamera != null)
        {

            cinemachineVirtualCamera.Follow = NetworkPlayer.Local.playerModel;
            cinemachineVirtualCamera.LookAt = NetworkPlayer.Local.playerModel;
            cinemachineVirtualCamera.enabled = true;

            transform.parent = null;

            //// Set the layer of the local players model
            //Utils.SetRenderLayerInChildren(NetworkPlayer.Local.playerModel, LayerMask.NameToLayer("Default"));
        }


        // FPS

        //else 
        //{
        //    // カメラの切り替え
        //    if (cinemachineVirtualCamera.enabled)
        //    {
        //        cinemachineVirtualCamera.enabled = false;

        //        // Sets the layer of the local players model
        //        Utils.SetRenderLayerInChildren(NetworkPlayer.Local.playerModel, LayerMask.NameToLayer("LocalPlayerModel"));
        //    }

        //}
    }


    // 以下主にFPS実装

    //    // Move the camera to the position of the player
    //    localCamera.transform.position = cameraAnchorPoint.position; // カメラがプレイヤーと同じ位置に 


    //    // Calculate rotation
    //    cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed;
    //    cameraRotationX = Mathf.Clamp(cameraRotationX, 0, 0); // -90, 90 → -10, 20

    //    cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCustom.rotationSpeed;
    //    //cameraRotationY += SetViewInputVector().x * Time.deltaTime * NetworkCharacterControllerPrototypeCustom.rotationSpeed;

    //    //// Apply rotation
    //    localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);

    //    //localCamera.transform.rotation = cameraAnchorPoint.rotation; // 自作
    //}

    //// マウスによる視線移動は実施しないが　回転
    //public void SetViewInputVector(Vector2 viewInput)
    //{
    //    this.viewInput = viewInput;
    //}

    //private void OnDestroy()
    //{
    //    // xはいれていない
    //    if (cameraRotationY != 0)
    //    {

    //        GameManager.instance.cameraViewRotation.y = cameraRotationY;
    //    }
    //}
}