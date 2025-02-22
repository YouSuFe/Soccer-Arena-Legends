using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LockInButtonHandler : MonoBehaviour
{
    private Button lockInButton;

    private void Awake()
    {
        lockInButton = GetComponent<Button>();
    }
    private void Start()
    {
        lockInButton.interactable = false; // Default state
        SelectionNetwork.Instance.OnSelectionStateChanged += UpdateLockInState;
    }

    private void OnDestroy()
    {
        if (SelectionNetwork.Instance != null)
        {
            SelectionNetwork.Instance.OnSelectionStateChanged -= UpdateLockInState;
        }
    }

    private void UpdateLockInState()
    {
        if (!SelectionNetwork.Instance)
        {
            Debug.LogWarning("SelectionNetwork Instance is NULL!");
            return;
        }

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        var playerState = SelectionNetwork.Instance.GetPlayerState(clientId);

        if (playerState == null)
        {
            Debug.LogWarning($"Client {clientId} has NO player state. Lock-in button disabled.");
            lockInButton.interactable = false;
            return;
        }

        PlayerStatusState player = playerState.Value;

        bool canLockIn = !player.IsLockedIn && SelectionNetwork.Instance.CanLockIn(clientId);

        lockInButton.interactable = canLockIn;
    }

}
