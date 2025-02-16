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

    public void UpdateCharacterDisplay(PlayerSelectState state)
    {
        if (state.CharacterId != -1)
        {
            var character = characterDatabase.GetCharacterById(state.CharacterId);
            characterIconImage.sprite = character.Icon;
            characterIconImage.enabled = true;
            characterNameText.text = character.DisplayName;
        }
        else
        {
            characterIconImage.enabled = false;
            characterNameText.text = "No Character Selected";
        }

        playerNameText.text = state.IsLockedIn ? $"Player {state.ClientId}" : $"Player {state.ClientId} (Picking...)";

        if (!visuals.activeSelf)
        {
            visuals.SetActive(true);
        }
    }

    public void UpdateWeaponDisplay(PlayerSelectState state)
    {
        if (state.WeaponId != -1)
        {
            var weapon = weaponDatabase.GetWeaponById(state.WeaponId);
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
