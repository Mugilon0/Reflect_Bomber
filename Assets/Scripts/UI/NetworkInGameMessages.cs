using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkInGameMessages : NetworkBehaviour
{
    InGameScoreUIHandler inGameScoreUIHandler;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SendInGameRPCMessages(string userNickName, string message)
    {

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_InGameMessage(string message, RpcInfo info = default)
    {
        Debug.Log($"[RPC] InGameMessage {message}");

        //if (inGameScoreUIHandler != null)
            //inGameScoreUIHandler.OnGameMessageMessageReceived(message);

    }

}
