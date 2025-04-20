using TMPro;
using UnityEngine;

public class UserProfileNameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;

    private void Start()
    {
        UpdatePlayerName();
    }

    public void UpdatePlayerName()
    {
        string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Aslan Mashadov");
        playerNameText.text = playerName;
    }
}


