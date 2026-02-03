using PkgMaker.Models;
using PkgMaker.Utils;

namespace PkgMaker.Services;

public class FileReader
{
    public async Task ReadGameFolder(DirectoryInfo d, Values values, CancellationToken token)
    {
        var game = d.EnumerateDirectories("PS3_GAME").Single().GetFiles().ToDictionary(x => x.Name);
        var sfo = await FileUtils.ReadSfoFile(await ReadFile(game.GetValueOrDefault("PARAM.SFO"), token));
        values.Title = sfo?.Data.GetValueOrDefault("TITLE")?.Value as string;
        values.Game = sfo?.Data.GetValueOrDefault("TITLE_ID")?.Value as string ?? Path.GetFileNameWithoutExtension(d.FullName);
        values.Icon = await ReadFile(game.GetValueOrDefault("ICON0.PNG"), token);
        values.Background = await ReadFile(game.GetValueOrDefault("PIC1.PNG"), token);
        values.Overlay = await ReadFile(game.GetValueOrDefault("PIC0.PNG"), token);
        values.OverlaySd = await ReadFile(game.GetValueOrDefault("PIC2.PNG"), token);
        values.Video = await ReadFile(game.GetValueOrDefault("ICON1.PAM"), token);
        values.Sound = await ReadFile(game.GetValueOrDefault("SND0.AT3"), token);
    }

    public async Task ReadPlainFolder(DirectoryInfo d, Values values, CancellationToken token)
    {
        var files = d.GetFiles().ToDictionary(x => x.Name);
        var sfo = await FileUtils.ReadSfoFile(await ReadFile(files.GetValueOrDefault("PARAM.SFO"), token));
        values.Title = sfo?.Data.GetValueOrDefault("TITLE")?.Value as string;
        values.Game = sfo?.Data.GetValueOrDefault("TITLE_ID")?.Value as string ?? Path.GetFileNameWithoutExtension(d.FullName);
        values.Icon = await ReadFile(files.GetValueOrDefault("ICON0.PNG"), token);
        values.Background = await ReadFile(files.GetValueOrDefault("PIC1.PNG"), token);
        values.Overlay = await ReadFile(files.GetValueOrDefault("PIC0.PNG"), token);
        values.OverlaySd = await ReadFile(files.GetValueOrDefault("PIC2.PNG"), token);
        values.Video = await ReadFile(files.GetValueOrDefault("ICON1.PAM"), token);
        values.Sound = await ReadFile(files.GetValueOrDefault("SND0.AT3"), token);
    }

    public IEnumerable<FileSystemInfo> List(FileSystemInfo entry, bool recursive, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Main.Log($"Probing [{entry.FullName}]");
        if (entry is FileInfo && FileUtils.IsIso(entry.Name)) return [entry];

        if (entry is DirectoryInfo d)
        {
            if (FileUtils.IsGameFolder(d)) return [d];

            var result = new List<FileSystemInfo>();
            var contents = d.EnumerateFileSystemInfos();
            foreach (var x in contents)
            {
                var isFile = x is FileInfo;
                var isGameDir = x is DirectoryInfo tmp && FileUtils.IsGameFolder(tmp);
                var isDirWhenRecursive = x is DirectoryInfo && recursive;
                if (isFile || isGameDir || isDirWhenRecursive)
                {
                    var y = List(x, recursive, token);
                    result.AddRange(y);
                }
            }

            return result;
        }

        return [];
    }

    private async Task<byte[]?> ReadFile(FileInfo? f, CancellationToken token)
    {
        return f is null ? null : await File.ReadAllBytesAsync(f.FullName, token);
    }
}
