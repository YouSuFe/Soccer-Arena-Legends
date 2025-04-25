using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCard : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private GameObject visuals;

    [Header("Character UI")]
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text statusText;

    [Header("Weapon UI")]
    [SerializeField] private Image weaponIconImage;

    // ✅ Updates character UI, now using `PlayerSelectionState`
    public void UpdateCharacterDisplay(PlayerSelectionState selection, PlayerStatusState status)
    {
        if (selection.CharacterId != -1)
        {
            var character = characterDatabase.GetCharacterById(selection.CharacterId);
            characterIconImage.sprite = character.Icon;
            characterIconImage.enabled = true;
        }
        else
        {
            characterIconImage.enabled = false;
        }

        statusText.text = status.IsLockedIn ? $"Selected" : $"Selecting...";

        string finalPlayerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unkown");

        // ✅ Player name is now based on `PlayerStatusState`
        playerNameText.text = finalPlayerName;

        if (!visuals.activeSelf)
        {
            visuals.SetActive(true);
        }
    }

    // ✅ Updates weapon UI, now using `PlayerSelectionState`
    public void UpdateWeaponDisplay(PlayerSelectionState selection)
    {
        if (selection.WeaponId != -1)
        {
            var weapon = weaponDatabase.GetWeaponById(selection.WeaponId);
            weaponIconImage.sprite = weapon.Icon;
            weaponIconImage.enabled = true;
        }
        else
        {
            weaponIconImage.enabled = false;
        }
    }

    public void DisableDisplay()
    {
        if (visuals.activeSelf)
        {
            visuals.SetActive(false);
        }
    }
}
