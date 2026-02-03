namespace PkgMaker.Models.Pkg.Enums;

[Flags]
public enum PkgFlags : uint
{
    None = 0x0,
    Unknown0X1 = 0x1,
    Eboot = 0x2,
    RequireLicense = 0x4,
    Unknown0X8 = 0x8,
    CumulativePatch = 0x10,
    Unknown0X20 = 0x20,
    RenameDirectory = 0x40,
    Edat = 0x80,
    Unknown0X100 = 0x100,
    Emulator = 0x200,
    VshModule = 0x400,
    DiscBinded = 0x800
}