using System.Security.Cryptography;
using System.Text;
using PkgMaker.Models.Pkg.Entries;
using PkgMaker.Models.Pkg.Enums;
using PkgMaker.Utils;

namespace PkgMaker.Models.Pkg;

public class PkgBuilder
{
    private string PathToRoot => UseDangerousAbsolutePath ? "../../../" : string.Empty;
    public required string ContentId { get; init; }

    /// <summary>
    /// Enable installing files from /dev_hdd0/ TODO: does not work without some fiddling, needs more testing, eg THEME type
    /// </summary>
    public required bool UseDangerousAbsolutePath { get; init; }

    /// <summary>
    /// Content type, leave null for autodetect: GameData for normal files, GameExec if PARAM.SFO and USRDIR/EBOOT.BIN present
    /// </summary>
    public PkgContentType? ContentType { get; set; }

    private readonly OrderedDictionary<string, PkgFile> addedFiles = new();

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Dictionary.Add prevents from adding duplicate paths
    /// </remarks>
    public void AddFile(PkgFile file)
    {
        if (file.Path.Contains("..")) throw new ArgumentException($"Do not hack paths manually, enable {nameof(UseDangerousAbsolutePath)} instead");
        if (UseDangerousAbsolutePath && !file.Path.StartsWith("dev_")) throw new ArgumentException($"With {nameof(UseDangerousAbsolutePath)} enabled, file path must start with 'dev_'");

        if (file.Name == "EBOOT.BIN" && (file.Flags & ~PkgFileFlags.Overwrites) != PkgFileFlags.Npdrm) throw new ArgumentException($"{file.Name} must have npdrm flag only (and maybe owerwrite)");


        file.PkgPath = GetPkgPath(file.Path);
        addedFiles.Add(file.Path, file);
    }

    public async Task WriteTo(Stream s, CancellationToken token)
    {
        if (UseDangerousAbsolutePath) throw new NotSupportedException("disabled, needs more debuggung - currently packages with absolute paths fail to install");

        ContentType ??= DetectContentType(addedFiles);
        ArgumentNullException.ThrowIfNull(ContentType);


        var directories = addedFiles.Values
            .Select(x => x.Directory)
            .Distinct()
            .Where(x => x != string.Empty)
            .Order()
            .Select(x => new PkgDirectory(x)
            {
                PkgPath = GetPkgPath(x)
            });
        IReadOnlyList<IPkgEntry> entries = [..directories, ..addedFiles.Values];

        var offset = 0x20 * (uint) entries.Count;
        var dataToEncrypt = new MemoryStream();
        foreach (var entry in entries)
        {
            // calculate path offsets
            entry.NameOffset = offset;
            offset += entry.PkgPathSizeAligned;
        }

        foreach (var entry in entries)
        {
            // calculate content offsets
            entry.FileOffset = offset;
            offset += entry.SizeAligned;
        }

        foreach (var entry in entries)
        {
            // write file headers
            var fh = new FileHeader
            {
                NameOffset = entry.NameOffset,
                NameLength = (uint) entry.PkgPath.Length,
                DataOffset = entry.FileOffset,
                DataSize = entry.Size,
                Flags = (uint) entry.Flags
            };
            dataToEncrypt.Write(fh.Pack());
        }

        foreach (var entry in entries)
        {
            // write paths
            var name = new byte[entry.PkgPathSizeAligned];
            Encoding.UTF8.GetBytes(entry.PkgPath).CopyTo(name);
            dataToEncrypt.Write(name);
        }

        var fileInfoBlock = dataToEncrypt.ToArray();
        //await File.WriteAllBytesAsync(@"C:\vault\ToolStation\out\netFileBlock.bin", fileInfoBlock, token);

        var qaHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        qaHash.AppendData(fileInfoBlock);
        Log(Convert.ToHexString(qaHash.GetCurrentHash()));

        var dataSize = (ulong) (fileInfoBlock.Length + entries.Sum(x => x.SizeAligned));
        Log(dataSize);
        var header = new Header();
        header.NumberOfItems = (uint) entries.Count;
        header.DataSize = dataSize;
        header.TotalPackageSize = dataSize + 0x1A0;

        var headerMs = new MemoryStream();
        headerMs.Write(header.Pack());
        var headerBlock = headerMs.ToArray();
        //await File.WriteAllBytesAsync(@"C:\vault\ToolStation\out\netHeaderBlock.bin", headerBlock, token);
        qaHash.AppendData(headerBlock);
        qaHash.AppendData(fileInfoBlock);
        qaHash.GetCurrentHash()[..header.QaDigest.Length].CopyTo(header.QaDigest);
        Log(Convert.ToHexString(header.QaDigest));
        header.SetContentId(ContentId);
        Log(Convert.ToHexString(header.ContentId));

        var context = KeyToContext(header.QaDigest);
        SetContextTail(context, 0xFFFFFFFFFFFFFFFF);
        header.KLicensee = Crypt(context, header.KLicensee);

        // yooo now let's write data!

        var headerBytes = header.Pack();
        var headerSha = SHA1.HashData(headerBytes)[3..19];
        await s.WriteAsync(headerBytes, token);
        await s.WriteAsync(headerSha, token);

        var metaHeader = new MetaHeader();
        metaHeader.DataSize = dataSize;
        metaHeader.ContentType = (uint) ContentType;
        var metaBytes = metaHeader.Pack();
        var metaSha = SHA1.HashData(metaBytes)[3..19];
        var metaPad = new byte[0x30];
        //await File.WriteAllBytesAsync(@"C:\vault\ToolStation\out\netMetaBlock.bin", metaBytes, token);
        LogHex(metaSha, "meta sha");

        var metaEnc1 = Crypt(KeyToContext(metaSha), metaPad);
        var metaEnc2 = Crypt(KeyToContext(headerSha), metaEnc1);
        await s.WriteAsync(metaEnc2, token);
        await s.WriteAsync(metaBytes, token);
        await s.WriteAsync(metaSha, token);
        await s.WriteAsync(metaEnc1, token);

        var fileContext = KeyToContext(header.QaDigest);
        LogHex(metaSha, "key");
        if (fileInfoBlock.Length > 0)
        {
            var fileInfoEnc = Crypt(fileContext, fileInfoBlock);
            await s.WriteAsync(fileInfoEnc, token);
        }

        Log("============================================");

        foreach (var fileEntry in entries.OfType<PkgFile>())
        {
            Log($"WRITING {fileEntry.Path} / {fileEntry.Size} / {fileEntry.Content.Length}");
            var buffer = new byte[4*1024*1024]; // was 0x8000000 (128 MiB)
            foreach (var chunk in ReadByChunks(fileEntry.Content, buffer, token))
            {
                Log($"chunk len={chunk.Count}");
                var dataEnc = Crypt(fileContext, chunk);
                await s.WriteAsync(dataEnc, token);
            }

            LogHex(fileContext, "key after file");
            await s.WriteAsync(new byte[fileEntry.Size.PaddingTo16Length()], token);
        }

        await s.WriteAsync(new byte[0x60], token);
    }

