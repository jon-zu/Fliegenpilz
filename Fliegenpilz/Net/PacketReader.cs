using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using DotNext.Buffers;
using Fliegenpilz.Proto;

namespace Fliegenpilz.Net;

public static class PacketConstants
{
    public const int HeaderSize = 2;
}


public ref struct PacketReader(ReadOnlySequence<byte> seq)
{
    private SequenceReader<byte> _inner = new(seq);

    public PacketReader(IMemoryOwner<byte> owner)
        : this(new ReadOnlySequence<byte>(owner.Memory))
    {
    }
    
    public short ReadShort()
    {
        return _inner.TryReadLittleEndian(out short result) ? result : throw new InvalidOperationException();
    }
    
    public ushort ReadUShort()
    {
        return unchecked((ushort)this.ReadShort());
    }
    
    public int ReadInt()
    {
        return _inner.TryReadLittleEndian(out int result) ? result : throw new InvalidOperationException();
    }
    
    public uint ReadUInt()
    {
        return unchecked((uint)this.ReadInt());
    }
    
    public long ReadLong()
    {
        return _inner.TryReadLittleEndian(out long result) ? result : throw new InvalidOperationException();
    }
    
    public ulong ReadULong()
    {
        return unchecked((ulong)this.ReadLong());
    }

    public FileTime ReadTime()
    {
        return new FileTime(this.ReadLong());
    }

    public ReadOnlySequence<byte> ReadBytes(int len)
    {
        return _inner.TryReadExact(len, out var seq) ? seq : throw new InvalidOperationException();
    }
    
    public string ReadString()
    {
        var len = ReadShort();
        return len switch
        {
            < 0 => throw new InvalidOperationException(),
            0 => string.Empty,
            _ => ReadFixedString(len)
        };
    }

    public string ReadFixedString(int len)
    {
        return Encoding.Latin1.GetString(ReadBytes(len));
    }
    
    public UInt128 ReadUInt128()
    {
        Span<int> span =
        [
            ReadInt(),
            ReadInt(),
            ReadInt(),
            ReadInt()
        ];
        span.Reverse();
        
        var bytes = MemoryMarshal.AsBytes(span);
        return BinaryPrimitives.ReadUInt128LittleEndian(bytes);
    }

    public TimeSpan ReadDurMs16()
    {
        return TimeSpan.FromMilliseconds(ReadShort());
    }

    public TimeSpan ReadDurMs32()
    {
        return TimeSpan.FromMilliseconds(ReadInt());
    }

    public byte ReadByte()
    {
        return _inner.TryRead(out byte result) ? result : throw new InvalidOperationException();
    }

    public bool ReadBool()
    {
        return this.ReadByte() != 0;
    }

    public T Read<T>() where T : IDecodePacket<T>
    {
        return T.DecodePacket(ref this);
    }
}

public ref struct PacketWriter
{
    private BufferWriterSlim<byte> _inner;

    public PacketWriter(int initialCapacity = 4096, MemoryAllocator<byte>? allocator = null)
    {
        _inner = new BufferWriterSlim<byte>(initialCapacity);
    }
    
    public MemoryOwner<byte> DetachBuffer()
    {
        return _inner.TryDetachBuffer(out var owner) ? owner : throw new InvalidOperationException();
    }

    public void WriteOpcode(SendOpcodes opcode)
    {
        WriteShort((short)opcode);
    }
    
    public void WriteShort(short value)
    {
        _inner.WriteLittleEndian(value);
    }
    
    public void WriteUShort(ushort value)
    {
        this.WriteShort(unchecked((short)value));
    }
    
    public void WriteInt(int value)
    {
        _inner.WriteLittleEndian(value);
    }
    
    public void WriteUInt(uint value)
    {
        this.WriteInt(unchecked((int)value));
    }
    
    public void WriteLong(long value)
    {
        _inner.WriteLittleEndian(value);
    }
    
    public void WriteULong(ulong value)
    {
        WriteLong(unchecked((long)value));
    }
    
    public void WriteTime(FileTime value)
    {
        WriteLong(value.RawValue);
    }
    
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _inner.Write(value);
    }
    
    public void WriteString(string value)
    {
        WriteShort((short)value.Length);
        WriteFixedString(value);
    }
    
    public void WriteFixedString(string value)
    {
        WriteBytes(Encoding.Latin1.GetBytes(value));
    }

    public void WriteFixedSizeString(string value, int len)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        if(bytes.Length + 1 > len)
            throw new InvalidOperationException();

        WriteBytes(bytes);
        for(var i = bytes.Length; i < len; i++)
            WriteByte(0);
    }
    
    public void WriteUInt128(UInt128 value)
    {
        Span<byte> span = stackalloc byte[16];
        BinaryPrimitives.WriteUInt128LittleEndian(span, value);
        
        var data = MemoryMarshal.Cast<byte, int>(span);
        for(var i = 4; i > 0; i--)
            WriteInt(data[i - 1]);
    }
    
    public void WriteDurMs16(TimeSpan value)
    {
        WriteShort((short)value.TotalMilliseconds);
    }
    
    public void WriteDurMs32(TimeSpan value)
    {
        WriteInt((int)value.TotalMilliseconds);
    }

    public void WriteByte(byte value)
    {
        _inner.Add(value);
    }

    public void Write<T>(T value) where T: IEncodePacket
    {
        value.EncodePacket(ref this);
    }

    public void WriteBool(bool value)
    {
        WriteByte((byte)(value ? 1 : 0));
    }
}