using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadyUIHandler : MonoBehaviour
{
    [Header("Ready")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI buttonReadyText;

    [Header("観戦")]
    [SerializeField] private Button spectatorButton;
    [SerializeField] private GameObject spectatorIcon;
    [SerializeField] private GameObject spectatingIcon;

    [Header("レベル選択（ホストのみ・一時的）")]
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private Button[] levelButtons;

    [Header("カウントダウン")]
    [SerializeField] private TextMeshProUGUI countDownText;

    [Header("チャット")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button chatSendButton;

    private bool _localPlayerIsReady = false;

    private void OnEnable()
    {
        if (chatSendButton != null)
            chatSendButton.onClick.AddListener(OnChatSendClicked);
        if (spectatorButton != null)
            spectatorButton.onClick.AddListener(OnSpectatorButtonClicked);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;
            levelButtons[i]?.onClick.AddListener(() => OnLevelButtonClicked(index));
        }

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(true);
    }

    private void OnDisable()
    {
        if (chatSendButton != null)
            chatSendButton.onClick.RemoveListener(OnChatSendClicked);
        if (spectatorButton != null)
            spectatorButton.onClick.RemoveListener(OnSpectatorButtonClicked);

        for (int i = 0; i < levelButtons.Length; i++)
            levelButtons[i]?.onClick.RemoveAllListeners();
    }

    //private void Update()
    //{
    //    // 全クライアントでTickTimerを読んでカウントダウンを表示
    //    if (GameStateManager.Instance == null)
    //    {
    //        Debug.Log("GameStateManager.Instance is null");
    //        return;
    //    }

    //    Debug.Log($"Current={GameStateManager.Instance.Current}, Timer.IsRunning={GameStateManager.Instance.ReadyCountdownTimer.IsRunning}");

    //    if (GameStateManager.Instance.Current != EGameStateNet.Ready) return;

    //    var timer = GameStateManager.Instance.ReadyCountdownTimer;
    //    var runner = GameStateManager.Instance.Runner;

    //    if (timer.IsRunning)
    //    {
    //        float remaining = timer.RemainingTime(runner) ?? 0f;
    //        UpdateCountdownDisplay(remaining);
    //    }
    //    else
    //    {
    //        UpdateCountdownDisplay(0);
    //    }
    //}

    private void Update()
    {
        if (GameStateManager.Instance == null)
        {
            //Debug.Log("GameStateManager.Instance is null");
            return;
        }
        if (GameStateManager.Instance.Current != EGameStateNet.Ready) return;

        var timer = GameStateManager.Instance.ReadyCountdownTimer;
        var runner = GameStateManager.Instance.Runner;

        // IsRunningの代わりにExpiredOrNotRunningを使う
        if (!timer.ExpiredOrNotRunning(runner))
        {
            float remaining = timer.RemainingTime(runner) ?? 0f;
            UpdateCountdownDisplay(remaining);
        }
        else
        {
            UpdateCountdownDisplay(0);
        }
    }

    public void OnLevelButtonClicked(int levelIndex)
    {
        if (RoundManager.Instance == null) return;
        if (RoundManager.Instance.Runner.IsServer)
            RoundManager.Instance.CurrentLevel = levelIndex;
        Debug.Log($"Level {levelIndex} selected");
    }

    public void OnReadyButtonClicked()
    {
        _localPlayerIsReady = !_localPlayerIsReady;
        UpdateButtonDisplay();

        var playerState = NetworkPlayer.Local?.GetComponent<PlayerState>();
        playerState?.RPC_SetReady(_localPlayerIsReady);
    }

    public void OnSpectatorButtonClicked()
    {
        var playerState = NetworkPlayer.Local?.GetComponent<PlayerState>();
        if (playerState == null) return;

        bool willBeSpectator = !playerState.IsSpectator;
        playerState.RPC_ToggleSpectator();

        if (willBeSpectator)
        {
            _localPlayerIsReady = false;
            UpdateButtonDisplay();
        }

        UpdateSpectatorButtonDisplay(willBeSpectator);
    }

    private void OnChatSendClicked()
    {
        if (chatInputField == null) return;
        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        var playerState = NetworkPlayer.Local?.GetComponent<PlayerState>();
        playerState?.RPC_SendChatMessage(message);

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    public void OnPlayerReadyChanged(PlayerState playerState)
    {
        if (NetworkPlayer.Local != null &&
            playerState == NetworkPlayer.Local.GetComponent<PlayerState>())
        {
            _localPlayerIsReady = playerState.IsReady;
            UpdateButtonDisplay();
        }
    }

    public void UpdateCountdownDisplay(float remainingSeconds)
    {
        if (countDownText == null) return;
        countDownText.text = remainingSeconds > 0
            ? $"Battle starts in {Mathf.CeilToInt(remainingSeconds)}"
            : "";
    }

    public void ResetCountdown()
    {
        _localPlayerIsReady = false;
        UpdateButtonDisplay();
        if (countDownText != null)
            countDownText.text = "";
    }

    private void UpdateButtonDisplay()
    {
        if (buttonReadyText == null) return;
        buttonReadyText.text = _localPlayerIsReady ? "Not Ready" : "Ready";
    }

    private void UpdateSpectatorButtonDisplay(bool isSpectator)
    {
        if (spectatorIcon != null) spectatorIcon.SetActive(!isSpectator);
        if (spectatingIcon != null) spectatingIcon.SetActive(isSpectator);
    }
} 