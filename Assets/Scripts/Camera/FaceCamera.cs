using UnityEngine;
using Fusion; // NetworkPlayer.Local ���g�����߂ɒǉ�
using UnityEngine.SceneManagement;

// �N���X���� FaceLocalPlayerCamera �ȂǁA��蕪����₷�����O�ɕύX����̂������߂��܂�
public class FaceCamera : MonoBehaviour
{
    private Camera localPlayerCamera;

    void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name == "Ready")
            return;  // ready�V�[���ł͂Ȃ����e�L�X�g�����]���Ă��܂��̂Ŗ���

        // �܂����[�J���v���C���[�̃J�����������Ă��Ȃ���΁A�T�����݂�����
        if (localPlayerCamera == null)
        {
            // NetworkPlayer.Local �́A���̃N���C�A���g�ő��삵�Ă���v���C���[���w��
            if (NetworkPlayer.Local != null && NetworkPlayer.Local.localCameraHandler != null)
            {
                localPlayerCamera = NetworkPlayer.Local.localCameraHandler.localCamera;
            }

            // �܂�������Ȃ���΁A���������������I����
            if (localPlayerCamera == null)
            {
                return;
            }
        }

        // �J�����̕�������������
        this.transform.LookAt(
            this.transform.position + localPlayerCamera.transform.rotation * Vector3.forward,
            localPlayerCamera.transform.rotation * Vector3.up
        );
    }
}