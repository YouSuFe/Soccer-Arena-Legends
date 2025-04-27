using UnityEngine;

public class OptionsUIManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private OptionsUIController optionsUIController;

    #endregion

    #region Singleton

    public static OptionsUIManager Instance { get; private set; }

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

    #region Public Methods

    public void ToggleOptionsMenu()
    {
        optionsUIController.ToggleOptionsMenu();
    }

    #endregion
}

