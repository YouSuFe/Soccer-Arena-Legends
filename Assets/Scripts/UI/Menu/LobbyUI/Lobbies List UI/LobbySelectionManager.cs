using UnityEngine;

public class LobbySelectionManager : MonoBehaviour
{
    public static LobbySelectionManager Instance { get; private set; }

    private SelectableLobbiesListUI currentSelectedButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void SelectButton(SelectableLobbiesListUI newButton)
    {
        // Deselect current button if one is selected
        if (currentSelectedButton != null && currentSelectedButton != newButton)
        {
            currentSelectedButton.Deselect();
        }

        // Set the new button as the selected button
        currentSelectedButton = newButton;
    }

    public void ResetSelection()
    {
        if (currentSelectedButton != null)
        {
            currentSelectedButton.Deselect();
            currentSelectedButton = null;
        }
    }
}
