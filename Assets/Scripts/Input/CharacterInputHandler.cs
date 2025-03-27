using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour // ���[�J���̃��[�U�[�̓��͂��擾����X�N���v�g
{
    Vector2 moveInputVector = Vector2.zero; //���[�U�[����̓��͂����W����
    Vector2 viewInputVecor = Vector2.zero; // �������

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() // �����œ��͂����W
    {
        // View input
        // �}�E�X�ɂ��ړ��͎������Ȃ�


        // Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");
    }

    // OnInput�ł͕ێ����Ȃ��ł����ł���
    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        networkInputData.movementInput = moveInputVector;

        return networkInputData;
    }

}
