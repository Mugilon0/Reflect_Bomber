using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI lobbyPlayerCountText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void UpdatePlayerCountUI(int playerCount)
    {
        if (lobbyPlayerCountText != null)
        {
            lobbyPlayerCountText.text = $"Player: {playerCount} / 4";
            // Unity‘¤‚Å“ú–{Œê‚ªŽg‚¦‚È‚¢‚Á‚Û‚¢
        }
    }







}
