using System;
using Akka.IO;
using Fliegenpilz.Net;
using JetBrains.Annotations;
using Xunit;

namespace Fliegenpilz.Tests.Net;

[TestSubject(typeof(PacketWriter))]
public class PacketWriterTest
{
    [Theory]
    [InlineData("")]
    [InlineData("Fizz")]
    [InlineData("Buzz")]
    [InlineData("FizzBuzz")]
    [InlineData("4")]
    public void String(string value)
    {
        var pw = new PacketWriter();
        pw.WriteString(value);

        using var buf = pw.DetachBuffer();
        Assert.Equal(value.Length + 2, buf.Length);

        var pr = new PacketReader(buf);
        Assert.Equal(value, pr.ReadString());
    }

    public static TheoryData<UInt128> UInt128Data => new()
    {
            UInt128.One,
            UInt128.MaxValue,
            UInt128.MinValue,
            UInt128.Zero
    };
    
    [Theory]
    [MemberData(nameof(UInt128Data), MemberType = typeof(PacketWriterTest))]
    public void Ui128(UInt128 value)
    {
        var pw = new PacketWriter();
        pw.WriteUInt128(value);

        using var buf = pw.DetachBuffer();
        Assert.Equal(16, buf.Length);

        var pr = new PacketReader(buf);
        Assert.Equal(value, pr.ReadUInt128());
    }
}