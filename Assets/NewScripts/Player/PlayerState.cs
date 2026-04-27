using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーの状態管理クラス
/// Ready状態・IsSpectatorのみを担う
/// スコアはPlayerScoreで管理する
/// </summary>
public class PlayerState : NetworkBehaviour
{
    // Ready状態
    [Networked(OnChanged = nameof(OnIsReadyChanged))]
    public NetworkBool IsReady { get; private set; }

    // 観戦状態
    [Networked]
    public NetworkBool IsSpectator { get; private set; }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(NetworkBool isReady)
    {
        IsReady = isReady;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleSpectator()
    {
        IsSpectator = !IsSpectator;
        if (IsSpectator) IsReady = false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SendChatMessage(string message)
    {
        var networkPlayer = GetComponent<NetworkPlayer>();
        var chatUI = FindObjectOfType<ChatUIHandler>();
        if (chatUI != null)
            chatUI.AddNewMessage(networkPlayer != null ? networkPlayer.nickName.Value : "???", message);
    }

    static void OnIsReadyChanged(Changed<PlayerState> changed)
    {
        var readyUI = FindObjectOfType<ReadyUIHandler>();
        readyUI?.OnPlayerReadyChanged(changed.Behaviour);
    }
}