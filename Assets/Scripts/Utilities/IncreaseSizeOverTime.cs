using UnityEngine;

public class IncreaseSizeOverTime : MonoBehaviour
{
    [SerializeField] private Vector3 maxScale = new Vector3(5f, 5f, 5f); // Maximum scale of the object
    [SerializeField] private float scaleMultiplier = 1.01f; // Multiplier to increase scale exponentially

    private Vector3 currentScale;
    private CountdownTimer countdownTimer;

    void Start()
    {
        // Initialize the current scale with the object's initial scale
        currentScale = transform.localScale;

        // Calculate the time it takes to reach max scale based on the largest dimension
        float timeX = Mathf.Log(maxScale.x / currentScale.x) / Mathf.Log(scaleMultiplier);
        float timeY = Mathf.Log(maxScale.y / currentScale.y) / Mathf.Log(scaleMultiplier);
        float timeZ = Mathf.Log(maxScale.z / currentScale.z) / Mathf.Log(scaleMultiplier);
        float timeToMaxScale = Mathf.Max(timeX, timeY, timeZ);

        // Initialize the CountdownTimer with the calculated time
        countdownTimer = new CountdownTimer(timeToMaxScale);
        countdownTimer.OnTimerStop += DestroyGameObject; // Subscribe to the OnTimerStop event
        countdownTimer.Start();
    }

    void Update()
    {
        // Exponentially increase the scale over time
        if (currentScale.x < maxScale.x && currentScale.y < maxScale.y && currentScale.z < maxScale.z)
        {
            currentScale *= scaleMultiplier;
            transform.localScale = Vector3.Min(currentScale, maxScale); // Ensure scale does not exceed maxScale
        }

        // Tick the countdown timer
        countdownTimer.Tick(Time.deltaTime);
    }

    private void DestroyGameObject()
    {
        Destroy(gameObject);
        countdownTimer.OnTimerStop -= DestroyGameObject; // Subscribe to the OnTimerStop event

    }
}
