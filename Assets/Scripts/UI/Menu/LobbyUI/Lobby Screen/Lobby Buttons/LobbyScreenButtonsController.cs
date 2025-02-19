using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;


public class LobbyScreenButtonsController : MonoBehaviour
{
    [Header("Serialized Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveLobbyButton;

    [Header("Serialized Objects")]
    [SerializeField] private GameObject currentLobbyUI;
    [SerializeField] private GameObject mainMenuUI;


    private void Awake()
    {
        // Ensure the buttons and references are assigned
        if (startGameButton == null || leaveLobbyButton == null)
        {
            Debug.LogError("One or more buttons are not assigned in the Inspector!");
            return;
        }

        // Add listeners to the buttons
        startGameButton.onClick.AddListener(HandleStartGame);
        leaveLobbyButton.onClick.AddListener(HandleLeaveLobby);
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
        UpdateFindMatchButtonInteractable(); // Ensure the find match button state is updated
    }

    private async void HandleStartGame()
    {
        await HostSingleton.Instance.GameManager.StartHostAsync();
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

    private void UpdateFindMatchButtonInteractable()
    {
        startGameButton.interactable = LobbyManager.Instance.IsLobbyHost();
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
    }
}
