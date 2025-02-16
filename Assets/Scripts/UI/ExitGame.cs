using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitGame : NetworkBehaviour
{
    public void LeaveGame()
    {
        if (IsServer)
        {
            // Server-side cleanup
            HandleServerCleanup();
            if (LobbyManager.Instance.IsLobbyHost())
            {
                Debug.Log("It is host. Try to make state False from SERVER");
                LobbyManager.Instance.UpdateMatchmakingState(false);
            }
            else
            {
                Debug.Log("It is not host, so client from SERVER.");
            }
        }
        else
        {
            // Client disconnect
            ClientSingleton.Instance.GameManager.Disconnect();
            if (LobbyManager.Instance.IsLobbyHost())
            {
                Debug.Log("It is host. Try to make state Flase");
                LobbyManager.Instance.UpdateMatchmakingState(false);
                Debug.Log("It is host. Clear the lobby server");
                LobbyManager.Instance.ClearLobbyServerDetails();
            }
            else
            {
                Debug.Log("It is not host, so client.");
            }

        }
    }

    public void EndGame()
    {
        Debug.Log("Ending the skill game");
    }

    private void HandleServerCleanup()
    {
        // Notify clients that the game is ending
        NotifyClientsGameEnded();

        // Clear lobby server details
        LobbyManager.Instance.ClearLobbyServerDetails();

        // Shut down the NetworkManager
        NetworkManager.Singleton.Shutdown();
        Debug.Log("Server shutting down and cleaning up resources.");

        // Transition to menu
        SceneManager.LoadScene("MenuSceneName");
    }

    private void NotifyClientsGameEnded()
    {
        Debug.Log("Notifying clients that the game has ended...");
        EndGameClientRpc();
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
        // Clients disconnect and transition to menu
        ClientSingleton.Instance.GameManager.Disconnect();
    }
}
