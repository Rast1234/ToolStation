using System.Text;
using PkgMaker.Models;
using PkgMaker.Utils;

namespace PkgMaker.Pkg;

public class Header : IPackable
{
    public uint Magic = 0x7F504B47;
    public ushort Revision = 0; // or 0x8000 ?
    public ushort Type = 1;
    public uint MetadataOffset = 0xC0;
    public uint MetadataCount = 0x05; // unk1 ?
    public uint MetadataSize = 0x80;
    public uint NumberOfItems;
    public ulong TotalPackageSize;
    public ulong DataOffset = 0x140;
    public ulong DataSize;
    public byte[] ContentId = new byte[0x30];
    public byte[] QaDigest = new byte[0x10];
    public byte[] KLicensee = new byte[0x10];

    public void SetContentId(string value)
    {
        var id = Encoding.UTF8.GetBytes(value);
        if (id.Length > 0x30) throw new ArgumentException($"ContentId too long, expected <= 0x30 bytes: [{ContentId}]");

        id.CopyTo(ContentId);
    }

    public byte[] Pack()
    {
        var s = new MemoryStream();
        s.WriteUInt32BE(Magic);
        s.WriteUInt16BE(Revision);
        s.WriteUInt16BE(Type);
        s.WriteUInt32BE(MetadataOffset);
        s.WriteUInt32BE(MetadataCount);

        s.WriteUInt32BE(MetadataSize);
        s.WriteUInt32BE(NumberOfItems);
        s.WriteUInt64BE(TotalPackageSize);

        s.WriteUInt64BE(DataOffset);
        s.WriteUInt64BE(DataSize);

        s.Write(ContentId);
        s.Write(QaDigest);
        s.Write(KLicensee);
        return s.ToArray();
    }
}