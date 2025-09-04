using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterInputHandler : MonoBehaviour // ���[�J���̃��[�U�[�̓��͂��擾����X�N���v�g
{
    Vector2 moveInputVector = Vector2.zero; //���[�U�[����̓��͂����W����
    Vector2 viewInputVector = Vector2.zero; // �������

    //bool isGrenadeFireButtonPressed = false;

    // other�@components
    LocalCameraHandler localCameraHandler;
    CharacterMovementHandler characterMovementHandler;

    private float chargeTimer = 0f;
    private bool isCharging = false;
    public float longThrowThreshold = 0.3f; // ���̕b���ȏ�Œ������{���ɂȂ�

    // isGrenadeFireButtonPressed ���폜���A�ȉ��ɒu������
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



        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        // �����Ă���ԁA�^�C�}�[�����Z
        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
        }

        // �������u��
        if (Input.GetMouseButtonUp(0))
        {
            if (isCharging)
            {
                // �������l���Z����ΒZ�����{��
                if (chargeTimer < longThrowThreshold)
                {
                    shortThrowTriggered = true;
                }
                // �������l�ȏ�Ȃ璷�����{��
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
        networkInputData.isShortThrow = shortThrowTriggered;
        networkInputData.isLongThrow = longThrowTriggered;
        networkInputData.longThrowCharge = chargeTimer; // ���߂����Ԃ����̂܂ܓn��

        // �T�[�o�[�ɑ�������g���K�[�����Z�b�g
        shortThrowTriggered = false;
        longThrowTriggered = false;


        //isJumpButtonPressed = false;

        // ���߂̃g���K�[���l�b�g���[�N�C���v�b�g�f�[�^�ɃZ�b�g
        networkInputData.IsCharging = isCharging;

        return networkInputData;
    }

}
