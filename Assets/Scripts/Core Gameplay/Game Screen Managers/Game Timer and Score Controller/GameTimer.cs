using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameTimerText;

    private void Start()
    {
        TrySubscribeToGameTimer();

        if (TimerManager.Instance != null)
        {
            float currentTime = TimerManager.Instance.GetGameDurationValue();
            UpdateTimerUI(currentTime);
        }
    }

    private void OnDisable()
    {
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.GameNetworkDuration.OnValueChanged -= HandleGameValueChanged;
        }
    }

    private void TrySubscribeToGameTimer()
    {
        if (TimerManager.Instance != null)
        {
            Debug.Log($"Subscribe from {name} MultiplayerGameStateManager. Subscribtion is happened!");
            TimerManager.Instance.GameNetworkDuration.OnValueChanged += HandleGameValueChanged;
        }
    }

    private void HandleGameValueChanged(float oldVal, float newVal)
    {
        UpdateTimerUI(newVal);
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        float minutes = Mathf.FloorToInt(timeRemaining / 60);
        float seconds = Mathf.FloorToInt(timeRemaining % 60);
        gameTimerText.text = $"{minutes:00}:{seconds:00}";
    }
}
