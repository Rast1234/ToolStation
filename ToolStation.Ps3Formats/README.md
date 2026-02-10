# ToolStation.Ps3Formats

Reading/writing PS3 file formats. Used by [ToolStation](https://github.com/Rast1234/ToolStation). Based on [SCEllSharp](https://github.com/InvoxiPlayGames/SCEllSharp) and [pkg_custom.py](https://github.com/aldostools/webMAN-MOD/tree/master/_Projects_/pkglaunch/pypkg).

| Format | Read | Write | Nice things |
|--------|------|-------|-------------|
| PKG    | ❌    | ✔️    | ✔️          |
| SFO    | ✔️   | ✔️    | ✔️          |

## PKG

PS3 package files, for CFW-enabled consoles (and maybe HEN?). More info: [psdevwiki.com/ps3/PKG_files](https://www.psdevwiki.com/ps3/PKG_files)

Writing: only useful subset of package fields supported, eg only debug packages. Encryption is fast and works fine with big files. There are additional bells and whistles to help build a package with right header fields depending on content. See `PkgMaker` project - it uses most of these features.

> Note that pkg files are not simple archives.

Depending on content, eg PARAM.SFO or EBOOT.BIN, some flags have to be set properly. Also everything is signed and encrypted in a weird way, so a lot of options are hardcoded in a known-working way.

Reading or extracting: not difficult to support, i just did not need it and find GUI tools more useful for this.

### Examples

```cs
public static async Task DebugPkgWrite(CancellationToken token)
{
    var bin = await File.ReadAllBytesAsync(@"C:\example\homebrew.bin", token);
    var sfo = await File.ReadAllBytesAsync(@"C:\example\PARAM.SFO", token);
    var ico = await File.ReadAllBytesAsync(@"C:\example\pic.PNG", token);

    var pkg = new PkgBuilder
    {
        ContentId = "CUSTOM-GAME12345_00-0000000000000000",
        ForceAbsolutePaths = false
    };
    // simple file
    pkg.AddFile(new PkgFile("ICON0.PNG", new MemoryStream(ico)));
    // eboot.bin requires special flags
    pkg.AddFile(new PkgFile("USRDIR/EBOOT.BIN", new MemoryStream(bin), PkgFileFlags.Overwrites | PkgFileFlags.Npdrm));
    // param.sfo can be parsed and used for autodetecting pkg flags
    pkg.AddFile(ParamSfo.Read(sfo)!.ToPkgFile("PARAM.SFO"));

    await using var s = new FileStream(@"C:\example\out.pkg", FileMode.Create, FileAccess.Write, FileShare.None);
    await pkg.WriteTo(s, token);
}

public static async Task DebugPkgAbsPath(CancellationToken token)
{
    var pkg = new PkgBuilder
    {
        ContentId = "CUSTOM-INSTALLER_00-0000000000000000",
        ForceAbsolutePaths = true,
        ContentType = PkgContentType.Theme
    };
    // simple file with current date, extracted to arbitrary location
    var txt = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));
    pkg.AddFile(new PkgFile("/dev_hdd0/tmp/test.txt", new MemoryStream(txt)));

    await using var s = new FileStream(@"C:\example\abs.pkg", FileMode.Create, FileAccess.Write, FileShare.None);
    await pkg.WriteTo(s, token);
}
```

## SFO

Metadata file that controls how folders are displayed in XMB. More info: [psdevwiki.com/ps3/PARAM.SFO](https://www.psdevwiki.com/ps3/PARAM.SFO)

Internally it's simple binary key-value store with certain quirks.

Writing: study some existing files as examples. Some attributes are tricky. For instance,`LICENSE` attribute must have maxlen=512 or PS3 won't read SFO file. There are no checks for these things.

Reading: no surprises here. Use `ToString` to see what's inside parsed SFO.

There is also a helper method to convert `ParamSfo` object to a `PkgFile` - useful when building pkg to autodetect flags from sfo.

### Examples

```cs
public static void PrintSfo(string path)
{
    using var r = File.OpenRead(path);
    var s = ParamSfo.Read(r);
    Console.WriteLine(s);
}

public static async Task AddSfoToPkg(PkgBuilder pkg, CancellationToken token)
{
    // read or create sfo and convert it to PkgFile
    var sfo = await File.ReadAllBytesAsync(@"C:\example\PARAM.SFO", token);
    pkg.AddFile(ParamSfo.Read(sfo)!.ToPkgFile("PARAM.SFO"));
}

public static ParamSfo GenerateParamSfo()
{
    var sfo = new ParamSfo();
    // key, value, max length for strings
    sfo.Add("APP_VER", "01.00", 8);
    // key, value for integers
    sfo.Add("ATTRIBUTE", 0b00000001);
    sfo.Add("BOOTABLE", 0b00000001);
    sfo.Add("CATEGORY", "HG", 4);
    sfo.Add("LICENSE", "Homebrew application, use at your own risk", 512); // 512 is important here
    sfo.Add("PARENTAL_LEVEL", 0);
    sfo.Add("PS3_SYSTEM_VER", "01.8000", 8);
    sfo.Add("RESOLUTION", 63);
    sfo.Add("SOUND_FORMAT", 279);
    sfo.Add("TITLE", "Example homebrew", 128);
    sfo.Add("TITLE_ID", "GAME12345", 16);
    sfo.Add("VERSION", "01.00", 8);
    return sfo;
}
```
