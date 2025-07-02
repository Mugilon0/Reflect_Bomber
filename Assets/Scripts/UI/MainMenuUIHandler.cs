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
        // player��nickname��ۑ�
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
        Debug.Log("���r�[�ɐ������Q���ł��܂����IUI�{�^����update���܂�");

        // �����쐬�{�^����L���ɂ���
        if (createNewGameButton != null)
        {
            createNewGameButton.interactable = true;
        }
    }


    public void OnLobbyJoinFailure()
    {
        Debug.LogWarning("Lobby join failed. Re-enabling UI buttons.");

        // ���s�����̂ŁA�{�^�����ēx�L�������āA������x������悤�ɂ���
        if (findGameButton != null)
        {
            findGameButton.interactable = true;
        }
        if (createNewGameButton != null)
        {
            createNewGameButton.interactable = true;
        } 

        // �K�v�ł���΁A�X�e�[�^�X�p�l���ɃG���[���b�Z�[�W��\������Ȃ�
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
