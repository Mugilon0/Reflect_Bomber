using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class LocalCameraHandler : MonoBehaviour
{
    //public CinemachineVirtualCamera cinemachineVirtualCamera;
    public Transform cameraAnchorPoint;


    // Input
    Vector2 viewInput;

    //Rotation
    float cameraRotationX = 0;
    float cameraRotationY = 0;

    // other componets
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    Camera localCamera;
    private void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        //cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
    }
    // Start is called before the first frame update
    void Start()
    {
        //Detach camera if enabled ���[�J���̃J�����݂̂��L��
        if (localCamera.enabled)
            localCamera.transform.parent = null; // �J������charactermovementhandler����؂藣����� �L�����̓����ɂ���čX�V����Ȃ��悤�ɂ���
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (cameraAnchorPoint == null) // �N���C�A���g���폜������cameraAnchorPoint��null�ɂȂ�\��������
            return;
        if (!localCamera.enabled) // ���[�J���ŗL���łȂ��Ȃ�X�V����Ӗ��Ȃ�
            return;

        // Move the camera to the position of the player
        localCamera.transform.position = cameraAnchorPoint.position; // �J�������v���C���[�Ɠ����ʒu�� 


        // Calculate rotation
        cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX, 0, 0); // -90, 90 �� -10, 20

        cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCustom.rotationSpeed;
        //cameraRotationY += SetViewInputVector().x * Time.deltaTime * NetworkCharacterControllerPrototypeCustom.rotationSpeed;

        //// Apply rotation
        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);

        //localCamera.transform.rotation = cameraAnchorPoint.rotation; // ����
    }

    // �}�E�X�ɂ�鎋���ړ��͎��{���Ȃ����@��]
    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }
}
