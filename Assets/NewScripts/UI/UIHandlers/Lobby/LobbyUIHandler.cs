using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class LobbyUIHandler : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private UIScreen lobbySelectPanel;
    [SerializeField] private UIScreen sessionListPanel;
    [SerializeField] private UIScreen statusPanel;
    [SerializeField] private UIScreen createRoomPanel;

    [Header("SessionList")]
    [SerializeField] private SessionListUIHandler sessionListUIHandler;

    [Header("CreateRoom")]
    [SerializeField] private TMP_InputField sessionNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField hostCommentInput;
    [SerializeField] private UnityEngine.UI.Slider maxPlayersSlider;
    [SerializeField] private UnityEngine.UI.Slider maxRoundsSlider;

    [Header("Buttons")]
    [SerializeField] private Button moveToFindGameButton;
    [SerializeField] private Button moveToCreateRoomButton;

    [SerializeField] private NetworkRunnerHandler _networkRunnerHandler;

    private Spawner _spawner;

    // SetActive(true)で自動発火
    private void OnEnable()
    {
        _networkRunnerHandler.OnLobbyJoinSuccess -= OnLobbyJoinSuccess;
        _networkRunnerHandler.OnLobbyJoinFailure -= OnLobbyJoinFailure;
        _networkRunnerHandler.OnGameCreateFailed -= OnGameCreateFailed;
        _networkRunnerHandler.OnGameJoinFailed -= OnGameJoinFailed;

        _networkRunnerHandler.OnLobbyJoinSuccess += OnLobbyJoinSuccess;
        _networkRunnerHandler.OnLobbyJoinFailure += OnLobbyJoinFailure;
        _networkRunnerHandler.OnGameCreateFailed += OnGameCreateFailed;
        _networkRunnerHandler.OnGameJoinFailed += OnGameJoinFailed;

        sessionListUIHandler.OnJoinSessionRequested -= OnJoinSessionRequested;
        sessionListUIHandler.OnJoinSessionRequested += OnJoinSessionRequested;
    }

    // SetActive(false)で自動発火
    private void OnDisable()
    {
        _networkRunnerHandler.OnLobbyJoinSuccess -= OnLobbyJoinSuccess;
        _networkRunnerHandler.OnLobbyJoinFailure -= OnLobbyJoinFailure;
        _networkRunnerHandler.OnGameCreateFailed -= OnGameCreateFailed;
        _networkRunnerHandler.OnGameJoinFailed -= OnGameJoinFailed;

        if (_spawner != null)
        {
            _spawner.OnSessionListReceived -= OnSessionListReceived;
            _spawner.OnNoSessionFound -= OnNoSessionFound;
        }

        sessionListUIHandler.OnJoinSessionRequested -= OnJoinSessionRequested;
    }

    // ボタンから呼ばれる
    public void OnFindGameClicked()
    {
        moveToFindGameButton.interactable = true; //false
        UIScreen.Focus(sessionListPanel);
        sessionListUIHandler.ShowSearching();
    }

    public void OnRefreshClicked()
    {
        sessionListUIHandler.ShowSearching();
        _networkRunnerHandler.OnJoinLobby();
    }

    public void OnCreateRoomClicked()
    {
        UIScreen.Focus(createRoomPanel);
    }

    public void OnStartSessionClicked()
    {
        var config = new RoomConfig
        {
            SessionName = sessionNameInput.text,
            Password = passwordInput.text,
            HostComment = hostCommentInput.text,
            MaxPlayers = Mathf.RoundToInt(maxPlayersSlider.value),
            MaxRounds = Mathf.RoundToInt(maxRoundsSlider.value),
            RoomMode = string.IsNullOrEmpty(passwordInput.text)
                            ? RoomMode.Public
                            : RoomMode.Private
        };
        UIScreen.Focus(statusPanel);
        _networkRunnerHandler.CreateGame(config);
    }

    public void OnRandomMatchClicked()
    {
        _networkRunnerHandler.JoinRandomGame();
    }

    // NetworkRunnerHandlerのイベントハンドラ
    private void OnLobbyJoinSuccess()
    {
        moveToFindGameButton.interactable = true;

        _spawner = FindObjectOfType<Spawner>();
        if (_spawner != null)
        {
            _spawner.OnSessionListReceived -= OnSessionListReceived;
            _spawner.OnSessionListReceived += OnSessionListReceived;
            _spawner.OnNoSessionFound -= OnNoSessionFound;
            _spawner.OnNoSessionFound += OnNoSessionFound;
        }
    }

    private void OnLobbyJoinFailure()
    {
        moveToFindGameButton.interactable = true;
        Debug.LogWarning("ロビー参加失敗");
    }

    private void OnGameCreateFailed()
    {
        Debug.LogWarning("部屋作成失敗");
        UIScreen.Focus(createRoomPanel);
    }

    private void OnGameJoinFailed()
    {
        Debug.LogWarning("部屋参加失敗");
        UIScreen.Focus(sessionListPanel);
    }

    // Spawnerのイベントハンドラ
    private void OnSessionListReceived(List<SessionInfo> sessionList)
    {
        if (UIScreen.activeScreen != sessionListPanel) return;

        sessionListUIHandler.ClearList();
        foreach (var session in sessionList)
            sessionListUIHandler.AddToList(session);
    }

    private void OnNoSessionFound()
    {
        if (UIScreen.activeScreen != sessionListPanel) return;
        sessionListUIHandler.ShowNoSessionFound();
    }

    // セッション参加
    private void OnJoinSessionRequested(SessionInfo sessionInfo)
    {
        UIScreen.Focus(statusPanel);
        _networkRunnerHandler.JoinGame(sessionInfo);
    }

    public void OnBackClicked()
    {
        UIScreen.Focus(lobbySelectPanel);
    }
}