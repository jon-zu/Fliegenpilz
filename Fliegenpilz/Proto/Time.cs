using Fliegenpilz.Net;

namespace Fliegenpilz.Proto;

public class Time
{
    
}

public readonly struct DurationMs16(TimeSpan dur) : IEncodePacket, IDecodePacket<DurationMs16>
{
    public void EncodePacket(ref PacketWriter w) => w.WriteShort((short)dur.Milliseconds);

    public static DurationMs16 DecodePacket(ref PacketReader reader)
        => new(TimeSpan.FromMilliseconds(reader.ReadShort()));
}

public readonly struct DurationMs32(TimeSpan dur) : IEncodePacket, IDecodePacket<DurationMs32>
{
    public void EncodePacket(ref PacketWriter w) => w.WriteInt(dur.Milliseconds);

    public static DurationMs32 DecodePacket(ref PacketReader reader)
        => new(TimeSpan.FromMilliseconds(reader.ReadInt()));
}