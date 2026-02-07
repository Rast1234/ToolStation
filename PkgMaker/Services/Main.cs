using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PkgMaker.Models;
using PkgMaker.Models.Commands;
using PkgMaker.Utils;
using ToolStation.Ps3Formats.Pkg;
using ToolStation.Ps3Formats.Pkg.Entries;
using ToolStation.Ps3Formats.Pkg.Enums;

namespace PkgMaker.Services;

internal static class Main
{
    public static async Task Run(IReadOnlyList<string> sysArgs, CancellationToken token)
    {
        var rootCommand = new Root
        {
            Launcher = { Action = new RecognizedCommandAction(MakeLaunchPkg) },
            Prepare = { Action = new RecognizedCommandAction(PrepareScript) },
            Installer = { Action = new RecognizedCommandAction(MakeInstallPkg) },
            Manual = { Action = new RecognizedCommandAction(MakeManualPkg) },
        };
        rootCommand.Test.SetAction(Test);
        var args = rootCommand.Parse(sysArgs);
        if (args is { Tokens: [] } or { Action: RecognizedCommandAction, Tokens.Count: 1, Errors: [] })
        {
            // show help by default: if no args, or known command with no args
            args.InvokeCurrentCommandHelp();
            if (!string.IsNullOrEmpty(Process.GetCurrentProcess().MainWindowTitle))
            {
                // user ran .exe directly from explorer, show help and don't close
                Console.WriteLine("Press enter to close");
                Console.ReadLine();
            }

            return;
        }

        await args.InvokeAsync(cancellationToken: token);
    }

    private static async Task PrepareScript(ParseResult args, CancellationToken token)
    {
        var source = args.GetRequiredValue(Prepare.Source);
        var output = args.GetRequiredValue(Prepare.Output);
        var force = args.GetValue(Prepare.Force);
        var recursive = args.GetValue(Prepare.Recursive);
        var throttle = args.GetValue(Prepare.Throttle);
        Log($"""
             Initial parameters:
                 {nameof(source)}=[{source}], {nameof(recursive)}=[{recursive}], {nameof(force)}=[{force}]
                 {nameof(output)}=[{output}]
             """);

        if (output.Exists && !force)
        {
            throw new InvalidOperationException($"Output [{output.FullName}] exists. Pass -f to overwrite");
        }

        var app = Environment.ProcessPath;
        var entries = await ListEntries(source, recursive, throttle, token);
        var sb = new StringBuilder();
        foreach (var entry in entries.Order())
        {
            sb.Append($"{app} launcher -b \"{entry}\"\n");
        }

        await File.WriteAllTextAsync(output.FullName, sb.ToString(), token);
        Log($"Saved script to [{output.FullName}]: {entries.Count} entries");
    }

    private static async Task MakeLaunchPkg(ParseResult args, CancellationToken token)
    {
        var values = new Values
        {
            Now = DateTime.Now,
            Base = args.GetValue(Launcher.Base),
            Output = args.GetValue(Launcher.Output),
            Force = args.GetValue(Launcher.Force),
            Preview = args.GetValue(Launcher.Preview),
            Timeout = args.GetValue(Launcher.Timeout) ?? 1
        };
        Log(values.ToInitString());

        await ReadFromBase(values, token);
        Log(values.ToInfoString("Values read from base"));
        var p = new MetadataProvider();
        // required
        values.Title = MetadataProvider.FormatTitle(args.GetValue(Launcher.Title) ?? values.Title);
        values.Game = args.GetValue(Launcher.Game) ?? values.Game;
        // automatic
        values.TitleId = MetadataProvider.FormatTitleId(args.GetValue(Launcher.TitleId) ?? MetadataProvider.GenerateTitleId(values.Title));
        values.Label = MetadataProvider.FormatLabel(args.GetValue(Launcher.Label) ?? MetadataProvider.GenerateLabel(values.Title));
        values.ContentId = MetadataProvider.FormatLaunchContentId(values);
        values.Command = MetadataProvider.GenerateCommand(args.GetValue(Launcher.Command), values.Command, values.TitleId);
        values.Script = MetadataProvider.FormatScript(MetadataProvider.GenerateScript(args.GetValue(Launcher.Script), values.TitleId, values.Game, values.Timeout));
        values.ParamHis = MetadataProvider.FormatParamHis(MetadataProvider.GenerateParamHis(args.GetValue(Launcher.ParamHis), args, values), values.Now);
        values.ParamSfo = await MetadataProvider.GenerateParamSfo(args.GetValue(Launcher.ParamSfo), values, token);
        values.Icon = await MetadataProvider.ReadFileOrFallback(args.GetValue(Launcher.Icon), values.Icon, token) ?? Resources.GetEmbedded("ICON0.PNG");
        values.Background = await MetadataProvider.ReadFileOrFallback(args.GetValue(Launcher.Background), values.Background, token);
        values.Overlay = await MetadataProvider.ReadFileOrFallback(args.GetValue(Launcher.Overlay), values.Overlay, token);
        values.OverlaySd = await MetadataProvider.ReadFileOrFallback(args.GetValue(Launcher.OverlaySd), values.OverlaySd, token);
        values.Video = await MetadataProvider.ReadFileOrFallback(args.GetValue(Launcher.Video), values.Video, token);
        values.Sound = await MetadataProvider.ReadFileOrFallback(args.GetValue(Launcher.Sound), values.Sound, token);
        Log(values.ToInfoString("Overrides from arguments and auto-generated values"));
        var outFile = await GeneratePkg(values, token);
        Log($"Saved package to [{outFile.FullName}], {outFile.Length} bytes");
    }

