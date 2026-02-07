using System.Diagnostics.CodeAnalysis;
using ToolStation.Ps3Formats.Utils;

namespace ToolStation.Ps3Formats.Pkg;

[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "For clarity")]
public class MetaHeader : IPackable
{
    public uint Unk1 = 1;
    public uint Unk2 = 4;
    public uint DrmType = 3; //1 = Network, 2 = Local, 3 = Free, anything else = unknown
    public uint Unk4 = 2;

    public uint Unk21 = 4;
    public uint ContentType;
    public uint Unk23 = 3;
    public uint Unk24 = 4;

    public uint PackageType;
    public uint Unk32 = 4;
    public uint Unk33 = 8;
    public ushort SecondaryVersion = 0;
    public ushort Unk34 = 0;

    public float DataSize;
    public uint Unk42 = 5;
    public uint Unk43 = 4;
    public ushort PackagedBy = 0x1061;
    public ushort PackagedVersion = 0;

    public byte[] Pack()
    {
        var s = new MemoryStream();
        s.WriteUInt32BE(Unk1);
        s.WriteUInt32BE(Unk2);
        s.WriteUInt32BE(DrmType);
        s.WriteUInt32BE(Unk4);

        s.WriteUInt32BE(Unk21);
        s.WriteUInt32BE(ContentType);
        s.WriteUInt32BE(Unk23);
        s.WriteUInt32BE(Unk24);

        s.WriteUInt32BE(PackageType);
        s.WriteUInt32BE(Unk32);
        s.WriteUInt32BE(Unk33);
        s.WriteUInt16BE(SecondaryVersion);
        s.WriteUInt16BE(Unk34);

        //float to bytes to BE, not sure about this one
        s.Write(BitConverter.GetBytes(DataSize).Reverse().ToArray());
        s.WriteUInt32BE(Unk42);
        s.WriteUInt32BE(Unk43);
        s.WriteUInt16BE(PackagedBy);
        s.WriteUInt16BE(PackagedVersion);
        return s.ToArray();
    }
}
