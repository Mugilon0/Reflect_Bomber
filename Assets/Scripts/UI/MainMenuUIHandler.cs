using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuUIHandler : MonoBehaviour
{
    [Header("Panels")]
    public GameObject playerDetailsPanel;
    public GameObject sessionListPanel;
    public GameObject createRoomPanel;
    public GameObject statusPanel;


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
        // player‚Ìnickname‚ð•Û‘¶
        PlayerPrefs.SetString("PlayerNickName", playerNameInputField.text);
        PlayerPrefs.Save();

        GameManager.instance.playerNickName = playerNameInputField.text;

        NetworkRunnerHandler networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>();

        networkRunnerHandler.OnJoinLobby();

        HideAllPanels();


        sessionListPanel.gameObject.SetActive(true);
        FindObjectOfType<SessionListUIHandler>(true).OnLookingForGameSeession();
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
