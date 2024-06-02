using Fliegenpilz.Net;

namespace Fliegenpilz.Proto;

public readonly struct FootholdId(short id) : IEncodePacket, IDecodePacket<FootholdId>
{
    public static readonly FootholdId None = new(0);

    public void EncodePacket(ref PacketWriter w)
        => w.WriteShort(id);

    public static FootholdId DecodePacket(ref PacketReader r)
        => new(r.ReadShort());
}