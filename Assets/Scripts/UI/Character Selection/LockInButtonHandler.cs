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
        if (!SelectionNetwork.Instance) return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        var playerState = SelectionNetwork.Instance.GetPlayerState(clientId);

        if (playerState == null)
        {
            lockInButton.interactable = false;
            return;
        }

        PlayerSelectState player = playerState.Value;

        bool canLockIn = !player.IsLockedIn &&
                         SelectionNetwork.Instance.CanLockIn(clientId);

        lockInButton.interactable = canLockIn;
    }

}
