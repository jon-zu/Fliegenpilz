namespace Fliegenpilz.Net;



public class FileTime(long value)
{
    public static readonly FileTime MinValue = new(94_354_848_000_000_000); // 1/1/1900
    public static readonly FileTime MaxValue = new(150_842_304_000_000_000); // 1/1/2079

    public static FileTime FromDateTime(DateTime value) => new(value.ToFileTime());

    public DateTime ToDateTime() => DateTime.FromFileTime(value);

    public long RawValue => value;
    
    public bool IsMin => this == MinValue;
    public bool IsMax => this == MaxValue;
}