    private string GetPkgPath(string fullPath)
    {
        return UseDangerousAbsolutePath ? Path.Join(PathToRoot, fullPath) : fullPath;
    }

    private IEnumerable<ArraySegment<byte>> ReadByChunks(Stream s, byte[] buffer, CancellationToken token)
    {
        int x;
        while ((x = s.Read(buffer)) > 0)
        {
            token.ThrowIfCancellationRequested();
            yield return new ArraySegment<byte>(buffer, 0, x);
        }
    }

    private byte[] KeyToContext(byte[] key)
    {
        var ms = new MemoryStream(8 * 4 + 0x20);
        ms.Write(key, 0, 8);
        ms.Write(key, 0, 8);
        ms.Write(key, 8, 8);
        ms.Write(key, 8, 8);
        ms.Write(new byte[0x20]);
        return ms.ToArray();
    }

    private void SetContextTail(byte[] key, ulong num)
    {
        // pack x as big endian, 8-byte unsigned long long
        var bytes = BitConverter.GetBytes(num);
        Array.Reverse(bytes);
        // set last 8 bytes
        bytes.CopyTo(key, 0x38);
    }

    private byte[] Crypt(byte[] key, ArraySegment<byte> input)
    {
        LogHex(key, $"key for [{input.Count}]");
        var result = new byte[input.Count];
        var offset = 0;
        foreach (var chunk in input.Chunk(0x10))
        {
            var toHash = key[..Math.Min(key.Length, 0x40)]; // hash portion of key
            //LogHex(toHash, "SHA1_arg");
            var hash = SHA1.HashData(toHash);
            //LogHex(hash, "SHA1_ret");
            foreach (var x in chunk.Zip(hash, (c, h) => c ^ h))
            {
                result[offset] = (byte) x;
                offset++;
            }

            Manipulate(key);
        }

        //LogHex(result, "ret");
        return result;
    }

    private void Manipulate(byte[] key)
    {
        var bak = key[0x38..];
        var tmp = key[0x38..];
        Array.Reverse(tmp);
        var num = BitConverter.ToUInt64(tmp);
        num++;
        num &= 0xFFFFFFFFFFFFFFFF;
        SetContextTail(key, num);
        //LogHex([..bak, ..key[0x38..]], "MNPL");
    }

    private static void LogHex(ReadOnlySpan<byte> data, string msg)
    {
        Log($"{msg} {data.Length} hex: {Convert.ToHexString(data)}");
    }

    private static PkgContentType? DetectContentType(IReadOnlyDictionary<string, PkgFile> files)
    {
        if (files.TryGetValue("PARAM.SFO", out var x) && x is ParamSfoFile sfo)
        {
            if (files.ContainsKey("USRDIR/EBOOT.BIN")) return PkgContentType.GameExec;

            return sfo.Category switch
            {
                "WT" => PkgContentType.WebTv,
                "1P" => PkgContentType.Ps1Classic,
                "2P" => PkgContentType.Ps2Classic,
                "MN" => PkgContentType.Minis,
                "PE" => PkgContentType.PspRemaster,
                "TR" => PkgContentType.Theme,
                _ => PkgContentType.GameData
            };
        }

        var edats = files.Keys
            .Where(x => x.EndsWith(".edat", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (edats.Any(x => x.StartsWith("PSNA_"))) return PkgContentType.PsnAvatar;

        if (edats.Any()) return PkgContentType.License;

        // also seen this: contentid.contains(VSHMODULE) => 0x0C vshModule
        return null;
    }

    private static void Log(object value)
    {
        //Main.Log(value);
    }
}
