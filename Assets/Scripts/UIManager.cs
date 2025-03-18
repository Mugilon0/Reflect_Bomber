using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;


public class UIManager : MonoBehaviour
{
    public void OnRandomMatchButtonClicked()
    {
        GameLauncher.Instance.StartLobby(); // ���r�[�ɎQ��
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }
}