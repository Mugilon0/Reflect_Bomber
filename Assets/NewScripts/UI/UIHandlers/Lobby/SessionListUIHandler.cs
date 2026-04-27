using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class SessionListUIHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject sessionItemListPrefab;
    [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;

    // FindObjectOfTypeの代わりにイベントで通知
    public event Action<SessionInfo> OnJoinSessionRequested;

    private void Awake()
    {
        ClearList();
    }

    public void ClearList()
    {
        foreach (Transform child in verticalLayoutGroup.transform)
            Destroy(child.gameObject);
        statusText.gameObject.SetActive(false);
    }

    public void AddToList(SessionInfo sessionInfo)
    {
        var item = Instantiate(sessionItemListPrefab, verticalLayoutGroup.transform)
            .GetComponent<SessionInfoListUIItem>();
        item.SetInfomation(sessionInfo);
        item.OnJoinSession += OnJoinSession;
    }

    private void OnJoinSession(SessionInfo sessionInfo)
    {
        // FindObjectOfTypeを使わずイベントで通知
        OnJoinSessionRequested?.Invoke(sessionInfo);
    }

    public void ShowNoSessionFound()
    {
        ClearList();
        statusText.text = "No game room found";
        statusText.gameObject.SetActive(true);
    }

    public void ShowSearching()
    {
        ClearList();
        statusText.text = "Looking for game room...";
        statusText.gameObject.SetActive(true);
    }
}