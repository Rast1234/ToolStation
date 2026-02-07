using System.Diagnostics.CodeAnalysis;

namespace ToolStation.Ps3Formats.Pkg.Enums;

[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Binary packable")]
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Value not used")]
public enum PkgContentType : uint
{
    GameData = 0x4,
    GameExec = 0x5,
    Ps1Classic = 0x6,
    Psp = 0x7,
    Theme = 0x9,
    Widget = 0xA,
    License = 0xB,
    VshModule = 0xC,
    PsnAvatar = 0xD,
    PspGo = 0xE,
    Minis = 0xF,
    NeoGeo = 0x10,
    Vmc = 0x11,
    Ps2Classic = 0x12,
    PspRemaster = 0x14,
    WebTv = 0x19
}
