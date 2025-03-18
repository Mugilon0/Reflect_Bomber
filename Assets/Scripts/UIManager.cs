using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;


public class UIManager : MonoBehaviour
{
    public void OnRandomMatchButtonClicked()
    {
        GameLauncher.Instance.StartLobby(); // ÉçÉrÅ[Ç…éQâ¡
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }
}