using UnityEngine;

public class OptionsUIManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private GameObject optionsUI;

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

    #region MonoBehaviour Methods

    private void Start()
    {
        optionsUI.SetActive(false);
    }

    public void AdjustOptionsUIVisibility()
    {
        if(optionsUI.activeSelf)
        {
            optionsUI.SetActive(false);
        }
        else
        {
            optionsUI.SetActive(true);
        }
    }

    #endregion
}
