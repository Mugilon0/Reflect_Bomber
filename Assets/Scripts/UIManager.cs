//using UnityEngine;
//using Fusion;
//using System.Collections;
//using System.Collections.Generic;

//public class UIManager : MonoBehaviour
//{
//    private bool isLobbySearchComplete = false;

//    public void OnRandomMatchButtonClicked()
//    {
//        if (GameLauncher.Instance == null) return;

//        // ���r�[�������J�n
//        Debug.Log("���r�[�������J�n...");
//        isLobbySearchComplete = false;

//        // ��莞�ԃ��r�[���X�g�̍X�V��҂�
//        StartCoroutine(WaitForLobbyUpdate());
//    }

//    private IEnumerator WaitForLobbyUpdate()
//    {
//        float timeout = 10.0f; // �ő�10�b�҂�
//        float elapsedTime = 0f;

//        while (!isLobbySearchComplete && elapsedTime < timeout)
//        {
//            yield return new WaitForSeconds(0.5f); // 0.5�b���ƂɃ`�F�b�N
//            elapsedTime += 0.5f;
//        }



//        // �X�V����Ȃ������ꍇ�̏���
//        if (!isLobbySearchComplete)
//        {
//            Debug.LogWarning("���r�[��񂪍X�V����܂���ł����B�V�������r�[���쐬���܂�"); // �Ď��s���Ă��������B
//            //UI�Ƀ��b�Z�[�W��\���@LobbyUIManager.Instance.ShowErrorMessage("���r�[��������܂���ł����B������x�����Ă��������B");
//            GameLauncher.Instance.StartLobby(); // �� �V�K���r�[���쐬
//            yield break; // �����ŏ������I���i���r�[�ɓ���Ȃ��j
//        }



//        // �ŐV�̃��r�[��񂪎擾�ł����烍�r�[�ɎQ��
//        Debug.Log("���r�[���������I���r�[�ɐڑ����܂�...");
//        GameLauncher.Instance.StartLobby();
//    }


//    public void OnExitButtonClicked()
//    {
//        Application.Quit();
//    }

//    // GameLauncher ����Ă΂��R�[���o�b�N���쐬
//    public void OnLobbyUpdated()
//    {
//        Debug.Log("���r�[��񂪍X�V����܂����I");
//        isLobbySearchComplete = true;
//    }
//}
