using TMPro;
using UnityEngine;

public class UICountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI countdownText;
    public GameObject startCountdownObject;
    public Color finalSecondsColor = Color.red;

    private Color originalColor;

    private void Awake()
    {
        originalColor = countdownText.color;
    }

    private void OnEnable()
    {
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
    }

    private void OnDisable()
    {
        Debug.Log($"On Disable from {name} Countdown Timer. Subscribing...");
        if (TimerManager.Instance != null)
        {
            Debug.Log($"On Disable from {name} Countdown Timer. Subscribtion is happened!");
            TimerManager.Instance.PrepNetworkDuration.OnValueChanged -= HandlePrepValueChanged;
        }
    }

    private void TrySubscribeToPrepTimer()
    {
        Debug.Log($"Subscribe from {name} Countdown Timer. Subscribing...");

        if (TimerManager.Instance != null)
        {
            Debug.Log($"Subscribe from {name} Countdown Timer. Subscribtion is happened!");

            TimerManager.Instance.PrepNetworkDuration.OnValueChanged -= HandlePrepValueChanged;
            TimerManager.Instance.PrepNetworkDuration.OnValueChanged += HandlePrepValueChanged;
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
            if (!startCountdownObject.activeSelf)
                startCountdownObject.SetActive(true);

            int displayTime = Mathf.CeilToInt(timeRemaining);
            countdownText.text = displayTime.ToString();
            countdownText.color = displayTime <= 3 ? finalSecondsColor : originalColor;
        }
        else
        {
            countdownText.text = "GO!";
            countdownText.color = Color.green;
            Invoke(nameof(FinishCountdown), 1f);
        }
    }

    private void FinishCountdown()
    {
        startCountdownObject.SetActive(false);
    }
}
