using PkgMaker.Pkg.Enums;

namespace PkgMaker.Pkg.Entries;

public class PkgDirectory(string path, PkgFileFlags? flags = null) : PkgEntryBase
{
    public override string Path => path;

    public override PkgFileFlags Flags => flags ?? PkgFileFlags.Overwrites | PkgFileFlags.Directory;

    public override uint Size => 0;
}