    private static async Task MakeInstallPkg(ParseResult args, CancellationToken token)
    {
        var input = args.GetRequiredValue(Installer.Input);
        var output = args.GetRequiredValue(Installer.Output);
        var name = args.GetValue(Installer.PkgName) ?? $"{input.Name}";
        var force = args.GetValue(Installer.Force);
        await MakePkg(input, name, force, PkgContentType.Theme, null, true, output, token);
    }

    private static async Task MakeManualPkg(ParseResult args, CancellationToken token)
    {
        var input = args.GetRequiredValue(Manual.Input);
        var output = args.GetRequiredValue(Manual.Output);
        var name = args.GetValue(Manual.PkgName) ?? $"{input.Name}";
        var force = args.GetValue(Manual.Force);
        var contentType = PkgContentType.ParseAny(args.GetValue(Manual.ContentType));
        var pkgType = PkgType.ParseAny(args.GetValue(Manual.PackageType));
        await MakePkg(input, name, force, contentType, pkgType, false, output, token);
    }

    private static async Task MakePkg(DirectoryInfo input, string name, bool force, PkgContentType? contentType, PkgType? pkgType, bool absolutePaths, DirectoryInfo output, CancellationToken token)
    {
        var fileName = name.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase)
            ? name
            : name + ".pkg";
        var outFile = new FileInfo(Path.Join(output.FullName, fileName));
        if (outFile.Exists && !force)
        {
            throw new InvalidOperationException($"Output [{outFile.FullName}] exists. Pass -f to overwrite");
        }

        outFile.Directory!.Create();
        var pkg = new PkgBuilder
        {
            ContentId = MetadataProvider.FormatInstallContentId(name),
            ForceAbsolutePaths = absolutePaths,
            ContentType = contentType,
            PackageType = pkgType
        };

        foreach (var x in input.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var path = Path.GetRelativePath(input.FullName, x.FullName).Replace('\\', '/').Trim('/');

            PkgFileFlags? flags = x.Name == "EBOOT.BIN"
                ? PkgFileFlags.Npdrm | PkgFileFlags.Overwrites
                : null;
            pkg.AddFile(new PkgFile(path, x.OpenRead(), flags));
        }

        await using (var o = outFile.OpenWrite())
        {
            await pkg.WriteTo(o, token);
        }

