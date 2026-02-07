using System.Diagnostics.CodeAnalysis;

namespace ToolStation.Ps3Formats.Pkg.Enums;

[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Binary packable")]
public enum PkgType : uint
{
    DemoAndKeyNoOverwrite = 0x0,
    DemoAndKey = 0x8,
    Patch = 0x10,
    Normal = 0xE,
}
