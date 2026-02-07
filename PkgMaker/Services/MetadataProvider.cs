using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NetBase32;
using PkgMaker.Models;
using ToolStation.Ps3Formats.Sfo;

namespace PkgMaker.Services;

[SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.", Justification = "What performance lol")]
internal sealed class MetadataProvider
{
    public static byte[]? GenerateCommand(string? arg, byte[]? fallback, string? appId)
    {
        // TODO something special required for PSPISO? f'/mount_ps3{path};/wait.ps3?8;/browser.ps3$focus_segment_index xmb_app3 0;/wait.ps3?1;/browser.ps3$exec_push;/wait.ps3?1;/browser.ps3$focus_index 0 4;/wait.ps3?1;/browser.ps3$exec_push;/wait.ps3?1;/browser.ps3$exec_push;/wait.ps3?1;/browser.ps3$exec_push'
        ArgumentException.ThrowIfNullOrEmpty(appId);
        if (arg != null)
        {
            return Encoding.UTF8.GetBytes(arg);
        }

        if (fallback != null)
        {
            return fallback;
        }

        var defaultCmd = $"/wait.ps3?xmb;/play.ps3/dev_hdd0/game/{appId}/USRDIR/script.txt";
        return Encoding.UTF8.GetBytes(defaultCmd);
    }

    public static string? GenerateScript(string? scriptFile, string? appId, string? game, int timeout)
    {
        if (string.IsNullOrWhiteSpace(scriptFile))
        {
            ArgumentException.ThrowIfNullOrEmpty(appId);
            if (string.IsNullOrWhiteSpace(game))
            {
                throw new ArgumentException("Game is not set. Specify with -g or use -b to autodetect");
            }

            return GetDefaultScript(appId, game, timeout);
        }

        if (scriptFile.Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            // disable script file if received empty string
            return null;
        }

        return File.ReadAllText(scriptFile);
    }

    public static async Task<ParamSfo?> GenerateParamSfo(string? file, Values values, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            var sfo = new ParamSfo();
            sfo.Add("APP_VER", "01.00", 8);
            sfo.Add("ATTRIBUTE", 0b00000001);
            sfo.Add("BOOTABLE", 0b00000001);
            sfo.Add("CATEGORY", "HG", 4);
            sfo.Add("LICENSE", "Homebrew application, use at your own risk", 512); // 512 is important here
            sfo.Add("PARENTAL_LEVEL", 0);
            sfo.Add("PS3_SYSTEM_VER", "01.8000", 8);
            sfo.Add("RESOLUTION", 63);
            sfo.Add("SOUND_FORMAT", 279);
            sfo.Add("TITLE", values.Title!, 128);
            sfo.Add("TITLE_ID", values.TitleId!, 16);
            sfo.Add("VERSION", "01.00", 8);
            return sfo;
        }

        if (file.Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            // disable script file if received empty string
            return null;
        }

        return ParamSfo.Read(await File.ReadAllBytesAsync(file, token));
    }

    public static string GenerateTitleId(string title)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(title));
        return $"{Prefix}{ZBase32.Encode(hash, FormatOptions.None)}"[..9];
    }

    public static string GenerateLabel(string title) => title;

    public static string? GenerateParamHis(string? hisFile, ParseResult args, Values values)
    {
        if (string.IsNullOrWhiteSpace(hisFile))
        {
            return $"""
                    Hello there! This is shortcut launcher {values.TitleId}
                    Created with ToolStation PkgMaker
                    launch.txt: {Encoding.UTF8.GetString(values.Command ?? [])}
                    script.txt: {Encoding.UTF8.GetString(values.Script ?? [])}
                    Args: {args}

                    """;
        }

        if (hisFile.Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            // disable file
            return null;
        }

        return File.ReadAllText(hisFile);
    }

    public static byte[]? FormatScript(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var pattern = new Regex(@"[\r\n]+");
        var script = pattern.Replace(value.Trim(), "\n") + "\n"; // trailing newline is required
        ArgumentException.ThrowIfNullOrWhiteSpace(script);
        return Encoding.UTF8.GetBytes(script);
    }

    public static string FormatTitle(string? value)
    {
        if (value is null)
        {
            throw new ArgumentException("Title is not set. Specify with -t or use -b to autodetect");
        }

        var pattern = new Regex(@"[\r\n]+");
        var title = pattern.Replace(value.Trim(), "\n");
        if (title.Length > 127 || title.Count('\n') > 2)
        {
            throw new ArgumentException($"Title must have length <= 127 and no more than 2 newlines, got [{title}]");
        }

        return title;
    }

    public static string? FormatTitleId(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var titleId = value.Trim().ToUpperInvariant();
        if (titleId.Length != 9 || AllowedChars.IsMatch(titleId))
        {
            throw new ArgumentException($"TitleId must have length = 9 and have only [A-Z0-9] characters, got [{titleId}]");
        }

        return titleId;
    }

    public static string FormatLabel(string value)
    {
        var label = AllowedChars.Replace(value.Trim().ToUpperInvariant(), "");
        return label.PadRight(16, '0')[..16];
    }

    public static async Task<byte[]?> ReadFileOrFallback(string? file, byte[]? fallback, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return fallback;
        }

        if (file.Equals("NONE", StringComparison.OrdinalIgnoreCase))

        {
            // disable file
            return null;
        }

        return await File.ReadAllBytesAsync(file, token);
    }

    public static string FormatLaunchContentId(Values values) => $"LAUNCH-{values.TitleId}_00-{values.Label}";

    public static byte[]? FormatParamHis(string? history, DateTimeOffset now)
    {
        if (history is null)
        {
            return null;
        }

        //Main.Log($"PARAM.HIS {history}");
        var ts = BitConverter.GetBytes(now.ToUnixTimeSeconds());
        Array.Reverse(ts);
        return [1, ..ts, 2, ..Encoding.UTF8.GetBytes(history)];
    }

    private static string GetDefaultScript(string appId, string gameId, int timeout) =>
        $"""
         /mount.ps3/unmount
         /mount.ps3?{gameId}
         wait /dev_bdvd
         if exist /dev_bdvd
             # sometimes autoplay breaks, eg when there are notifications like low controller battery. trying to mitigate with wait
             wait {timeout}
             # for some reason simple /play.ps3 didnt work for me
             /play.ps3?col=game&seg=seg_device
         else
             beep2
             /popup.ps3$Launch%20script%20{appId}%20failed%0AFound%20zero%20or%20multiple%20games%20with%20%22{gameId}%22%20in%20name.%20Check%20your%20files%20or%20rebuild%20this%20launcher.%20This%20notification%20will%20close%20in%2015s...&icon=23
             wait 15
             /popup.ps3*
         end if

         """;

    public static string FormatInstallContentId(string value)
    {
        var text = AllowedChars.Replace(value.Trim().ToUpperInvariant(), "");
        const int length = 6 + 9 + 16;
        var x = text.PadRight(length, '0')[..length];
        var result = $"{x[..6]}-{x[6..15]}_00-{x[15..]}";
        //Main.Log(result);
        return result;
    }

    private static readonly Regex AllowedChars = new Regex(@"[^A-Z0-9]");

    public const char Prefix = 'W';
}
