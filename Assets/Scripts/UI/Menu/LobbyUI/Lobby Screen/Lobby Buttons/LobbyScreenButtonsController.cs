using System.Threading.Tasks;
using QFSW.QC;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;


public class LobbyScreenButtonsController : MonoBehaviour
{
    [Header("Serialized Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeTeamButton;

    [Header("Serialized Objects")]
    [SerializeField] private GameObject currentLobbyUI;

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
        changeTeamButton.onClick.AddListener(HandleChangeTeam);
    }

    private void Start()
    {
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;

        UpdateButtonStates();
    }

    private void LobbyManager_OnLeftLobby()
    {
        HandleLeaveLobby();
    }

    private void UpdateLobby_Event(Lobby obj)
    {
        UpdateButtonStates(); // Ensure the find match button state is updated
    }

    private async void HandleStartGame()
    {
        // Disable to prevent spam
        startGameButton.interactable = false;

        var latestLobby = await LobbyManager.Instance.GetLatestLobbyAsync();
        if (latestLobby == null || !LobbyManager.Instance.CanStartGame())
        {
            Debug.LogWarning("Cannot start game due to invalid team setup.");
            startGameButton.interactable = true; // 🔓 Re-enable
            return;
        }

        Debug.Log("Starting game...");
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

#if UNITY_EDITOR
    // ToDo: Delete this after
    [Command]
    public async static void StartGameSession()
    {

        Debug.Log("Starting game...");
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }
#endif
    private async void HandleChangeTeam()
    {
        if (!LobbyManager.Instance.CanSwitchTeam())
        {
            Debug.Log("You can't switch teams right now.");
            return;
        }

        changeTeamButton.interactable = false;


        var playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        int currentTeamIndex = LobbyManager.Instance.GetPlayerTeamIndex(playerId);
        GameEnumsUtil.PlayerTeam newTeam = currentTeamIndex == (int)GameEnumsUtil.PlayerTeam.Blue
            ? GameEnumsUtil.PlayerTeam.Red
            : GameEnumsUtil.PlayerTeam.Blue;

        LobbyManager.Instance.UpdatePlayerTeam(newTeam);

        await Task.Delay(500); // Optional small delay

        changeTeamButton.interactable = true; // Re-enable after operation
    }

    private void HandleLeaveLobby()
    {
        Debug.Log("Leaving Lobby...");
        if (currentLobbyUI.activeSelf)
        {
            LobbyManager.Instance.LeaveLobby();
        }
    }

    /// <summary>
    /// Update UI button states (visibility + interactability)
    /// based on role (host or player) and team balance logic
    /// </summary>
    private void UpdateButtonStates()
    {
        bool isHost = LobbyManager.Instance.IsLobbyHost();

        // Start Game Button
        startGameButton.gameObject.SetActive(isHost);
        startGameButton.interactable = LobbyManager.Instance.CanStartGame();

        // Change Team Button (always visible, only interactable when allowed)
        changeTeamButton.gameObject.SetActive(!isHost);
        changeTeamButton.interactable = !isHost && LobbyManager.Instance.CanSwitchTeam();
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
    }
}
