using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Database", menuName = "Character Selection/Weapons/Database")]
public class WeaponDatabase : ScriptableObject
{
    [SerializeField] private Weapon[] weapons = new Weapon[0];

    public Weapon[] GetAllWeapons() => weapons;

    public Weapon GetWeaponById(int id)
    {
        return weapons.FirstOrDefault(w => w.Id == id);
    }

    public bool IsValidWeaponId(int id)
    {
        return weapons.Any(w => w.Id == id);
    }

    public List<int> GetAllWeaponIds()
    {
        return weapons.Select(w => w.Id).ToList();
    }
}
