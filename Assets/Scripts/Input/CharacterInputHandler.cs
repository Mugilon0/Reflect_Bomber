using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour // ローカルのユーザーの入力を取得するスクリプト
{
    Vector2 moveInputVector = Vector2.zero; //ユーザーからの入力を収集する
    Vector2 viewInputVecor = Vector2.zero; // 見る方向

    // othe rcomponents
    CharacterMovementHandler characterMovmentHandler;
    LocalCameraHandler localCameraHandler;

    // Start is called before the first frame update
    private void Awake()
    {
        localCameraHandler = GetComponentInChildren<LocalCameraHandler>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // カーソルがはみ出ないようにする
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() // ここで入力を収集
    {
        // View input
        // マウスによる移動は実装しない


        // Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");


        // if (Input.GetButtonDown("Jump"))
        //isJumpButtonPressed = true;

        // Set view
        //localCameraHandler.SetViewInputVector(viewInputVector);
    }

    // OnInputでは保持しないでここでする
    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        // Aim data
        networkInputData.aimForwardVector = localCameraHandler.transform.forward;

        // Move data

        networkInputData.movementInput = moveInputVector;

        // Jump data
        //networkInputData.isJumpPressed = isJumpButtonPressed;

        //Reset variables now that we have reaad their states
        //isJumpButtonPressed = false;

        return networkInputData;
    }

}
