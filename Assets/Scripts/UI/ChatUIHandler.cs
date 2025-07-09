using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ChatUIHandler : MonoBehaviour
{
    // Inspector����A�`���b�g1�s���̃v���n�u��ݒ�
    [SerializeField] private GameObject messagePrefab;
    // Inspector����A�����������b�Z�[�W�v���n�u�̐e�ƂȂ�I�u�W�F�N�g��ݒ�
    [SerializeField] private Transform logParent;

    // �\�����郁�b�Z�[�W�̍ő吔��ݒ�
    [SerializeField] private int maxMessages = 4;

    // �����������b�Z�[�W�I�u�W�F�N�g���Ǘ����邽�߂̃��X�g
    private List<GameObject> messageList = new List<GameObject>();

    /// <summary>
    /// �V�����`���b�g���b�Z�[�W�����O�ɒǉ�����
    /// </summary>
    public void AddNewMessage(string playerName, string message)
    {
        Debug.Log($"--- AddNewMessage Called ---");
        Debug.Log($"Current message count (before add): {messageList.Count}");
        Debug.Log($"Max messages allowed: {maxMessages}");

        if (messagePrefab == null || logParent == null) return;

        // ���b�Z�[�W�����ő�𒴂��Ă�����A��ԌÂ����̂���폜����
        if (messageList.Count >= maxMessages)
        {
            Destroy(messageList[0]);
            messageList.RemoveAt(0);
        }
        // ���b�Z�[�W�v���n�u�𐶐�
        var messageInstance = Instantiate(messagePrefab, logParent);
        messageList.Add(messageInstance);

        // TextMeshPro�R���|�[�l���g���擾���āA�e�L�X�g��ݒ�
        var messageText = messageInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (messageText != null)
        {
            if (playerName == "System")
            {
                // �V�X�e�����b�Z�[�W�̏ꍇ�́A���ʂȐF�Ń��b�Z�[�W������\��
                messageText.text = $"<i><color=yellow>{message}</color></i>";
            }
            else
            {
                messageText.text = $"<color=yellow>{playerName}</color>: {message}";
            }
        }
    }
}