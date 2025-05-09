using UnityEngine;

public class UIInputManager : MonoBehaviour
{
    #region Singleton

    public static UIInputManager Instance { get; private set; }

    #endregion

    #region Serialized Fields

    [Header("UI Input Reader")]
    [SerializeField] private InputReader uiInputReader;
    [SerializeField] private InputReader gameplayInputReader;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        uiInputReader.EnableInputActions();
    }

    private void OnEnable()
    {
        if (uiInputReader == null) return;

        uiInputReader.OnOptionTabOpen += HandleOptionsMenuOpen;
        uiInputReader.OnStatisticTabOpen += HandleScoreboardOpen;
        uiInputReader.OnStatisticTabClose += HandleScoreboardClose;
    }

    private void OnDisable()
    {
        if (uiInputReader == null) return;

        uiInputReader.OnOptionTabOpen -= HandleOptionsMenuOpen;
        uiInputReader.OnStatisticTabOpen -= HandleScoreboardOpen;
        uiInputReader.OnStatisticTabClose -= HandleScoreboardClose;
    }

    #endregion

    #region Input Event Handlers

    private void HandleOptionsMenuOpen()
    {
        OptionsUIManager.Instance?.ToggleOptionsMenu();
    }

    private void HandleScoreboardOpen(bool isVisible)
    {
        ScoreboardManager.Instance?.AdjustScoreboardVisibility(isVisible);
    }

    private void HandleScoreboardClose(bool isVisible)
    {
        ScoreboardManager.Instance?.AdjustScoreboardVisibility(isVisible);
    }

    #endregion

    #region Public Methods

    public void DisableUIInputs()
    {
        uiInputReader?.DisableInputActions();
    }

    public void EnableUIInputs()
    {
        uiInputReader?.EnableInputActions();
    }

    private void DisableGameplayInputs()
    {
        gameplayInputReader?.DisableInputActions();
    }

    private void EnableGameplayInputs()
    {
        gameplayInputReader?.EnableInputActions();
    }

    public void HandleOptionsMenuStateChanged(bool isOpen)
    {
        if (isOpen)
        {
            DisableGameplayInputs();
        }
        else
        {
            EnableGameplayInputs();
        }
    }
    #endregion
}
