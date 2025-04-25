using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class SelectableLobbiesListUI : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private float doubleClickThreshold = 0.5f; // Time in seconds for a valid double click
    [SerializeField] private GameObject selectionBorder;

    private float lastClickTime = -1f;
    private int clickCount = 0;
    private bool isSelected = false;

    private Lobby lobby;

    private Button thisButton;

    private void Awake()
    {
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(OnClick);

        // Ensure the border is disabled initially
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(false);
        }
    }

    private void OnClick()
    {
        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick < doubleClickThreshold)
        {
            clickCount++;
        }
        else
        {
            clickCount = 1; // Reset if time exceeded
        }

        lastClickTime = Time.time;

        if (clickCount == 1)
        {
            // Select this button
            Select();
        }
        else if (clickCount == 2)
        {
            // Activate logic
            ActivateLogic();
            // Reset selection
            LobbySelectionManager.Instance.ResetSelection();
        }
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
    }

    public void Select()
    {
        if (!isSelected)
        {
            isSelected = true;
            LobbySelectionManager.Instance.SelectButton(this);

            // Show selection visual
            if (selectionBorder != null)
            {
                selectionBorder.SetActive(true);
            }

            Debug.Log($"{gameObject.name} is selected.");
        }
    }

    public void Deselect()
    {
        if (isSelected)
        {
            isSelected = false;

            // Hide selection visual
            if (selectionBorder != null)
            {
                selectionBorder.SetActive(false);
            }

            Debug.Log($"{gameObject.name} is deselected.");
        }
    }

    private void ActivateLogic()
    {
        if (lobby == null)
        {
            Debug.LogError("Lobby is null! Ensure SetLobby is called before using this button.");
            return;
        }

        // ✅ Client-side pre-check: Custom Max players from lobby.Data
        if (lobby.Data.TryGetValue(LobbyManager.KEY_MAX_PLAYERS, out var maxPlayersData))
        {
            int softMaxPlayers = int.Parse(maxPlayersData.Value);

            if (lobby.Players.Count >= softMaxPlayers)
            {
                Debug.LogWarning("Lobby is full (according to custom MaxPlayers)!");

                PopupManager.Instance.ShowPopup("Lobby Full", "This lobby is already full. Please join another one.", PopupMessageType.Error);

                return;
            }
        }
        else
        {
            Debug.LogWarning("No custom MaxPlayers found in lobby.Data! Joining anyway...");
        }

        // ✅ Proceed to join if allowed
        LobbyScreenUI.Instance.Show();
        LobbyManager.Instance.JoinLobby(lobby, OnJoinLobbySuccess, OnJoinLobbyFailed);
    }


    private void OnJoinLobbySuccess()
    {
        Debug.Log("Successfully joined the lobby!");
        LobbyScreenUI.Instance.Show();
    }

    private void OnJoinLobbyFailed(string errorMessage)
    {
        Debug.LogError($"Failed to join lobby: {errorMessage}");
        // Show error feedback to the player
    }
}
