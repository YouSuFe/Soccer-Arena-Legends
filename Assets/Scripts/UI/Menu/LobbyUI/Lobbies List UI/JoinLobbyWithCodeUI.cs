using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinLobbyWithCodeUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private Button joinLobbyButton;

    [Header("Popup UI")]
    [SerializeField] private PopupMessageUI popupMessageUI;

    private void Start()
    {
        if (joinLobbyButton != null)
        {
            joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
        }

        LobbyManager.Instance.OnJoinLobbyByCodeSuccess += OnJoinLobbySuccess;
        LobbyManager.Instance.OnJoinLobbyByCodeFailure += OnJoinLobbyFailure;
    }

    private void OnJoinLobbyClicked()
    {
        string lobbyCode = lobbyCodeInputField.text.Trim();

        if (string.IsNullOrEmpty(lobbyCode))
        {
            ShowError("Join Failed", "Lobby code cannot be empty.");
            return;
        }

        Debug.Log("Joining lobby...");
        LobbyManager.Instance.JoinLobbyByCode(lobbyCode);
    }

    private void OnJoinLobbySuccess(string message)
    {
        Debug.Log($"[JoinLobby Success] {message}");
        // Optional: You could show an info popup here, or just let the UI proceed
    }

    private void OnJoinLobbyFailure(string message)
    {
        ShowError("Join Failed", message);
    }

    private void ShowError(string title, string message)
    {
        if (popupMessageUI != null)
        {
            popupMessageUI.Show(title, message, PopupMessageType.Error);
        }
        else
        {
            Debug.LogError($"[Popup Error] {title}: {message}");
        }
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinLobbyByCodeSuccess -= OnJoinLobbySuccess;
        LobbyManager.Instance.OnJoinLobbyByCodeFailure -= OnJoinLobbyFailure;
    }
}
