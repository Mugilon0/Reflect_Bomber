using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{
    [Networked]
    public TickTimer MoveLockTimer { get; set; } // ���샍�b�N�p�̃^�C�}�[
    public bool CanMove => MoveLockTimer.ExpiredOrNotRunning(Runner); // �^�C�}�[�������Ă��Ȃ�������true�ɂȂ�



    bool isRespawnRequested = false;

    //other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    HPHandler hpHandler;
    private ScoreCalculator scoreCalculator;
    //Camera localCamera; // �}�E�X�ŃJ�����������Ȃ��Ȃ�܂��s�v

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
        //localCamera = GetComponentInChildren<Camera>(); // �܂��s�v
        scoreCalculator = GetComponent<ScoreCalculator>();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    //private void Update()�@
    //{
    //    cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed; // �}�E�X�̏㉺�ɂ��J�����̌�����������
    //    cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90); // ���܂艓���܂ł����Ȃ��悤��

    //    localCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0); //�@�J�����ɓK�p
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

        if (GetInput(out NetworkInputData networkInputData)) //�@Get the input from the network
        {
            // Rotate the input from the network
            transform.forward = networkInputData.aimForwardVector; // �������ȕ��@ �}�Ɍ����������ƕςɂȂ邩��


            // Cancel out rotation on X axis as we dont want our character to tilt �㉺�Ɍ������Ƃ��X���Ȃ��悤�ɂ���
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, rotation.eulerAngles.z);
            transform.rotation = rotation;

            // Move
            if (CanMove)
            {
                Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
                moveDirection.Normalize();

                networkCharacterControllerPrototypeCustom.Move(moveDirection); // �f�[�^�͂����炭CharacterInputHandler������炤
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
        isRespawnRequested = true; // ���X�|�[�����Ƀg���K�[����
    }

    void Respawn()
    {
        if (scoreCalculator != null)
        {
            scoreCalculator.OnDeathPenalty();
        }


        networkCharacterControllerPrototypeCustom.TeleportToPosition(Utils.GetRandomSpawnPoint()); // transform.position����������ok

        hpHandler.OnRespawned();

        isRespawnRequested = false;
    }

    // �v���C���[���̃q�b�g�{�b�N�X�ɂ��Ԃ���Ȃ��悤�ɂ��邽��
    public void SetCharacterControllerEnabled(bool isEnabled)
    {
        networkCharacterControllerPrototypeCustom.Controller.enabled = isEnabled;
    }
}
