using System.Net;
using AngleSharp;
using AngleSharp.Dom;
using PkgMaker.Models;
using PkgMaker.Utils;

namespace PkgMaker.Services;

public class HttpReader
{
    public async Task Parse(Uri uri, Values values, CancellationToken token)
    {
        //http://10.10.10.3/dev_hdd0/PS3ISO/Just%20Dance%202014%20BLES01955.iso
        //http://10.10.10.3/dev_hdd0/GAMES/Just%20Dance%20Unlimited-[NPEB57613]

        using var client = InitClient();
        var path = FileUtils.GetSanePath(uri);
        var probe = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        var contentType = probe.Content.Headers.ContentType?.MediaType ?? "";

        if (FileUtils.IsIso(path) && contentType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            Stream Factory()
            {
                return client.GetStreamAsync(uri, token).Result;
            }

            await using var wrapper = new IsoWorkaroundStream(Factory);
            await FileUtils.ReadIso(wrapper, values, path, token);
            return;
        }

        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            // screw html parsing, just probe and expect 404
            var game = Path.Join(path, "PS3_GAME");

            Uri FileAddr(string x)
            {
                return FileUtils.ReplacePath(uri, game, x);
            }

            var sfo = await FileUtils.ReadSfoFile(await ReadFile(client, FileAddr("PARAM.SFO"), token));
            values.Title = sfo?.Data.GetValueOrDefault("TITLE")?.Value as string;
            values.Game = sfo?.Data.GetValueOrDefault("TITLE_ID")?.Value as string ?? Path.GetFileNameWithoutExtension(path);
            Main.Log($"[{path}] - [{values.Game}]");
            values.Icon = await ReadFile(client, FileAddr("ICON0.PNG"), token);
            values.Background = await ReadFile(client, FileAddr("PIC1.PNG"), token);
            values.Overlay = await ReadFile(client, FileAddr("PIC0.PNG"), token);
            values.OverlaySd = await ReadFile(client, FileAddr("PIC2.PNG"), token);
            values.Video = await ReadFile(client, FileAddr("ICON1.PAM"), token);
            values.Sound = await ReadFile(client, FileAddr("SND0.AT3"), token);
        }
    }

    public async Task<IReadOnlyList<string>> List(Uri uri, bool recursive, TimeSpan throttle, CancellationToken token)
    {
        using var client = InitClient();
        var result = await ProcessItem(client, uri, recursive, throttle, token);
        return result.ToList();
    }

    private async Task<byte[]?> ReadFile(HttpClient client, Uri? uri, CancellationToken token)
    {
        var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Unexpected content-type for file: {contentType}");

        return await response.Content.ReadAsByteArrayAsync(token);
    }

    private bool IsGameFolder(IEnumerable<string> entries)
    {
        return entries.Any(x => x == "PS3_GAME");
    }

    private async Task<IEnumerable<string>> ProcessItem(HttpClient client, Uri item, bool recursive, TimeSpan throttle, CancellationToken token)
    {
        var path = FileUtils.GetSanePath(item);
        Main.Log($"Probing [{path}]");
        if (FileUtils.IsBlacklisted(path)) return [];
        if (FileUtils.IsIso(item.ToString())) return [item.ToString()];

        var entries = await ReadDir(client, item, throttle, token);
        if (entries != null)
        {
            if (IsGameFolder(entries)) return [item.ToString()];

            var result = new List<string>();
            var urls = entries.Select(x => FileUtils.ReplacePath(item, item.AbsolutePath, x));
            foreach (var x in urls)
            {
                var contents = await ReadDir(client, x, throttle, token);
                var isFile = contents == null;
                var isGameDir = IsGameFolder(contents ?? []);
                var isDirWhenRecursive = contents != null && recursive;
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

    /// <summary>
    /// Entries if directory, null if file
    /// </summary>
    /// <returns></returns>
    private async Task<IReadOnlyList<string>?> ReadDir(HttpClient client, Uri uri, TimeSpan throttle, CancellationToken token)
    {
        await Task.Delay(throttle, token);
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.BadRequest)
            // let's pretend it's never happened. for example app_home returns this
            return null;
        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (!contentType.Contains("html", StringComparison.OrdinalIgnoreCase)) return null;

        await using var content = await response.Content.ReadAsStreamAsync(token);
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(content), token);
        return document
            .QuerySelectorAll("table#files tr td:first-of-type a")
            .Select(x => x.Text().Trim())
            .Where(x => x != "..")
            .ToList();
    }

    private static HttpClient InitClient()
    {
        var client = new HttpClient();
        client.MaxResponseContentBufferSize = Constants.IsoStreamingBufferSize;
        return client;
    }
}
