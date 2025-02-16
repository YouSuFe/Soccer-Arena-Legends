using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameModeSelectionEffect : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image buttonImage;

    [SerializeField] private TextMeshProUGUI titleText; // Reference to title text
    [SerializeField] private TextMeshProUGUI explanationText; // Reference to explanation text

    [SerializeField] private Color selectedColor = Color.white; // Highlight color for button
    [SerializeField] private Color selectedTextColor = Color.yellow; // Highlight color for text
    [SerializeField] private float selectedScale = 1.1f; // Scale for selected button

    private Vector3 originalScale;
    private Color originalColor;
    private Color originalTextColor;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();

        originalScale = rectTransform.localScale;
        originalColor = buttonImage.color;

        // Store original text color from title (assuming title and explanation share the same color)
        if (titleText != null)
        {
            originalTextColor = titleText.color;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            rectTransform.localScale = originalScale * selectedScale;
            buttonImage.color = selectedColor;

            // Change text colors
            if (titleText != null) titleText.color = selectedTextColor;
            if (explanationText != null) explanationText.color = selectedTextColor;
        }
        else
        {
            rectTransform.localScale = originalScale;
            buttonImage.color = originalColor;

            // Reset text colors
            if (titleText != null) titleText.color = originalTextColor;
            if (explanationText != null) explanationText.color = originalTextColor;
        }
    }

    /// <summary>
    /// Resets the button to its original state, restoring all original values.
    /// </summary>
    public void ResetToOriginal()
    {
        rectTransform.localScale = originalScale;
        buttonImage.color = originalColor;

        if (titleText != null) titleText.color = originalTextColor;
        if (explanationText != null) explanationText.color = originalTextColor;
    }

}
