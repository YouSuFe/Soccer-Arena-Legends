using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    #region Singleton

    public static ScoreboardManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance of ScoreboardManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion

    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private ScoreboardUI matchScoreboardUI;

    #endregion

    #region MonoBehaviour Methods

    private void Start()
    {
        // Subscribe to all stat changes across all connected players
        foreach (var kvp in GameManager.Instance.GetAllBoundStats())
        {
            var statSync = kvp.Value;
            statSync.OnStatsChanged += HandleStatChanged;
        }

        // Optionally hide scoreboard on game start
        AdjustScoreboardVisibility(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks or null refs
        if (GameManager.Instance == null) return;

        foreach (var kvp in GameManager.Instance.GetAllBoundStats())
        {
            var statSync = kvp.Value;
            statSync.OnStatsChanged -= HandleStatChanged;
        }
    }

    #endregion

    #region Scoreboard Logic

    /// <summary>
    /// Toggles the visibility of the in-match scoreboard UI.
    /// </summary>
    /// <param name="value">True to show, false to hide.</param>
    public void AdjustScoreboardVisibility(bool value)
    {
        matchScoreboardUI?.ToggleVisibility(value);
    }

    /// <summary>
    /// Called when any stat value changes on a player.
    /// Refreshes the scoreboard if it's currently visible.
    /// </summary>
    private void HandleStatChanged()
    {
        if (matchScoreboardUI != null && matchScoreboardUI.gameObject.activeSelf)
        {
            matchScoreboardUI.Refresh();
        }
    }

    /// <summary>
    /// Refreshes the scoreboard UI only if it is currently visible.
    /// This is used for stat updates (like accidental goals) that are not part of PlayerStatSync,
    /// so changes can still be reflected in real-time without needing the user to close and reopen the scoreboard.
    /// </summary>
    public void RefreshIfVisible()
    {
        if (matchScoreboardUI != null && matchScoreboardUI.gameObject.activeSelf)
        {
            matchScoreboardUI.Refresh();
        }
    }


    #endregion
}
