using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreationUI : MonoBehaviour
{
    public static LobbyCreationUI Instance { get; private set; }

    [SerializeField] private Button createLobbyButton;
    [SerializeField] private GameModeManager gameModeManager;
    [SerializeField] private GameObject LobbyScreenUI;

    private string lobbyName;
    private bool isPrivate;
    private int maxPlayers = 10;
    private GameEnumsUtil.GameMode gameMode;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void Start()
    {
        gameModeManager.ResetSelectedIndex();
        //createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
    }

    private void OnCreateLobbyClicked()
    {
        LobbyManager.Instance.CreateLobby(
            lobbyName,
            maxPlayers,
            isPrivate,
            gameModeManager.GetSelectedGameMode()
        );

        // Reset selected index for next time
        gameModeManager.ResetSelectedIndex();

        Hide();
        LobbyScreenUI.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
