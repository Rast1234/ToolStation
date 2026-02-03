using System.Security.Cryptography;
using System.Text;
using PkgMaker.Models;
using PkgMaker.Pkg;
using PkgMaker.Pkg.Entries;
using PkgMaker.Pkg.Enums;
using PkgMaker.Services;
using PkgMaker.Sfo;

namespace PkgMaker.Utils;

/// <summary>
/// Various unused methods that were used for debugging or could be useful later
/// </summary>
public static class Scratch
{
    public static void PrintSfo(string path)
    {
        var s = ParamSfo.Read(File.OpenRead(path));
        foreach (var x in s.Data.Values)
        {
            var type = x.Value is int ? "int" : "str";
            Main.Log($"{type} {x.Name} [{x.Length}/{x.MaxLength}] [{x.Value}]");
        }
    }

    public static async Task DebugPkgWrite(CancellationToken token)
    {
        var bin = await File.ReadAllBytesAsync(@"C:\vault\ToolStation\out\generated\USRDIR\EBOOT.BIN", token);
        var sfo = await File.ReadAllBytesAsync(@"C:\vault\ToolStation\out\generated\PARAM.SFO", token);
        var ico = await File.ReadAllBytesAsync(@"C:\vault\ToolStation\out\generated\ICON0.PNG", token);

        var pkg = new PkgBuilder
        {
            ContentId = "CUSTOM-INSTALLER_00-0000000000000000",
            UseDangerousAbsolutePath = false
        };
        pkg.AddFile(new PkgFile("USRDIR/EBOOT.BIN", new MemoryStream(bin), PkgFileFlags.Overwrites | PkgFileFlags.Npdrm));
        pkg.AddFile(new PkgFile("ICON0.PNG", new MemoryStream(ico)));
        pkg.AddFile(ParamSfo.Read(sfo)!.ToPkgFile("PARAM.SFO"));

        await using var s = new FileStream(@"C:\vault\ToolStation\out\netDebug.pkg", FileMode.Create, FileAccess.Write, FileShare.None);
        await pkg.WriteTo(s, token);
    }

    public static async Task DebugPkgAbsPath(CancellationToken token)
    {
        var txt = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));

        var pkg = new PkgBuilder
        {
            ContentId = "CUSTOM-INSTALLER_00-0000000000000000",
            UseDangerousAbsolutePath = true,
            ContentType = PkgContentType.Theme
        };
        pkg.AddFile(new PkgFile("/dev_hdd0/tmp/test.txt", new MemoryStream(txt)));

        await using var s = new FileStream(@"C:\vault\ToolStation\out\absDebug.pkg", FileMode.Create, FileAccess.Write, FileShare.None);
        await pkg.WriteTo(s, token);
    }

    public static async Task DebugDumpFiles(Values values, byte[] ebootBin, CancellationToken token)
    {
        var dir = values.Output!.CreateSubdirectory("debug");
        var usrdir = dir.CreateSubdirectory("USRDIR");

        await File.WriteAllBytesAsync(Path.Join(dir.FullName, "PARAM.SFO"), values.ParamSfo?.Pack() ?? [], token);
        await File.WriteAllBytesAsync(Path.Join(dir.FullName, "ICON0.PNG"), values.Icon ?? [], token);
        await File.WriteAllBytesAsync(Path.Join(usrdir.FullName, "EBOOT.BIN"), ebootBin, token);
    }

    public static async Task Debug(CancellationToken token)
    {
        //await Scratch.DebugPkgAbsPath(token); return;

        var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        Log("init", sha1.GetCurrentHash());
        sha1.AppendData("lorem ipsum dolor sit amet"u8);
        Log("updt", sha1.GetCurrentHash());

        var sha2 = SHA1.HashData("lorem ipsum dolor sit amet"u8);
        Log("drct", sha2);

        var ms = new MemoryStream(new byte[] {0, 1, 2, 3, 4});
        Log($"init, pos {ms.Position}", ms.ToArray());
        var tmp = new byte[10];
        ms.ReadExactly(tmp.AsSpan(new Range(0, 2)));
        Log($"read 2, pos {ms.Position}", tmp);
        ms.ReadExactly(tmp.AsSpan(new Range(0, 2)));
        Log($"read 2, pos {ms.Position}", tmp);


        Console.WriteLine($"{0x8000000}");
        Console.WriteLine($"{0x8000000u}");

        var b = Encoding.UTF8.GetBytes("a" + "b");
        Console.WriteLine($"{b.Length}");

        Console.WriteLine(Foo().GetType().Name);
    }

    private static void Log(string message, byte[]? value)
    {
        if (value is null)
        {
            Console.WriteLine($"{message} [null] (0)");
            return;
        }

        Console.WriteLine($"{message} [{Convert.ToHexString(value)}] ({value.Length})");
    }

    private static void LogList<T>(string message, IReadOnlyList<T> value)
    {
        var join = string.Join(",", value);
        Console.WriteLine($"{message} [{join}] ({value.Count})");
    }

    private static IEnumerable<string> Foo()
    {
        return [];
    }
}
