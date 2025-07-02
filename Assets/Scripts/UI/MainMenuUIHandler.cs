using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIHandler : MonoBehaviour
{
    [Header("Panels")]
    public GameObject playerDetailsPanel;
    public GameObject sessionListPanel;
    public GameObject createRoomPanel;
    public GameObject statusPanel;

    [Header("Buttons")]
    public Button findGameButton; 
    public Button createNewGameButton;


    [Header("Player setting")]
    public TMP_InputField playerNameInputField;

    [Header("New game session")]
    public TMP_InputField sessionNameInputField;


    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerNickname"))
            playerNameInputField.text = PlayerPrefs.GetString("PlayerNickName");
    }

    void HideAllPanels()
    {
        playerDetailsPanel.SetActive(false);
        sessionListPanel.SetActive(false);
        statusPanel.SetActive(false);
        createRoomPanel.SetActive(false);
    }



    public void OnFindGameClicked()
    {
        // playerのnicknameを保存
        PlayerPrefs.SetString("PlayerNickName", playerNameInputField.text);
        PlayerPrefs.Save();

        GameManager.instance.playerNickName = playerNameInputField.text;

        if (createNewGameButton != null)
        {
            createNewGameButton.interactable = false;
        }


        NetworkRunnerHandler networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>();

        networkRunnerHandler.OnJoinLobby(this);

        HideAllPanels();


        sessionListPanel.gameObject.SetActive(true);
        FindObjectOfType<SessionListUIHandler>(true).OnLookingForGameSeession();
    }


    public void OnLobbyJoinSuccess()
    {
        Debug.Log("ロビーに正しく参加できました！UIボタンをupdateします");

        // 部屋作成ボタンを有効にする
        if (createNewGameButton != null)
        {
            createNewGameButton.interactable = true;
        }
    }


    public void OnLobbyJoinFailure()
    {
        Debug.LogWarning("Lobby join failed. Re-enabling UI buttons.");

        // 失敗したので、ボタンを再度有効化して、もう一度試せるようにする
        if (findGameButton != null)
        {
            findGameButton.interactable = true;
        }
        if (createNewGameButton != null)
        {
            createNewGameButton.interactable = true;
        } 

        // 必要であれば、ステータスパネルにエラーメッセージを表示するなど
        // statusPanel.SetActive(true);
        // statusText.text = "Failed to join lobby. Please try again.";
    }

    public void OnCreateNewGameClicked()
    {
        HideAllPanels();

        createRoomPanel.SetActive(true);
    }

    public void OnStartNewSessionClicked()
    {
        NetworkRunnerHandler networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>();

        networkRunnerHandler.CreateGame(sessionNameInputField.text, "Ready");

        HideAllPanels();

        statusPanel.gameObject.SetActive(true);
    }

    public void OnJoinServer()
    {
        HideAllPanels();

        statusPanel.gameObject.SetActive(true);
    }


}
