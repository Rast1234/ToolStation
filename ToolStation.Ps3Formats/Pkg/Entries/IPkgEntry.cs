using ToolStation.Ps3Formats.Pkg.Enums;

namespace ToolStation.Ps3Formats.Pkg.Entries;

public interface IPkgEntry
{
    string Path { get; }

    PkgFileFlags Flags { get; }

    uint Size { get; }

    uint SizeAligned { get; }

    uint PkgPathSizeAligned { get; }

    uint NameOffset { get; set; }

    uint FileOffset { get; set; }

    string PkgPath { get; set; }
}
