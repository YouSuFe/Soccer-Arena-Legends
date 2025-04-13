using TMPro;
using UnityEngine;

public class UICountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject startCountdownObject;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color finalSecondsColor = Color.red;

    private Color originalColor;

    private void Awake()
    {
        originalColor = defaultColor;
    }

    private void OnEnable()
    {
        Debug.Log($"On Enable from {name} Countdown Timer. Try to Subscribing...");

        TrySubscribeToPrepTimer();
    }

    private void Start()
    {
        TrySubscribeToPrepTimer();


        // Initialize immediately
        if (TimerManager.Instance != null)
        {
            float time = TimerManager.Instance.GetPrepDurationValue();
            UpdatePrepUI(time);
        }

        // Disable object after subscription and initialization happened.
        startCountdownObject.SetActive(false); // Start hidden
    }

    private void OnDisable()
    {
        Debug.Log($"On Disable from {name} Countdown Timer. Subscribing...");
        if (TimerManager.Instance != null)
        {
            Debug.Log($"On Disable from {name} Countdown Timer. Subscribtion is happened!");
            TimerManager.Instance.PrepNetworkDuration.OnValueChanged -= HandlePrepValueChanged;
        }

        if (MultiplayerGameStateManager.Instance != null)
        {
            Debug.Log($"Subscribe from {name} MultiplayerGameStateManager. Subscribtion is happened!");
            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged -= HandleGameStateChanged;

        }
        else
        {
            Debug.LogWarning($"Subscribe from {name} MultiplayerGameStateManager. Subscribtion is not happened!");

        }
    }

    private void TrySubscribeToPrepTimer()
    {
        if (TimerManager.Instance != null)
        {
            Debug.Log($"Subscribe from {name} Countdown Timer. Subscribtion is happened!");
            TimerManager.Instance.PrepNetworkDuration.OnValueChanged -= HandlePrepValueChanged;
            TimerManager.Instance.PrepNetworkDuration.OnValueChanged += HandlePrepValueChanged;
        }
        else
        {
            Debug.LogWarning($"Subscribe from {name} Countdown Timer. Timer MAnager is null");

        }

        if (MultiplayerGameStateManager.Instance != null )
        {
            Debug.Log($"Subscribe from {name} MultiplayerGameStateManager. Subscribtion is happened!");
            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged -= HandleGameStateChanged;
            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged += HandleGameStateChanged;

        }
        else
        {
            Debug.LogWarning($"Subscribe from {name} MultiplayerGameStateManager. Subscribtion is not happened!");

        }
    }

    private void HandleGameStateChanged(GameState previous, GameState newState)
    {
        if (newState == GameState.PreGame)
        {
            startCountdownObject.SetActive(true); // Show countdown
            UpdatePrepUI(TimerManager.Instance.GetPrepDurationValue()); // Force update immediately
            Debug.Log($"UI Countdown Prep Duration Value is {TimerManager.Instance.GetPrepDurationValue()}");
        }
        else
        {
            startCountdownObject.SetActive(false); // Hide when no longer in PreGame
        }
    }

    private void HandlePrepValueChanged(float oldVal, float newVal)
    {
        UpdatePrepUI(newVal);
    }

    private void UpdatePrepUI(float timeRemaining)
    {
        if (timeRemaining > 0)
        {
            int displayTime = Mathf.CeilToInt(timeRemaining);
            countdownText.text = displayTime.ToString();
            countdownText.color = displayTime <= 3 ? finalSecondsColor : originalColor;
        }
        else
        {
            countdownText.text = "GO!";
            countdownText.color = originalColor;
            Invoke(nameof(FinishCountdown), 1f);
        }
    }

    private void FinishCountdown()
    {
        startCountdownObject.SetActive(false);
    }
}
