using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{
    [Networked]
    public TickTimer MoveLockTimer { get; set; } // 操作ロック用のタイマー
    public bool CanMove => MoveLockTimer.ExpiredOrNotRunning(Runner); // タイマーが動いていない時だけtrueになる



    bool isRespawnRequested = false;

    //other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    HPHandler hpHandler;
    private ScoreCalculator scoreCalculator;
    //Camera localCamera; // マウスでカメラ動かさないならまだ不要

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
        //localCamera = GetComponentInChildren<Camera>(); // まだ不要
        scoreCalculator = GetComponent<ScoreCalculator>();
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

        if (GetInput(out NetworkInputData networkInputData)) //　Get the input from the network
        {
            // Rotate the input from the network
            transform.forward = networkInputData.aimForwardVector; // たいだな方法 急に向きがかわると変になるかも


            // Cancel out rotation on X axis as we dont want our character to tilt 上下に向いたとき傾かないようにする
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, rotation.eulerAngles.z);
            transform.rotation = rotation;

            // Move
            if (CanMove)
            {
                Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
                moveDirection.Normalize();

                networkCharacterControllerPrototypeCustom.Move(moveDirection); // データはおそらくCharacterInputHandlerからもらう
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
        isRespawnRequested = true; // リスポーン時にトリガーする
    }

    void Respawn()
    {
        if (scoreCalculator != null)
        {
            scoreCalculator.OnDeathPenalty();
        }


        networkCharacterControllerPrototypeCustom.TeleportToPosition(Utils.GetRandomSpawnPoint()); // transform.positionをかえるよりok

        hpHandler.OnRespawned();

        isRespawnRequested = false;
    }

    // プレイヤー内のヒットボックスにもぶつからないようにするため
    public void SetCharacterControllerEnabled(bool isEnabled)
    {
        networkCharacterControllerPrototypeCustom.Controller.enabled = isEnabled;
    }
}