        outFile.Refresh();
        Log($"Saved package to [{outFile.FullName}], {outFile.Length} bytes");
    }

    private static async Task Test(ParseResult args, CancellationToken token)
    {
        if (Environment.MachineName == "AMARU")
        {
            await Scratch.Debug(token);
            return;
        }

        Log("Congratulations! You found hidden developer command. Here's your reward!");
        using var proc = new Process();
        proc.StartInfo.UseShellExecute = true;
        proc.StartInfo.FileName = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly93d3cueW91dHViZS5jb20vd2F0Y2g/dj1kUXc0dzlXZ1hjUQ=="));
        proc.Start();
    }

    private static async Task ReadFromBase(Values values, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(values.Base))
        {
            return;
        }

        var f = new FileInfo(values.Base);
        if (f.Exists && FileUtils.IsIso(f.Name))
        {
            await using var s = f.OpenRead();
            await FileUtils.ReadIso(s, values, f.FullName, token);
            return;
        }

        var d = new DirectoryInfo(values.Base);
        if (d.Exists)
        {
            if (FileUtils.HasGameSfo(d))
            {
                await FileReader.ReadGameFolder(d, values, token);
            }
            else
            {
                await FileReader.ReadPlainFolder(d, values, token);
            }

            return;
        }

        if (Uri.TryCreate(values.Base, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme is "http" or "https")
            {
                await new HttpReader().Parse(uri, values, token);
                return;
            }

            if (uri.Scheme is "ftp")
            {
                await new FtpReader().Parse(uri, values, token);
                return;
            }
        }

        throw new ArgumentException($"Invalid base [{values.Base}]. Expected folder, iso, http or ftp url");
    }

    private static async Task<IReadOnlyList<string>> ListEntries(string source, bool recursive, double? throttle, CancellationToken token)
    {
        var d = new DirectoryInfo(source);
        if (d.Exists)
        {
            return FileReader.List(d, recursive, token).Select(x => x.FullName).ToList();
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme is "http" or "https")
            {
                var t = TimeSpan.FromSeconds(throttle ?? 0.3);
                return await HttpReader.List(uri, recursive, t, token);
            }

            if (uri.Scheme is "ftp")
            {
                var t = TimeSpan.FromSeconds(throttle ?? 0);
                return await FtpReader.List(uri, recursive, t, token);
            }
        }

        throw new ArgumentException($"Invalid base [{source}]. Expected folder, iso, http or ftp url");
    }

    public static void Log(object? value) => Console.WriteLine(value);

    private static async Task<FileInfo> GeneratePkg(Values values, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(values.ContentId);
        ArgumentNullException.ThrowIfNull(values.Icon);
        ArgumentNullException.ThrowIfNull(values.Command);
        ArgumentNullException.ThrowIfNull(values.Output);

        //var elf = Scratch.GenerateEboot(data);
        //var bin = await Scratch.EncryptEboot(elf, token);
        var bin = Resources.GetEmbedded("EBOOT.BIN");

        var pkg = new PkgBuilder
        {
            ContentId = values.ContentId,
            ForceAbsolutePaths = false
        };
        AddFile(pkg, "USRDIR/EBOOT.BIN", bin, PkgFileFlags.Overwrites | PkgFileFlags.Npdrm);
        if (values.ParamSfo != null)
        {
            pkg.AddFile(values.ParamSfo.ToPkgFile("PARAM.SFO"));
        }

        AddFile(pkg, "PARAM.HIS", values.ParamHis);

        AddPicture(pkg, "ICON0.PNG", values.Icon, 320, 176);
        AddPicture(pkg, "PIC1.PNG", values.Background, 1920, 1080);
        AddPicture(pkg, "PIC0.PNG", values.Overlay, 1000, 560);
        AddPicture(pkg, "PIC2.PNG", values.OverlaySd, 310, 250);
        AddFile(pkg, "ICON1.PAM", values.Video);
        AddFile(pkg, "SND0.AT3", values.Sound);

        AddFile(pkg, "USRDIR/launch.txt", values.Command);
        AddFile(pkg, "USRDIR/script.txt", values.Script);

        var outFile = new FileInfo(Path.Join(values.Output.FullName, $"{values.ContentId}.pkg"));
        if (outFile.Exists && !values.Force)
        {
            throw new InvalidOperationException($"Output [{outFile.FullName}] exists. Pass -f to overwrite");
        }

        outFile.Directory!.Create();
        //await Scratch.DebugDumpFiles(values, bin, token);
        await using (var output = outFile.OpenWrite())
        {
            await pkg.WriteTo(output, token);
        }

        outFile.Refresh();
        await GeneratePreview(values, outFile, token);
        return outFile;
    }

    private static async Task GeneratePreview(Values values, FileInfo outFile, CancellationToken token)
    {
        if (!values.Preview)
        {
            return;
        }

        var outPng = new FileInfo(Path.ChangeExtension(outFile.FullName, ".png"));
        if (outPng.Exists && !values.Force)
        {
            throw new InvalidOperationException($"Preview [{outFile.FullName}] exists. Pass -f to overwrite");
        }

        var preview = await Paint.RenderPreview(values.Icon!, values.Background, values.Overlay, token);
        await using var output = outPng.OpenWrite();
        await output.WriteAsync(preview, token);
        Log($"Saved preview to [{outPng.FullName}]");
    }

    private static void AddPicture(PkgBuilder pkg, string name, byte[]? data, uint expectedWidth, uint expectedHeight)
    {
        if (data is null)
        {
            return;
        }

        Paint.CheckPngDimensions(data, expectedWidth, expectedHeight, name);
        pkg.AddFile(new PkgFile(name, new MemoryStream(data)));
    }

    private static void AddFile(PkgBuilder pkg, string name, byte[]? data, PkgFileFlags? flags = null)
    {
        if (data is null)
        {
            return;
        }

        pkg.AddFile(new PkgFile(name, new MemoryStream(data), flags));
    }
}
