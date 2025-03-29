using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{

    //other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    //Camera localCamera; // マウスでカメラ動かさないならまだ不要

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        //localCamera = GetComponentInChildren<Camera>(); // まだ不要
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    //private void Update()　
    //{
    //    cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed; // マウスの上下によりカメラの向きをかえる
    //    cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90); // あまり遠くまでいかないように

    //    localCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0); //　カメラに適用
    //}




    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData)) //　Get the input from the network
        {
            // Rotate the input from the network
            transform.forward = networkInputData.aimForwardVector; // たいだな方法 急に向きがかわると変になるかも


            // Cancel out rotation on X axis as we dont want our character to tilt 上下に向いたとき傾かないようにする
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, rotation.eulerAngles.z);
            transform.rotation = rotation;

            // Move
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
            moveDirection.Normalize();

            networkCharacterControllerPrototypeCustom.Move(moveDirection); // データはおそらくCharacterInputHandlerからもらう

            // Check if we've fallen off the world
            CheckFallRespawn();
        
        }
    }



    void CheckFallRespawn()
    {
        if (transform.position.y < -12)
        {
            transform.position = Utils.GetRandomSpawnPoint();
        }
    }


}
