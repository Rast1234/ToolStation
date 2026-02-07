using System.Collections.Immutable;
using DiscUtils.Iso9660;
using FluentFTP;
using PkgMaker.Models;
using ToolStation.Ps3Formats.Sfo;

namespace PkgMaker.Utils;

internal static class FileUtils
{
    public static Uri ReplacePath(Uri from, string path, string entry)
    {
        var baseAddr = new Uri(from.GetLeftPart(UriPartial.Authority));
        return new Uri(baseAddr, Path.Join(path, entry));
    }

    public static bool HasGameSfo(DirectoryInfo d) => d.EnumerateDirectories("PS3_GAME").SingleOrDefault()?.EnumerateFiles("PARAM.SFO").SingleOrDefault() != null;

    public static string GetSanePath(Uri uri) => "/" + Uri.UnescapeDataString(uri.AbsolutePath.Replace('+', ' ')).Trim('/', '\\');

    public static bool IsIso(string name) => Path.GetExtension(name).Equals(".iso", StringComparison.OrdinalIgnoreCase);

    public static bool IsGameFolder(DirectoryInfo dir) => dir.GetDirectories().SingleOrDefault(x => x.Name == "PS3_GAME") != null;

    public static bool IsIso(FtpListItem item) => item.Type == FtpObjectType.File && Path.GetExtension(item.Name).Equals(".iso", StringComparison.OrdinalIgnoreCase);

    public static async Task<ParamSfo?> ReadSfoFile(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        await using var ms = new MemoryStream(data);
        return ParamSfo.Read(ms);
    }

    public static async Task ReadIso(Stream s, Values values, string isoPath, CancellationToken token)
    {
        using var iso = new CDReader(s, true);
        var sfo = await ReadSfoFile(ReadIsoFile("\\PS3_GAME\\PARAM.SFO"));
        values.Title = sfo?.Data.GetValueOrDefault("TITLE")?.Value as string;
        values.Game = sfo?.Data.GetValueOrDefault("TITLE_ID")?.Value as string ?? Path.GetFileNameWithoutExtension(isoPath);
        values.Icon = ReadIsoFile("\\PS3_GAME\\ICON0.PNG");
        values.Background = ReadIsoFile("\\PS3_GAME\\PIC1.PNG");
        values.Overlay = ReadIsoFile("\\PS3_GAME\\PIC0.PNG");
        values.OverlaySd = ReadIsoFile("\\PS3_GAME\\PIC2.PNG");
        values.Video = ReadIsoFile("\\PS3_GAME\\ICON1.PAM");
        values.Sound = ReadIsoFile("\\PS3_GAME\\SND0.AT3");

        // TODO: PSP .ISO -  as sfo and png, could be supported but need docs on media files there
        byte[]? ReadIsoFile(string path) => iso.FileExists(path)
            ? iso.ReadAllBytes(path)
            : null;
    }

    /// <summary>
    /// Avoid traversing unnecessary paths over network to save time and avoid server lockup
    /// </summary>
    public static bool IsBlacklisted(string path)
    {
        if (!path.StartsWith('/'))
        {
            throw new ArgumentException($"Not absolute path [{path}]");
        }

        return BlacklistPathsPrefixes.Any(path.StartsWith);
    }

    private static readonly ImmutableHashSet<string> BlacklistPathsPrefixes = new HashSet<string>
    {
        "/dev_hdd0/exdata",
        "/dev_hdd0/game",
        "/dev_hdd0/home",
        "/dev_hdd0/mms",
        "/dev_hdd0/tmp",
        "/dev_hdd0/photo",
        "/dev_hdd0/video",
        "/dev_hdd0/savedata",
        "/dev_hdd0/vm",
        "/dev_hdd0/vsh",
        "/dev_hdd0/xmlhost",
        "/app_home",
        "/dev_bdvd",
        "/dev_flash",
        "/dev_blind"
    }.ToImmutableHashSet();
}
