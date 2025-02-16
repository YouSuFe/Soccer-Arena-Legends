using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Character Selection/Weapons/Weapon")]
public class Weapon : ScriptableObject
{
    [SerializeField] private int id = -1;
    [SerializeField] private string displayName = "New Weapon Name";
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject modelPrefab;

    public int Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public GameObject ModelPrefab => modelPrefab;
}
