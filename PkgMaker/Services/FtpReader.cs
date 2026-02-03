using System.Net;
using FluentFTP;
using PkgMaker.Models;
using PkgMaker.Utils;

namespace PkgMaker.Services;

public class FtpReader
{
    public async Task Parse(Uri uri, Values values, CancellationToken token)
    {
        await using var client = InitClient(uri);
        await client.AutoConnect(token);
        var path = FileUtils.GetSanePath(uri);

        if (FileUtils.IsIso(path) && await client.FileExists(path, token))
        {
            Stream Factory(long x)
            {
                return client.OpenRead(path, restart: x, token: token).Result;
            }

            await using var wrapper = new IsoWorkaroundStream(Factory);
            await FileUtils.ReadIso(wrapper, values, path, token);
            return;
        }

        if (await client.DirectoryExists(path, token))
        {
            var dir = await client.GetListing(path, token);
            var ps3Dir = await client.GetListing(dir.Single(x => x.Name == "PS3_GAME").FullName, token);
            var game = ps3Dir.ToDictionary(x => x.Name, x => x.FullName);
            var sfo = await FileUtils.ReadSfoFile(await ReadFile(client, game.GetValueOrDefault("PARAM.SFO"), token));
            values.Title = sfo?.Data.GetValueOrDefault("TITLE")?.Value as string;
            values.Game = sfo?.Data.GetValueOrDefault("TITLE_ID")?.Value as string ?? Path.GetFileNameWithoutExtension(path);
            values.Icon = await ReadFile(client, game.GetValueOrDefault("ICON0.PNG"), token);
            values.Background = await ReadFile(client, game.GetValueOrDefault("PIC1.PNG"), token);
            values.Overlay = await ReadFile(client, game.GetValueOrDefault("PIC0.PNG"), token);
            values.OverlaySd = await ReadFile(client, game.GetValueOrDefault("PIC2.PNG"), token);
            values.Video = await ReadFile(client, game.GetValueOrDefault("ICON1.PAM"), token);
            values.Sound = await ReadFile(client, game.GetValueOrDefault("SND0.AT3"), token);
        }
    }

    public async Task<IReadOnlyList<string>> List(Uri uri, bool recursive, TimeSpan throttle, CancellationToken token)
    {
        await using var client = InitClient(uri);
        await client.AutoConnect(token);
        var path = FileUtils.GetSanePath(uri);
        var startItem = await InitFirst(client, path, throttle, token);
        var paths = await ProcessItem(client, startItem, recursive, throttle, token);
        return paths
            .Select(x => FileUtils.ReplacePath(uri, x, string.Empty).ToString())
            .ToList();
    }

    private async Task<byte[]?> ReadFile(AsyncFtpClient client, string? f, CancellationToken token)
    {
        return f is null ? null : await client.DownloadBytes(f, token);
    }

    private async Task<bool> IsGameFolder(AsyncFtpClient client, FtpListItem path, TimeSpan throttle, CancellationToken token)
    {
        if (path.Type != FtpObjectType.Directory) return false;

        await Task.Delay(throttle, token);
        var entries = await client.GetListing(path.FullName, token);
        return entries.Any(x => x.Type == FtpObjectType.Directory && x.Name == "PS3_GAME");
    }

    private async Task<FtpListItem> InitFirst(AsyncFtpClient client, string path, TimeSpan throttle, CancellationToken token)
    {
        await Task.Delay(throttle, token);
        if (await client.DirectoryExists(path, token)) return new FtpListItem(Path.GetFileName(path), 0, FtpObjectType.Directory, DateTime.MinValue) {FullName = path};

        await Task.Delay(throttle, token);
        if (await client.FileExists(path, token)) return new FtpListItem(Path.GetFileName(path), 0, FtpObjectType.File, DateTime.MinValue) {FullName = path};

        throw new InvalidOperationException($"Failed to get ftp file or directory [{path}]");
    }

    private async Task<IEnumerable<string>> ProcessItem(AsyncFtpClient client, FtpListItem ftpItem, bool recursive, TimeSpan throttle, CancellationToken token)
    {
        Main.Log($"Probing [{ftpItem.FullName}]");
        if (FileUtils.IsBlacklisted(ftpItem.FullName)) return [];
        if (FileUtils.IsIso(ftpItem)) return [ftpItem.FullName];

        if (ftpItem.Type == FtpObjectType.Directory)
        {
            if (await IsGameFolder(client, ftpItem, throttle, token)) return [ftpItem.FullName];

            var result = new List<string>();
            await Task.Delay(throttle, token);
            var contents = await client.GetListing(ftpItem.FullName, token);
            foreach (var x in contents)
            {
                var isFile = x.Type == FtpObjectType.File;
                var isGameDir = await IsGameFolder(client, x, throttle, token);
                var isDirWhenRecursive = x.Type == FtpObjectType.Directory && recursive;
                if (isFile || isGameDir || isDirWhenRecursive)
                {
                    var y = await ProcessItem(client, x, recursive, throttle, token);
                    result.AddRange(y);
                }
            }

            return result;
        }

        return [];
    }

    private static AsyncFtpClient InitClient(Uri uri)
    {
        var client = new AsyncFtpClient(uri.Host, uri.Port);
        var creds = uri.UserInfo?.Split(':');
        if (creds?.Length == 2) client.Credentials = new NetworkCredential(creds[0], creds[1]);

        client.Config.RetryAttempts = 3;
        return client;
    }
}
