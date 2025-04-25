using TMPro;
using UnityEngine;

public class SelectionTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        if (SelectionNetwork.Instance == null) { return; }

        float timeLeft = SelectionNetwork.Instance.GetRemainingTime();
        timerText.text = $"{Mathf.CeilToInt(timeLeft)}";
    }
}
