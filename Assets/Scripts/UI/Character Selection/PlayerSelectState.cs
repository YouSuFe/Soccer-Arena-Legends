using System;
using Unity.Netcode;

public struct PlayerSelectState : INetworkSerializable, IEquatable<PlayerSelectState>
{
    public ulong ClientId;
    public int CharacterId;
    public int WeaponId;
    public bool IsLockedIn;

    public PlayerSelectState(ulong clientId, int characterId = -1, int weaponId = -1, bool isLockedIn = false)
    {
        ClientId = clientId;
        CharacterId = characterId;
        WeaponId = weaponId;
        IsLockedIn = isLockedIn;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref CharacterId);
        serializer.SerializeValue(ref WeaponId);
        serializer.SerializeValue(ref IsLockedIn);
    }

    public bool Equals(PlayerSelectState other)
    {
        return ClientId == other.ClientId &&
               CharacterId == other.CharacterId &&
               WeaponId == other.WeaponId &&
               IsLockedIn == other.IsLockedIn;
    }
}