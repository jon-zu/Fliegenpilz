using Fliegenpilz.Net;

namespace Fliegenpilz.Proto;

public readonly struct Vec2(short x, short y) : IEncodePacket, IDecodePacket<Vec2>
{
    public void EncodePacket(ref PacketWriter w)
    {
        w.WriteShort(x);
        w.WriteShort(y);
    }

    public static Vec2 DecodePacket(ref PacketReader r) => new(r.ReadShort(), r.ReadShort());
}