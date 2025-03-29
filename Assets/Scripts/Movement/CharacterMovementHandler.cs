using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{

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
        if (GetInput(out NetworkInputData networkInputData)) //�@Get the input from the network
        {
            // Rotate the input from the network
            transform.forward = networkInputData.aimForwardVector; // �������ȕ��@ �}�Ɍ����������ƕςɂȂ邩��


            // Cancel out rotation on X axis as we dont want our character to tilt �㉺�Ɍ������Ƃ��X���Ȃ��悤�ɂ���
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, rotation.eulerAngles.z);
            transform.rotation = rotation;

            // Move
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
            moveDirection.Normalize();

            networkCharacterControllerPrototypeCustom.Move(moveDirection); // �f�[�^�͂����炭CharacterInputHandler������炤

            // Check if we've fallen off the world
            CheckFallRespawn();
        
        }
    }



    void CheckFallRespawn()
    {
        if (transform.position.y < -12)
        {
            transform.position = Utils.GetRandomSpawnPoint();
        }
    }


}
