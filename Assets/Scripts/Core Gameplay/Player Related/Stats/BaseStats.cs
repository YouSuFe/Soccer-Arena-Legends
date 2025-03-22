using UnityEngine;

[CreateAssetMenu(fileName = "BaseStats", menuName = "Game Screen Data/Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    public int health = 100;
    public int strength = 20;
    public int speed = 10;
}
