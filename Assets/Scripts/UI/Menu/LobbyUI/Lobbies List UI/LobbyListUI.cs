using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LobbyFilterUI filterUI;

    [Header("UI")]
    [SerializeField] private Transform lobbySingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button refreshButton;

    private List<Lobby> currentLobbyList = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy the duplicate
            return;
        }

        Instance = this;

        refreshButton.onClick.AddListener(RefreshButtonClick);
    }

    private void Start()
    {
        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnKickedFromLobby;

        filterUI.OnFilterChanged += filter => UpdateLobbyList(currentLobbyList, filter);

        Hide();
    }

    private void LobbyManager_OnKickedFromLobby(Lobby lobby)
    {
        PopupManager.Instance.ShowPopup("Kicked!", "You have been kicked by the host from the lobby!", PopupMessageType.Error);
        Show();
    }

    private void LobbyManager_OnLeftLobby()
    {
        Show();
    }

    private void LobbyManager_OnJoinedLobby(Lobby lobby)
    {
        Hide();
    }

    private void LobbyManager_OnLobbyListChanged(List<Lobby> lobbyList)
    {
        UpdateLobbyList(lobbyList, filterUI?.GetFilterData());
    }

    private void UpdateLobbyList(List<Lobby> lobbyList, FilterData filter = null)
    {
        currentLobbyList = lobbyList;

        foreach (Transform child in container)
        {
            if (child == lobbySingleTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            if (filter != null && !filter.Matches(lobby)) continue;

            Transform lobbySingleTransform = Instantiate(lobbySingleTemplate, container);
            lobbySingleTransform.gameObject.SetActive(true);
            lobbySingleTransform.GetComponent<LobbyListSingleUI>().UpdateLobby(lobby);
        }
    }


    private async void RefreshButtonClick()
    {
        refreshButton.interactable = false;

        LobbyManager.Instance.RefreshLobbyList();

        await Task.Delay(2000); // Optional small delay

        refreshButton.interactable = true;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyListChanged -= LobbyManager_OnLobbyListChanged;
            LobbyManager.Instance.OnJoinedLobby -= LobbyManager_OnJoinedLobby;
            LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
            LobbyManager.Instance.OnKickedFromLobby -= LobbyManager_OnKickedFromLobby;


        }

        filterUI.OnFilterChanged -= filter => UpdateLobbyList(currentLobbyList, filter);
    }

}