namespace PkgMaker.Pkg.Enums;

public enum PkgMetadataType
{
    None = 0x0,
    DrmType = 0x1,
    ContentType = 0x2,
    PackageType = 0x3,
    PackageSize = 0x4,
    PackageVersion = 0x5,
    QaDigest = 0x7,
    SystemAndAppVersion = 0x8,
    UnknownAllZeroes = 0x9,
    InstallDirectory = 0xA
}