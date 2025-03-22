using UnityEngine;
using System;

[Serializable]
public class PlayerDebuffData
{
    [field: SerializeField] public PlayerStunnedData PlayerStunnedData { get; private set; }
    [field: SerializeField] public PlayerFrozenData PlayerFrozenData { get; private set; }
    [field: SerializeField] public PlayerShockedData PlayerShockedData { get; private set; }

}
