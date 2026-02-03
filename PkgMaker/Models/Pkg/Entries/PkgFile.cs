using PkgMaker.Models.Pkg.Enums;
using PkgMaker.Utils;

namespace PkgMaker.Models.Pkg.Entries;

public class PkgFile : PkgEntryBase
{
    public override string Path { get; }

    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    public string Name => System.IO.Path.GetFileName(Path);

    public string Ext => System.IO.Path.GetExtension(Path);

    public Stream Content { get; }

    public override PkgFileFlags Flags { get; }

    public override uint Size => (uint) Content.Length;

    public PkgFile(string path, Stream content, PkgFileFlags? flags = null)
    {
        Path = GetPath(path);
        Content = GetStream(Name, content);
        Flags = flags ?? PkgFileFlags.Overwrites | PkgFileFlags.Raw;
    }

    private static string GetPath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static Stream GetStream(string name, Stream content)
    {
        if (content.Position != 0) throw new ArgumentException("File content stream must be at position 0");

        try
        {
            if (content.Length == 0) throw new ArgumentException("File content stream must be non-empty");
        }
        catch (NotSupportedException)
        {
            throw new ArgumentException("File content stream must have known length");
        }

        //some WTF magic when adding USRDIR/EBOOT.BIN:
        //fileSize = ((file.fileSize - 0x30 + 63) & ~63) + 0x30
        if (name == "EBOOT.BIN")
        {
            var pad = content.Length - ((content.Length - 0x30 + 63) & ~63) + 0x30;
            return new ConcatStreams([content, new MemoryStream(new byte[pad])]);
        }

        return content;
    }
}