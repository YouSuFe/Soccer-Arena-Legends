using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject disabledOverlay;
    [SerializeField] private Button button;

    private WeaponSelectDisplay weaponSelect;

    public Weapon Weapon { get; private set; }
    public bool IsDisabled { get; private set; }

    public void SetWeapon(WeaponSelectDisplay weaponSelect, Weapon weapon)
    {
        iconImage.sprite = weapon.Icon;
        this.weaponSelect = weaponSelect;
        Weapon = weapon;
    }

    public void SelectWeapon()
    {
        weaponSelect.Select(Weapon);
    }

    public void SetDisabled()
    {
        IsDisabled = true;
        disabledOverlay.SetActive(true);
        button.interactable = false;
    }
}