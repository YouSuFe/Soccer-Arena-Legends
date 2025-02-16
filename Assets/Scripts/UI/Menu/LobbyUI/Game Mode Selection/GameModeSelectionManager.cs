using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    [SerializeField] private GameModeSelectionEffect[] gameModeButtons; // Assign buttons in Inspector
    private int selectedIndex = -1;

    private void OnEnable()
    {
        if(selectedIndex != -1)
        {
            ResetSelectedIndex();
        }
    }

    public void OnGameModeClicked(int index)
    {
        // Deselect previously selected button
        if (selectedIndex >= 0)
            gameModeButtons[selectedIndex].SetSelected(false);

        // Select new button
        selectedIndex = index;
        gameModeButtons[selectedIndex].SetSelected(true);

        Debug.Log($"Game Mode selected : {GetSelectedGameMode().ToString()}");
    }

    public void ResetSelectedIndex()
    {
        selectedIndex = -1;

        // Return original version of object
        foreach(GameModeSelectionEffect effect in gameModeButtons)
        {
            effect.ResetToOriginal();
        }
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    public LobbyManager.LobbyGameMode GetSelectedGameMode()
    {
        if (selectedIndex >= 0)
        {
            Debug.Log("Selected index : " + selectedIndex);
            return (LobbyManager.LobbyGameMode)selectedIndex;
        }
        Debug.Log("Selected index : " + selectedIndex);

        return 0; // Automatically first game mode is selected
    }
}
