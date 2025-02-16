using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;


public class LobbyScreenButtonsController : MonoBehaviour
{
    [Header("Serialized Buttons")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeGameModeButton;
    [SerializeField] private Button cancelMatchmakingButton;

    [Header("Serialized Objects")]
    [SerializeField] private GameObject currentLobbyUI;
    [SerializeField] private GameObject mainMenuUI;


    private void Awake()
    {
        // Ensure the buttons and references are assigned
        if (findMatchButton == null || leaveLobbyButton == null || changeGameModeButton == null)
        {
            Debug.LogError("One or more buttons are not assigned in the Inspector!");
            return;
        }

        // Add listeners to the buttons
        findMatchButton.onClick.AddListener(HandleFindMatch);
        leaveLobbyButton.onClick.AddListener(HandleLeaveLobby);
        changeGameModeButton.onClick.AddListener(HandleChangeGameMode);
        cancelMatchmakingButton.onClick.AddListener(HandleCancellingTheMatchmaking);
    }

    private void Start()
    {
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;

        UpdateFindMatchButtonInteractable();


    }

    private void LobbyManager_OnLeftLobby()
    {
        HandleLeaveLobby();
    }

    private void UpdateLobby_Event(Lobby obj)
    {
        changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        UpdateFindMatchButtonInteractable(); // Ensure the find match button state is updated
    }

    private void HandleFindMatch()
    {
        
    }

    private void HandleCancellingTheMatchmaking()
    {
       
    }

    private void HandleLeaveLobby()
    {
        Debug.Log("Leaving Lobby...");
        if (currentLobbyUI.activeSelf)
        {
            LobbyManager.Instance.LeaveLobby();

            currentLobbyUI.SetActive(false); // Close current UI
            mainMenuUI.SetActive(true); // Open the main menu UI
        }
    }

    private void HandleChangeGameMode()
    {
        Debug.Log("Changing Game Mode...");
        LobbyManager.Instance.ChangeGameMode();
    }

    private void UpdateFindMatchButtonInteractable()
    {
        findMatchButton.interactable = LobbyManager.Instance.IsLobbyHost();
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
    }
}
