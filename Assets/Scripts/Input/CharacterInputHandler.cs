using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterInputHandler : MonoBehaviour // ���[�J���̃��[�U�[�̓��͂��擾����X�N���v�g
{
    Vector2 moveInputVector = Vector2.zero; //���[�U�[����̓��͂����W����
    Vector2 viewInputVector = Vector2.zero; // �������

    bool isGrenadeFireButtonPressed = false;

    // other�@components
    LocalCameraHandler localCameraHandler;
    CharacterMovementHandler characterMovementHandler;


    // Start is called before the first frame update
    private void Awake()
    {
        localCameraHandler = GetComponentInChildren<LocalCameraHandler>();
        characterMovementHandler = GetComponent<CharacterMovementHandler>();
    }

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked; // �J�[�\�����͂ݏo�Ȃ��悤�ɂ���
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() // �����œ��͂����W
    {
        if (!characterMovementHandler.Object.HasInputAuthority)
            return;

        if (SceneManager.GetActiveScene().name == "Ready")
            return;  // �摖���Ď����@�Ӗ��Ȃ�

        // View input
        // �}�E�X�ɂ��ړ��͎������Ȃ�
        viewInputVector.x = Input.GetAxis("Mouse X");
        viewInputVector.y = Input.GetAxis("Mouse Y") * -1;


        // Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");



        // Throw grenade
        if (Input.GetMouseButtonDown(0))
            isGrenadeFireButtonPressed = true;
        

        // if (Input.GetButtonDown("Jump"))
        //isJumpButtonPressed = true;

        if (Input.GetKeyDown(KeyCode.C))
        {
            //
            Debug.Log("C�{�^����������");
            NetworkPlayer.Local.is3rdPersonCamera = !NetworkPlayer.Local.is3rdPersonCamera;
        }



        // Set view ���[�J���J�����̒l���X�V����
        localCameraHandler.SetViewInputVector(viewInputVector);
    }

    // OnInput�ł͕ێ����Ȃ��ł����ł��� network�ɓ����������������蓖�Ă�
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
        networkInputData.isGrenadeFireButtonPressed = isGrenadeFireButtonPressed; //�l�b�g���[�N�ɓn��

        //Reset variables now that we have reaad their states
        isGrenadeFireButtonPressed = false;


        //isJumpButtonPressed = false;

        return networkInputData;
    }

}
