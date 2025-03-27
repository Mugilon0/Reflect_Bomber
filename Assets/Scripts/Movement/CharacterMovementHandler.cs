using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{
    //Vector2 viewInput; //

    //other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    //Camera localCamera; // �}�E�X�ŃJ�����������Ȃ��Ȃ�܂��s�v

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        //localCamera = GetComponentInChildren<Camera>(); // �܂��s�v
    }
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // �J�[�\�����͂ݏo�Ȃ��悤�ɂ���
        Cursor.visible = false;
    }

    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData)) //�@Get the input from the network
        {
            // Move
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
            moveDirection.Normalize();

            networkCharacterControllerPrototypeCustom.Move(moveDirection); // �f�[�^�͂����炭CharacterInputHandler������炤
        }
    }


    //public void SetViewInputVector(Vector2 viewInput) //���_�̌������󂯎��A���s����
    //{
    //    this.viewInput = viewInput;
    //}

}
