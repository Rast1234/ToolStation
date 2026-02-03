using PkgMaker.Utils;

namespace PkgMaker.Models.Pkg;

public class MetaHeader : IPackable
{
    public uint Unk1 = 1;
    public uint Unk2 = 4;
    public uint DrmType = 3; //1 = Network, 2 = Local, 3 = Free, anything else = unknown
    public uint Unk4 = 2;

    public uint Unk21 = 4;
    public uint ContentType; //9 == Theme, 5 == gameexec, 4 == gamedata
    public uint Unk23 = 3;
    public uint Unk24 = 4;

    public uint PackageType = 0x0E; //packageType 0x10 == patch, 0x8 == Demo&Key, 0x0 == Demo&Key (AND UserFiles = NotOverWrite), 0xE == normal, use 0xE for gamexec, and 8 for gamedata
    public uint Unk32 = 4;
    public uint Unk33 = 8;
    public ushort SecondaryVersion = 0;
    public ushort Unk34 = 0;

    public float DataSize;
    public uint Unk42 = 5;
    public uint Unk43 = 4;
    public ushort PackagedBy = 0x1061;
    public ushort PackagedVersion;


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