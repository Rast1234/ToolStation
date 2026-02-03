namespace PkgMaker.Models.Pkg.Enums;

public enum PkgContentType : uint
{
    Unknown = 0x0,
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
    Ps2Classic = 0x12,
    PspRemaster = 0x14,
    WebTv = 0x19
}