using UnityEngine;
using Fusion;

public class ScoreCalculator : NetworkBehaviour
{
    // �Q�Ƃ���R���|�[�l���g
    private NetworkPlayer networkPlayer;

    private void Awake()
    {
        // �K�v�ȃR���|�[�l���g�����炩���ߎ擾���Ă���
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    /// <summary>
    /// ���̃v���C���[��|�������ɃX�R�A�����Z����
    /// </summary>
    public void OnKill()
    {
        // �T�[�o�[�ȊO�ł͉������Ȃ�
        //if (!Object.HasStateAuthority) return;

        // �����łȂ���΃X�R�A�����Z
        // (���̃��\�b�h���Ăяo�����Ŏ����`�F�b�N������̂ŁA�����ł͕s�v)
        networkPlayer.score++;
    }

    /// <summary>
    /// ���S���ă��X�|�[�����鎞�ɃX�R�A�����Z����
    /// </summary>
    public void OnDeathPenalty()
    {
        // �T�[�o�[�ȊO�ł͉������Ȃ�
        if (!Object.HasStateAuthority) return;

        int currentScore = networkPlayer.score;
        int penalty = 0;

        // ���[���Ɋ�Â��Č��_�����v�Z
        if (currentScore > 0)
        {
            // �V�������[���F�X�R�A��3�Ŋ���������1�𑫂��ƁA�y�i���e�B�_���ɂȂ�
            // (��: �X�R�A6 -> 6/3 + 1 = 3�_���_)
            // (��: �X�R�A9 -> 9/3 + 1 = 4�_���_)
            penalty = (currentScore / 3) + 1;
        }

        //if-else���͂�����������炵��
        //if (currentScore > 0)
        //{
        //    penalty = 1 + (currentScore - 1) / 3;
        //    penalty = Mathf.Min(penalty, 3);
        //}

        // �X�R�A�����Z���A0�����ɂȂ�Ȃ��悤�ɂ���
        networkPlayer.score = Mathf.Max(0, currentScore - penalty);
    }
}