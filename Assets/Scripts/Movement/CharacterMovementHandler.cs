using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{
    [Networked]
    public TickTimer MoveLockTimer { get; set; }
    [Networked]
    public NetworkBool IsLocked { get; set; }
    public bool CanMove => !IsLocked && MoveLockTimer.ExpiredOrNotRunning(Runner);




    bool isRespawnRequested = false;

    //other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    HPHandler hpHandler;
    //Camera localCamera; // マウスでカメラ動かさないならまだ不要

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
        //localCamera = GetComponentInChildren<Camera>(); // まだ不要

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
        if (Object.HasStateAuthority)
        {
            if (isRespawnRequested)
            {
                Respawn();
                return;
            }

            // Don't update the clients position when they are dead
            if (hpHandler.isDead)
                return;

        }
        // added 7/10
        if (SceneManager.GetActiveScene().name == "Ready")
            return;

        if (GetInput(out NetworkInputData networkInputData)) 
        {

            networkCharacterControllerPrototypeCustom.Rotate(networkInputData.rotationInput);

            // Rotate the input from the network
            //transform.forward = networkInputData.aimForwardVector; 


            // Cancel out rotation on X axis as we dont want our character to tilt 
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, rotation.eulerAngles.z);
            transform.rotation = rotation;

            // Move
            if (CanMove)
            {
                //Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
                Vector3 moveDirection = transform.forward * networkInputData.movementInput.y;
                moveDirection.Normalize();

                //networkCharacterControllerPrototypeCustom.Move(moveDirection); 
                networkCharacterControllerPrototypeCustom.Move(moveDirection, networkInputData.movementInput.y); 
            }


            // Check if we've fallen off the world
            CheckFallRespawn();

        }
    }



    void CheckFallRespawn()
    {
        if (transform.position.y < -12)
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log($"{Time.time} Respawn due to fall outside of map at position {transform.position}");

                Respawn();
            }


            transform.position = Utils.GetRandomSpawnPoint();
        }
    }

    public void RequestRespawn()
    {
        isRespawnRequested = true;
    }

    void Respawn()
    {

        networkCharacterControllerPrototypeCustom.TeleportToPosition(Utils.GetRandomSpawnPoint()); 

        hpHandler.OnRespawned();

        isRespawnRequested = false;
    }

    public void SetCharacterControllerEnabled(bool isEnabled)
    {
        networkCharacterControllerPrototypeCustom.Controller.enabled = isEnabled;
    }
}
