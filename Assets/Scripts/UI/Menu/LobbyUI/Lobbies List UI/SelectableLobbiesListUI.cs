using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class SelectableLobbiesListUI : MonoBehaviour
{
    public float doubleClickThreshold = 0.5f; // Time in seconds for a valid double click
    private float lastClickTime = -1f;
    private int clickCount = 0;

    public bool IsSelected { get; private set; } = false;

    // Visual feedback components
    public GameObject selectionBorder;

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
        if (!IsSelected)
        {
            IsSelected = true;
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
        if (IsSelected)
        {
            IsSelected = false;

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

        // Client-side pre-check: Max players
        if (lobby.Players.Count >= lobby.MaxPlayers)
        {
            Debug.LogWarning("Lobby is full! Cannot join.");
            // Show feedback to the player
            return;
        }

        if (lobby != null)
        {
            LobbyScreenUI.Instance.Show();
            LobbyManager.Instance.JoinLobby(lobby, OnJoinLobbySuccess, OnJoinLobbyFailed);
        }
        else
        {
            Debug.LogError("Lobby is null! Ensure SetLobby is called before using this button.");
        }
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
