using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterInputHandler : MonoBehaviour // ローカルのユーザーの入力を取得するスクリプト
{
    Vector2 moveInputVector = Vector2.zero; //ユーザーからの入力を収集する
    Vector2 viewInputVector = Vector2.zero; // 見る方向

    //bool isGrenadeFireButtonPressed = false;

    // other　components
    LocalCameraHandler localCameraHandler;
    CharacterMovementHandler characterMovementHandler;

    private float chargeTimer = 0f;
    private bool isCharging = false;
    public float longThrowThreshold = 0.3f; // この秒数以上で長距離ボムになる

    // isGrenadeFireButtonPressed を削除し、以下に置き換え
    private bool shortThrowTriggered = false;
    private bool longThrowTriggered = false;


    // Start is called before the first frame update
    private void Awake()
    {
        localCameraHandler = GetComponentInChildren<LocalCameraHandler>();
        characterMovementHandler = GetComponent<CharacterMovementHandler>();
    }

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked; // カーソルがはみ出ないようにする
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() // ここで入力を収集
    {
        if (!characterMovementHandler.Object.HasInputAuthority)
            return;

        if (SceneManager.GetActiveScene().name == "Ready")
            return;  // 先走って実装　意味ない


        // View input
        // マウスによる移動は実装しない
        viewInputVector.x = Input.GetAxis("Mouse X");
        viewInputVector.y = Input.GetAxis("Mouse Y") * -1;


        // Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");



        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        // 押している間、タイマーを加算
        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
        }

        // 離した瞬間
        if (Input.GetMouseButtonUp(0))
        {
            if (isCharging)
            {
                // しきい値より短ければ短距離ボム
                if (chargeTimer < longThrowThreshold)
                {
                    shortThrowTriggered = true;
                }
                // しきい値以上なら長距離ボム
                else
                {
                    longThrowTriggered = true;
                }
            }
            isCharging = false;
        }

        // if (Input.GetButtonDown("Jump"))
        //isJumpButtonPressed = true;

        if (Input.GetKeyDown(KeyCode.C))
        {
            //
            Debug.Log("Cボタンを押した");
            NetworkPlayer.Local.is3rdPersonCamera = !NetworkPlayer.Local.is3rdPersonCamera;
        }



        // Set view ローカルカメラの値を更新する
        localCameraHandler.SetViewInputVector(viewInputVector);
    }

    // OnInputでは保持しないでここでする networkに同期したい情報を割り当てる
    public NetworkInputData GetNetworkInput() 
    {
        NetworkInputData networkInputData = new NetworkInputData();

        // Aim data
        networkInputData.aimForwardVector = localCameraHandler.transform.forward;

        // Move data

        networkInputData.movementInput = moveInputVector;

        // Jump data
        //networkInputData.isJumpPressed = isJumpButtonPressed;


        // Grenade fire data
        networkInputData.isShortThrow = shortThrowTriggered;
        networkInputData.isLongThrow = longThrowTriggered;
        networkInputData.longThrowCharge = chargeTimer; // 溜めた時間をそのまま渡す

        // サーバーに送ったらトリガーをリセット
        shortThrowTriggered = false;
        longThrowTriggered = false;


        //isJumpButtonPressed = false;

        // 溜めのトリガーをネットワークインプットデータにセット
        networkInputData.IsCharging = isCharging;

        return networkInputData;
    }

}
