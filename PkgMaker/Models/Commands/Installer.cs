using System.CommandLine;
using System.CommandLine.Help;
using PkgMaker.Utils;

namespace PkgMaker.Models.Commands;

internal sealed class Installer : Command
{
    public Installer() : base(nameof(Installer).ToLowerInvariant(), Desc)
    {
        Options.Add(Input);
        Options.Add(PkgName);
        Options.Add(Output);
        Options.Add(Force);
        Options.Add(new HelpOption { Action = new ExtendedHelpAction(Examples) });
    }

    private static string Examples(string app) =>
        $"""

         Examples:

           Pack your webman config from C:\example\dev_hdd0\tmp\wm_config.bin
           > {app} installer -i "C:\example"
             out/example.pkg that writes file to /dev_hdd0/tmp/wm_config.bin

           Pack mod for a game my_mod/dev_hdd0/game/BLES00000/*
           > {app} installer -i "my_mod"
             out/my_mod.pkg that writes data to dev_hdd0/game/BLES00000

           Pack flash mod from danger/dev_blind/*
           > {app} installer -i "danger" -n brick
             out/brick.pkg that writes data to /dev_blind - writable flash should be enabled before installing

         Notes:
           * Does not create any visible entries in XMB - no way to uninstall
           * Always overwrites existing files - be careful!
         """;

    private const string Desc = """
                                Generate .pkg that installs files to arbitrary locations
                                """;

    public static readonly Option<DirectoryInfo> Input = new("--input", "-i")
    {
        Description = "Directory to pack. Everything inside is installed into PS3 root, so replicate directory structure starting with dev_*/",
        HelpName = "dir"
    };

    public static readonly Option<DirectoryInfo> Output = new("--output", "-o")
    {
        Description = "Directory to place generated .pkg",
        DefaultValueFactory = _ => new DirectoryInfo("out"),
        HelpName = "dir"
    };

    public static readonly Option<bool> Force = new("--force", "-f") { Description = "Overwrite existing files" };

    public static readonly Option<string?> PkgName = new("--name", "-n")
    {
        Description = "Package name. [default: name of input directory]",
        HelpName = "name",
    };

}
