using PkgMaker.Utils;

namespace PkgMaker.Models.Pkg;

public class FileHeader : IPackable
{
    public uint NameOffset;
    public uint NameLength;
    public ulong DataOffset;

    public ulong DataSize;
    public uint Flags;
    public uint Padding;

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
