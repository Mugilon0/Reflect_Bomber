//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;
//using TMPro;
//using UnityEngine.SceneManagement;
//using Cinemachine;
//using System;
//using UnityEngine.SocialPlatforms;
//using AAMAP;

//public class NetworkPlayer : NetworkBehaviour, IPlayerLeft {
//    [Networked]
//    public PlayerRef Ref { get; private set; }
//    [Networked]
//    public byte Index { get; private set; }

//    public bool IsSpectator => false; // 将来実装、今は常にfalse


//    // --- Server_Init()メソッドを追加 ---

//    public void Server_Init(PlayerRef pRef, byte index)
//    {
//        Ref = pRef;
//        Index = index;
//    }

//    public void PlayerLeft(PlayerRef player)
//    {
//        if (player == Object.InputAuthority)
//            Runner.Despawn(Object);
//    }
//}


