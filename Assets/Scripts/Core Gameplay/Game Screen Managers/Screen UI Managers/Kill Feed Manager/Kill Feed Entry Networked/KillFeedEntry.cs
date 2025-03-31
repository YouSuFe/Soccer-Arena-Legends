using Unity.Netcode;
using Unity.Collections;

public enum DeathType
{
    Knife,
    Skill,
    Ball,
    Fall,
    Zone
}

public struct KillFeedEntry : INetworkSerializable
{
    public FixedString32Bytes KillerName;
    public FixedString32Bytes VictimName;
    public DeathType DeathType;
    public int KillerTeamIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref KillerName);
        serializer.SerializeValue(ref VictimName);
        serializer.SerializeValue(ref DeathType);
        serializer.SerializeValue(ref KillerTeamIndex);
    }
}
