using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour // ���[�J���̃��[�U�[�̓��͂��擾����X�N���v�g
{
    Vector2 moveInputVector = Vector2.zero; //���[�U�[����̓��͂����W����
    Vector2 viewInputVecor = Vector2.zero; // �������

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
        Cursor.lockState = CursorLockMode.Locked; // �J�[�\�����͂ݏo�Ȃ��悤�ɂ���
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() // �����œ��͂����W
    {
        // View input
        // �}�E�X�ɂ��ړ��͎������Ȃ�


        // Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");


        // if (Input.GetButtonDown("Jump"))
        //isJumpButtonPressed = true;

        // Set view
        //localCameraHandler.SetViewInputVector(viewInputVector);
    }

    // OnInput�ł͕ێ����Ȃ��ł����ł���
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
