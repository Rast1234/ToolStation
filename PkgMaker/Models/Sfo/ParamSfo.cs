using System.Text;
using PkgMaker.Models.Pkg.Entries;
using PkgMaker.Utils;

namespace PkgMaker.Models.Sfo;

public class ParamSfo : IPackable
{
    public IReadOnlyDictionary<string, SfoKey> Data => data;

    private readonly OrderedDictionary<string, SfoKey> data = new();

    public void Add(string key, string value, int maxLen)
    {
        // 0-terminated string
        byte[] s = [..Encoding.UTF8.GetBytes(value), 0];
        var item = new SfoKey
        {
            Name = key,
            Value = value,
            ByteValue = s,
            Length = s.Length,
            MaxLength = maxLen
        };
        data.Add(key, item);
    }

    public void Add(string key, int value)
    {
        var item = new SfoKey
        {
            Name = key,
            Value = value,
            ByteValue = BitConverter.GetBytes(value),
            Length = 4,
            MaxLength = 4
        };
        data.Add(key, item);
    }

    public ParamSfoFile ToPkgFile(string fullPath)
    {
        return new ParamSfoFile((string) data["CATEGORY"].Value, fullPath, new MemoryStream(Pack()));
    }

    public byte[] Pack()
    {
        // TODO refactor? it works though
        var s = new MemoryStream();
        long indexTableStart = 0x14;
        var keyTableStart = indexTableStart + data.Count * 0x10;

        // calculate the sizes of the key and data table
        long keyTableSize = 0;
        long dataTableSize = 0;
        foreach (var k in data.Values)
        {
            keyTableSize += k.Name!.Length + 1; // add space for null terminator
            dataTableSize += k.MaxLength;
            if (k.MaxLength % 0x4 > 0) // hacky alignment to 0x4 bytes
                dataTableSize += 0x4 - k.MaxLength % 0x4;
        }

        if (keyTableSize % 0x4 > 0)
            keyTableSize += 0x4 - keyTableSize % 0x4;
        var dataTableStart = keyTableStart + keyTableSize;

        var keyTable = new byte[keyTableSize];
        // might be inefficient to do it like this for larger files
        var dataTable = new byte[dataTableSize];

        // write out the PARAM.SFO header
        s.WriteUInt32BE(0x00505346); // "PSF"
        s.WriteInt32LE(0x00000101); // 1.1
        s.WriteUInt32LE((uint) keyTableStart);
        s.WriteUInt32LE((uint) dataTableStart);
        s.WriteInt32LE(data.Count);

        long keyTableUsed = 0;
        long dataTableUsed = 0;
        foreach (var k in data.Values)
        {
            var type = k.Value switch
            {
                int => SfoDataFormat.Int32,
                string => SfoDataFormat.Utf8,
                _ => throw new ArgumentOutOfRangeException()
            };

            // write out the entry in the sfo index table
            s.WriteInt16LE((short) keyTableUsed);
            s.WriteInt16LE((short) type);
            s.WriteInt32LE(k.Length);
            s.WriteInt32LE(k.MaxLength);
            s.WriteInt32LE((int) dataTableUsed);

            // copy the key into the key table
            var keyNameBytes = Encoding.UTF8.GetBytes(k.Name!);
            Array.Copy(keyNameBytes, 0, keyTable, keyTableUsed, keyNameBytes.Length);
            keyTableUsed += keyNameBytes.Length + 1; // +1 for null terminator

            // copy the data into the data table
            Array.Copy(k.ByteValue, 0, dataTable, dataTableUsed, k.ByteValue.Length);
            dataTableUsed += k.MaxLength;
            if (k.MaxLength % 0x4 > 0) // hacky alignment to 0x4 bytes
                dataTableUsed += 0x4 - k.MaxLength % 0x4;
        }

        // write out the key and data tables
        s.Write(keyTable);
        s.Write(dataTable);
        return s.ToArray();
    }

    public static ParamSfo? Read(byte[]? x)
    {
        if (x is null) return null;

        return Read(new MemoryStream(x));
    }

    public static ParamSfo Read(Stream s)
    {
        if (s.Position != 0) throw new ArgumentException("SFO stream must be at position 0");

        var sfo = new ParamSfo();

        if (s.ReadInt32BE() != 0x00505346) throw new Exception("Invalid header");
        if (s.ReadInt32LE() != 0x00000101) throw new Exception("Invalid version");
        var keyBlockStart = s.ReadUInt32LE();
        var dataBlockStart = s.ReadUInt32LE();
        var keys = s.ReadUInt32LE();
        foreach (var i in Enumerable.Range(0, (int) keys))
        {
            s.Position = 0x14 + i * 0x10;

            var nameOffset = s.ReadUInt16LE();
            var format = (SfoDataFormat) s.ReadInt16LE();
            var valueLen = s.ReadInt32LE();
            var valueMax = s.ReadInt32LE();
            var valueOffset = s.ReadUInt32LE();

            s.Position = keyBlockStart + nameOffset;
            var name = s.ReadUtf8NullTerminated();

            s.Position = dataBlockStart + valueOffset;
            var byteValue = s.ReadBytes(valueMax);
            object value = format switch
            {
                SfoDataFormat.Utf8 => Encoding.UTF8.GetString(byteValue, 0, valueLen - 1),
                SfoDataFormat.Int32 => BitConverter.ToInt32(byteValue),
                _ => throw new ArgumentOutOfRangeException()
            };

            var x = new SfoKey
            {
                Name = name,
                ByteValue = byteValue,
                MaxLength = valueMax,
                Length = valueLen,
                Value = value
            };
            sfo.data.Add(x.Name, x);
        }

        return sfo;
    }
}
