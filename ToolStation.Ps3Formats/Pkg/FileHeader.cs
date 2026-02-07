using System.Diagnostics.CodeAnalysis;
using ToolStation.Ps3Formats.Utils;

namespace ToolStation.Ps3Formats.Pkg;

[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "For clarity")]
public class FileHeader : IPackable
{
    public uint NameOffset;
    public uint NameLength;
    public ulong DataOffset;

    public ulong DataSize;
    public uint Flags;
    public uint Padding = 0;

    public byte[] Pack()
    {
        var s = new MemoryStream();
        s.WriteUInt32BE(NameOffset);
        s.WriteUInt32BE(NameLength);
        s.WriteUInt64BE(DataOffset);

        s.WriteUInt64BE(DataSize);
        s.WriteUInt32BE(Flags);
        s.WriteUInt32BE(Padding);
        return s.ToArray();
    }
}
