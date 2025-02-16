using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinLobbyWithCodeUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private Button joinLobbyButton;

    private TextMeshProUGUI feedbackText;

    private void Start()
    {
        if (joinLobbyButton != null)
        {
            joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
        }

        LobbyManager.Instance.OnJoinLobbyByCodeSuccess += OnJoinLobbySuccess;
        LobbyManager.Instance.OnJoinLobbyByCodeFailure += OnJoinLobbyFailure;
    }


    private void OnJoinLobbySuccess(string message)
    {
        DisplayFeedback(message);
    }

    private void OnJoinLobbyFailure(string message)
    {
        DisplayFeedback(message);
    }

    private void OnJoinLobbyClicked()
    {
        string lobbyCode = lobbyCodeInputField.text.Trim();

        if (string.IsNullOrEmpty(lobbyCode))
        {
            DisplayFeedback("Lobby code cannot be empty.");
            return;
        }

        DisplayFeedback("Joining lobby...");
        LobbyManager.Instance.JoinLobbyByCode(lobbyCode);
    }

    private void DisplayFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        Debug.Log(message);
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinLobbyByCodeSuccess -= OnJoinLobbySuccess;
        LobbyManager.Instance.OnJoinLobbyByCodeFailure -= OnJoinLobbyFailure;
    }
}
