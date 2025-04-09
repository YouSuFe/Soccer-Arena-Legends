using UnityEngine;
/// <summary>
/// The Presenter in the MVP architecture that bridges the PlayerModelManager (Model) and PlayerUIManager (View).
/// Listens to model events and updates the UI accordingly.
/// </summary>
public class HUDCanvasManager : MonoBehaviour
{
    public static HUDCanvasManager Instance { get; private set; }

    [SerializeField] private PlayerUIController playerUIController;
    public PlayerUIController PlayerUIController => playerUIController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Attach the UI to this player's data
    /// </summary>
    public void AttachToPlayer(PlayerAbstract player)
    {
        if (player == null)
        {
            Debug.LogWarning("Trying to attach HUD to a null player.");
            return;
        }

        Debug.Log($"[HUD] Attaching HUD to player: {player.name}");

        playerUIController.Cleanup(); // safely dispose previous
        playerUIController.Initialize(player, player.Stats, player.Stats.Mediator);

        // Give the player a reference to the HUD
        player.SetPlayerUIManager(playerUIController);
    }

    public void CleanupHUD()
    {
        playerUIController.Cleanup();
    }
}