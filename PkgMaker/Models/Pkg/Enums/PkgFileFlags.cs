namespace PkgMaker.Models.Pkg.Enums;

[Flags]
public enum PkgFileFlags : uint
{
    None = 0,
    Npdrm = 0x1,
    Edat = 0x2,
    Raw = 0x3,
    Directory = 0x4,
    Self = 0x100,
    PspCrypto = 0x10000000,
    Overwrites = 0x80000000
}