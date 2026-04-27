using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Lobby")]
    [SerializeField] private UIScreen lobbySelectPanel;

    [Header("Ready")]
    [SerializeField] private UIScreen readyPanel;

    [Header("Intro")]
    [SerializeField] private Animator introAnimator; // IntroUIのAnimator

    [Header("Others")]
    [SerializeField] private UIScreen loadingUI;
    [SerializeField] private UIScreen gameUI;
    [SerializeField] private UIScreen outroUI;
    [SerializeField] private UIScreen postGameUI;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        UIScreen.Focus(lobbySelectPanel);
    }

    public void Subscribe(GameStateManager gameStateManager)
    {
        gameStateManager.OnStateChanged += HandleStateChanged;
    }

    public void Unsubscribe(GameStateManager gameStateManager)
    {
        gameStateManager.OnStateChanged -= HandleStateChanged;
    }

    public void StartCountdown()
    {
        introAnimator?.SetTrigger("StartCountdown");
    }

    private void HandleStateChanged(EGameStateNet newState)
    {
        Debug.Log($"HandleStateChanged: {newState}, activeScreen: {UIScreen.activeScreen?.gameObject.name}");

        switch (newState)
        {
            case EGameStateNet.Lobby: UIScreen.Focus(lobbySelectPanel); break;
            case EGameStateNet.Ready: UIScreen.Focus(readyPanel); break;
            case EGameStateNet.Loading: UIScreen.Focus(loadingUI); break;
            case EGameStateNet.Intro:
                // GameUIを表示してカウントダウンをオーバーレイで再生
                UIScreen.Focus(gameUI);
                StartCountdown();
                break;
            case EGameStateNet.Game:
                // IntroでGameUIが既に表示されているので何もしない
                break;
            case EGameStateNet.Outro:
                //Debug.Log("Outroに入った");
                UIScreen.Focus(outroUI); break;
            case EGameStateNet.PostGame: UIScreen.Focus(postGameUI); break;
        }
    }
}
