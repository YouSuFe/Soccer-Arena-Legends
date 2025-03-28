using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI Instance { get; private set; }

    [SerializeField] private Transform lobbySingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button refreshButton;

    private void Awake() {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Öylemiymiş yav, hadi yav");
            Destroy(gameObject); // Destroy the duplicate
            return;
        }

        Instance = this;

        refreshButton.onClick.AddListener(RefreshButtonClick);
    }

    private void Start() {
        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnKickedFromLobby;

        Hide();
    }



    private void LobbyManager_OnKickedFromLobby(Lobby lobby) {
        Show();
    }

    private void LobbyManager_OnLeftLobby() {
        Show();
    }

    private void LobbyManager_OnJoinedLobby(Lobby lobby) {
        Hide();
    }

    private void LobbyManager_OnLobbyListChanged(List<Lobby> lobbyList) {
        UpdateLobbyList(lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList) {
        if (container == null)
        {
            Debug.Log("Container is null");
            if(lobbySingleTemplate == null)
            {
                Debug.Log("Template is null");
                return;
            }
            return;
        }

        foreach (Transform child in container) {
            if (child == lobbySingleTemplate) continue;

            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList) {
            Transform lobbySingleTransform = Instantiate(lobbySingleTemplate, container);
            lobbySingleTransform.gameObject.SetActive(true);
            LobbyListSingleUI lobbyListSingleUI = lobbySingleTransform.GetComponent<LobbyListSingleUI>();
            lobbyListSingleUI.UpdateLobby(lobby);
        }
    }

    private async void RefreshButtonClick()
    {
        refreshButton.interactable = false;

        LobbyManager.Instance.RefreshLobbyList();

        await Task.Delay(2000); // Optional small delay

        refreshButton.interactable = true;
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void Show() {
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
    }

}