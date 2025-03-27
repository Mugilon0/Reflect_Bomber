using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour // ローカルのユーザーの入力を取得するスクリプト
{
    Vector2 moveInputVector = Vector2.zero; //ユーザーからの入力を収集する
    Vector2 viewInputVecor = Vector2.zero; // 見る方向

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() // ここで入力を収集
    {
        // View input
        // マウスによる移動は実装しない


        // Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");
    }

    // OnInputでは保持しないでここでする
    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        networkInputData.movementInput = moveInputVector;

        return networkInputData;
    }

}
