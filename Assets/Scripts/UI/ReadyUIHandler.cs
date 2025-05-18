using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReadyUIHandler : NetworkBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI buttonReadyText;
    public TextMeshProUGUI countDownText;

    bool isReady = false;


    // カウントダウン
    TickTimer countDownTickTimer = TickTimer.None;

    [Networked(OnChanged = nameof(OnCountdownChanged))]
    byte countDown { get; set; }

    void Start()
    {
        countDownText.text = "";
    }

    private void Update()
    {
        if (countDownTickTimer.Expired(Runner))
        {
            StartGame();

            countDownTickTimer = TickTimer.None;
        }
        else if (countDownTickTimer.IsRunning)
        {
            countDown = (byte)countDownTickTimer.RemainingTime(Runner);
        }

    }


    void StartGame()
    {
        //Lock the session, so no other client can join
        Runner.SessionInfo.IsOpen = false;


        GameObject[] gameObjectsToTransfer = GameObject.FindGameObjectsWithTag("Player"); // playerをすべて取得し、配列に格納する

        foreach (GameObject gameObjectToTransfer in gameObjectsToTransfer)
        {
            // playerがworld1にスポーンする際に破壊されないようにする
            DontDestroyOnLoad(gameObjectToTransfer);

            ////Check if the player is ready チェックしていないクライアントをキックする
            //if (!gameObjectToTransfer.GetComponent<CharacterOutfitHandler>().isDoneWithCharacterSelection)
            //    Runner.Disconnect(gameObjectToTransfer.GetComponent<NetworkObject>().InputAuthority);

        }

        //Update scene for the network
        Runner.SetActiveScene("World1");

    }

    public void OnReady()
    {
        if (isReady)
            isReady = false;
        else isReady = true;


        if (isReady)
        {
            buttonReadyText.text = "Not Ready";
        }
        else
        {
            buttonReadyText.text = "Ready";
        }


        if (Runner.IsServer)
        {
            if (isReady)
                countDownTickTimer = TickTimer.CreateFromSeconds(Runner, 10);
            else
            {
                countDownTickTimer = TickTimer.None;
                countDown = 0;
            }
        }

        NetworkPlayer.Local.GetComponent<CharacterOutfitHandler>().OnReady(isReady);
    }

    static void OnCountdownChanged(Changed<ReadyUIHandler> changed)
    {
        changed.Behaviour.OnCountdownChanged();
    }

    private void OnCountdownChanged()
    {
        if (countDown == 0) //開始しようとしていないとき
            countDownText.text = $"";
        else countDownText.text = $"Battle starts in {countDown}";
    }

}
