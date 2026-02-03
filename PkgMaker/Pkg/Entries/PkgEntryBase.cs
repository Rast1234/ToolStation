using PkgMaker.Pkg.Enums;
using PkgMaker.Utils;

namespace PkgMaker.Pkg.Entries;

public abstract class PkgEntryBase : IPkgEntry
{
    public abstract string Path { get; }
    public abstract PkgFileFlags Flags { get; }

    public abstract uint Size { get; }
    public uint SizeAligned => Size + Size.PaddingTo16Length();
    public uint PkgPathSizeAligned => (uint) (PkgPath.Length + PkgPath.Length.PaddingTo16Length());

    public uint NameOffset { get; set; }
    public uint FileOffset { get; set; }
    public string PkgPath { get; set; } = null!;
}