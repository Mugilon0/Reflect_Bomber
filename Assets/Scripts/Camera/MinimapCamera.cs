using UnityEngine;
using Fusion; // NetworkObject���g�����߂ɒǉ�

public class MinimapCamera : MonoBehaviour
{
    private Transform target;

    [SerializeField] 
    public Vector3 offset = new Vector3(0, 100, 0);

    void Start()
    {
        // �܂��A�������ǂ̃v���C���[�ɏ������Ă��邩���m�F����
        NetworkObject nwo = GetComponentInParent<NetworkObject>();

        // �����A�����ɑ��쌠��������i�����[�J���v���C���[�́j�J�����Ȃ�A�����𑱍s
        if (nwo != null && nwo.HasInputAuthority)
        {
            // �^�[�Q�b�g�Ƃ��Ď������g�̐e�i�v���C���[�j��ݒ�
            target = transform.parent;
            // �J�������g���e����؂藣���A�Ɨ����ē�����悤�ɂ���
            transform.SetParent(null);

            Debug.Log("Local MinimapCamera has been activated and detached.");
        }
        else
        {
            // �����̂��̂łȂ��i�������[�g�v���C���[�́j�J�����Ȃ�A
            // ���������Ɏ��g���\���ɂ��āA���������S�ɒ�~����
            gameObject.SetActive(false);
            Debug.Log("Remote MinimapCamera has been deactivated.");
        }
    }

    void LateUpdate()
    {
        // �^�[�Q�b�g���ݒ肳��Ă���i�����[�J���J�����ł���j�ꍇ�̂݁A�Ǐ]�������s��
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}