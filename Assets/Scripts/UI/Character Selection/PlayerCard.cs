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
    [SerializeField] private TMP_Text characterNameText;

    [Header("Weapon UI")]
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TMP_Text weaponNameText;

    // ✅ Updates character UI, now using `PlayerSelectionState`
    public void UpdateCharacterDisplay(PlayerSelectionState selection, PlayerStatusState status)
    {
        if (selection.CharacterId != -1)
        {
            var character = characterDatabase.GetCharacterById(selection.CharacterId);
            characterIconImage.sprite = character.Icon;
            characterIconImage.enabled = true;
            characterNameText.text = character.DisplayName;
        }
        else
        {
            characterIconImage.enabled = false;
            characterNameText.text = "No Character Selected";
        }

        // ✅ Player name is now based on `PlayerStatusState`
        playerNameText.text = status.IsLockedIn ? $"Player {selection.ClientId}" : $"Player {selection.ClientId} (Picking...)";

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
            weaponNameText.text = weapon.DisplayName;
        }
        else
        {
            weaponIconImage.enabled = false;
            weaponNameText.text = "No Weapon Selected";
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
