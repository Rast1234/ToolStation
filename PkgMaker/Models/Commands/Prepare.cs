using System.CommandLine;
using System.CommandLine.Help;
using PkgMaker.Utils;

namespace PkgMaker.Models.Commands;

internal sealed class Prepare : Command
{
    public Prepare() : base(nameof(Prepare).ToLowerInvariant(), Desc)
    {
        Options.Add(Source);
        Options.Add(Output);
        Options.Add(Recursive);
        Options.Add(Force);
        Options.Add(Throttle);
        Options.Add(new HelpOption { Action = new ExtendedHelpAction(Examples) });
    }

    private static string Examples(string app) =>
        $"""

         Examples:

           Remote PS3ISO
           > {app} prepare -s "ftp://192.168.0.3/dev_hdd0/PS3ISO"
             For every ISO, script will have a line to build launcher pkg using remote ISO as base

           Remote folders
           > {app} prepare -s "http://192.168.0.3/dev_hdd0/GAMES"
             Same for every game folder

           Local games
           > {app} prepare -s "path/to/backups/" -r
             Scan for ISOs or game folders recursively

           Remote everything
           > {app} prepare -s "ftp://192.168.0.3" -r
             Scan for ISOs or game folders everywhere on remote system

         Notes:
           * Idea is to scan for existing games, use them as base (titles, icons, etc), then manually review and customize script before making packages
           * For PS1/PS2/PSP ISOs you have edit script and add title (-t) manually, or making launcher will fail
           * Other formats mountable by Webman (ROMS, .BIN/.CUE, etc) are not supported yet. Create Github issue if you can help with testing
           * Some folders are blacklisted from search over FTP and HTTP to avoid wasting time, eg /dev_blind or /dev_hdd0/game

         """;

    private const string Desc = """
                                Generate script to make launcher pkgs for every located game
                                """;

    public static readonly Option<string> Source = new("--source", "-s")
    {
        Description = "Directory, iso, HTTP or FTP address to read game list",
        HelpName = "dir_iso_url"
    };

    public static readonly Option<FileInfo> Output = new("--output", "-o")
    {
        Description = "Script file. Depending on OS, it's either .ps1 or .sh file",
        DefaultValueFactory = _ => new FileInfo($"make_launch_pkgs.{(OperatingSystem.IsWindows() ? "ps1" : "sh")}"),
        HelpName = "name"
    };

    public static readonly Option<bool> Recursive = new("--recursive", "-r") { Description = "Include nested directories" };

    public static readonly Option<bool> Force = new("--force", "-f") { Description = "Overwrite existing file" };

    public static readonly Option<double?> Throttle = new("--throttle", "-t")
    {
        Description = "Wait before http or ftp requests to prevent server lockups, 0 to disable [default: 0.3 for http, 0 for ftp]",
        HelpName = "sec"
    };
}
