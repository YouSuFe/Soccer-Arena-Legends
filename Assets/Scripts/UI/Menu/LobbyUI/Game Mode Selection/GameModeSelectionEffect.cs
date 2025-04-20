using UnityEngine;

public class GameModeSelectionEffect : MonoBehaviour
{
    [SerializeField] private GameObject buttonGO; // Actual button GameObject
    [SerializeField] private GameObject hoverGO;  // Hover visual GameObject

    public void SetSelected(bool isSelected)
    {
        buttonGO.SetActive(!isSelected);  // Disable button when selected
        hoverGO.SetActive(isSelected);    // Enable hover when selected
    }

    public void ResetToOriginal()
    {
        buttonGO.SetActive(true);  // Always reset to button visible
        hoverGO.SetActive(false);  // Hover hidden by default
    }
}
