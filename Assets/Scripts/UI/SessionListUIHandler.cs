using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using JetBrains.Annotations;

public class SessionListUIHandler : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public GameObject sessionItemListPrefab;
    public VerticalLayoutGroup verticalLayoutGroup;

    public void ClearList()
    {
        // Delete all children of the vertical layout group
        foreach (Transform child in verticalLayoutGroup.transform)
        {
            Destroy(child.gameObject);
        }

        // Hide the status message
        statusText.gameObject.SetActive(false);
    }

    public void AddToList(SessionInfo sessionInfo)
    {
        // Add a new item to the list
        SessionInfoListUIItem addSessionInfoListUIItem = Instantiate(sessionItemListPrefab, verticalLayoutGroup.transform).GetComponent<SessionInfoListUIItem>();

        addSessionInfoListUIItem.SetInfomation(sessionInfo);

        // Hook up events
        addSessionInfoListUIItem.OnJoinSession += AddedSessionInfoListUIItem_OnJoinSession;

    }

    private void AddedSessionInfoListUIItem_OnJoinSession(SessionInfo obj)
    {

    }

    // �Z�b�V������T�������݂���Ȃ������ۂ̃��\�b�h1
    public void OnNoSessionFound()
    {
        statusText.text = "No game room found";
        statusText.gameObject.SetActive(true);
    }

    // �Z�b�V������T���Ă����
    public void OnLookingForGameSeession()
    {
        statusText.text = "Looking for game room";
        statusText.gameObject.SetActive(true);
    }

}
