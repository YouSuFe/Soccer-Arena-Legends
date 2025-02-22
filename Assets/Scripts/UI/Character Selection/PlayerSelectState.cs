using System;
using System.Collections.Generic;
using Unity.Netcode;

public struct PlayerSelectionState : INetworkSerializable, IEquatable<PlayerSelectionState>
{
    public ulong ClientId;
    public int CharacterId;
    public int WeaponId;

    public PlayerSelectionState(ulong clientId, int characterId = -1, int weaponId = -1)
    {
        ClientId = clientId;
        CharacterId = characterId;
        WeaponId = weaponId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref CharacterId);
        serializer.SerializeValue(ref WeaponId);
    }

    public bool Equals(PlayerSelectionState other)
    {
        return ClientId == other.ClientId &&
               CharacterId == other.CharacterId &&
               WeaponId == other.WeaponId;
    }
}

public struct PlayerStatusState : INetworkSerializable, IEquatable<PlayerStatusState>
{
    public ulong ClientId;
    public bool IsLockedIn;
    public int TeamIndex;

    public PlayerStatusState(ulong clientId, bool isLockedIn = false, int teamIndex = -1)
    {
        ClientId = clientId;
        IsLockedIn = isLockedIn;
        TeamIndex = teamIndex;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsLockedIn);
        serializer.SerializeValue(ref TeamIndex);
    }

    public bool Equals(PlayerStatusState other)
    {
        return ClientId == other.ClientId &&
               IsLockedIn == other.IsLockedIn &&
               TeamIndex == other.TeamIndex;
    }
}

// It is for late joiners to sync character and weapons
public struct TeamLockData : INetworkSerializable
{
    public int TeamIndex;
    public int[] LockedCharacters;
    public int[] LockedWeapons;

    public TeamLockData(int teamIndex, HashSet<int> lockedCharacters, HashSet<int> lockedWeapons)
    {
        TeamIndex = teamIndex;
        LockedCharacters = new int[lockedCharacters.Count];
        LockedWeapons = new int[lockedWeapons.Count];

        lockedCharacters.CopyTo(LockedCharacters);
        lockedWeapons.CopyTo(LockedWeapons);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TeamIndex);
        serializer.SerializeValue(ref LockedCharacters);
        serializer.SerializeValue(ref LockedWeapons);
    }
}