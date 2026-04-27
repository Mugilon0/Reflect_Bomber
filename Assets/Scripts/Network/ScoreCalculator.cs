using UnityEngine;
using Fusion;

public class ScoreCalculator : NetworkBehaviour
{
    [Header("Score Popup Settings")]
    [SerializeField] private Vector3 popupOffset = new Vector3(1.5f, 2.0f, 0);

    private PlayerScore _playerScore;

    private void Awake()
    {
        _playerScore = GetComponent<PlayerScore>();
    }

    private void OnEnable()
    {
        HPHandler.OnKilled -= OnHit;
        HPHandler.OnKilled += OnHit;
        HPHandler.OnDied -= OnDied;
        HPHandler.OnDied += OnDied;
    }

    private void OnDisable()
    {
        HPHandler.OnKilled -= OnHit;
        HPHandler.OnDied -= OnDied;
    }

    // 攻撃を与えた時（サーバー側でのみ発火するイベントを受け取る）
    private void OnHit(NetworkPlayer attacker, NetworkPlayer victim)
    {
        if (attacker != GetComponent<NetworkPlayer>()) return;

        // サーバー側でのみ発火するのでStateAuthorityチェック不要
        _playerScore.RoundScore++;
        _playerScore.RoundKills++;

        RPC_PlayPointGainSound();
        RPC_ShowScorePopup();
    }

    // 自分が死んだ時（サーバー側でのみ発火するイベントを受け取る）
    private void OnDied(NetworkPlayer victim)
    {
        if (victim != GetComponent<NetworkPlayer>()) return;

        _playerScore.RoundDeaths++;

        int currentScore = _playerScore.RoundScore;
        if (currentScore > 0)
        {
            int penalty = (currentScore / 3) + 1;
            _playerScore.RoundScore = Mathf.Max(0, currentScore - penalty);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_PlayPointGainSound(RpcInfo info = default)
    {
        AudioManager.Play("PointGainSFX", AudioManager.MixerTarget.SFX);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ShowScorePopup(RpcInfo info = default)
    {
        ScorePopupManager.ShowPopup(transform.position, popupOffset);
    }
}